using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;

namespace TrAInr.Api.Services;

public enum AiEffort { Low, Medium }

// En AI-leverantör tar en prompt och ett JSON-schema och svarar med JSON som följer schemat.
public interface IAiProvider
{
    string Name { get; }
    bool Enabled { get; }
    Task<string> GenerateJsonAsync(string system, string prompt, Dictionary<string, JsonElement> schema, AiEffort effort);
}

public class AiProviderException(string message) : Exception(message);

public class ClaudeProvider(IConfiguration config) : IAiProvider
{
    private readonly string? _apiKey = FirstNonEmpty(config["Anthropic:ApiKey"], Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
    private AnthropicClient? _client;

    public string Name => "Claude";
    public bool Enabled => _apiKey is not null;

    private AnthropicClient Client => _client ??= new AnthropicClient
    {
        ApiKey = _apiKey!,
        BaseUrl = config["Anthropic:BaseUrl"] ?? "https://api.anthropic.com",
    };

    public async Task<string> GenerateJsonAsync(string system, string prompt, Dictionary<string, JsonElement> schema, AiEffort effort)
    {
        var response = await Client.Messages.Create(new MessageCreateParams
        {
            Model = config["Ai:Model"] ?? "claude-opus-5-5",
            MaxTokens = 16000,
            System = system,
            OutputConfig = new OutputConfig
            {
                Effort = effort == AiEffort.Medium ? Effort.Medium : Effort.Low,
                Format = new JsonOutputFormat { Schema = schema },
            },
            Messages = [new() { Role = Role.User, Content = prompt }],
        });

        if (response.StopReason == StopReason.Refusal)
            throw new AiProviderException("AI:n avböjde att svara på den här frågan.");

        return string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
    }

    internal static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}

// Google Gemini via REST. Har en gratisnivå: skapa nyckel på https://aistudio.google.com/apikey
public class GeminiProvider(IConfiguration config, HttpClient http) : IAiProvider
{
    private readonly string? _apiKey = ClaudeProvider.FirstNonEmpty(config["Gemini:ApiKey"], Environment.GetEnvironmentVariable("GEMINI_API_KEY"));

    public string Name => "Gemini";
    public bool Enabled => _apiKey is not null;

    public async Task<string> GenerateJsonAsync(string system, string prompt, Dictionary<string, JsonElement> schema, AiEffort effort)
    {
        var model = config["Gemini:Model"] ?? "gemini-3.6-flash";
        var baseUrl = (config["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com").TrimEnd('/');
        // Hur mycket modellen "tänker" innan den svarar. Standard för Flash är medium, vilket är
        // långsamt; minimal/low räcker gott för de här uppgifterna och svarar mycket snabbare.
        var thinkingLevel = ClaudeProvider.FirstNonEmpty(config["Gemini:ThinkingLevel"])
            ?? (effort == AiEffort.Medium ? "low" : "minimal");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1beta/models/{model}:generateContent")
        {
            // Skickas som buffrad text (med Content-Length) i stället för chunkad ström.
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                systemInstruction = new { parts = new[] { new { text = system } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseJsonSchema = schema,
                    thinkingConfig = new { thinkingLevel },
                },
            }), System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-goog-api-key", _apiKey);

        HttpResponseMessage response;
        string body;
        try
        {
            response = await http.SendAsync(request);
            body = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new AiProviderException("Kunde inte nå Gemini. Kontrollera internetanslutningen och försök igen.");
        }
        using var _ = response;
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException(response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "Gemini: gratiskvoten är slut för stunden, försök igen om en minut (eller i morgon).",
                HttpStatusCode.BadRequest when body.Contains("API_KEY_INVALID") => "Ogiltig GEMINI_API_KEY.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Ogiltig GEMINI_API_KEY.",
                _ => $"Gemini-fel ({(int)response.StatusCode}): {ErrorMessage(body)}",
            });
        }

        var candidate = JsonNode.Parse(body)?["candidates"]?[0];
        var finishReason = candidate?["finishReason"]?.GetValue<string>();
        var parts = candidate?["content"]?["parts"]?.AsArray() ?? [];
        var text = string.Concat(parts.Select(p => p?["text"]?.GetValue<string>() ?? ""));
        if (text.Length == 0)
            throw new AiProviderException($"Gemini gav inget svar (finishReason: {finishReason ?? "okänd"}).");
        return text;
    }

    private static string ErrorMessage(string body)
    {
        try { return JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>() ?? body; }
        catch (JsonException) { return body; }
    }
}

// Groq via det OpenAI-kompatibla REST-API:t. Mycket snabbt och har en gratisnivå:
// skapa nyckel på https://console.groq.com/keys
public class GroqProvider(IConfiguration config, HttpClient http) : IAiProvider
{
    private readonly string? _apiKey = ClaudeProvider.FirstNonEmpty(config["Groq:ApiKey"], Environment.GetEnvironmentVariable("GROQ_API_KEY"));

    public string Name => "Groq";
    public bool Enabled => _apiKey is not null;

    public async Task<string> GenerateJsonAsync(string system, string prompt, Dictionary<string, JsonElement> schema, AiEffort effort)
    {
        var baseUrl = (config["Groq:BaseUrl"] ?? "https://api.groq.com").TrimEnd('/');
        var reasoningEffort = ClaudeProvider.FirstNonEmpty(config["Groq:ReasoningEffort"])
            ?? (effort == AiEffort.Medium ? "medium" : "low");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = config["Groq:Model"] ?? "openai/gpt-oss-120b",
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = prompt },
                },
                reasoning_effort = reasoningEffort,
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new { name = "svar", strict = true, schema },
                },
            }), System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        HttpResponseMessage response;
        string body;
        try
        {
            response = await http.SendAsync(request);
            body = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new AiProviderException("Kunde inte nå Groq. Kontrollera internetanslutningen och försök igen.");
        }
        using var _ = response;
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException(response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "Groq: gratiskvoten är slut för stunden, försök igen om en minut.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Ogiltig GROQ_API_KEY.",
                _ => $"Groq-fel ({(int)response.StatusCode}): {ErrorMessage(body)}",
            });
        }

        var choice = JsonNode.Parse(body)?["choices"]?[0];
        var text = choice?["message"]?["content"]?.GetValue<string>() ?? "";
        if (text.Length == 0)
            throw new AiProviderException($"Groq gav inget svar (finish_reason: {choice?["finish_reason"]?.GetValue<string>() ?? "okänd"}).");
        return text;
    }

    private static string ErrorMessage(string body)
    {
        try { return JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>() ?? body; }
        catch (JsonException) { return body; }
    }
}

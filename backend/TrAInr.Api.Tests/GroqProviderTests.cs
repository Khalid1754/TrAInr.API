using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class GroqProviderTests
{
    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task SendsStrictSchemaAndKey_ReturnsText()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """
            {"choices":[{"message":{"role":"assistant","content":"{\"kcal\":1,\"proteinG\":2,\"carbsG\":3,\"fatG\":4}"},"finish_reason":"stop"}]}
            """);
        var provider = new GroqProvider(Config(new() { ["Groq:ApiKey"] = "test-key" }), new HttpClient(handler));

        var text = await provider.GenerateJsonAsync("system", "prompt", TrainerAi.SchemaFor<Macros>(), AiEffort.Low);

        Assert.Equal("""{"kcal":1,"proteinG":2,"carbsG":3,"fatG":4}""", text);
        Assert.Equal("/openai/v1/chat/completions", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer test-key", handler.Request.Headers.Authorization!.ToString());
        var body = JsonNode.Parse(handler.RequestBody!)!;
        Assert.Equal("openai/gpt-oss-120b", body["model"]!.GetValue<string>());
        Assert.Equal("low", body["reasoning_effort"]!.GetValue<string>());
        Assert.Equal("system", body["messages"]![0]!["role"]!.GetValue<string>());
        var format = body["response_format"]!;
        Assert.Equal("json_schema", format["type"]!.GetValue<string>());
        Assert.True(format["json_schema"]!["strict"]!.GetValue<bool>());
        Assert.NotNull(format["json_schema"]!["schema"]!["properties"]!["proteinG"]);
    }

    [Fact]
    public async Task InvalidKey_GivesSwedishMessage()
    {
        var provider = new GroqProvider(Config(new() { ["Groq:ApiKey"] = "k" }),
            new HttpClient(new FakeHandler(HttpStatusCode.Unauthorized, """{"error":{"message":"Invalid API Key"}}""")));
        var error = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.GenerateJsonAsync("s", "p", TrainerAi.SchemaFor<Macros>(), AiEffort.Low));
        Assert.Equal("Ogiltig GROQ_API_KEY.", error.Message);
    }

    [Fact]
    public void TrainerAi_PrefersGroqOverGemini()
    {
        var empty = Config(new());
        var groq = new GroqProvider(Config(new() { ["Groq:ApiKey"] = "k" }), new HttpClient());
        var gemini = new GeminiProvider(Config(new() { ["Gemini:ApiKey"] = "k" }), new HttpClient());

        Assert.Equal("Groq", new TrainerAi(empty, [groq, gemini]).ProviderName);
        Assert.Equal("Gemini", new TrainerAi(Config(new() { ["Ai:Provider"] = "gemini" }), [groq, gemini]).ProviderName);
    }
}

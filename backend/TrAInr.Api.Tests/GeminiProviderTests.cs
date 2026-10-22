using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class GeminiProviderTests
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
    public async Task SendsSchemaAndKey_ReturnsText()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """
            {"candidates":[{"content":{"parts":[{"text":"{\"kcal\":1,"},{"text":"\"proteinG\":2,\"carbsG\":3,\"fatG\":4}"}]},"finishReason":"STOP"}]}
            """);
        var provider = new GeminiProvider(Config(new() { ["Gemini:ApiKey"] = "test-key" }), new HttpClient(handler));

        var text = await provider.GenerateJsonAsync("system", "prompt", TrainerAi.SchemaFor<Macros>(), AiEffort.Low);

        Assert.Equal("""{"kcal":1,"proteinG":2,"carbsG":3,"fatG":4}""", text);
        Assert.EndsWith("/v1beta/models/gemini-3.6-flash:generateContent", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("test-key", handler.Request.Headers.GetValues("x-goog-api-key").Single());
        var body = JsonNode.Parse(handler.RequestBody!)!;
        Assert.Equal("application/json", body["generationConfig"]!["responseMimeType"]!.GetValue<string>());
        Assert.NotNull(body["generationConfig"]!["responseJsonSchema"]!["properties"]!["proteinG"]);
        Assert.Equal("system", body["systemInstruction"]!["parts"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task RateLimit_GivesSwedishMessage()
    {
        var provider = new GeminiProvider(Config(new() { ["Gemini:ApiKey"] = "k" }),
            new HttpClient(new FakeHandler(HttpStatusCode.TooManyRequests, "{}")));
        var error = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.GenerateJsonAsync("s", "p", TrainerAi.SchemaFor<Macros>(), AiEffort.Low));
        Assert.Contains("gratiskvoten", error.Message);
    }

    [Fact]
    public void TrainerAi_PicksFirstProviderWithKey_OrConfiguredOne()
    {
        var empty = Config(new());
        var claudeWithoutKey = new ClaudeProvider(Config(new() { ["Anthropic:ApiKey"] = "" }));
        var gemini = new GeminiProvider(Config(new() { ["Gemini:ApiKey"] = "k" }), new HttpClient());

        Assert.Equal("Gemini", new TrainerAi(empty, [claudeWithoutKey, gemini]).ProviderName);

        var claude = new ClaudeProvider(Config(new() { ["Anthropic:ApiKey"] = "k" }));
        Assert.Equal("Claude", new TrainerAi(empty, [claude, gemini]).ProviderName);
        Assert.Equal("Gemini", new TrainerAi(Config(new() { ["Ai:Provider"] = "gemini" }), [claude, gemini]).ProviderName);
        Assert.False(new TrainerAi(empty, [claudeWithoutKey]).Enabled);
    }
}

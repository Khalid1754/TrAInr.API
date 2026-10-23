using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TrAInr.Api.Dtos;
using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class ProgressionTests
{
    private static Workout Pass(int id, string name, int sets, int reps, double kg, double? minutes = null) => new()
    {
        Id = id,
        Date = new DateOnly(2026, 10, 1).AddDays(id * 3),
        Exercises = [new() { Name = name, Sets = sets, Reps = reps, WeightKg = kg, DurationMin = minutes }],
    };

    [Theory]
    [InlineData("Knäböj", 5)]
    [InlineData("Bänkpress", 2.5)]
    [InlineData("Bicepscurl", 1)]
    public void Increment_DependsOnExercise(string name, double expected) =>
        Assert.Equal(expected, Progression.Increment(name));

    [Fact]
    public void NewExercise_GetsStartAdvice() =>
        Assert.Equal(0, Progression.Suggest("Bänkpress", []).WeightKg);

    [Fact]
    public void SameOrBetterThanBefore_IncreasesWeight()
    {
        var next = Progression.Suggest("bänkpress", [Pass(1, "Bänkpress", 3, 8, 70), Pass(2, "Bänkpress", 3, 8, 70)]);
        Assert.Equal((3, 8, 72.5), (next.Sets, next.Reps, next.WeightKg));
        Assert.Equal("Bänkpress", next.Name);
        Assert.Contains("72,5 kg", next.Reason);
    }

    [Fact]
    public void FewerReps_KeepsWeightAndAimsForOldReps()
    {
        var next = Progression.Suggest("Knäböj", [Pass(1, "Knäböj", 3, 8, 100), Pass(2, "Knäböj", 3, 6, 100)]);
        Assert.Equal((100.0, 8), (next.WeightKg, next.Reps));
    }

    [Fact]
    public void ThreeSessionsWithoutProgress_Deloads()
    {
        var next = Progression.Suggest("Knäböj", [Pass(1, "Knäböj", 3, 8, 100), Pass(2, "Knäböj", 3, 7, 100), Pass(3, "Knäböj", 3, 6, 100)]);
        Assert.Equal(90, next.WeightKg);
    }

    [Fact]
    public void CardioAndBodyweight_ProgressByTimeOrReps()
    {
        Assert.Equal(22, Progression.Suggest("Löpning", [Pass(1, "Löpning", 1, 1, 0, 20)]).DurationMin);
        Assert.Equal(11, Progression.Suggest("Chins", [Pass(1, "Chins", 3, 10, 0)]).Reps);
    }

    private sealed class FakeProvider(string answer) : IAiProvider
    {
        public string? LastPrompt { get; private set; }
        public string? LastSystem { get; private set; }
        public string Name => "Fake";
        public bool Enabled => true;
        public Task<string> GenerateJsonAsync(string system, string prompt, Dictionary<string, JsonElement> schema, AiEffort effort)
        {
            LastPrompt = prompt;
            LastSystem = system;
            return Task.FromResult(answer);
        }
    }

    [Fact]
    public async Task Chat_SendsConversationAndHistory()
    {
        var provider = new FakeProvider("""{"reply":"Kör på!"}""");
        var ai = new TrainerAi(new ConfigurationBuilder().Build(), [provider]);
        List<Workout> history = [Pass(1, "Bänkpress", 3, 8, 70)];
        var stats = Statistics.Calculate(history, 80, new DateOnly(2026, 10, 23));

        var result = await ai.ChatAsync(new UserProfile(), history, stats, [new ChatMessage("user", "Hur går det med bänken?")]);

        Assert.Equal("Kör på!", result.Reply);
        Assert.Contains("Användaren: Hur går det med bänken?", provider.LastPrompt);
        Assert.Contains("Bänkpress: 3 set × 8 reps @ 70 kg", provider.LastPrompt);
        Assert.Contains("bäst 70 kg", provider.LastPrompt);
    }

    [Fact]
    public async Task Chat_AsksForEnglishWhenEnglishIsChosen()
    {
        var provider = new FakeProvider("""{"reply":"Keep going!"}""");
        var ai = new TrainerAi(new ConfigurationBuilder().Build(), [provider]);
        var stats = Statistics.Calculate([], 80, new DateOnly(2026, 10, 23));

        await ai.ChatAsync(new UserProfile(), [], stats, [new ChatMessage("user", "How am I doing?")], Lang.En);
        Assert.Contains("Always answer in English", provider.LastSystem);

        await ai.ChatAsync(new UserProfile(), [], stats, [new ChatMessage("user", "Hur går det?")]);
        Assert.Contains("Svara alltid på svenska", provider.LastSystem);
    }

    [Fact]
    public void Suggest_ExplainsInChosenLanguage()
    {
        List<Workout> history = [Pass(1, "Bänkpress", 3, 8, 72.5), Pass(2, "Bänkpress", 3, 8, 72.5)];
        Assert.Equal("Du klarade 3 × 8 på 72,5 kg. Öka till 75 kg.", Progression.Suggest("Bänkpress", history).Reason);
        Assert.Equal("You did 3 × 8 at 72.5 kg. Go up to 75 kg.", Progression.Suggest("Bänkpress", history, Lang.En).Reason);
    }

    [Fact]
    public void NextWorkoutSchema_AllowsNullDuration()
    {
        var schema = JsonSerializer.SerializeToElement(TrainerAi.SchemaFor<NextWorkoutResult>());
        var exercise = schema.GetProperty("properties").GetProperty("exercises").GetProperty("items");
        Assert.Contains("durationMin", exercise.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("null", exercise.GetProperty("properties").GetProperty("durationMin").GetRawText());
    }
}

public class MergeWithRulesTests
{
    [Fact]
    public void KeepsRuleWhenAiSkipsExerciseOrDuration()
    {
        List<NextExercise> rules = [new("Bänkpress", 3, 8, 72.5, null, "regel"), new("Löpning", 1, 1, 0, 22, "regel"), new("Chins", 3, 9, 0, null, "regel")];
        List<NextExercise> ai = [new("löpning", 1, 1, 0, null, "ai"), new("bänkpress", 3, 6, 75, null, "ai")];

        var merged = Progression.MergeWithRules(ai, rules);

        Assert.Equal(["Bänkpress", "Löpning", "Chins"], merged.Select(e => e.Name));
        Assert.Equal((75.0, "ai"), (merged[0].WeightKg, merged[0].Reason));
        Assert.Equal(22, merged[1].DurationMin);
        Assert.Equal("regel", merged[2].Reason);
    }
}

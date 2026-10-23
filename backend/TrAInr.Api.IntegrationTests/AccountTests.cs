using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TrAInr.Api.IntegrationTests;

[Collection(nameof(SqlServer))]
public class AccountTests(SqlServer sql)
{
    private static readonly object Workout = new
    {
        name = "Bröst",
        date = "2026-10-20",
        notes = "",
        exercises = new[] { new { name = "Bänkpress", sets = 3, reps = 8, weightKg = 70.0, durationMin = (double?)null } },
    };

    [Fact]
    public async Task ApiRequiresLogin()
    {
        await using var app = new TestApp(sql.NewDatabase());
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/workouts")).StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.False(me.GetProperty("loggedIn").GetBoolean());
    }

    [Fact]
    public async Task RegisterLoginAndLogout()
    {
        await using var app = new TestApp(sql.NewDatabase());
        var client = await app.RegisterAsync("Khalid@Test.se");

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("khalid@test.se", me.GetProperty("email").GetString());

        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/workouts")).StatusCode);

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = "khalid@test.se", password = "fel-lösen" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        var right = await client.PostAsJsonAsync("/api/auth/login", new { email = "KHALID@test.se", password = "hemligt123" });
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/workouts")).StatusCode);
    }

    [Fact]
    public async Task RegistrationRules()
    {
        await using var app = new TestApp(sql.NewDatabase(), inviteCode: "kod");
        var client = app.CreateClient();

        var noCode = await client.PostAsJsonAsync("/api/auth/register", new { email = "a@test.se", password = "hemligt123" });
        Assert.Equal(HttpStatusCode.BadRequest, noCode.StatusCode);
        var shortPassword = await client.PostAsJsonAsync("/api/auth/register", new { email = "a@test.se", password = "kort", inviteCode = "kod" });
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);

        await app.RegisterAsync("a@test.se", invite: "kod");
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { email = "A@test.se", password = "hemligt123", inviteCode = "kod" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task UsersOnlySeeTheirOwnWorkoutsAndProfile()
    {
        await using var app = new TestApp(sql.NewDatabase());
        var anna = await app.RegisterAsync("anna@test.se");
        var bert = await app.RegisterAsync("bert@test.se");

        var created = await (await anna.PostAsJsonAsync("/api/workouts", Workout)).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        Assert.Single(await anna.GetFromJsonAsync<JsonElement[]>("/api/workouts") ?? []);
        Assert.Empty(await bert.GetFromJsonAsync<JsonElement[]>("/api/workouts") ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await bert.GetAsync($"/api/workouts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bert.PutAsJsonAsync($"/api/workouts/{id}", Workout)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bert.DeleteAsync($"/api/workouts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anna.GetAsync($"/api/workouts/{id}")).StatusCode);

        var profile = new { name = "Bert", bodyWeightKg = 90.0, heightCm = 185, age = 40, sex = "man", goal = "styrka", experience = "medel", daysPerWeek = 4, dietaryPreferences = "" };
        (await bert.PutAsJsonAsync("/api/profile", profile)).EnsureSuccessStatusCode();
        var annasProfile = await anna.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.NotEqual(90.0, annasProfile.GetProperty("bodyWeightKg").GetDouble());
    }

    [Fact]
    public async Task StatsAndSuggestionsUseTheUsersWorkouts()
    {
        await using var app = new TestApp(sql.NewDatabase());
        var client = await app.RegisterAsync("stats@test.se");
        (await client.PostAsJsonAsync("/api/workouts", Workout)).EnsureSuccessStatusCode();

        var stats = await client.GetFromJsonAsync<JsonElement>("/api/stats");
        Assert.Equal(1, stats.GetProperty("summary").GetProperty("workouts").GetInt32());
        Assert.Equal("Bänkpress", stats.GetProperty("exercises")[0].GetProperty("name").GetString());

        var next = await (await client.PostAsJsonAsync("/api/coach/next", new { exercises = new[] { "bänkpress" }, useAi = false }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("regler", next.GetProperty("source").GetString());
        Assert.Equal(70, next.GetProperty("exercises")[0].GetProperty("weightKg").GetDouble());

        var exercises = await client.GetFromJsonAsync<JsonElement>("/api/exercises");
        Assert.Equal("Bänkpress", exercises.GetProperty("recent")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task AiEndpointsSayAiIsOffWithoutKey()
    {
        await using var app = new TestApp(sql.NewDatabase());
        var client = await app.RegisterAsync("ai@test.se");

        var response = await client.PostAsJsonAsync("/api/ai/chat", new { messages = new[] { new { role = "user", text = "Hej" } } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var invalid = await client.PostAsJsonAsync("/api/ai/chat", new { messages = new[] { new { role = "system", text = "Hej" } } });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}

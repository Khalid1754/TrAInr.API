using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using TrAInr.Api.Data;

namespace TrAInr.Api.IntegrationTests;

// Säkerställer att en uppdatering i molnet aldrig tappar data som redan finns.
[Collection(nameof(SqlServer))]
public class MigrationTests(SqlServer sql)
{
    [Fact]
    public async Task DataFromBeforeAccountsBelongsToTheFirstUser()
    {
        var connectionString = sql.NewDatabase();

        // En databas som den såg ut före konton, med ett pass och en ifylld profil.
        var options = new DbContextOptionsBuilder<TrainrDbContext>().UseSqlServer(connectionString).Options;
        await using (var db = new TrainrDbContext(options))
            await db.GetService<IMigrator>().MigrateAsync("20261019031421_InitialCreate");
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                UPDATE Profiles SET Name = N'Khalid', BodyWeightKg = 82 WHERE Id = 1;
                INSERT Workouts (Name, Date, Notes, CreatedAt) VALUES (N'Gammalt pass', '2026-10-01', N'', SYSUTCDATETIME());
                INSERT Exercises (WorkoutId, SortOrder, Name, Sets, Reps, WeightKg) VALUES (SCOPE_IDENTITY(), 0, N'Knäböj', 5, 5, 100);
                """, connection);
            await command.ExecuteNonQueryAsync();
        }

        // Den nya versionen startar och migrerar till senaste, sedan skapar två personer konto.
        await using var app = new TestApp(connectionString);
        var first = await app.RegisterAsync("forst@test.se");
        var second = await app.RegisterAsync("sen@test.se");

        var workouts = await first.GetFromJsonAsync<JsonElement[]>("/api/workouts");
        Assert.Equal("Gammalt pass", Assert.Single(workouts!).GetProperty("name").GetString());
        var profile = await first.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(82, profile.GetProperty("bodyWeightKg").GetDouble());

        Assert.Empty(await second.GetFromJsonAsync<JsonElement[]>("/api/workouts") ?? []);
    }

    [Fact]
    public async Task AllMigrationsAreApplied()
    {
        await using var app = new TestApp(sql.NewDatabase());
        app.CreateClient();
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainrDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "Modellen har ändringar som saknar migrering. Kör dotnet ef migrations add.");
    }
}

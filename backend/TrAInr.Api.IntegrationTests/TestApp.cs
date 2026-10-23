using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace TrAInr.Api.IntegrationTests;

// Startar en riktig SQL Server i Docker en gång för alla tester. Varje test får en egen databas.
public sealed class SqlServer : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public string NewDatabase() =>
        new SqlConnectionStringBuilder(container.GetConnectionString()) { InitialCatalog = $"TrAInr_{Guid.NewGuid():N}" }.ConnectionString;
}

[CollectionDefinition(nameof(SqlServer))]
public class SqlServerCollection : ICollectionFixture<SqlServer>;

// Hela API:t i minnet, kopplat till en testdatabas. Migreringarna körs när appen startar, som i molnet.
public sealed class TestApp(string connectionString, string? inviteCode = null) : WebApplicationFactory<Program>
{
    public string ConnectionString => connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Trainr", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Auth:InviteCode", inviteCode ?? "");
        builder.UseSetting("Auth:LoginAttemptsPerMinute", "1000");
        builder.UseSetting("Ai:Provider", "ingen"); // AI av, så att testerna aldrig anropar en riktig AI
    }

    // En ny klient med egna kakor, alltså en egen inloggad användare.
    public async Task<HttpClient> RegisterAsync(string email, string password = "hemligt123", string? invite = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password, name = "Test", inviteCode = invite });
        response.EnsureSuccessStatusCode();
        return client;
    }
}

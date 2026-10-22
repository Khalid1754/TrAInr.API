using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TrAInr.Api.Data;

// Håller koll på om databasen gick att nå. Misslyckas det vid start försöker /api/status igen,
// och felet visas i appen så att man slipper leta i Azures loggar.
public class DatabaseStatus(IConfiguration config, IWebHostEnvironment env, ILogger<DatabaseStatus> logger)
{
    private readonly bool migrate = env.IsDevelopment() || config.GetValue<bool>("Database:MigrateOnStartup");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool ready;

    public string? Error { get; private set; }

    public async Task<string?> CheckAsync(TrainrDbContext db)
    {
        if (ready) return null;
        await gate.WaitAsync();
        try
        {
            if (ready) return null;
            if (migrate)
                await db.Database.MigrateAsync();
            else
            {
                await db.Database.OpenConnectionAsync();
                await db.Database.CloseConnectionAsync();
            }
            ready = true;
            Error = null;
        }
        catch (Exception e)
        {
            Error = Describe(e);
            logger.LogError(e, "Kunde inte nå databasen. Kontrollera ConnectionStrings__Trainr och SQL-serverns brandvägg.");
        }
        finally
        {
            gate.Release();
        }
        return Error;
    }

    // SQL Servers eget meddelande säger mest (t.ex. fel lösenord eller blockerad IP-adress).
    private static string Describe(Exception e)
    {
        for (var inner = e; inner is not null; inner = inner.InnerException)
            if (inner is SqlException sql) return sql.Message;
        return e.Message;
    }
}

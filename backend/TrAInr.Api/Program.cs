using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Anthropic.Exceptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TrAInr.Api.Data;
using TrAInr.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrainrDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Trainr"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddSingleton<IAiProvider, ClaudeProvider>();
builder.Services.AddHttpClient<GroqProvider>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddSingleton<IAiProvider>(sp => sp.GetRequiredService<GroqProvider>());
builder.Services.AddHttpClient<GeminiProvider>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddSingleton<IAiProvider>(sp => sp.GetRequiredService<GeminiProvider>());
builder.Services.AddSingleton<TrainerAi>();
builder.Services.AddSingleton<DatabaseStatus>();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();

// Inloggning med konto (e-post och lösenord). Kakan gäller ett år så att mobilen förblir inloggad.
builder.Services.AddSingleton<InviteCode>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "trainr";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromDays(365);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    // Kakor från tiden med ett gemensamt lösenord saknar användare och gäller inte längre.
    o.Events.OnValidatePrincipal = ctx =>
    {
        if (ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier) is null)
            ctx.RejectPrincipal();
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
// Högst fem inloggnings- och registreringsförsök per minut och IP-adress, så att ingen kan gissa lösenord.
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("Auth:LoginAttemptsPerMinute", 5),
            Window = TimeSpan.FromMinutes(1),
        }));
    o.OnRejected = (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        return new ValueTask(ctx.HttpContext.Response.WriteAsJsonAsync(
            new { error = "För många försök. Vänta en minut och försök igen." }, token));
    };
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Översätter fel till svenska meddelanden som frontend kan visa.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, message) = error switch
    {
        AiUnavailableException e => (503, e.Message),
        AnthropicUnauthorizedException => (502, "Ogiltig ANTHROPIC_API_KEY."),
        AnthropicRateLimitException => (502, "AI:n är överbelastad just nu, försök igen om en stund."),
        AnthropicApiException e => (502, $"AI-fel: {e.Message}"),
        AiProviderException e => (502, e.Message),
        SqlException or RetryLimitExceededException =>
            (503, "Databasen svarar inte just nu. Vänta en minut och försök igen."),
        _ => (500, "Något gick fel på servern."),
    };
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { error = message });
}));

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Skapar/uppdaterar databasen vid start (under utveckling och när Database:MigrateOnStartup är satt).
// Kraschar inte om databasen inte går att nå, då skulle appen startas om i en loop.
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TrainrDbContext>();
    await app.Services.GetRequiredService<DatabaseStatus>().CheckAsync(db);
}

// I molnet ligger den byggda React-appen i wwwroot och serveras av API:t. I Docker sköter nginx det.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapFallbackToFile("index.html");

app.Run();

// Gör Program synlig för integrationstesterna (WebApplicationFactory).
public partial class Program;

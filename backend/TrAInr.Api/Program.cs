using System.Text.Json.Serialization;
using Anthropic.Exceptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TrAInr.Api.Controllers;
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
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();

// Inloggning med ett lösenord. Kakan gäller ett år så att mobilen förblir inloggad.
builder.Services.AddSingleton<AppPassword>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "trainr";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromDays(365);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    o.Events.OnValidatePrincipal = ctx =>
    {
        var password = ctx.HttpContext.RequestServices.GetRequiredService<AppPassword>();
        if (ctx.Principal?.FindFirst(AuthController.FingerprintClaim)?.Value != password.Fingerprint)
            ctx.RejectPrincipal();
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
// Högst fem inloggningsförsök per minut, så att ingen kan gissa lösenordet.
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("login", l => { l.PermitLimit = 5; l.Window = TimeSpan.FromMinutes(1); });
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

// Skapar/uppdaterar databasen automatiskt under utveckling och i Docker.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    // Kraschar inte om databasen inte går att nå, då skulle appen startas om i en loop.
    // Felet syns i loggen och anropen svarar "Databasen svarar inte" tills det är löst.
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TrainrDbContext>().Database.Migrate();
    }
    catch (Exception e)
    {
        app.Logger.LogError(e, "Kunde inte nå databasen vid start. Kontrollera ConnectionStrings__Trainr och SQL-serverns brandvägg.");
    }
}

// I molnet ligger den byggda React-appen i wwwroot och serveras av API:t. I Docker sköter nginx det.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

var controllers = app.MapControllers();
if (app.Services.GetRequiredService<AppPassword>().Required)
    controllers.RequireAuthorization();
app.MapFallbackToFile("index.html");

app.Run();

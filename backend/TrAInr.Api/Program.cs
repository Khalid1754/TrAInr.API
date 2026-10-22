using System.Text.Json.Serialization;
using Anthropic.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TrAInr.Api.Data;
using TrAInr.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrainrDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Trainr"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddSingleton<IAiProvider, ClaudeProvider>();
builder.Services.AddHttpClient<GeminiProvider>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddSingleton<IAiProvider>(sp => sp.GetRequiredService<GeminiProvider>());
builder.Services.AddSingleton<TrainerAi>();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();
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
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<TrainrDbContext>().Database.Migrate();
}

app.UseCors();
app.MapControllers();

app.Run();

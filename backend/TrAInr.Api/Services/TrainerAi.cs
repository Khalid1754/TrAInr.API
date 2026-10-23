using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using TrAInr.Api.Dtos;
using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public class AiUnavailableException(string message) : Exception(message);

public class TrainerAi(IConfiguration config, IEnumerable<IAiProvider> providers)
{
    private const string SystemPrompt = """
        Du är TrAInr, en kunnig och peppande personlig tränare och kostrådgivare.
        Svara alltid på svenska. Basera råd på etablerad träningsvetenskap (progressiv överbelastning,
        volym per muskelgrupp och vecka, återhämtning) och näringslära (protein 1,6–2,2 g/kg/dag,
        kolhydrater för att fylla på glykogen). Var konkret och anpassa efter användarens profil och historik.
        Du ger allmänna råd, inte medicinsk rådgivning.
        """;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
    };

    // Ai:Provider väljer leverantör ("claude", "groq" eller "gemini"). Utan inställning används
    // den första som har en nyckel, i ordningen Claude, Groq, Gemini.
    private readonly IAiProvider? _provider = config["Ai:Provider"] is { Length: > 0 } name
        ? providers.FirstOrDefault(p => p.Enabled && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        : providers.FirstOrDefault(p => p.Enabled);

    public bool Enabled => _provider is not null;
    public string? ProviderName => _provider?.Name;

    public Task<PlanResult> SuggestPlanAsync(UserProfile profile, IEnumerable<Workout> history, string? wishes)
    {
        var historyText = string.Join("\n\n", history.Take(20).Select(Describe));
        return AskAsync<PlanResult>($"""
            {Describe(profile)}

            Senaste träningspassen (nyast först):
            {(historyText.Length > 0 ? historyText : "Ingen historik ännu.")}

            {(string.IsNullOrWhiteSpace(wishes) ? "" : $"Användarens önskemål: {wishes}\n")}
            Ta fram det bästa veckoschemat för användaren med exakt {profile.DaysPerWeek} träningsdagar.
            Bygg vidare på övningar användaren redan gör och föreslå vikter utifrån historiken.
            """, AiEffort.Medium);
    }

    public Task<CaloriesResult> EstimateCaloriesAsync(UserProfile profile, Workout workout, WorkoutEstimate baseline)
    {
        var perExercise = string.Join(", ", baseline.Exercises.Select(e => $"{e.Name}: {e.Kcal} kcal"));
        return AskAsync<CaloriesResult>($"""
            {Describe(profile)}

            {Describe(workout)}

            En regelbaserad MET-uppskattning gav {baseline.TotalKcal} kcal över ca {baseline.TotalMinutes} min ({perExercise}).

            Gör en mer träffsäker uppskattning av hur många kalorier användaren förbränt på passet, utifrån
            övningstyp, vikt, reps, set och kroppsvikt. Ange ett rimligt intervall.
            """, AiEffort.Low);
    }

    public Task<MealResult> SuggestMealAsync(UserProfile profile, Workout workout, int caloriesBurned) =>
        AskAsync<MealResult>($"""
            {Describe(profile)}

            {Describe(workout)}
            Uppskattad förbränning: ca {caloriesBurned} kcal.

            Föreslå den ideala återhämtningsmåltiden efter detta pass för bästa möjliga återhämtning,
            anpassad efter användarens mål, kroppsvikt och kostpreferenser. Använd ingredienser som är lätta att hitta i en svensk matbutik.
            """, AiEffort.Low);

    public Task<NextWorkoutResult> SuggestNextAsync(UserProfile profile, IEnumerable<Workout> history, IReadOnlyList<NextExercise> baseline)
    {
        var historyText = string.Join("\n\n", history.Take(10).Select(Describe));
        var rules = string.Join("\n", baseline.Select(e => $"- {e.Name}: {e.Sets} × {e.Reps} @ {e.WeightKg} kg ({e.Reason})"));
        return AskAsync<NextWorkoutResult>($"""
            {Describe(profile)}

            Senaste träningspassen (nyast först):
            {(historyText.Length > 0 ? historyText : "Ingen historik ännu.")}

            Användaren ska göra de här övningarna i dag. Regelbaserade förslag (progressiv överbelastning):
            {rules}

            Föreslå set, reps och vikt för varje övning i dag, i samma ordning och med samma namn.
            Utgå från förslagen men justera om historiken, målet eller anteckningarna talar för det
            (t.ex. trötthet, smärta eller att det gick lätt). Vikter i steg om 0,5, 1, 2,5 eller 5 kg.
            Sätt durationMin för konditionsövningar och null annars.
            """, AiEffort.Low);
    }

    public Task<ChatResult> ChatAsync(UserProfile profile, IEnumerable<Workout> history, StatsResult stats, IReadOnlyList<ChatMessage> messages)
    {
        var historyText = string.Join("\n\n", history.Take(15).Select(Describe));
        var records = string.Join("\n", stats.Exercises.Where(e => e.BestWeightKg > 0).Take(15)
            .Select(e => $"- {e.Name}: bäst {e.BestWeightKg} kg, maxlyft ≈ {e.BestEstimated1RmKg} kg, {e.Sessions} pass"));
        var conversation = string.Join("\n\n", messages.Select(m => $"{(m.Role == "user" ? "Användaren" : "Coachen")}: {m.Text}"));
        return AskAsync<ChatResult>($"""
            {Describe(profile)}

            Statistik: {stats.Summary.Workouts} pass totalt, {stats.Summary.WorkoutsLast30Days} senaste 30 dagarna,
            {stats.Summary.WeekStreak} veckor i rad med träning.
            Personbästa:
            {(records.Length > 0 ? records : "inga ännu")}

            Senaste träningspassen (nyast först):
            {(historyText.Length > 0 ? historyText : "Ingen historik ännu.")}

            Samtalet hittills:
            {conversation}

            Svara på användarens senaste meddelande som deras personliga tränare. Använd siffrorna ovan
            när det passar. Handlar frågan om skada eller sjukdom, ge allmänna råd och tipsa om vården.
            """, AiEffort.Low);
    }

    private async Task<T> AskAsync<T>(string prompt, AiEffort effort)
    {
        if (_provider is null)
            throw new AiUnavailableException("AI är inte aktiverat. Sätt GEMINI_API_KEY (gratis) eller ANTHROPIC_API_KEY och starta om servern.");

        var text = await _provider.GenerateJsonAsync(SystemPrompt, prompt, SchemaFor<T>(), effort);
        try
        {
            return JsonSerializer.Deserialize<T>(text, Json)
                ?? throw new JsonException("Tomt svar");
        }
        catch (JsonException)
        {
            throw new AiProviderException($"AI-svaret från {_provider.Name} kunde inte tolkas.");
        }
    }

    // Bygger ett JSON-schema från C#-typen: alla fält obligatoriska och inga extra fält,
    // vilket är vad structured outputs kräver.
    internal static Dictionary<string, JsonElement> SchemaFor<T>()
    {
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) =>
            {
                if (node is not JsonObject obj) return node;
                var description = context.PropertyInfo?.AttributeProvider?
                    .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
                    .OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
                if (description is not null) obj["description"] = description;
                if (obj["properties"] is JsonObject props)
                {
                    obj["required"] = new JsonArray(props.Select(p => (JsonNode)p.Key).ToArray());
                    obj["additionalProperties"] = false;
                }
                return obj;
            },
        };
        var schema = Json.GetJsonSchemaAsNode(typeof(T), exporterOptions);
        return schema.Deserialize<Dictionary<string, JsonElement>>()!;
    }

    private static string Describe(UserProfile p) => $"""
        Profil: {(p.Name.Length > 0 ? p.Name : "okänd")}, {p.Age} år, kön: {p.Sex}, {p.HeightCm} cm, {p.BodyWeightKg} kg.
        Mål: {p.Goal}. Erfarenhet: {p.Experience}. Kan träna {p.DaysPerWeek} dagar/vecka.
        Kostpreferenser/allergier: {(p.DietaryPreferences.Length > 0 ? p.DietaryPreferences : "inga angivna")}.
        """;

    private static string Describe(Workout w)
    {
        var sb = new StringBuilder($"Pass \"{w.Name}\" ({w.Date:yyyy-MM-dd})");
        if (w.Notes.Length > 0) sb.Append($", anteckning: {w.Notes}");
        foreach (var e in w.Exercises.OrderBy(e => e.SortOrder))
            sb.Append(e.DurationMin is > 0
                ? $"\n- {e.Name}: {e.DurationMin} min"
                : $"\n- {e.Name}: {e.Sets} set × {e.Reps} reps @ {e.WeightKg} kg");
        return sb.ToString();
    }
}

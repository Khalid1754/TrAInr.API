using System.Globalization;
using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public record NextExercise(
    string Name,
    int Sets,
    int Reps,
    double WeightKg,
    double? DurationMin,
    [property: System.ComponentModel.Description("Kort förklaring, en mening")] string Reason);

// Regelbaserad progression (progressiv överbelastning). Fungerar utan AI och är utgångspunkten som AI:n får se.
public static class Progression
{
    private static readonly string[] LowerBody = ["knäböj", "squat", "marklyft", "deadlift", "benpress", "leg press", "hip thrust", "utfall", "lunge", "böj"];

    // Hur mycket vikten ökas: mer för stora benövningar, mindre för små övningar.
    public static double Increment(string name)
    {
        var n = name.ToLowerInvariant();
        return CalorieEstimator.Categorize(name) switch
        {
            ExerciseCategory.Compound when LowerBody.Any(n.Contains) => 5,
            ExerciseCategory.Compound => 2.5,
            _ => 1,
        };
    }

    public static NextExercise Suggest(string name, IReadOnlyList<Workout> workouts, Lang lang = Lang.Sv)
    {
        string Kg(double value) => FormatKg(value, lang);

        var key = name.Trim().ToLowerInvariant();
        // Den tyngsta raden per pass, nyast först.
        var sessions = workouts
            .Select(w => (w.Date, w.Id, Top: w.Exercises
                .Where(e => e.Name.Trim().ToLowerInvariant() == key)
                .OrderByDescending(e => e.WeightKg).ThenByDescending(e => e.Reps).FirstOrDefault()))
            .Where(s => s.Top is not null)
            .OrderByDescending(s => s.Date).ThenByDescending(s => s.Id)
            .Select(s => s.Top!)
            .Take(3)
            .ToList();

        if (sessions.Count == 0)
            return CalorieEstimator.Categorize(name) == ExerciseCategory.Cardio
                ? new(name, 1, 1, 0, 20, lang.Pick("Ny övning. Börja lugnt i 20 minuter.", "New exercise. Start easy with 20 minutes."))
                : new(name, 3, 10, 0, null, lang.Pick("Ny övning. Välj en vikt du klarar 10 gånger med god teknik.",
                    "New exercise. Pick a weight you can lift 10 times with good form."));

        var last = sessions[0];
        var displayName = last.Name.Trim();

        if (last.DurationMin is > 0)
            return new(displayName, last.Sets, last.Reps, last.WeightKg, last.DurationMin + 2,
                lang.Pick($"Förra gången {Kg(last.DurationMin.Value)} min. Lägg på två minuter.",
                    $"Last time {Kg(last.DurationMin.Value)} min. Add two minutes."));

        if (last.WeightKg <= 0)
            return new(displayName, last.Sets, last.Reps + 1, 0, null,
                lang.Pick($"Förra gången {last.Sets} × {last.Reps}. Försök med en rep till per set.",
                    $"Last time {last.Sets} × {last.Reps}. Try one more rep per set."));

        if (sessions.Count == 1)
            return new(displayName, last.Sets, last.Reps + 1, last.WeightKg, null,
                lang.Pick("Samma vikt som förra gången och en rep till per set.", "Same weight as last time and one more rep per set."));

        var prev = sessions[1];
        var notWorse = last.WeightKg > prev.WeightKg || (last.WeightKg == prev.WeightKg && last.Reps >= prev.Reps);

        // Tre pass i rad utan framsteg: backa 10 % och bygg upp igen.
        if (sessions.Count == 3 && !Better(last, prev) && !Better(prev, sessions[2]))
            return new(displayName, last.Sets, last.Reps, RoundTo(last.WeightKg * 0.9, Increment(name) / 2), null,
                lang.Pick("Tre pass utan framsteg. Ta 10 % lättare en gång och bygg upp igen.",
                    "Three workouts without progress. Go 10% lighter once and build back up."));

        if (notWorse)
        {
            var next = last.WeightKg + Increment(name);
            return new(displayName, last.Sets, last.Reps, next, null,
                lang.Pick($"Du klarade {last.Sets} × {last.Reps} på {Kg(last.WeightKg)} kg. Öka till {Kg(next)} kg.",
                    $"You did {last.Sets} × {last.Reps} at {Kg(last.WeightKg)} kg. Go up to {Kg(next)} kg."));
        }

        return new(displayName, last.Sets, prev.Reps, last.WeightKg, null,
            lang.Pick($"Förra gången blev det färre reps. Kör {Kg(last.WeightKg)} kg igen och sikta på {prev.Reps} reps.",
                $"Fewer reps last time. Do {Kg(last.WeightKg)} kg again and aim for {prev.Reps} reps."));
    }

    // AI:ns förslag i regelförslagens ordning. Saknas en övning eller konditionstiden i AI-svaret används regeln.
    public static List<NextExercise> MergeWithRules(IReadOnlyList<NextExercise> fromAi, IReadOnlyList<NextExercise> rules)
    {
        var byName = fromAi.GroupBy(e => e.Name.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());
        return rules.Select(rule =>
        {
            if (!byName.TryGetValue(rule.Name.ToLowerInvariant(), out var suggestion)) return rule;
            return suggestion with { Name = rule.Name, DurationMin = suggestion.DurationMin ?? rule.DurationMin };
        }).ToList();
    }

    private static bool Better(Exercise a, Exercise b) =>
        a.WeightKg > b.WeightKg || (a.WeightKg == b.WeightKg && a.Reps > b.Reps);

    // 72,5 på svenska och 72.5 på engelska. Utan kulturdata, så att det fungerar på alla servrar.
    private static string FormatKg(double value, Lang lang)
    {
        var text = value.ToString("0.##", CultureInfo.InvariantCulture);
        return lang == Lang.En ? text : text.Replace('.', ',');
    }

    private static double RoundTo(double value, double step) => Math.Round(value / step) * step;
}

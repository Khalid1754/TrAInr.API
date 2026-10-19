using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public enum ExerciseCategory { Compound, Isolation, Bodyweight, Cardio }

public record ExerciseEstimate(string Name, ExerciseCategory Category, double Minutes, int Kcal);
public record WorkoutEstimate(int TotalKcal, double TotalMinutes, IReadOnlyList<ExerciseEstimate> Exercises);

// Regelbaserad kaloriuppskattning (MET-metoden) som alltid fungerar, även utan AI-nyckel.
// kcal = MET × kroppsvikt (kg) × tid (h). Tiden uppskattas från set, reps och vila.
public static class CalorieEstimator
{
    private const double SecondsPerRep = 3;
    private const double RestSecondsPerSet = 90;

    // Grova MET-värden per övningstyp (Compendium of Physical Activities).
    private static readonly Dictionary<ExerciseCategory, double> Met = new()
    {
        [ExerciseCategory.Compound] = 6.0,
        [ExerciseCategory.Isolation] = 3.5,
        [ExerciseCategory.Bodyweight] = 4.0,
        [ExerciseCategory.Cardio] = 8.0,
    };

    private static readonly (ExerciseCategory Category, string[] Keywords)[] Keywords =
    [
        (ExerciseCategory.Cardio, ["löp", "run", "cykel", "bike", "simning", "swim", "roddmaskin", "rower", "hopprep", "jump rope", "intervall", "hiit"]),
        (ExerciseCategory.Bodyweight, ["armhäv", "push-up", "pushup", "chins", "pull-up", "pullup", "dips", "burpee", "plank", "sit-up", "situp"]),
        (ExerciseCategory.Compound, ["knäböj", "squat", "marklyft", "deadlift", "bänk", "bench", "press", "rodd", "row", "utfall", "lunge", "clean", "snatch", "thruster", "hip thrust"]),
    ];

    public static ExerciseCategory Categorize(string name)
    {
        var n = name.ToLowerInvariant();
        foreach (var (category, words) in Keywords)
            if (words.Any(n.Contains)) return category;
        return ExerciseCategory.Isolation;
    }

    public static ExerciseEstimate EstimateExercise(Exercise exercise, double bodyWeightKg)
    {
        var category = Categorize(exercise.Name);
        var minutes = exercise.DurationMin is > 0
            ? exercise.DurationMin.Value
            : exercise.Sets * (exercise.Reps * SecondsPerRep + RestSecondsPerSet) / 60;
        // Tunga vikter relativt kroppsvikten höjer intensiteten något (max +30 %).
        var intensity = category == ExerciseCategory.Cardio
            ? 1
            : 1 + Math.Min(exercise.WeightKg / bodyWeightKg, 1.5) * 0.2;
        var kcal = Met[category] * bodyWeightKg * (minutes / 60) * intensity;
        return new ExerciseEstimate(exercise.Name, category, Math.Round(minutes, 1), (int)Math.Round(kcal));
    }

    public static WorkoutEstimate EstimateWorkout(IEnumerable<Exercise> exercises, double bodyWeightKg)
    {
        var list = exercises.Select(e => EstimateExercise(e, bodyWeightKg)).ToList();
        return new WorkoutEstimate(list.Sum(e => e.Kcal), Math.Round(list.Sum(e => e.Minutes), 1), list);
    }

    public static double TotalVolumeKg(IEnumerable<Exercise> exercises) =>
        exercises.Sum(e => e.Sets * e.Reps * e.WeightKg);
}

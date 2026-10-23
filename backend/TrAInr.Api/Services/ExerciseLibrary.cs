using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public record LibraryExercise(string Name, string Muscle, ExerciseCategory Category);

// Senaste gången användaren gjorde en övning, så att raden kan fyllas i med samma värden.
public record RecentExercise(string Name, DateOnly LastDate, int Times, int Sets, int Reps, double WeightKg, double? DurationMin);

public record ExercisesResult(IReadOnlyList<string> Muscles, IReadOnlyList<LibraryExercise> Library, IReadOnlyList<RecentExercise> Recent);

public static class ExerciseLibrary
{
    private static readonly (string Muscle, string[] Names)[] Groups =
    [
        ("Ben", ["Knäböj", "Frontböj", "Benpress", "Utfallssteg", "Bulgarisk utfallsböj", "Rumänsk marklyft", "Benspark", "Lårcurl", "Vadpress", "Step-up", "Goblet squat"]),
        ("Rygg", ["Marklyft", "Skivstångsrodd", "Hantelrodd", "Latsdrag", "Chins", "Pull-ups", "Sittande rodd", "Face pulls", "Ryggresningar"]),
        ("Bröst", ["Bänkpress", "Lutande bänkpress", "Hantelpress", "Flyes", "Armhävningar", "Dips", "Cable crossover"]),
        ("Axlar", ["Militärpress", "Hantelpress stående", "Sidolyft", "Främre lyft", "Omvända flyes", "Arnoldpress", "Upright row"]),
        ("Armar", ["Bicepscurl", "Hammercurl", "Skivstångscurl", "Triceps pushdown", "Fransk press", "Tricepsdips", "Koncentrationscurl"]),
        ("Säte", ["Hip thrust", "Glute bridge", "Kickbacks", "Sumo marklyft"]),
        ("Mage", ["Plankan", "Sit-ups", "Crunches", "Hängande benlyft", "Ab wheel", "Russian twist", "Sidoplanka"]),
        ("Kondition", ["Löpning", "Cykling", "Roddmaskin", "Simning", "Hopprep", "Promenad", "Crosstrainer", "Intervaller", "Burpees"]),
    ];

    public static IReadOnlyList<string> Muscles { get; } = Groups.Select(g => g.Muscle).ToList();

    public static IReadOnlyList<LibraryExercise> All { get; } =
        Groups.SelectMany(g => g.Names.Select(n => new LibraryExercise(n, g.Muscle, CalorieEstimator.Categorize(n)))).ToList();

    // Övningar man har gjort förut, senast använda först. Samma namn med olika stora bokstäver räknas som en.
    public static List<RecentExercise> Recent(IEnumerable<Workout> workouts) =>
        workouts
            .SelectMany(w => w.Exercises.Select(e => (w.Date, w.Id, Exercise: e)))
            .GroupBy(x => x.Exercise.Name.Trim().ToLowerInvariant())
            .Select(g =>
            {
                var last = g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ThenBy(x => x.Exercise.SortOrder).First();
                var e = last.Exercise;
                return new RecentExercise(e.Name.Trim(), last.Date, g.Select(x => x.Id).Distinct().Count(), e.Sets, e.Reps, e.WeightKg, e.DurationMin);
            })
            .OrderByDescending(r => r.LastDate).ThenByDescending(r => r.Times)
            .ToList();
}

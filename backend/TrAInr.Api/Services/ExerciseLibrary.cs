using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public record LibraryExercise(string Name, string Muscle, ExerciseCategory Category);

// Senaste gången användaren gjorde en övning, så att raden kan fyllas i med samma värden.
public record RecentExercise(string Name, DateOnly LastDate, int Times, int Sets, int Reps, double WeightKg, double? DurationMin);

public record ExercisesResult(IReadOnlyList<string> Muscles, IReadOnlyList<LibraryExercise> Library, IReadOnlyList<RecentExercise> Recent);

public static class ExerciseLibrary
{
    // Muskelgrupp och övningar på svenska och engelska. Det namn som väljs är det som sparas i passet.
    private static readonly (string Sv, string En, (string Sv, string En)[] Names)[] Groups =
    [
        ("Ben", "Legs", [("Knäböj", "Squat"), ("Frontböj", "Front squat"), ("Benpress", "Leg press"), ("Utfallssteg", "Lunges"),
            ("Bulgarisk utfallsböj", "Bulgarian split squat"), ("Rumänsk marklyft", "Romanian deadlift"), ("Benspark", "Leg extension"),
            ("Lårcurl", "Leg curl"), ("Vadpress", "Calf press"), ("Step-up", "Step-up"), ("Goblet squat", "Goblet squat")]),
        ("Rygg", "Back", [("Marklyft", "Deadlift"), ("Skivstångsrodd", "Barbell row"), ("Hantelrodd", "Dumbbell row"), ("Latsdrag", "Lat pulldown"),
            ("Chins", "Chin-ups"), ("Pull-ups", "Pull-ups"), ("Sittande rodd", "Seated cable row"), ("Face pulls", "Face pulls"),
            ("Ryggresningar", "Back extensions")]),
        ("Bröst", "Chest", [("Bänkpress", "Bench press"), ("Lutande bänkpress", "Incline bench press"), ("Hantelpress", "Dumbbell press"),
            ("Flyes", "Flyes"), ("Armhävningar", "Push-ups"), ("Dips", "Dips"), ("Cable crossover", "Cable crossover")]),
        ("Axlar", "Shoulders", [("Militärpress", "Overhead press"), ("Hantelpress stående", "Standing dumbbell press"), ("Sidolyft", "Lateral raise"),
            ("Främre lyft", "Front raise"), ("Omvända flyes", "Reverse flyes"), ("Arnoldpress", "Arnold press"), ("Upright row", "Upright row")]),
        ("Armar", "Arms", [("Bicepscurl", "Biceps curl"), ("Hammercurl", "Hammer curl"), ("Skivstångscurl", "Barbell curl"),
            ("Triceps pushdown", "Triceps pushdown"), ("Fransk press", "French press"), ("Tricepsdips", "Triceps dips"),
            ("Koncentrationscurl", "Concentration curl")]),
        ("Säte", "Glutes", [("Hip thrust", "Hip thrust"), ("Glute bridge", "Glute bridge"), ("Kickbacks", "Kickbacks"), ("Sumo marklyft", "Sumo deadlift")]),
        ("Mage", "Core", [("Plankan", "Plank"), ("Sit-ups", "Sit-ups"), ("Crunches", "Crunches"), ("Hängande benlyft", "Hanging leg raise"),
            ("Ab wheel", "Ab wheel"), ("Russian twist", "Russian twist"), ("Sidoplanka", "Side plank")]),
        ("Kondition", "Cardio", [("Löpning", "Running"), ("Cykling", "Cycling"), ("Roddmaskin", "Rowing machine"), ("Simning", "Swimming"),
            ("Hopprep", "Jump rope"), ("Promenad", "Walking"), ("Crosstrainer", "Cross trainer"), ("Intervaller", "Intervals"), ("Burpees", "Burpees")]),
    ];

    public static IReadOnlyList<string> MusclesIn(Lang lang) => Groups.Select(g => lang.Pick(g.Sv, g.En)).ToList();

    public static IReadOnlyList<LibraryExercise> AllIn(Lang lang) =>
        Groups.SelectMany(g => g.Names.Select(n => lang.Pick(n.Sv, n.En))
            .Select(name => new LibraryExercise(name, lang.Pick(g.Sv, g.En), CalorieEstimator.Categorize(name)))).ToList();

    // Svenska namnpar, för att kunna jämföra att båda språken räknas som samma typ av övning.
    internal static IEnumerable<(string Sv, string En)> Pairs => Groups.SelectMany(g => g.Names);

    public static IReadOnlyList<string> Muscles { get; } = MusclesIn(Lang.Sv);

    public static IReadOnlyList<LibraryExercise> All { get; } = AllIn(Lang.Sv);

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

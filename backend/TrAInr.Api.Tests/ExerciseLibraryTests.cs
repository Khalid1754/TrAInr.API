using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class ExerciseLibraryTests
{
    [Fact]
    public void LibraryHasUniqueNamesAndKnownMuscles()
    {
        Assert.Equal(ExerciseLibrary.All.Count, ExerciseLibrary.All.Select(e => e.Name.ToLowerInvariant()).Distinct().Count());
        Assert.All(ExerciseLibrary.All, e => Assert.Contains(e.Muscle, ExerciseLibrary.Muscles));
    }

    [Fact]
    public void CardioExercisesAreCategorizedAsCardio() =>
        Assert.All(ExerciseLibrary.All.Where(e => e.Muscle == "Kondition" && e.Name != "Burpees"),
            e => Assert.Equal(ExerciseCategory.Cardio, e.Category));

    [Fact]
    public void RecentUsesTheLatestValues()
    {
        List<Workout> workouts =
        [
            new() { Id = 1, Date = new(2026, 10, 1), Exercises = [new() { Name = "Bänkpress", Sets = 3, Reps = 8, WeightKg = 70 }] },
            new() { Id = 2, Date = new(2026, 10, 8), Exercises = [new() { Name = "bänkpress ", Sets = 4, Reps = 6, WeightKg = 75 }, new() { Name = "Löpning", DurationMin = 20 }] },
            new() { Id = 3, Date = new(2026, 10, 5), Exercises = [new() { Name = "Knäböj", Sets = 5, Reps = 5, WeightKg = 100 }] },
        ];

        var recent = ExerciseLibrary.Recent(workouts);

        var bench = recent.Single(r => r.Name.Equals("bänkpress", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, bench.Times);
        Assert.Equal((4, 6, 75.0), (bench.Sets, bench.Reps, bench.WeightKg));
        Assert.Equal("Knäböj", recent[^1].Name); // äldst sist
    }
}

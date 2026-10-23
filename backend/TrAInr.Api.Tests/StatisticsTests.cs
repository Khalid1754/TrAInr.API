using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class StatisticsTests
{
    private static readonly DateOnly Today = new(2026, 10, 23); // en fredag

    private static Workout Pass(int id, string date, params (string Name, int Sets, int Reps, double Kg)[] exercises) => new()
    {
        Id = id,
        Date = DateOnly.Parse(date),
        Exercises = exercises.Select(e => new Exercise { Name = e.Name, Sets = e.Sets, Reps = e.Reps, WeightKg = e.Kg }).ToList(),
    };

    [Fact]
    public void WeekStartsOnMonday()
    {
        Assert.Equal(new DateOnly(2026, 10, 19), Statistics.WeekStart(Today));
        Assert.Equal(new DateOnly(2026, 10, 19), Statistics.WeekStart(new DateOnly(2026, 10, 25)));
    }

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 10, 133.3)]
    [InlineData(0, 10, 0)]
    public void Estimated1Rm_UsesEpley(double kg, int reps, double expected) =>
        Assert.Equal(expected, Statistics.Estimated1Rm(kg, reps));

    [Fact]
    public void WeeksIncludeEmptyWeeksAndVolume()
    {
        var result = Statistics.Calculate(
            [Pass(1, "2026-10-20", ("Knäböj", 3, 5, 100)), Pass(2, "2026-10-06", ("Knäböj", 3, 5, 90))], 80, Today, weeks: 4);

        Assert.Equal(4, result.Weeks.Count);
        Assert.Equal([0, 1, 0, 1], result.Weeks.Select(w => w.Workouts));
        Assert.Equal(1500, result.Weeks[^1].VolumeKg);
        Assert.True(result.Weeks[^1].Kcal > 0);
    }

    [Fact]
    public void StreakCountsConsecutiveWeeks()
    {
        // Ingen träning ännu den här veckan, men de två senaste veckorna i rad.
        var result = Statistics.Calculate(
            [Pass(1, "2026-10-14", ("Löpning", 1, 1, 0)), Pass(2, "2026-10-07", ("Löpning", 1, 1, 0)), Pass(3, "2026-09-16", ("Löpning", 1, 1, 0))],
            80, Today);
        Assert.Equal(2, result.Summary.WeekStreak);
        Assert.Equal(2, result.Summary.WorkoutsLast30Days);
    }

    [Fact]
    public void ExercisesAreGroupedByNameWithPersonalBest()
    {
        var result = Statistics.Calculate(
        [
            Pass(1, "2026-10-01", ("bänkpress", 3, 8, 70)),
            Pass(2, "2026-10-08", ("Bänkpress ", 3, 5, 80), ("Bänkpress", 1, 10, 60)),
            Pass(3, "2026-10-15", ("Bänkpress", 3, 8, 75), ("Plankan", 3, 1, 0)),
        ], 80, Today);

        var bench = result.Exercises[0];
        Assert.Equal("Bänkpress", bench.Name);
        Assert.Equal(3, bench.Sessions);
        Assert.Equal(80, bench.BestWeightKg);
        Assert.Equal(new DateOnly(2026, 10, 8), bench.BestWeightDate);
        Assert.Equal([70, 80, 75], bench.History.Select(p => p.TopWeightKg));
        Assert.Equal(3 * 5 * 80 + 10 * 60, bench.History[1].VolumeKg);

        var plank = result.Exercises.Single(e => e.Name == "Plankan");
        Assert.Equal(0, plank.BestWeightKg);
        Assert.Null(plank.BestWeightDate);
    }

    [Fact]
    public void AiCaloriesAreUsedWhenAvailable()
    {
        var pass = Pass(1, "2026-10-20", ("Knäböj", 3, 5, 100));
        pass.AiCaloriesJson = """{"totalKcal":321,"rangeLowKcal":300,"rangeHighKcal":350,"exercises":[],"afterburnKcal":20,"explanation":""}""";
        Assert.Equal(321, Statistics.Calculate([pass], 80, Today).Summary.TotalKcal);
    }
}

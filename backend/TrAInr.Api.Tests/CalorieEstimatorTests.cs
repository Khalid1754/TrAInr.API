using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class CalorieEstimatorTests
{
    [Theory]
    [InlineData("Knäböj", ExerciseCategory.Compound)]
    [InlineData("Chins", ExerciseCategory.Bodyweight)]
    [InlineData("Löpning", ExerciseCategory.Cardio)]
    [InlineData("Bicepscurl", ExerciseCategory.Isolation)]
    public void Categorize_RecognizesExerciseTypes(string name, ExerciseCategory expected) =>
        Assert.Equal(expected, CalorieEstimator.Categorize(name));

    [Fact]
    public void HeavierWeightAndMoreSets_BurnMoreCalories()
    {
        var light = CalorieEstimator.EstimateExercise(new Exercise { Name = "Knäböj", Sets = 3, Reps = 8, WeightKg = 40 }, 80);
        var heavy = CalorieEstimator.EstimateExercise(new Exercise { Name = "Knäböj", Sets = 3, Reps = 8, WeightKg = 120 }, 80);
        var more = CalorieEstimator.EstimateExercise(new Exercise { Name = "Knäböj", Sets = 5, Reps = 8, WeightKg = 40 }, 80);
        Assert.True(heavy.Kcal > light.Kcal);
        Assert.True(more.Kcal > light.Kcal);
    }

    [Fact]
    public void StrengthWorkout_HasReasonableTotal()
    {
        List<Exercise> exercises =
        [
            new() { Name = "Knäböj", Sets = 4, Reps = 8, WeightKg = 100 },
            new() { Name = "Bänkpress", Sets = 4, Reps = 8, WeightKg = 80 },
            new() { Name = "Bicepscurl", Sets = 3, Reps = 12, WeightKg = 14 },
        ];
        var estimate = CalorieEstimator.EstimateWorkout(exercises, 80);
        Assert.InRange(estimate.TotalKcal, 100, 400);
        Assert.Equal(4 * 8 * 100 + 4 * 8 * 80 + 3 * 12 * 14, CalorieEstimator.TotalVolumeKg(exercises));
    }

    [Fact]
    public void Cardio_UsesDuration()
    {
        var run = CalorieEstimator.EstimateExercise(new Exercise { Name = "Löpning", DurationMin = 30 }, 70);
        Assert.Equal(30, run.Minutes);
        Assert.Equal((int)Math.Round(8 * 70 * 0.5), run.Kcal);
    }
}

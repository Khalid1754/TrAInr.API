using System.ComponentModel.DataAnnotations;
using TrAInr.Api.Services;

namespace TrAInr.Api.Dtos;

public record ExerciseInput(
    [Required, MaxLength(100)] string Name,
    [Range(0, 100)] int Sets,
    [Range(0, 1000)] int Reps,
    [Range(0, 1000)] double WeightKg,
    [Range(0, 600)] double? DurationMin);

public record WorkoutInput(
    [MaxLength(100)] string? Name,
    DateOnly? Date,
    [MaxLength(1000)] string? Notes,
    [Required, MinLength(1)] List<ExerciseInput> Exercises);

public record ExerciseDto(string Name, int Sets, int Reps, double WeightKg, double? DurationMin);

public record WorkoutDto(
    int Id,
    string Name,
    DateOnly Date,
    string Notes,
    IReadOnlyList<ExerciseDto> Exercises,
    double VolumeKg,
    WorkoutEstimate Estimate,
    CaloriesResult? AiCalories,
    MealResult? AiMeal);

public record EstimateDto(double VolumeKg, WorkoutEstimate Estimate);

public record PlanRequest([MaxLength(500)] string? Wishes);

public record ProfileInput(
    [MaxLength(100)] string Name,
    [Range(30, 300)] double BodyWeightKg,
    [Range(100, 250)] int HeightCm,
    [Range(10, 100)] int Age,
    [MaxLength(20)] string Sex,
    [MaxLength(50)] string Goal,
    [MaxLength(50)] string Experience,
    [Range(1, 7)] int DaysPerWeek,
    [MaxLength(500)] string DietaryPreferences);

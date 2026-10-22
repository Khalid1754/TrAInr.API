using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrAInr.Api.Data;
using TrAInr.Api.Dtos;
using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Controllers;

[ApiController]
[Route("api")]
public class WorkoutsController(TrainrDbContext db, TrainerAi ai, DatabaseStatus dbStatus) : ControllerBase
{
    [HttpGet("status")]
    public async Task<object> Status() =>
        new { ai = ai.Enabled, provider = ai.ProviderName, database = await dbStatus.CheckAsync(db) };

    [HttpGet("workouts")]
    public async Task<IEnumerable<WorkoutDto>> List()
    {
        var profile = await GetProfile();
        var workouts = await db.Workouts.Include(w => w.Exercises)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).ToListAsync();
        return workouts.Select(w => ToDto(w, profile));
    }

    [HttpGet("workouts/{id:int}")]
    public async Task<ActionResult<WorkoutDto>> Get(int id)
    {
        var workout = await Load(id);
        return workout is null ? NotFound() : ToDto(workout, await GetProfile());
    }

    [HttpPost("workouts")]
    public async Task<ActionResult<WorkoutDto>> Create(WorkoutInput input)
    {
        var workout = new Workout();
        Apply(workout, input);
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = workout.Id }, ToDto(workout, await GetProfile()));
    }

    [HttpPut("workouts/{id:int}")]
    public async Task<ActionResult<WorkoutDto>> Update(int id, WorkoutInput input)
    {
        var workout = await Load(id);
        if (workout is null) return NotFound();
        db.Exercises.RemoveRange(workout.Exercises);
        Apply(workout, input);
        workout.AiCaloriesJson = null; // passet ändrat, gamla AI-svar gäller inte längre
        workout.AiMealJson = null;
        await db.SaveChangesAsync();
        return ToDto(workout, await GetProfile());
    }

    [HttpDelete("workouts/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await db.Workouts.Where(w => w.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }

    // Snabb kaloriuppskattning för ett pass som inte är sparat ännu (visas medan man loggar).
    [HttpPost("estimate")]
    public async Task<EstimateDto> Estimate(WorkoutInput input)
    {
        var exercises = ToExercises(input);
        var profile = await GetProfile();
        return new EstimateDto(CalorieEstimator.TotalVolumeKg(exercises), CalorieEstimator.EstimateWorkout(exercises, profile.BodyWeightKg));
    }

    [HttpPost("ai/plan")]
    public async Task<PlanResult> Plan(PlanRequest request)
    {
        var history = await db.Workouts.Include(w => w.Exercises)
            .OrderByDescending(w => w.Date).Take(20).ToListAsync();
        return await ai.SuggestPlanAsync(await GetProfile(), history, request.Wishes);
    }

    [HttpPost("workouts/{id:int}/ai/calories")]
    public async Task<ActionResult<CaloriesResult>> AiCalories(int id)
    {
        var workout = await Load(id);
        if (workout is null) return NotFound();
        var profile = await GetProfile();
        var result = await ai.EstimateCaloriesAsync(profile, workout, CalorieEstimator.EstimateWorkout(workout.Exercises, profile.BodyWeightKg));
        workout.AiCaloriesJson = JsonSerializer.Serialize(result, TrainerAi.Json);
        await db.SaveChangesAsync();
        return result;
    }

    [HttpPost("workouts/{id:int}/ai/meal")]
    public async Task<ActionResult<MealResult>> AiMeal(int id)
    {
        var workout = await Load(id);
        if (workout is null) return NotFound();
        var profile = await GetProfile();
        var burned = Deserialize<CaloriesResult>(workout.AiCaloriesJson)?.TotalKcal
            ?? CalorieEstimator.EstimateWorkout(workout.Exercises, profile.BodyWeightKg).TotalKcal;
        var result = await ai.SuggestMealAsync(profile, workout, burned);
        workout.AiMealJson = JsonSerializer.Serialize(result, TrainerAi.Json);
        await db.SaveChangesAsync();
        return result;
    }

    private Task<Workout?> Load(int id) =>
        db.Workouts.Include(w => w.Exercises).FirstOrDefaultAsync(w => w.Id == id);

    private async Task<UserProfile> GetProfile() => await db.Profiles.FindAsync(1) ?? new UserProfile { Id = 1 };

    private static List<Exercise> ToExercises(WorkoutInput input) =>
        input.Exercises
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Select((e, i) => new Exercise
            {
                SortOrder = i,
                Name = e.Name.Trim(),
                Sets = e.Sets,
                Reps = e.Reps,
                WeightKg = e.WeightKg,
                DurationMin = e.DurationMin is > 0 ? e.DurationMin : null,
            })
            .ToList();

    private static void Apply(Workout workout, WorkoutInput input)
    {
        workout.Name = string.IsNullOrWhiteSpace(input.Name) ? "Träningspass" : input.Name.Trim();
        workout.Date = input.Date ?? DateOnly.FromDateTime(DateTime.Today);
        workout.Notes = input.Notes?.Trim() ?? "";
        workout.Exercises = ToExercises(input);
    }

    private static WorkoutDto ToDto(Workout w, UserProfile profile)
    {
        var exercises = w.Exercises.OrderBy(e => e.SortOrder).ToList();
        return new WorkoutDto(
            w.Id, w.Name, w.Date, w.Notes,
            exercises.Select(e => new ExerciseDto(e.Name, e.Sets, e.Reps, e.WeightKg, e.DurationMin)).ToList(),
            CalorieEstimator.TotalVolumeKg(exercises),
            CalorieEstimator.EstimateWorkout(exercises, profile.BodyWeightKg),
            Deserialize<CaloriesResult>(w.AiCaloriesJson),
            Deserialize<MealResult>(w.AiMealJson));
    }

    private static T? Deserialize<T>(string? json) where T : class =>
        json is null ? null : JsonSerializer.Deserialize<T>(json, TrainerAi.Json);
}

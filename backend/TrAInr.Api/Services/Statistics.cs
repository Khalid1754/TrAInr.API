using System.Text.Json;
using TrAInr.Api.Models;

namespace TrAInr.Api.Services;

public record StatsSummary(int Workouts, int WorkoutsLast30Days, int WeekStreak, double TotalVolumeKg, int TotalKcal);

public record WeekStat(DateOnly WeekStart, int Workouts, double VolumeKg, int Kcal);

public record ExercisePoint(DateOnly Date, double TopWeightKg, int RepsAtTop, double Estimated1RmKg, double VolumeKg);

public record ExerciseStat(
    string Name,
    int Sessions,
    double BestWeightKg,
    DateOnly? BestWeightDate,
    double BestEstimated1RmKg,
    IReadOnlyList<ExercisePoint> History);

public record StatsResult(StatsSummary Summary, IReadOnlyList<WeekStat> Weeks, IReadOnlyList<ExerciseStat> Exercises);

// Räknar fram statistiken ur passen. Ren funktion utan databas, så att den är lätt att testa.
public static class Statistics
{
    public static StatsResult Calculate(IReadOnlyList<Workout> workouts, double bodyWeightKg, DateOnly today, int weeks = 12)
    {
        var kcal = workouts.ToDictionary(w => w, w => Kcal(w, bodyWeightKg));
        var thisWeek = WeekStart(today);

        var weekStats = Enumerable.Range(0, weeks)
            .Select(i => thisWeek.AddDays(-7 * (weeks - 1 - i)))
            .Select(start =>
            {
                var inWeek = workouts.Where(w => WeekStart(w.Date) == start).ToList();
                return new WeekStat(start, inWeek.Count,
                    Math.Round(inWeek.Sum(w => CalorieEstimator.TotalVolumeKg(w.Exercises))), inWeek.Sum(w => kcal[w]));
            })
            .ToList();

        var summary = new StatsSummary(
            workouts.Count,
            workouts.Count(w => w.Date > today.AddDays(-30) && w.Date <= today),
            WeekStreak(workouts, thisWeek),
            Math.Round(workouts.Sum(w => CalorieEstimator.TotalVolumeKg(w.Exercises))),
            workouts.Sum(w => kcal[w]));

        return new StatsResult(summary, weekStats, Exercises(workouts));
    }

    // Epleys formel: ungefär hur mycket man skulle orka lyfta en gång.
    public static double Estimated1Rm(double weightKg, int reps) =>
        weightKg <= 0 || reps <= 0 ? 0 : reps == 1 ? weightKg : Math.Round(weightKg * (1 + reps / 30.0), 1);

    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    // Antal veckor i rad med minst ett pass. Den här veckan räknas inte bort bara för att den inte är slut än.
    private static int WeekStreak(IReadOnlyList<Workout> workouts, DateOnly thisWeek)
    {
        var trained = workouts.Select(w => WeekStart(w.Date)).ToHashSet();
        var week = trained.Contains(thisWeek) ? thisWeek : thisWeek.AddDays(-7);
        var streak = 0;
        while (trained.Contains(week))
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }

    private static List<ExerciseStat> Exercises(IReadOnlyList<Workout> workouts) =>
        workouts
            .SelectMany(w => w.Exercises.Select(e => (w.Date, w.Id, Exercise: e)))
            .GroupBy(x => x.Exercise.Name.Trim().ToLowerInvariant())
            .Select(g =>
            {
                // Ett pass kan ha samma övning flera gånger, då slås raderna ihop till en punkt.
                var history = g.GroupBy(x => (x.Date, x.Id))
                    .OrderBy(s => s.Key.Date).ThenBy(s => s.Key.Id)
                    .Select(s =>
                    {
                        var top = s.Select(x => x.Exercise).OrderByDescending(e => e.WeightKg).ThenByDescending(e => e.Reps).First();
                        return new ExercisePoint(s.Key.Date, top.WeightKg, top.Reps,
                            s.Max(x => Estimated1Rm(x.Exercise.WeightKg, x.Exercise.Reps)),
                            CalorieEstimator.TotalVolumeKg(s.Select(x => x.Exercise)));
                    })
                    .ToList();
                var best = history.Where(p => p.TopWeightKg > 0).MaxBy(p => p.TopWeightKg);
                var name = g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).First().Exercise.Name.Trim();
                return new ExerciseStat(name, history.Count, best?.TopWeightKg ?? 0, best?.Date,
                    history.Count == 0 ? 0 : history.Max(p => p.Estimated1RmKg), history);
            })
            .OrderByDescending(e => e.Sessions).ThenBy(e => e.Name)
            .ToList();

    // AI:ns uppskattning om den finns, annars den vanliga.
    private static int Kcal(Workout w, double bodyWeightKg)
    {
        if (w.AiCaloriesJson is not null)
        {
            try
            {
                var ai = JsonSerializer.Deserialize<CaloriesResult>(w.AiCaloriesJson, TrainerAi.Json);
                if (ai is not null) return ai.TotalKcal;
            }
            catch (JsonException) { }
        }
        return CalorieEstimator.EstimateWorkout(w.Exercises, bodyWeightKg).TotalKcal;
    }
}

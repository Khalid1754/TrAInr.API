namespace TrAInr.Api.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double BodyWeightKg { get; set; } = 75;
    public int HeightCm { get; set; } = 178;
    public int Age { get; set; } = 30;
    public string Sex { get; set; } = "unspecified";
    public string Goal { get; set; } = "muskelbygge";
    public string Experience { get; set; } = "medel";
    public int DaysPerWeek { get; set; } = 3;
    public string DietaryPreferences { get; set; } = "";
}

public class Workout
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Exercise> Exercises { get; set; } = [];

    // Senaste AI-svaren sparas som JSON så att de syns i historiken.
    public string? AiCaloriesJson { get; set; }
    public string? AiMealJson { get; set; }
}

public class Exercise
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; } = "";
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double WeightKg { get; set; }
    public double? DurationMin { get; set; }
}

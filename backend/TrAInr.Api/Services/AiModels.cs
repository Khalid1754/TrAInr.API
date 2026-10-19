using System.ComponentModel;

namespace TrAInr.Api.Services;

// Typerna nedan blir JSON-scheman som Claude måste följa (structured outputs).

public record PlanResult(
    [property: Description("Kort motivering till upplägget, 2–4 meningar")] string Summary,
    [property: Description("Vad historiken visar: styrkor, obalanser, progression")] string Analysis,
    List<PlanDay> Days,
    List<string> ProgressionTips);

public record PlanDay(
    [property: Description("t.ex. Måndag")] string Day,
    [property: Description("t.ex. Underkropp – styrka")] string Focus,
    List<PlanExercise> Exercises);

public record PlanExercise(
    string Name,
    int Sets,
    [property: Description("t.ex. 6-8")] string Reps,
    [property: Description("Förslag baserat på historiken, null om okänt")] double? SuggestedWeightKg,
    string Note);

public record CaloriesResult(
    int TotalKcal,
    int RangeLowKcal,
    int RangeHighKcal,
    List<ExerciseCalories> Exercises,
    [property: Description("Uppskattad EPOC/efterförbränning")] int AfterburnKcal,
    [property: Description("Hur uppskattningen gjordes, 2–3 meningar")] string Explanation);

public record ExerciseCalories(string Name, int Kcal, string Comment);

public record MealResult(
    string Title,
    [property: Description("Varför måltiden passar just det här passet")] string Why,
    [property: Description("När den bör ätas")] string Timing,
    Macros Macros,
    List<Ingredient> Ingredients,
    List<string> Steps,
    [property: Description("Snabba alternativ om man inte hinner laga mat")] List<string> Alternatives);

public record Macros(int Kcal, int ProteinG, int CarbsG, int FatG);

public record Ingredient(string Item, string Amount);

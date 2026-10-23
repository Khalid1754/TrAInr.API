using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class LanguageTests
{
    [Theory]
    [InlineData("en", Lang.En)]
    [InlineData("en-GB,en;q=0.9", Lang.En)]
    [InlineData("sv", Lang.Sv)]
    [InlineData("sv-SE,sv;q=0.9,en;q=0.8", Lang.Sv)]
    [InlineData("", Lang.Sv)]
    [InlineData(null, Lang.Sv)]
    public void Parse_DefaultsToSwedish(string? header, Lang expected) =>
        Assert.Equal(expected, Language.Parse(header));

    [Fact]
    public void EnglishLibraryHasUniqueNamesAndKnownMuscles()
    {
        var all = ExerciseLibrary.AllIn(Lang.En);
        Assert.Equal(ExerciseLibrary.All.Count, all.Count);
        Assert.Equal(all.Count, all.Select(e => e.Name.ToLowerInvariant()).Distinct().Count());
        Assert.All(all, e => Assert.Contains(e.Muscle, ExerciseLibrary.MusclesIn(Lang.En)));
    }

    // En övning ska ge samma kaloriuppskattning och progression oavsett vilket språk den loggades på.
    [Fact]
    public void BothLanguagesGiveSameCategory() =>
        Assert.All(ExerciseLibrary.Pairs, p => Assert.Equal(CalorieEstimator.Categorize(p.Sv), CalorieEstimator.Categorize(p.En)));
}

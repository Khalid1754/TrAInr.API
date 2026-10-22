using Microsoft.Extensions.Configuration;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class AppPasswordTests
{
    private static AppPassword Create(string? password) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:Password"] = password }).Build());

    [Fact]
    public void NoPasswordMeansNoLogin()
    {
        var password = Create(null);
        Assert.False(password.Required);
        Assert.False(password.Matches(""));
    }

    [Fact]
    public void MatchesOnlyTheRightPassword()
    {
        var password = Create("hemligt");
        Assert.True(password.Required);
        Assert.True(password.Matches("hemligt"));
        Assert.False(password.Matches("Hemligt"));
        Assert.False(password.Matches(null));
    }

    [Fact]
    public void FingerprintChangesWithPassword() =>
        Assert.NotEqual(Create("ett").Fingerprint, Create("två").Fingerprint);
}

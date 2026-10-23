using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class AccountsTests
{
    private static InviteCode Create(Dictionary<string, string?> settings) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    [Fact]
    public void EmailIsTrimmedAndLowercased() =>
        Assert.Equal("khalid@exempel.se", Accounts.NormalizeEmail("  Khalid@Exempel.SE "));

    [Fact]
    public void ShortPasswordsAreRejected()
    {
        Assert.NotNull(Accounts.ValidatePassword("kort"));
        Assert.Null(Accounts.ValidatePassword("tillräckligt långt"));
    }

    [Fact]
    public void UserIdComesFromTheLoginCookie()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")], "test"));
        Assert.Equal(42, user.UserId());
    }

    [Fact]
    public void NoInviteCodeMeansOpenRegistration()
    {
        var invite = Create([]);
        Assert.False(invite.Required);
        Assert.True(invite.Matches(null));
    }

    [Fact]
    public void InviteCodeMustMatch()
    {
        var invite = Create(new() { ["Auth:InviteCode"] = "hemligt" });
        Assert.True(invite.Required);
        Assert.True(invite.Matches(" hemligt "));
        Assert.False(invite.Matches("Hemligt"));
        Assert.False(invite.Matches(null));
    }

    [Fact]
    public void OldAppPasswordWorksAsInviteCode()
    {
        var invite = Create(new() { ["Auth:Password"] = "gammalt" });
        Assert.True(invite.Matches("gammalt"));
        Assert.True(Create(new() { ["Auth:Password"] = "gammalt", ["Auth:InviteCode"] = "nytt" }).Matches("nytt"));
    }
}

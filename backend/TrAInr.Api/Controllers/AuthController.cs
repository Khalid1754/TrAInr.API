using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TrAInr.Api.Data;
using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Controllers;

public record LoginInput([Required, MaxLength(256)] string Email, [Required, MaxLength(100)] string Password);

public record RegisterInput(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string Password,
    [MaxLength(100)] string? Name,
    [MaxLength(100)] string? InviteCode);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(TrainrDbContext db, InviteCode invite) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    [HttpGet("me")]
    public object Me() => new
    {
        loggedIn = User.Identity?.IsAuthenticated == true,
        email = User.FindFirstValue(ClaimTypes.Email),
        inviteRequired = invite.Required,
    };

    [HttpPost("register")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!invite.Matches(input.InviteCode)) return BadRequest(new { error = "Fel inbjudningskod." });
        if (Accounts.ValidatePassword(input.Password) is { } problem) return BadRequest(new { error = problem });

        var email = Accounts.NormalizeEmail(input.Email);
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "Det finns redan ett konto med den e-postadressen." });

        var isFirstUser = !await db.Users.AnyAsync();
        var user = new User { Email = email };
        user.PasswordHash = Hasher.HashPassword(user, input.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Pass och profil från tiden före konton tillhör den som registrerar sig först.
        UserProfile? profile = null;
        if (isFirstUser)
        {
            await db.Workouts.Where(w => w.UserId == null).ExecuteUpdateAsync(s => s.SetProperty(w => w.UserId, user.Id));
            profile = await db.Profiles.Where(p => p.UserId == null).OrderBy(p => p.Id).FirstOrDefaultAsync();
        }
        if (profile is null) db.Profiles.Add(profile = new UserProfile());
        profile.UserId = user.Id;
        if (!string.IsNullOrWhiteSpace(input.Name)) profile.Name = input.Name.Trim();
        await db.SaveChangesAsync();

        await SignIn(user);
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var email = Accounts.NormalizeEmail(input.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        var result = user is null ? PasswordVerificationResult.Failed : Hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Fel e-post eller lösenord." });

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user!.PasswordHash = Hasher.HashPassword(user, input.Password);
            await db.SaveChangesAsync();
        }
        await SignIn(user!);
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    private Task SignIn(User user)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
    }
}

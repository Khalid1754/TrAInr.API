using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrAInr.Api.Services;

namespace TrAInr.Api.Controllers;

public record LoginInput(string Password);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(AppPassword password) : ControllerBase
{
    public const string FingerprintClaim = "pw";

    [HttpGet("me")]
    public object Me() => new
    {
        loginRequired = password.Required,
        loggedIn = !password.Required || User.Identity?.IsAuthenticated == true,
    };

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!password.Required) return NoContent();
        if (!password.Matches(input.Password)) return Unauthorized(new { error = "Fel lösenord." });

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ägare"), new Claim(FingerprintClaim, password.Fingerprint)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}

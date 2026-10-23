using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TrAInr.Api.Services;

public static class Accounts
{
    public const int MinPasswordLength = 8;

    // E-post jämförs utan skillnad på stora och små bokstäver.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string? ValidatePassword(string password, Lang lang = Lang.Sv) =>
        password.Length < MinPasswordLength
            ? lang.Pick($"Lösenordet måste ha minst {MinPasswordLength} tecken.", $"The password must be at least {MinPasswordLength} characters.")
            : null;

    // Id för den inloggade användaren. Alla pass och profiler hämtas med det.
    public static int UserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Inte inloggad."));
}

// Kräver en inbjudningskod (Auth:InviteCode) för att registrera sig, så att inte vem som helst kan skapa konto
// och använda dina AI-nycklar. Tomt = fri registrering. Har den inte satts används det gamla Auth:Password,
// så att en app som redan ligger i molnet fortsätter vara skyddad.
public class InviteCode(IConfiguration config)
{
    private readonly string? code = Coalesce(config["Auth:InviteCode"], config["Auth:Password"]);

    public bool Required => !string.IsNullOrEmpty(code);

    public bool Matches(string? given) =>
        !Required || CryptographicOperations.FixedTimeEquals(Hash(code!), Hash(given?.Trim() ?? ""));

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static string? Coalesce(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}

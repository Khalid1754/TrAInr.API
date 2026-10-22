using System.Security.Cryptography;
using System.Text;

namespace TrAInr.Api.Services;

// Ett enda lösenord (Auth:Password / AUTH__PASSWORD) skyddar appen. Tomt = ingen inloggning, som lokalt i Docker.
public class AppPassword(IConfiguration config)
{
    private readonly string? password = config["Auth:Password"];

    public bool Required => !string.IsNullOrEmpty(password);

    public bool Matches(string? given) =>
        Required && CryptographicOperations.FixedTimeEquals(Hash(password!), Hash(given ?? ""));

    // Sparas i inloggningskakan så att ett nytt lösenord loggar ut alla gamla inloggningar.
    public string Fingerprint => Required ? Convert.ToHexString(Hash(password!))[..16] : "";

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}

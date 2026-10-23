namespace TrAInr.Api.Services;

public enum Lang { Sv, En }

// Appen skickar valt språk i Accept-Language ("sv" eller "en"). Svenska om inget annat anges.
public static class Language
{
    public static Lang Parse(string? acceptLanguage) =>
        acceptLanguage?.TrimStart().StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? Lang.En : Lang.Sv;

    public static Lang PreferredLang(this HttpRequest request) => Parse(request.Headers.AcceptLanguage);

    // Väljer den svenska eller engelska texten.
    public static string Pick(this Lang lang, string sv, string en) => lang == Lang.En ? en : sv;
}

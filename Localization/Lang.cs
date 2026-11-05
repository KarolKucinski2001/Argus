using System.Globalization;

namespace _bootstrap_scaffold.Localization;

public enum UiLanguage
{
    Polish,
    English
}

/// <summary>
/// Request-scoped UI language. Polish source strings are the lookup keys, so a missing
/// English entry falls back to Polish instead of breaking the page.
/// </summary>
public static class Lang
{
    public const string CookieName = "argus-lang";

    private static readonly AsyncLocal<UiLanguage?> current = new();
    private static readonly CultureInfo PolishCulture = CultureInfo.GetCultureInfo("pl-PL");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    public static IReadOnlyDictionary<string, string> EnglishEntries { get; } = EnglishCatalog.Build();

    public static UiLanguage Current
    {
        get => current.Value ?? UiLanguage.Polish;
        set => current.Value = value;
    }

    public static bool IsEnglish => Current == UiLanguage.English;

    public static string HtmlLang => IsEnglish ? "en" : "pl";

    public static CultureInfo Culture => IsEnglish ? EnglishCulture : PolishCulture;

    public static string T(string polish) =>
        IsEnglish && EnglishEntries.TryGetValue(polish, out var english) ? english : polish;

    public static string T(string polishFormat, params object?[] args) =>
        string.Format(Culture, T(polishFormat), args);

    public static UiLanguage Parse(string? value) =>
        string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? UiLanguage.English : UiLanguage.Polish;

    public static string Code(UiLanguage language) => language == UiLanguage.English ? "en" : "pl";

    /// <summary>Temporarily switches the language and restores the previous one when disposed.</summary>
    public static IDisposable Use(UiLanguage language)
    {
        var previous = current.Value;
        current.Value = language;
        return new Restore(previous);
    }

    private sealed class Restore(UiLanguage? previous) : IDisposable
    {
        public void Dispose() => current.Value = previous;
    }
}
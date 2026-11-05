namespace _bootstrap_scaffold.Localization;

public static class Theme
{
    public const string CookieName = "argus-theme";
    public const string Dark = "dark";
    public const string Light = "light";

    public static string Normalize(string? value) =>
        string.Equals(value, Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;
}
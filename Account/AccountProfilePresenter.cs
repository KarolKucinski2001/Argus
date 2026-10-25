namespace _bootstrap_scaffold.Account;

public static class AccountProfilePresenter
{
    public const string FallbackInitials = "?";

    private static readonly char[] EmailSeparators = ['.', '_', '-', '+'];

    public static string Initials(string? displayName, string? email)
    {
        var fromName = FirstLetters(displayName?.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (fromName.Length > 0)
        {
            return fromName;
        }

        var localPart = email?.Split('@')[0];
        var fromEmail = FirstLetters(localPart?.Split(EmailSeparators, StringSplitOptions.RemoveEmptyEntries));
        return fromEmail.Length > 0 ? fromEmail : FallbackInitials;
    }

    private static string FirstLetters(string[]? parts) =>
        parts is null
            ? string.Empty
            : string.Concat(parts
                .Select(part => part.FirstOrDefault(char.IsLetterOrDigit))
                .Where(letter => letter != default)
                .Take(2)
                .Select(char.ToUpperInvariant));
}

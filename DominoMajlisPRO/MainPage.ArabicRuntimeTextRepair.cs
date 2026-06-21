namespace DominoMajlisPRO;

public partial class MainPage
{
    static string NormalizeBrokenUiText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        var text = value.Trim()
            .Replace("âœ“", "✓", StringComparison.Ordinal)
            .Replace("â–¼", "+", StringComparison.Ordinal)
            .Replace("â–²", "-", StringComparison.Ordinal)
            .Replace("âŒƒ", "-", StringComparison.Ordinal)
            .Replace("âŒ„", "+", StringComparison.Ordinal);

        if (text.Contains('�') ||
            text.Contains("?�", StringComparison.Ordinal) ||
            text.Contains("â", StringComparison.Ordinal) ||
            text.Contains("Ã", StringComparison.Ordinal))
        {
            return "غير متاح";
        }

        return text;
    }
}

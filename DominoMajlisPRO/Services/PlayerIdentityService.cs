namespace DominoMajlisPRO.Services;

public static class PlayerIdentityService
{
    public static string NormalizePlayerName(
        string? playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return string.Empty;

        string normalized =
            playerName.Trim();

        normalized =
            normalized.Replace("أ", "ا")
                      .Replace("إ", "ا")
                      .Replace("آ", "ا")
                      .Replace("ة", "ه")
                      .Replace("ى", "ي")
                      .Replace("ؤ", "و")
                      .Replace("ئ", "ي");

        // إزالة التطويل
        normalized =
            normalized.Replace("ـ", "");

        // إزالة جميع المسافات
        normalized =
            string.Concat(
                normalized.Where(
                    c => !char.IsWhiteSpace(c)));

        return normalized
            .ToLowerInvariant();
    }
}
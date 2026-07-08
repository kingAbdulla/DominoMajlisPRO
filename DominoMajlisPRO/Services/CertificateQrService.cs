using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DominoMajlisPRO.Models;
using QRCoder;

namespace DominoMajlisPRO.Services;

/// <summary>
/// Builds a stable, verifiable certificate identity + QR payload for a match
/// and renders it to a scannable QR image. Fully null-safe: never throws,
/// always returns a usable payload / image.
/// </summary>
public static class CertificateQrService
{
    public const string CertificateType = "DominoMajlisPRO.Certificate";
    public const int PayloadVersion = 1;

    // Verification endpoints. QR encodes a URL (deep link preferred, HTTPS
    // fallback) — never raw JSON. Full JSON stays available internally via
    // BuildQrPayload for local verification / export.
    public const string DeepLinkScheme = "dominomajlispro";
    public const string VerificationBaseUrl =
        "https://dominomajlispro.app/certificate/verify";

    /// <summary>
    /// Deterministic certificate id for a match. The same match always maps to
    /// the same id (never random per call). Prefers the persisted MatchId, and
    /// falls back to a hash of match date + team ids + final score.
    /// </summary>
    public static string BuildCertificateId(SavedMatch? match)
    {
        if (match == null)
            return "DMC-UNKNOWN";

        var matchGuid = match.MatchId.ToString("N");
        if (!string.IsNullOrWhiteSpace(matchGuid) &&
            match.MatchId != Guid.Empty)
        {
            return $"DMC-{matchGuid[..Math.Min(12, matchGuid.Length)].ToUpperInvariant()}";
        }

        var seed = string.Join(
            '|',
            match.MatchDate.ToString("yyyyMMddHHmmss"),
            match.Team1Id,
            match.Team2Id,
            $"{match.Team1Score}-{match.Team2Score}");

        return $"DMC-{ShortHash(seed)}";
    }

    /// <summary>
    /// The string actually encoded into the QR image: an HTTPS verification URL
    /// carrying the stable IDs. Never raw JSON. Falls back to a readable compact
    /// text if URL building fails.
    /// </summary>
    public static string BuildQrContent(SavedMatch? match)
    {
        try
        {
            if (match == null)
                return BuildCompactText(match);

            return BuildVerificationUrl(match);
        }
        catch
        {
            return BuildCompactText(match);
        }
    }

    /// <summary>
    /// HTTPS verification URL, e.g.
    /// https://dominomajlispro.app/certificate/verify?certificateId=...&amp;matchId=...
    /// Uses IDs so it stays verifiable if display names change.
    /// </summary>
    public static string BuildVerificationUrl(SavedMatch? match) =>
        BuildUrl(VerificationBaseUrl, match);

    /// <summary>
    /// Custom-scheme deep link for when in-app deep linking is wired up:
    /// dominomajlispro://certificate/verify?certificateId=...&amp;matchId=...
    /// </summary>
    public static string BuildDeepLink(SavedMatch? match) =>
        BuildUrl($"{DeepLinkScheme}://certificate/verify", match);

    static string BuildUrl(string baseUrl, SavedMatch? match)
    {
        if (match == null)
            return $"{baseUrl}?certificateId=DMC-UNKNOWN";

        var q = new StringBuilder(baseUrl);
        q.Append('?');
        Append(q, "certificateId", BuildCertificateId(match), true);
        Append(q, "matchId", match.MatchId.ToString(), false);
        Append(q, "winnerTeamId", match.WinnerTeamId, false);
        Append(q, "team1Id", match.Team1Id, false);
        Append(q, "team2Id", match.Team2Id, false);
        Append(q, "score", $"{match.Team1Score}-{match.Team2Score}", false);
        Append(q, "date", match.MatchDate.ToString("yyyy-MM-ddTHH:mm:ss"), false);

        return q.ToString();
    }

    static void Append(StringBuilder sb, string key, string? value, bool first)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!first)
            sb.Append('&');

        sb.Append(key)
          .Append('=')
          .Append(Uri.EscapeDataString(value.Trim()));
    }

    /// <summary>
    /// Human-readable compact fallback used only when URL building fails.
    /// </summary>
    public static string BuildCompactText(SavedMatch? match)
    {
        if (match == null)
        {
            return
                "DominoMajlisPRO Certificate Verification\n" +
                "Certificate ID: DMC-UNKNOWN";
        }

        var winner = string.IsNullOrWhiteSpace(match.WinnerTeamName)
            ? match.WinnerTeam
            : match.WinnerTeamName;

        return
            "DominoMajlisPRO Certificate Verification\n" +
            $"Certificate ID: {BuildCertificateId(match)}\n" +
            $"Match ID: {match.MatchId}\n" +
            $"Winner: {Safe(winner)}\n" +
            $"Score: {match.Team1Score}-{match.Team2Score}\n" +
            $"Date: {match.MatchDate:yyyy-MM-ddTHH:mm:ss}";
    }

    /// <summary>
    /// Compact JSON verification payload kept INTERNALLY (not encoded in the QR).
    /// Uses IDs (not only display names) so the certificate stays verifiable if
    /// names change.
    /// </summary>
    public static string BuildQrPayload(SavedMatch? match)
    {
        try
        {
            if (match == null)
                return FallbackPayload();

            var payload = new
            {
                type = CertificateType,
                version = PayloadVersion,
                matchId = match.MatchId.ToString(),
                certificateId = BuildCertificateId(match),
                winnerTeamId = Safe(match.WinnerTeamId),
                winnerTeamName = Safe(
                    string.IsNullOrWhiteSpace(match.WinnerTeamName)
                        ? match.WinnerTeam
                        : match.WinnerTeamName),
                team1Id = Safe(match.Team1Id),
                team1Name = Safe(match.Team1Name),
                team2Id = Safe(match.Team2Id),
                team2Name = Safe(match.Team2Name),
                score = $"{match.Team1Score}-{match.Team2Score}",
                rules = match.IsLocalRules ? "Local" : "International",
                matchDate = match.MatchDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                issuedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                app = "DominoMajlisPRO"
            };

            return JsonSerializer.Serialize(payload);
        }
        catch
        {
            return FallbackPayload();
        }
    }

    /// <summary>
    /// Renders the given payload to a QR PNG. Returns null only if generation
    /// completely fails (callers should keep a static fallback image ready).
    /// </summary>
    public static byte[]? GenerateQrPng(string? payload, int pixelsPerModule = 12)
    {
        try
        {
            var text = string.IsNullOrWhiteSpace(payload)
                ? FallbackPayload()
                : payload;

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(
                text,
                QRCodeGenerator.ECCLevel.Q);

            var qr = new PngByteQRCode(data);
            return qr.GetGraphic(Math.Max(4, pixelsPerModule));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a MAUI ImageSource for the match certificate QR. Null-safe:
    /// falls back to the bundled qr_gold.png if native generation fails.
    /// </summary>
    public static ImageSource GenerateQrImageSource(
        SavedMatch? match,
        int pixelsPerModule = 12)
    {
        var content = BuildQrContent(match);
        return GenerateQrImageSource(content, pixelsPerModule);
    }

    public static ImageSource GenerateQrImageSource(
        string? payload,
        int pixelsPerModule = 12)
    {
        var bytes = GenerateQrPng(payload, pixelsPerModule);
        if (bytes == null || bytes.Length == 0)
            return ImageSource.FromFile("qr_gold.png");

        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    static string FallbackPayload() =>
        $"{VerificationBaseUrl}?certificateId=DMC-UNKNOWN";

    static string Safe(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    static string ShortHash(string input)
    {
        try
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            for (int i = 0; i < 6 && i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("X2"));
            return sb.ToString();
        }
        catch
        {
            return "UNKNOWN";
        }
    }
}

using System.Text;
using System.Text.Json;

namespace DominoMajlisPRO.Localization;

public static class ArabicTextRecoveryService
{
    private static readonly string RepairMarkerFile =
        Path.Combine(FileSystem.AppDataDirectory, "arabic_text_recovery_v1.done");

    private static bool _providerRegistered;

    public static async Task RepairAppDataJsonFilesOnceAsync()
    {
        if (File.Exists(RepairMarkerFile))
            return;

        RegisterEncodingProvider();

        try
        {
            var root = FileSystem.AppDataDirectory;
            if (!Directory.Exists(root))
                return;

            var jsonFiles = Directory
                .EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .ToList();

            foreach (var file in jsonFiles)
                await RepairJsonFileAsync(file);

            await File.WriteAllTextAsync(
                RepairMarkerFile,
                DateTime.UtcNow.ToString("O"),
                new UTF8Encoding(false));
        }
        catch
        {
            // Recovery must never block app launch.
        }
    }

    public static string RecoverDisplayText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var current = value.Trim();
        if (!LooksBroken(current))
            return current;

        RegisterEncodingProvider();

        var best = current;
        var bestScore = BrokenScore(current);

        foreach (var candidate in GenerateCandidates(current))
        {
            var score = BrokenScore(candidate);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    public static bool LooksBroken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return BrokenScore(value) >= 2 ||
               value.Contains('\uFFFD') ||
               value.Contains("\u00E2\u20AC") ||
               value.Contains("\u00C3") ||
               value.Contains("\u00D8") ||
               value.Contains("\u00D9") ||
               value.Contains("\u0637") ||
               value.Contains("\u0638");
    }

    private static async Task RepairJsonFileAsync(string path)
    {
        string original;
        try
        {
            original = await File.ReadAllTextAsync(path, Encoding.UTF8);
        }
        catch
        {
            return;
        }

        if (!LooksBroken(original))
            return;

        var repaired = RepairJsonStringValues(original);
        if (string.Equals(original, repaired, StringComparison.Ordinal))
            return;

        var backup = path + ".arabic-recovery-v1.bak";
        if (!File.Exists(backup))
            await File.WriteAllTextAsync(backup, original, new UTF8Encoding(false));

        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, repaired, new UTF8Encoding(false));

        if (File.Exists(path))
            File.Delete(path);

        File.Move(temp, path);
    }

    private static string RepairJsonStringValues(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
        }
        catch
        {
            return json;
        }

        var builder = new StringBuilder(json.Length);
        bool inString = false;
        bool escape = false;
        var token = new StringBuilder();

        for (var index = 0; index < json.Length; index++)
        {
            var ch = json[index];

            if (!inString)
            {
                builder.Append(ch);
                if (ch == '"')
                {
                    inString = true;
                    escape = false;
                    token.Clear();
                }
                continue;
            }

            if (escape)
            {
                token.Append('\\');
                token.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                var raw = token.ToString();
                var decoded = DecodeJsonString(raw);
                var fixedText = RecoverDisplayText(decoded);
                builder.Append(EscapeJsonString(fixedText));
                builder.Append('"');
                inString = false;
                token.Clear();
                continue;
            }

            token.Append(ch);
        }

        return builder.ToString();
    }

    private static string DecodeJsonString(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<string>("\"" + raw + "\"") ?? raw;
        }
        catch
        {
            return raw;
        }
    }

    private static string EscapeJsonString(string value)
    {
        return JsonSerializer.Serialize(value)[1..^1];
    }

    private static IEnumerable<string> GenerateCandidates(string value)
    {
        foreach (var codePage in new[] { 1256, 1252 })
        {
            string current = value;
            for (var i = 0; i < 3; i++)
            {
                string candidate;
                try
                {
                    var encoding = Encoding.GetEncoding(codePage);
                    candidate = Encoding.UTF8.GetString(encoding.GetBytes(current));
                }
                catch
                {
                    break;
                }

                if (string.Equals(candidate, current, StringComparison.Ordinal))
                    break;

                yield return candidate;
                current = candidate;
            }
        }
    }

    private static int BrokenScore(string value)
    {
        var score = 0;
        foreach (var marker in new[]
        {
            "\u0637",
            "\u0638",
            "\u00D8",
            "\u00D9",
            "\u00C3",
            "\u00C2",
            "\u00E2\u20AC",
            "\u00E2\u20AC\u00A2",
            "\u00C3\u00A2",
            "\uFFFD"
        })
        {
            score += Count(value, marker);
        }

        return score;
    }

    private static int Count(string value, string marker)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }

    private static void RegisterEncodingProvider()
    {
        if (_providerRegistered)
            return;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _providerRegistered = true;
    }
}

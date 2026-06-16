using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class GameService
{
    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "matches.json");

    // =========================
    // تحميل المباريات
    // =========================

    public static async Task<List<SavedMatch>> LoadMatchesAsync()
    {
        try
        {
            if (!File.Exists(filePath))
                return new List<SavedMatch>();

            string json =
                await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<SavedMatch>();

            return System.Text.Json.JsonSerializer
                .Deserialize<List<SavedMatch>>(json)
                ?? new List<SavedMatch>();
        }
        catch
        {
            return new List<SavedMatch>();
        }
    }

    // =========================
    // حفظ أو تحديث مباراة
    // =========================

    public static async Task SaveMatchAsync(
        SavedMatch match)
    {
        List<SavedMatch> matches =
            await LoadMatchesAsync();

        // =========================
        // البحث عن نفس المباراة
        // =========================

        SavedMatch? existingMatch =
            matches.FirstOrDefault(x =>
                x.MatchId == match.MatchId);

        // =========================
        // تحديث المباراة الحالية
        // =========================

        if (existingMatch != null)
        {
            int index =
                matches.IndexOf(existingMatch);

            matches[index] = match;
        }

        // =========================
        // إضافة مباراة جديدة
        // =========================

        else
        {
            matches.Insert(0, match);
        }

        string json =
            JsonSerializer.Serialize(
                matches,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            filePath,
            json);
    }

    // =========================
    // حذف مباراة
    // =========================

    public static async Task DeleteMatchAsync(
        SavedMatch match)
    {
        List<SavedMatch> matches =
            await LoadMatchesAsync();

        SavedMatch? target =
            matches.FirstOrDefault(x =>
                x.MatchId == match.MatchId);

        if (target != null)
        {
            matches.Remove(target);
        }

        string json =
            JsonSerializer.Serialize(
                matches,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            filePath,
            json);
    }

    // =========================
    // حذف جميع المباريات
    // =========================

    public static async Task DeleteAllMatches()
    {
        await File.WriteAllTextAsync(
            filePath,
            "[]");
    }

    // =========================
    // تحميل آخر مباراة غير منتهية
    // =========================

    public static async Task<SavedMatch?>
        GetLastUnfinishedMatchAsync()
    {
        List<SavedMatch> matches =
            await LoadMatchesAsync();

        return matches
            .Where(x => !x.IsFinished)
            .OrderByDescending(
                x => x.LastPlayedTime)
            .FirstOrDefault();
    }
}
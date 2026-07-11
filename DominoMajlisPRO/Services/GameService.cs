using System.Text.Json;
using DominoMajlisPRO.Cloud;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class GameService
{
    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "matches.json");

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

            return JsonSerializer
                .Deserialize<List<SavedMatch>>(json)
                ?? new List<SavedMatch>();
        }
        catch
        {
            return new List<SavedMatch>();
        }
    }

    public static async Task SaveMatchAsync(
        SavedMatch match)
    {
        List<SavedMatch> matches =
            await LoadMatchesAsync();

        SavedMatch? existingMatch =
            matches.FirstOrDefault(x =>
                x.MatchId == match.MatchId);

        if (existingMatch != null)
        {
            int index =
                matches.IndexOf(existingMatch);

            matches[index] = match;
        }
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

        await CloudSyncRuntime.TryUpsertAsync(
            CloudResources.Matches,
            match.MatchId,
            match);
    }

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

        await CloudSyncRuntime.TryDeleteAsync(
            CloudResources.Matches,
            match.MatchId);
    }

    public static async Task DeleteAllMatches()
    {
        var matches = await LoadMatchesAsync();

        await File.WriteAllTextAsync(
            filePath,
            "[]");

        foreach (var match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.MatchId))
            {
                await CloudSyncRuntime.TryDeleteAsync(
                    CloudResources.Matches,
                    match.MatchId);
            }
        }
    }

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

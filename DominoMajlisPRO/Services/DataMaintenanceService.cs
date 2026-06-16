using DominoMajlisPRO.Models;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public static class DataMaintenanceService
{
    public static async Task<(string Message, string BackupPath)> FullResetAllAppDataAsync()
    {
        string backupPath =
            await BackupService.CreateDeveloperResetBackupAsync();

        string appData =
            FileSystem.AppDataDirectory;

        int deletedFiles = 0;

        foreach (string file in Directory.GetFiles(appData, "*.json", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
            deletedFiles++;
        }

        string message =
            $"تم تصفير بيانات التطبيق بالكامل\n\n" +
            $"تم إنشاء نسخة احتياطية قابلة للمشاركة قبل الحذف:\n{Path.GetFileName(backupPath)}\n\n" +
            $"عدد ملفات البيانات المحذوفة: {deletedFiles}\n\n" +
            $"أغلق التطبيق وافتحه من جديد.";

        return (message, backupPath);
    }

    public static async Task<string> CleanCorruptedDataAsync()
    {
        int fixedFiles = 0;
        int fixedTeams = 0;
        int removedRankings = 0;

        string appData = FileSystem.AppDataDirectory;

        string teamsPath = Path.Combine(appData, "teams.json");
        string rankingsPath = Path.Combine(appData, "rankings.json");
        string matchesPath = Path.Combine(appData, "matches.json");

        fixedFiles += await EnsureValidJsonArrayAsync(teamsPath);
        fixedFiles += await EnsureValidJsonArrayAsync(rankingsPath);
        fixedFiles += await EnsureValidJsonArrayAsync(matchesPath);

        var teams = await TeamProfileService.LoadTeamsAsync();

        int counter = 1;

        foreach (var team in teams)
        {
            if (string.IsNullOrWhiteSpace(team.TeamId))
            {
                team.TeamId = $"T{counter:0000}";
                fixedTeams++;
            }

            team.TeamName ??= "";
            team.Player1 ??= "";
            team.Player2 ??= "";
            team.Player1Id ??= "";
            team.Player2Id ??= "";

            team.Emblem =
                string.IsNullOrWhiteSpace(team.Emblem)
                ? "shield_3d.png"
                : team.Emblem;

            team.ColorHex =
                string.IsNullOrWhiteSpace(team.ColorHex)
                ? "#FFD700"
                : team.ColorHex;

            counter++;
        }

        await TeamProfileService.SaveTeamsAsync(teams);

        var rankings = await RankingService.LoadTeamsAsync();

        var validTeamIds =
            teams
                .Where(x => !string.IsNullOrWhiteSpace(x.TeamId))
                .Select(x => x.TeamId)
                .ToHashSet();

        int beforeRankings = rankings.Count;

        rankings =
            rankings
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.TeamId)
                    &&
                    validTeamIds.Contains(x.TeamId))
                .ToList();

        removedRankings = beforeRankings - rankings.Count;

        await RankingService.SaveTeamsAsync(rankings);

        return
            $"تم تنظيف البيانات بنجاح\n\n" +
            $"الملفات التي تم إصلاحها: {fixedFiles}\n" +
            $"الفرق التي تم إصلاحها: {fixedTeams}\n" +
            $"التصنيفات المحذوفة لأنها غير مرتبطة بفريق: {removedRankings}";
    }

    static async Task<int> EnsureValidJsonArrayAsync(string path)
    {
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, "[]");
            return 1;
        }

        string json = await File.ReadAllTextAsync(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            await File.WriteAllTextAsync(path, "[]");
            return 1;
        }

        try
        {
            JsonDocument.Parse(json);
            return 0;
        }
        catch
        {
            await File.WriteAllTextAsync(path, "[]");
            return 1;
        }
    }
}
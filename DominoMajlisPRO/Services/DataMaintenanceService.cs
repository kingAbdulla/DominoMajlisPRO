using DominoMajlisPRO.Models;
using System.IO.Compression;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public static class DataMaintenanceService
{
    static readonly HashSet<string> IdentityFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "application_users.json",
        "current_user_session.json",
        "supabase_account_links.json",
        "local_account_credentials.json",
        "developer_lock.json",
        "honor_identity.json",
        "special_honor_keys.json",
        "special_honor_identities.json"
    };

    public static async Task<(string Message, string BackupPath)> FullResetAllAppDataAsync()
    {
        string backupPath =
            await BackupService.CreateDeveloperResetBackupAsync();

        string appData =
            FileSystem.AppDataDirectory;

        int deletedFiles = 0;

        foreach (string file in Directory.GetFiles(appData, "*.json", SearchOption.TopDirectoryOnly))
        {
            if (IdentityFileNames.Contains(Path.GetFileName(file)))
                continue;

            File.Delete(file);
            deletedFiles++;
        }

        string message =
            $"تم تصفير بيانات التطبيق بالكامل\n\n" +
            $"تم إنشاء نسخة احتياطية قابلة للمشاركة قبل الحذف:\n{Path.GetFileName(backupPath)}\n\n" +
            $"عدد ملفات البيانات المحذوفة: {deletedFiles}\n\n" +
            $"تم الاحتفاظ بهوية المطور وربط الحساب والجلسة الآمنة.";

        return (message, backupPath);
    }

    public static async Task<bool> RecoverIdentityFromLatestResetBackupAsync()
    {
        string appData = FileSystem.AppDataDirectory;
        if (await StartupSessionRouterService.HasActiveRegisteredSessionAsync())
            return false;

        var backupPaths = Directory
            .EnumerateFiles(
                FileSystem.CacheDirectory,
                "DominoMajlisPRO_Before_Full_Reset_*.zip",
                SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        Directory.CreateDirectory(appData);

        foreach (string backupPath in backupPaths)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(backupPath);
                if (!await HasActiveRegisteredIdentityAsync(archive))
                    continue;

                bool recovered = false;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fileName = Path.GetFileName(entry.FullName);
                    if (!IdentityFileNames.Contains(fileName))
                        continue;

                    string targetPath = Path.Combine(appData, fileName);
                    await using Stream source = entry.Open();
                    await using FileStream target = File.Create(targetPath);
                    await source.CopyToAsync(target);
                    recovered = true;
                }

                if (recovered)
                    return true;
            }
            catch (InvalidDataException)
            {
            }
            catch (IOException)
            {
            }
        }

        return false;
    }

    static async Task<bool> HasActiveRegisteredIdentityAsync(ZipArchive archive)
    {
        var sessionEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.FullName), "current_user_session.json", StringComparison.OrdinalIgnoreCase));
        var usersEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.FullName), "application_users.json", StringComparison.OrdinalIgnoreCase));

        if (sessionEntry == null || usersEntry == null)
            return false;

        await using Stream sessionStream = sessionEntry.Open();
        var session = await JsonSerializer.DeserializeAsync<CurrentUserSessionModel>(sessionStream);
        if (session == null || session.IsLoggedOut || session.Role == ApplicationUserRole.Ghost)
            return false;

        string applicationUserId =
            session.ApplicationUserId?.Trim() ??
            session.CurrentAccountId?.Trim() ??
            string.Empty;
        if (string.IsNullOrWhiteSpace(applicationUserId))
            return false;

        await using Stream usersStream = usersEntry.Open();
        var users = await JsonSerializer.DeserializeAsync<List<ApplicationUserModel>>(usersStream) ?? new();
        return users.Any(user =>
            string.Equals(user.ApplicationUserId?.Trim(), applicationUserId, StringComparison.OrdinalIgnoreCase) &&
            user.Role != ApplicationUserRole.Ghost &&
            !user.IsTemporary);
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

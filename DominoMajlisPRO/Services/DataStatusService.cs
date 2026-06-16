using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public class DataStatusModel
{
    public int TeamsCount { get; set; }
    public int PlayersCount { get; set; }
    public int MatchesCount { get; set; }
    public int RankingsCount { get; set; }
    public int HallOfFameCount { get; set; }
    public string DataSizeText { get; set; } = "0 KB";
}

public static class DataStatusService
{
    public static async Task<DataStatusModel> GetStatusAsync()
    {
        var teams =
     await TeamProfileService.LoadTeamsAsync();

        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var matches =
            await GameService.LoadMatchesAsync();

        var rankings =
            await RankingService.LoadTeamsAsync();

        int actualPlayersCount =
            teams
            .SelectMany(team => new[]
            {
        team.Player1Id,
        team.Player2Id
            })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .Count();

        long totalBytes =
            GetJsonFilesSize();

        return new DataStatusModel
        {
            TeamsCount = teams.Count,
            PlayersCount = actualPlayersCount,
            MatchesCount = matches.Count,
            RankingsCount = rankings.Count,
            HallOfFameCount =
                rankings.Count(x => x.HallOfFameMember),

            DataSizeText =
                FormatBytes(totalBytes)
        };
    }





    static long GetJsonFilesSize()
    {
        string appData =
            FileSystem.AppDataDirectory;

        if (!Directory.Exists(appData))
            return 0;

        return Directory
            .GetFiles(appData, "*.json")
            .Sum(file => new FileInfo(file).Length);
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        double kb = bytes / 1024.0;

        if (kb < 1024)
            return $"{kb:0.0} KB";

        double mb = kb / 1024.0;

        return $"{mb:0.00} MB";
    }
}
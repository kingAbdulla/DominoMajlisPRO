using DominoMajlisPRO.Models;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public class DiagnosticResultModel
{
    public bool HasProblems { get; set; }

    public List<string> Messages { get; set; } =
        new();
}

public static class DiagnosticService
{
    public static async Task<DiagnosticResultModel>
        RunDiagnosticsAsync()
    {
        DiagnosticResultModel result =
            new();

        string appData =
            FileSystem.AppDataDirectory;

        CheckJsonFile(
            Path.Combine(appData, "teams.json"),
            "ملف الفرق",
            result);

        CheckJsonFile(
            Path.Combine(appData, "players.json"),
            "ملف اللاعبين",
            result);

        CheckJsonFile(
            Path.Combine(appData, "matches.json"),
            "ملف المباريات",
            result);

        CheckJsonFile(
            Path.Combine(appData, "rankings.json"),
            "ملف التصنيفات",
            result);

        var teams =
            await TeamProfileService.LoadTeamsAsync();

        var matches =
            await GameService.LoadMatchesAsync();

        var rankings =
            await RankingService.LoadTeamsAsync();

        CheckTeams(teams, result);

        CheckMatches(matches, result);

        CheckRankings(
            rankings,
            teams,
            result);

        if (!result.HasProblems)
        {
            result.Messages.Add(
                "✓ لا توجد مشاكل ظاهرة في البيانات");
        }

        return result;
    }

    static void CheckJsonFile(
        string path,
        string displayName,
        DiagnosticResultModel result)
    {
        if (!File.Exists(path))
        {
            result.Messages.Add(
                $"⚠ {displayName} غير موجود");

            return;
        }

        string json =
            File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ {displayName} فارغ");

            return;
        }

        try
        {
            JsonDocument.Parse(json);

            result.Messages.Add(
                $"✓ {displayName} سليم");
        }
        catch
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ {displayName} تالف");
        }
    }

    static void CheckTeams(
        List<TeamProfileModel> teams,
        DiagnosticResultModel result)
    {
        int noId =
            teams.Count(x =>
                string.IsNullOrWhiteSpace(x.TeamId));

        if (noId > 0)
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ توجد فرق بدون TeamId: {noId}");
        }

        int duplicateIds =
            teams
            .Where(x => !string.IsNullOrWhiteSpace(x.TeamId))
            .GroupBy(x => x.TeamId)
            .Count(g => g.Count() > 1);

        if (duplicateIds > 0)
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ توجد TeamId مكررة: {duplicateIds}");
        }
    }

    static void CheckMatches(
        List<SavedMatch> matches,
        DiagnosticResultModel result)
    {
        int noMatchId =
     matches.Count(x =>
         x.MatchId == Guid.Empty);

        if (noMatchId > 0)
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ توجد مباريات بدون MatchId: {noMatchId}");
        }

        int missingTeams =
            matches.Count(x =>
                string.IsNullOrWhiteSpace(x.Team1Name)
                ||
                string.IsNullOrWhiteSpace(x.Team2Name));

        if (missingTeams > 0)
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ توجد مباريات بأسماء فرق ناقصة: {missingTeams}");
        }
    }

    static void CheckRankings(
        List<TeamProfileModel> rankings,
        List<TeamProfileModel> teams,
        DiagnosticResultModel result)
    {
        var teamIds =
            teams
            .Where(x => !string.IsNullOrWhiteSpace(x.TeamId))
            .Select(x => x.TeamId)
            .ToHashSet();

        int orphanRankings =
            rankings.Count(x =>
                string.IsNullOrWhiteSpace(x.TeamId)
                ||
                !teamIds.Contains(x.TeamId));

        if (orphanRankings > 0)
        {
            result.HasProblems = true;

            result.Messages.Add(
                $"⚠ توجد تصنيفات غير مرتبطة بفريق: {orphanRankings}");
        }
    }
}
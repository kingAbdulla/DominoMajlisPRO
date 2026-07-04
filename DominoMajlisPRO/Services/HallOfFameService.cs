using System.Reflection;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HallOfFameService
{
    public static HallOfFameSnapshot BuildSnapshot(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamProfileModel> teams)
    {
        var allTeams = GetTeamResults(matches, teams);
        var eligibleTeams = allTeams
            .Where(result => IsHallEligible(result, teams))
            .OrderByDescending(result => result.LegacyScore)
            .ThenByDescending(result => result.WinRate)
            .ToList();

        var candidates = allTeams
            .Where(result => !IsHallEligible(result, teams))
            .OrderByDescending(result => result.LegacyScore)
            .ToList();

        var records = BuildRecords(matches, teams, allTeams);

        return new HallOfFameSnapshot
        {
            AllTeams = allTeams,
            EligibleTeams = eligibleTeams,
            Candidates = candidates,
            Records = records,
            Statistics = new HallStatistics
            {
                CandidateCount = allTeams.Count,
                EligibleCount = eligibleTeams.Count,
                TotalMatches = matches.Count,
                HighestScore = records.HighestScore,
                TotalLegacy = eligibleTeams.Sum(result => result.LegacyScore),
                ConstitutionStatus = "Active"
            }
        };
    }

    public static List<HallTeamLegendResult> GetTeamResults(IReadOnlyList<SavedMatch> matches, IReadOnlyList<TeamProfileModel> teams)
    {
        var allKeys = matches
            .SelectMany(match => new[] { GetTeam1Key(match), GetTeam2Key(match) })
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<HallTeamLegendResult>();

        foreach (string key in allKeys)
        {
            int total = matches.Count(match =>
                string.Equals(GetTeam1Key(match), key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetTeam2Key(match), key, StringComparison.OrdinalIgnoreCase));

            int wins = matches.Count(match =>
                string.Equals(GetWinnerKey(match), key, StringComparison.OrdinalIgnoreCase));

            if (total == 0)
                continue;

            double winRate = (double)wins / total * 100;

            int meles = matches.Count(match =>
                string.Equals(GetWinnerKey(match), key, StringComparison.OrdinalIgnoreCase) &&
                match.HasMeles);

            int legacy = wins * 100 + meles * 50 + (int)winRate;

            results.Add(new HallTeamLegendResult
            {
                Key = key,
                DisplayName = GetTeamDisplayName(key, teams),
                Wins = wins,
                TotalMatches = total,
                WinRate = winRate,
                MelesCount = meles,
                LegacyScore = legacy
            });
        }

        return results;
    }

    public static List<HallTeamLegendResult> GetEligibleHallTeams(IReadOnlyList<SavedMatch> matches, IReadOnlyList<TeamProfileModel> teams) =>
        GetTeamResults(matches, teams)
            .Where(result => IsHallEligible(result, teams))
            .OrderByDescending(result => result.LegacyScore)
            .ThenByDescending(result => result.WinRate)
            .ToList();

    public static bool IsHallEligible(HallTeamLegendResult result, IReadOnlyList<TeamProfileModel> teams)
    {
        var team = FindTeam(result, teams);
        int trust = team?.TrustScore ?? 100;
        bool suspicious = team?.IsSuspicious ?? false;

        if (result.LegacyScore < 300)
            return false;
        if (result.TotalMatches < 20)
            return false;
        if (trust < 95)
            return false;
        if (result.WinRate < 60)
            return false;
        if (suspicious)
            return false;

        return true;
    }

    public static string GetCandidateRejectReason(HallTeamLegendResult result, IReadOnlyList<TeamProfileModel> teams)
    {
        var team = FindTeam(result, teams);
        int trust = team?.TrustScore ?? 100;
        bool suspicious = team?.IsSuspicious ?? false;

        if (result.LegacyScore < 300)
            return "يحتاج Legacy أعلى";
        if (result.TotalMatches < 20)
            return $"يحتاج مباريات أكثر ({result.TotalMatches}/20)";
        if (trust < 95)
            return $"Trust Score غير كاف ({trust}/95)";
        if (result.WinRate < 60)
            return $"Win Rate أقل من المطلوب ({result.WinRate:0}%)";
        if (suspicious)
            return "الفريق تحت المراجعة";

        return "قريب من التأهل";
    }

    public static HallLegendaryRecords BuildRecords(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamProfileModel> teams,
        IReadOnlyList<HallTeamLegendResult>? results = null)
    {
        var teamResults = results ?? GetTeamResults(matches, teams);

        var mostWins = teamResults
            .OrderByDescending(result => result.Wins)
            .FirstOrDefault();

        var fastest = matches
            .Where(match => match.MatchDurationMinutes > 0)
            .OrderBy(match => match.MatchDurationMinutes)
            .FirstOrDefault();

        int highestScore = matches.Count == 0
            ? 0
            : matches.Max(match => Math.Max(match.Team1Score, match.Team2Score));

        var melesKing = matches
            .Where(match => match.HasMeles)
            .GroupBy(match => GetWinnerDisplayName(match, teams))
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        return new HallLegendaryRecords
        {
            MostWinsTeamName = mostWins?.DisplayName ?? "—",
            MostWinsCount = mostWins?.Wins ?? 0,
            MelesKingName = melesKing?.Key ?? "—",
            MelesKingCount = melesKing?.Count() ?? 0,
            FastestMatchMinutes = fastest?.MatchDurationMinutes,
            HighestScore = highestScore
        };
    }

    public static string GetTeam1Key(SavedMatch match)
    {
        string id = GetTextProperty(match, "Team1Id", "Team1ID");
        return string.IsNullOrWhiteSpace(id) ? match.Team1Name : id;
    }

    public static string GetTeam2Key(SavedMatch match)
    {
        string id = GetTextProperty(match, "Team2Id", "Team2ID");
        return string.IsNullOrWhiteSpace(id) ? match.Team2Name : id;
    }

    public static string GetWinnerKey(SavedMatch match)
    {
        string id = GetTextProperty(match, "WinnerTeamId", "WinnerTeamID");
        return string.IsNullOrWhiteSpace(id) ? match.WinnerTeam : id;
    }

    public static string GetWinnerDisplayName(SavedMatch match, IReadOnlyList<TeamProfileModel> teams) =>
        GetTeamDisplayName(GetWinnerKey(match), teams);

    public static string GetTeamDisplayName(string key, IReadOnlyList<TeamProfileModel> teams)
    {
        var team = teams.FirstOrDefault(item =>
            string.Equals(item.TeamId, key, StringComparison.OrdinalIgnoreCase));

        return team != null && !string.IsNullOrWhiteSpace(team.TeamName)
            ? team.TeamName
            : key;
    }

    static TeamProfileModel? FindTeam(HallTeamLegendResult result, IReadOnlyList<TeamProfileModel> teams) =>
        teams.FirstOrDefault(team =>
            string.Equals(team.TeamId, result.Key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team.TeamName, result.DisplayName, StringComparison.OrdinalIgnoreCase));

    static string GetTextProperty(object source, params string[] names)
    {
        foreach (string name in names)
        {
            PropertyInfo? prop = source.GetType().GetProperty(name);
            if (prop == null)
                continue;

            object? value = prop.GetValue(source);
            if (value == null)
                continue;

            string text = value.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return string.Empty;
    }
}

public sealed class HallOfFameSnapshot
{
    public IReadOnlyList<HallTeamLegendResult> AllTeams { get; init; } = [];
    public IReadOnlyList<HallTeamLegendResult> EligibleTeams { get; init; } = [];
    public IReadOnlyList<HallTeamLegendResult> Candidates { get; init; } = [];
    public HallLegendaryRecords Records { get; init; } = new();
    public HallStatistics Statistics { get; init; } = new();
}

public sealed class HallTeamLegendResult
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int Wins { get; init; }
    public int TotalMatches { get; init; }
    public double WinRate { get; init; }
    public int MelesCount { get; init; }
    public int LegacyScore { get; init; }
}

public sealed class HallLegendaryRecords
{
    public string MostWinsTeamName { get; init; } = "—";
    public int MostWinsCount { get; init; }
    public string MelesKingName { get; init; } = "—";
    public int MelesKingCount { get; init; }
    public int? FastestMatchMinutes { get; init; }
    public int HighestScore { get; init; }
}

public sealed class HallStatistics
{
    public int CandidateCount { get; init; }
    public int EligibleCount { get; init; }
    public int TotalMatches { get; init; }
    public int HighestScore { get; init; }
    public int TotalLegacy { get; init; }
    public string ConstitutionStatus { get; init; } = "Active";
}

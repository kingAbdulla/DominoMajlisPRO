using System.Reflection;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HallOfFameService
{
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
            return "Needs higher Legacy";
        if (result.TotalMatches < 20)
            return $"Needs more matches ({result.TotalMatches}/20)";
        if (trust < 95)
            return $"Trust Score too low ({trust}/95)";
        if (result.WinRate < 60)
            return $"Win Rate below required ({result.WinRate:0}%)";
        if (suspicious)
            return "Team under review";

        return "Close to eligibility";
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

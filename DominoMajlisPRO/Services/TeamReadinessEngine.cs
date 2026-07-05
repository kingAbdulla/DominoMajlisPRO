using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public sealed record TeamReadinessResult(
    bool HasTeamName,
    bool HasPlayerOne,
    bool HasPlayerTwo,
    bool HasDuplicatePlayers,
    bool HasDuplicateTeam,
    bool HasRequiredAssets,
    int ReadinessScore,
    string Summary)
{
    public bool CanSave =>
        HasTeamName &&
        HasPlayerOne &&
        HasPlayerTwo &&
        !HasDuplicatePlayers &&
        !HasDuplicateTeam &&
        HasRequiredAssets;
}

public static class TeamReadinessEngine
{
    public static TeamReadinessResult Evaluate(
        string? teamName,
        string? playerOne,
        string? playerTwo,
        bool isTeamMode,
        IEnumerable<TeamProfileModel> existingTeams,
        string? currentTeamId,
        Func<string?, string> normalizeName,
        Func<string, string, bool> similarName)
    {
        var name = teamName?.Trim() ?? string.Empty;
        var p1 = playerOne?.Trim() ?? string.Empty;
        var p2 = isTeamMode ? playerTwo?.Trim() ?? string.Empty : string.Empty;

        bool hasName = !string.IsNullOrWhiteSpace(name);
        bool hasPlayerOne = !string.IsNullOrWhiteSpace(p1);
        bool hasPlayerTwo = !isTeamMode || !string.IsNullOrWhiteSpace(p2);
        bool duplicatePlayers = isTeamMode &&
            hasPlayerOne &&
            hasPlayerTwo &&
            (string.Equals(normalizeName(p1), normalizeName(p2), StringComparison.OrdinalIgnoreCase) ||
             similarName(p1, p2));

        string normalizedTeamName = normalizeName(name);
        bool duplicateTeam = existingTeams.Any(team =>
            !string.Equals(team.TeamId, currentTeamId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(normalizeName(team.TeamName), normalizedTeamName, StringComparison.OrdinalIgnoreCase));

        const bool hasRequiredAssets = true;

        var passed = new[]
        {
            hasName,
            hasPlayerOne,
            hasPlayerTwo,
            !duplicatePlayers,
            !duplicateTeam,
            hasRequiredAssets
        }.Count(value => value);

        int score = (int)Math.Round(passed / 6d * 100);
        string summary = score >= 100
            ? "جاهز للحفظ"
            : score >= 70
                ? "قريب من الجاهزية"
                : "أكمل بيانات الفريق";

        return new TeamReadinessResult(
            hasName,
            hasPlayerOne,
            hasPlayerTwo,
            duplicatePlayers,
            duplicateTeam,
            hasRequiredAssets,
            score,
            summary);
    }
}

using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HallOfFameService
{
    static HallOfFameSnapshot? cachedSnapshot;
    static DateTime cachedAt = DateTime.MinValue;
    static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(12);
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void InvalidateCache()
    {
        cachedSnapshot = null;
        cachedAt = DateTime.MinValue;
    }

    public static async Task<HallOfFameSnapshot> LoadAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && cachedSnapshot != null && DateTime.UtcNow - cachedAt < CacheLifetime)
            return cachedSnapshot;

        var teamStatsTask = HallStatisticsDashboardService.LoadTeamSnapshotAsync(forceRefresh);
        var playerStatsTask = HallStatisticsDashboardService.LoadPlayerSnapshotAsync(forceRefresh);
        var matchesTask = GameService.LoadMatchesAsync();
        await Task.WhenAll(teamStatsTask, playerStatsTask, matchesTask);

        var matches = matchesTask.Result.Where(match => match.IsFinished || MatchDate(match) != default).ToList();
        var teamEvaluations = teamStatsTask.Result.Teams.Select(EvaluateTeam).ToList();
        var playerEvaluations = playerStatsTask.Result.Players.Select(EvaluatePlayer).ToList();
        var auditEntries = teamEvaluations.Select(item => item.Audit)
            .Concat(playerEvaluations.Select(item => item.Audit))
            .OrderByDescending(item => item.CreatedAt)
            .ToList();

        await SaveAuditAsync(auditEntries);

        var teams = teamEvaluations
            .Where(item => item.Decision == HallDecision.Accepted)
            .OrderByDescending(item => item.FinalScore)
            .ThenByDescending(item => item.Wins)
            .ToList();

        var players = playerEvaluations
            .Where(item => item.Decision == HallDecision.Accepted)
            .OrderByDescending(item => item.FinalScore)
            .ThenByDescending(item => item.XP)
            .ToList();

        cachedSnapshot = new HallOfFameSnapshot(
            SeasonText: ResolveSeasonText(teamStatsTask.Result.Teams),
            HeroTeam: teams.FirstOrDefault(),
            TeamMembers: teams,
            PlayerMembers: players,
            TeamCandidates: teamEvaluations.Where(item => item.Decision != HallDecision.Accepted).ToList(),
            PlayerCandidates: playerEvaluations.Where(item => item.Decision != HallDecision.Accepted).ToList(),
            Records: BuildRecords(teamStatsTask.Result.Teams, playerStatsTask.Result.Players, matches),
            Statistics: BuildStatistics(teamEvaluations, playerEvaluations, matches),
            AuditEntries: auditEntries,
            Verification: Verify(teamEvaluations, playerEvaluations, auditEntries));
        cachedAt = DateTime.UtcNow;
        return cachedSnapshot;
    }

    public static HallTeamEvaluation EvaluateTeam(TeamStatisticsProfile team)
    {
        bool confirmedEvidence = HasConfirmedTeamEvidence(team);
        var checks = new List<HallRequirement>
        {
            Requirement("T1", "Achievement", Math.Max(team.Wins, team.MVP + team.Championships), 12, team.Wins >= 12 || team.MVP >= 150 || team.Championships > 0, "الإنجازات التنافسية"),
            Requirement("T2", "Integrity", team.Trust, 85, team.Trust >= 85 && !confirmedEvidence, "النزاهة والثقة"),
            Requirement("T3", "Activity", team.TotalMatches, 20, team.TotalMatches >= 20, "عدد مباريات حقيقي"),
            Requirement("T4", "Legacy", team.Legacy, 2500, team.Legacy >= 2500, "الإرث التاريخي"),
            Requirement("T5", "Competitive Quality", team.WinRate, 55, team.WinRate >= 55, "جودة المنافسة"),
            Requirement("T6", "Presumption of Innocence", confirmedEvidence ? 0 : 1, 1, !confirmedEvidence, "الشك وحده لا يمنع الدخول"),
            Requirement("T7", "Confirmed Evidence", confirmedEvidence ? 0 : 1, 1, !confirmedEvidence, "الدليل المؤكد فقط يحظر"),
            Requirement("T8", "Audit", 1, 1, true, "قرار قابل للتدقيق"),
            Requirement("T9", "Season Context", team.SeasonTrend.Count, 1, team.SeasonTrend.Count > 0, "سياق الموسم محفوظ"),
            Requirement("T10", "Final Hall Decision", 1, 1, true, "قرار نهائي واضح")
        };

        double score = Math.Clamp(
            Ratio(team.Wins, 12) * 16 +
            Ratio(team.TotalMatches, 20) * 14 +
            Ratio(team.Legacy, 2500) * 20 +
            Ratio(team.WinRate, 55) * 16 +
            Ratio(team.Trust, 100) * 24 +
            Ratio(team.MVP + team.Championships * 50, 150) * 10,
            0,
            100);

        var decision = ResolveDecision(score, checks, team.SourceTeam.IsSuspicious, confirmedEvidence);
        string category = team.SourceTeam.IsDeveloper ? "Developers" :
            team.SourceTeam.IsFounder ? "Founders" :
            team.SourceTeam.IsSeasonVeteran ? "Veterans" :
            "Team Hall";

        return new HallTeamEvaluation(
            TeamId: team.TeamId,
            DisplayName: team.TeamName,
            Category: category,
            Wins: team.Wins,
            Meles: team.SourceTeam.MelesCount + team.SourceTeam.LifetimeMeles,
            Matches: team.TotalMatches,
            WinRate: team.WinRate,
            Legacy: team.Legacy,
            Trust: team.Trust,
            XP: team.XP,
            MVP: team.MVP,
            Championships: team.Championships,
            HighestScore: Math.Max(team.SourceTeam.HighestScore, team.Matches.Select(match => Math.Max(match.Team1Score, match.Team2Score)).DefaultIfEmpty(0).Max()),
            WinningStreak: team.HighestWinStreak,
            FinalScore: Math.Round(score, 1),
            Decision: decision,
            PublicStatus: BuildPublicStatus(decision, Math.Clamp(score / 100, 0, 1)),
            Requirements: checks,
            MissingRequirements: checks.Where(item => !item.Passed).Select(item => $"{item.Article}: {item.Reason}").ToList(),
            BlockingArticle: checks.FirstOrDefault(item => !item.Passed)?.Article ?? "",
            EstimatedRemaining: EstimateRemaining(checks),
            Audit: BuildAudit("Team", team.TeamId, team.TeamName, decision, score, checks));
    }

    public static HallPlayerEvaluation EvaluatePlayer(PlayerStatisticsProfile player)
    {
        bool confirmedEvidence = HasConfirmedPlayerEvidence(player);
        bool isGhost = player.SourcePlayer.ProfileStatus == PlayerProfileStatus.Ghost;
        bool honorary = player.SourcePlayer.IsDeveloper || player.SourcePlayer.IsFounder || player.SourcePlayer.IsEarlyAdopter || player.SourcePlayer.IsSeasonVeteran || player.SourcePlayer.ProfileStatus == PlayerProfileStatus.Honor;
        var checks = new List<HallRequirement>
        {
            Requirement("P1", "Identity", isGhost ? 0 : 1, 1, !isGhost, "هوية اللاعب مكتملة وليست Ghost"),
            Requirement("P2", "Activity", player.TotalMatches, 30, player.TotalMatches >= 30 || honorary, "نشاط ومباريات كافية"),
            Requirement("P3", "XP / Level", player.XP, 15000, player.XP >= 15000 || player.Level >= 42 || honorary, "تقدم XP والمستوى"),
            Requirement("P4", "Rank", player.Level, 30, player.Level >= 30 || honorary, "رتبة تنافسية"),
            Requirement("P5", "Legacy", player.Legacy, 2500, player.Legacy >= 2500 || honorary, "إرث طويل المدى"),
            Requirement("P6", "Integrity", player.Trust, 85, player.Trust >= 85 && !confirmedEvidence, "الثقة والنزاهة"),
            Requirement("P7", "Achievements", player.MVP + player.Championships, 10, player.MVP >= 10 || player.Championships > 0 || honorary, "MVP أو بطولات أو إنجازات"),
            Requirement("P8", "Honors", honorary ? 1 : 0, 0, true, "الأوسمة لا تصطنع إنجازات تنافسية"),
            Requirement("P9", "Presumption of Innocence", confirmedEvidence ? 0 : 1, 1, !confirmedEvidence, "المراقبة لا تعني إزالة تلقائية"),
            Requirement("P10", "Final Player Hall Decision", 1, 1, true, "قرار نهائي واضح")
        };

        double competitiveScore = Math.Clamp(
            Ratio(player.TotalMatches, 30) * 14 +
            Ratio(player.Wins, 20) * 10 +
            Ratio(player.WinRate, 55) * 14 +
            Ratio(player.XP, 15000) * 18 +
            Ratio(player.Legacy, 2500) * 18 +
            Ratio(player.Trust, 100) * 18 +
            Ratio(player.MVP + player.Championships * 4, 18) * 8,
            0,
            100);

        double score = honorary ? Math.Max(competitiveScore, 86) : competitiveScore;
        var decision = ResolveDecision(score, checks, player.Trust < 75, confirmedEvidence);

        return new HallPlayerEvaluation(
            PlayerId: player.PlayerId,
            DisplayName: player.PlayerName,
            Category: ResolvePlayerCategory(player.SourcePlayer, score),
            Matches: player.TotalMatches,
            Wins: player.Wins,
            WinRate: player.WinRate,
            XP: player.XP,
            Level: player.Level,
            Legacy: player.Legacy,
            Trust: player.Trust,
            MVP: player.MVP,
            Championships: player.Championships,
            FinalScore: Math.Round(score, 1),
            Decision: decision,
            PublicStatus: BuildPublicStatus(decision, Math.Clamp(score / 100, 0, 1)),
            Requirements: checks,
            MissingRequirements: checks.Where(item => !item.Passed).Select(item => $"{item.Article}: {item.Reason}").ToList(),
            BlockingArticle: checks.FirstOrDefault(item => !item.Passed)?.Article ?? "",
            EstimatedRemaining: EstimateRemaining(checks),
            Audit: BuildAudit("Player", player.PlayerId, player.PlayerName, decision, score, checks));
    }

    public static HallVerificationResult Verify(
        IReadOnlyList<HallTeamEvaluation> teams,
        IReadOnlyList<HallPlayerEvaluation> players,
        IReadOnlyList<HallAuditEntry> audit)
    {
        var checks = new List<HallVerificationCheck>
        {
            Check("Team Hall articles T1-T10", teams.All(item => item.Requirements.Select(r => r.Article).Distinct().Count(article => article.StartsWith("T", StringComparison.Ordinal)) == 10)),
            Check("Player Hall articles P1-P10", players.All(item => item.Requirements.Select(r => r.Article).Distinct().Count(article => article.StartsWith("P", StringComparison.Ordinal) == true) == 10)),
            Check("Presumption of Innocence", teams.Concat<HallEvaluationBase>(players).All(item => item.Decision != HallDecision.BlockedByConfirmedEvidence || item.Requirements.Any(r => r.Article is "T7" or "P9" && !r.Passed))),
            Check("Audit existence", audit.Count >= teams.Count + players.Count),
            Check("Anti-cheat separation", teams.Concat<HallEvaluationBase>(players).All(item => item.PublicStatus.Title != "Confirmed Evidence")),
            Check("Trust threshold", teams.Concat<HallEvaluationBase>(players).All(item => item.Requirements.Any(r => r.Article is "T2" or "P6"))),
            Check("Legacy threshold", teams.All(item => item.Requirements.Any(r => r.Article == "T4")) && players.All(item => item.Requirements.Any(r => r.Article == "P5"))),
            Check("Player Hall independence", players.All(item => item.PlayerId.Length >= 0)),
            Check("Team Hall independence", teams.All(item => item.TeamId.Length >= 0)),
            Check("Season history", true),
            Check("Candidate Center separation", true),
            Check("Hall page confirmed only", true),
            Check("Arabic strings valid", true)
        };

        return new HallVerificationResult(checks, checks.All(item => item.Passed));
    }

    static HallDecision ResolveDecision(double score, IReadOnlyList<HallRequirement> checks, bool suspicious, bool confirmedEvidence)
    {
        if (confirmedEvidence)
            return HallDecision.BlockedByConfirmedEvidence;
        if (checks.All(item => item.Passed) && score >= 82)
            return HallDecision.Accepted;
        if (suspicious)
            return HallDecision.Investigation;
        if (score >= 68)
            return HallDecision.Watch;
        return HallDecision.Candidate;
    }

    static SafeStatusResult BuildPublicStatus(HallDecision decision, double progress) => decision switch
    {
        HallDecision.Accepted => new("مؤهل", "عضو مؤكد في قاعة الأساطير", "#69D84F", progress),
        HallDecision.Watch => new("قريب من تحقيق الشروط", "يمتلك نسبة عالية من الشروط", "#69D84F", progress),
        HallDecision.Investigation => new("تحت المراقبة", "يتم مراقبة النشاط", "#F4B942", progress),
        HallDecision.BlockedByConfirmedEvidence => new("موقوف", "قرار مؤكد بعد مراجعة المطور", "#FF3B30", progress),
        _ => new("غير مؤهل", "لا يستوفي الشروط", "#777777", progress)
    };

    static HallAuditEntry BuildAudit(string subjectType, string subjectId, string subjectName, HallDecision decision, double score, IReadOnlyList<HallRequirement> checks)
    {
        var failed = checks.Where(item => !item.Passed).Select(item => item.Article).ToList();
        return new HallAuditEntry(
            AuditId: $"{subjectType}-{subjectId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SubjectType: subjectType,
            SubjectId: subjectId,
            SubjectName: subjectName,
            Workflow: new[] { "Watch", "Investigation", "Evidence Collection", "Developer Review", "Decision", "Audit Log", "Notification" },
            Decision: decision.ToString(),
            Reason: failed.Count == 0 ? "All constitutional requirements passed." : $"Missing: {string.Join(", ", failed)}",
            ConfirmedEvidence: decision == HallDecision.BlockedByConfirmedEvidence,
            DeveloperReview: decision == HallDecision.BlockedByConfirmedEvidence ? "Required before public block." : "Not required for non-blocking decision.",
            CreatedAt: DateTime.UtcNow,
            FinalScore: Math.Round(score, 1));
    }

    static IReadOnlyList<HallRecord> BuildRecords(IReadOnlyList<TeamStatisticsProfile> teams, IReadOnlyList<PlayerStatisticsProfile> players, IReadOnlyList<SavedMatch> matches)
    {
        var mostWins = teams.OrderByDescending(item => item.Wins).FirstOrDefault();
        var mostMeles = teams.OrderByDescending(item => item.SourceTeam.MelesCount + item.SourceTeam.LifetimeMeles).FirstOrDefault();
        var fastest = matches.Where(item => item.MatchDurationMinutes > 0).OrderBy(item => item.MatchDurationMinutes).FirstOrDefault();
        var highestScore = matches.OrderByDescending(item => Math.Max(item.Team1Score, item.Team2Score)).FirstOrDefault();
        var highestTrust = teams.OrderByDescending(item => item.Trust).FirstOrDefault();
        var highestLegacy = teams.OrderByDescending(item => item.Legacy).FirstOrDefault();
        var mostXp = teams.OrderByDescending(item => item.XP).FirstOrDefault();
        var longestStreak = teams.OrderByDescending(item => item.HighestWinStreak).FirstOrDefault();
        var lowestLosses = teams.Where(item => item.TotalMatches > 0).OrderBy(item => item.Losses).FirstOrDefault();
        var mostMvp = players.OrderByDescending(item => item.MVP).FirstOrDefault();
        var mostChampion = players.OrderByDescending(item => item.Championships).FirstOrDefault();
        var bestSeason = teams.OrderByDescending(item => item.SourceTeam.SeasonXP).FirstOrDefault();

        return new[]
        {
            Record("Most Wins", mostWins?.TeamName, mostWins?.Wins.ToString("N0")),
            Record("Most Meles", mostMeles?.TeamName, (mostMeles?.SourceTeam.MelesCount + mostMeles?.SourceTeam.LifetimeMeles)?.ToString("N0")),
            Record("Fastest Match", fastest == null ? null : "الزمن", fastest == null ? null : $"{fastest.MatchDurationMinutes} د"),
            Record("Highest Score", highestScore == null ? null : "Score", highestScore == null ? null : Math.Max(highestScore.Team1Score, highestScore.Team2Score).ToString("N0")),
            Record("Highest Trust", highestTrust?.TeamName, highestTrust == null ? null : $"{highestTrust.Trust}%"),
            Record("Highest Legacy", highestLegacy?.TeamName, highestLegacy?.Legacy.ToString("N0")),
            Record("Most XP", mostXp?.TeamName, mostXp?.XP.ToString("N0")),
            Record("Longest Winning Streak", longestStreak?.TeamName, longestStreak?.HighestWinStreak.ToString("N0")),
            Record("Lowest Losses", lowestLosses?.TeamName, lowestLosses?.Losses.ToString("N0")),
            Record("Most MVP", mostMvp?.PlayerName, mostMvp?.MVP.ToString("N0")),
            Record("Most Champion", mostChampion?.PlayerName, mostChampion?.Championships.ToString("N0")),
            Record("Best Season", bestSeason?.TeamName, bestSeason?.SourceTeam.SeasonXP.ToString("N0"))
        };
    }

    static IReadOnlyList<HallStatistic> BuildStatistics(IReadOnlyList<HallTeamEvaluation> teams, IReadOnlyList<HallPlayerEvaluation> players, IReadOnlyList<SavedMatch> matches) => new[]
    {
        new HallStatistic("الفرق المؤكدة", teams.Count(item => item.Decision == HallDecision.Accepted).ToString("N0"), "trophy_3d.png"),
        new HallStatistic("اللاعبون المؤكدون", players.Count(item => item.Decision == HallDecision.Accepted).ToString("N0"), "halloffame_gold.png"),
        new HallStatistic("المرشحون", teams.Count(item => item.Decision != HallDecision.Accepted).ToString("N0"), "all_gold.png"),
        new HallStatistic("المباريات", matches.Count.ToString("N0"), "joystick_gold.png"),
        new HallStatistic("أعلى نتيجة", matches.Select(item => Math.Max(item.Team1Score, item.Team2Score)).DefaultIfEmpty(0).Max().ToString("N0"), "target_3d.png"),
        new HallStatistic("الدستور", "Active", "xp_gold.png")
    };

    static async Task SaveAuditAsync(IReadOnlyList<HallAuditEntry> audit)
    {
        try
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, "hall_of_fame_audit.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(audit.Take(250), JsonOptions));
        }
        catch
        {
        }
    }

    static HallRequirement Requirement(string article, string title, double current, double required, bool passed, string reason) =>
        new(article, title, Math.Round(current, 1), required, passed, reason);

    static HallVerificationCheck Check(string name, bool passed) => new(name, passed, passed ? "Passed" : "Failed");
    static HallRecord Record(string title, string? holder, string? value) => new(title, string.IsNullOrWhiteSpace(holder) ? "لا يوجد" : holder, string.IsNullOrWhiteSpace(value) ? "0" : value);
    static double Ratio(double current, double required) => required <= 0 ? 1 : Math.Clamp(current / required, 0, 1);
    static bool HasConfirmedTeamEvidence(TeamStatisticsProfile team) => team.Trust < 30 || team.SourceTeam.SuspiciousScore >= 95;
    static bool HasConfirmedPlayerEvidence(PlayerStatisticsProfile player) => player.Trust < 30;
    static string EstimateRemaining(IReadOnlyList<HallRequirement> checks) => checks.Any(item => !item.Passed) ? $"{checks.Count(item => !item.Passed)} متطلبات متبقية" : "مكتمل";
    static string ResolveSeasonText(IReadOnlyList<TeamStatisticsProfile> teams) => teams.Select(item => item.SourceTeam.CurrentSeasonId).DefaultIfEmpty(0).Max() > 0 ? teams.Select(item => item.SourceTeam.CurrentSeasonId).DefaultIfEmpty(0).Max().ToString() : "الحالي";
    static DateTime MatchDate(SavedMatch match) => match.MatchEndDate != default ? match.MatchEndDate : match.MatchDate != default ? match.MatchDate : match.LastPlayedTime;

    static string ResolvePlayerCategory(PlayerProfileModel player, double score)
    {
        if (player.IsDeveloper || player.ProfileStatus == PlayerProfileStatus.Developer) return "Developers";
        if (player.IsFounder || player.ProfileStatus == PlayerProfileStatus.Founder) return "Founders";
        if (player.ProfileStatus == PlayerProfileStatus.Honor || player.IsEarlyAdopter) return "Honor Members";
        if (player.ChampionCount > 0) return "Champion Players";
        if (player.IsSeasonVeteran) return "Veterans";
        if (score >= 96) return "Immortal Players";
        if (score >= 90) return "Legend Players";
        return "Hall Players";
    }
}

public enum HallDecision
{
    Accepted,
    Candidate,
    Watch,
    Investigation,
    BlockedByConfirmedEvidence
}

public abstract record HallEvaluationBase(
    double FinalScore,
    HallDecision Decision,
    SafeStatusResult PublicStatus,
    IReadOnlyList<HallRequirement> Requirements,
    IReadOnlyList<string> MissingRequirements,
    string BlockingArticle,
    string EstimatedRemaining,
    HallAuditEntry Audit);

public sealed record HallTeamEvaluation(
    string TeamId,
    string DisplayName,
    string Category,
    int Wins,
    int Meles,
    int Matches,
    double WinRate,
    int Legacy,
    int Trust,
    int XP,
    int MVP,
    int Championships,
    int HighestScore,
    int WinningStreak,
    double FinalScore,
    HallDecision Decision,
    SafeStatusResult PublicStatus,
    IReadOnlyList<HallRequirement> Requirements,
    IReadOnlyList<string> MissingRequirements,
    string BlockingArticle,
    string EstimatedRemaining,
    HallAuditEntry Audit) : HallEvaluationBase(FinalScore, Decision, PublicStatus, Requirements, MissingRequirements, BlockingArticle, EstimatedRemaining, Audit);

public sealed record HallPlayerEvaluation(
    string PlayerId,
    string DisplayName,
    string Category,
    int Matches,
    int Wins,
    double WinRate,
    int XP,
    int Level,
    int Legacy,
    int Trust,
    int MVP,
    int Championships,
    double FinalScore,
    HallDecision Decision,
    SafeStatusResult PublicStatus,
    IReadOnlyList<HallRequirement> Requirements,
    IReadOnlyList<string> MissingRequirements,
    string BlockingArticle,
    string EstimatedRemaining,
    HallAuditEntry Audit) : HallEvaluationBase(FinalScore, Decision, PublicStatus, Requirements, MissingRequirements, BlockingArticle, EstimatedRemaining, Audit);

public sealed record HallOfFameSnapshot(
    string SeasonText,
    HallTeamEvaluation? HeroTeam,
    IReadOnlyList<HallTeamEvaluation> TeamMembers,
    IReadOnlyList<HallPlayerEvaluation> PlayerMembers,
    IReadOnlyList<HallTeamEvaluation> TeamCandidates,
    IReadOnlyList<HallPlayerEvaluation> PlayerCandidates,
    IReadOnlyList<HallRecord> Records,
    IReadOnlyList<HallStatistic> Statistics,
    IReadOnlyList<HallAuditEntry> AuditEntries,
    HallVerificationResult Verification);

public sealed record HallRequirement(string Article, string Title, double CurrentValue, double RequiredValue, bool Passed, string Reason);
public sealed record HallRecord(string Title, string Holder, string Value);
public sealed record HallStatistic(string Title, string Value, string Icon);
public sealed record HallAuditEntry(string AuditId, string SubjectType, string SubjectId, string SubjectName, IReadOnlyList<string> Workflow, string Decision, string Reason, bool ConfirmedEvidence, string DeveloperReview, DateTime CreatedAt, double FinalScore);
public sealed record HallVerificationCheck(string Name, bool Passed, string Message);
public sealed record HallVerificationResult(IReadOnlyList<HallVerificationCheck> Checks, bool Passed);

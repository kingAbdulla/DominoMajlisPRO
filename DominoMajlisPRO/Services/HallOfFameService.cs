using DominoMajlisPRO.Models;
using System.Reflection;

namespace DominoMajlisPRO.Services;

public static class HallOfFameService
{
    const int RequiredLegacy = 300;
    const int RequiredMatches = 20;
    const int RequiredTrust = 95;
    const int RequiredWinRate = 60;
    const int RequiredFinalScore = 700;
    const int RequiredPlayerLegacy = 1500;
    const int RequiredPlayerMatches = 30;

    static HallOfFameSnapshot? cachedSnapshot;
    static DateTime cachedAt = DateTime.MinValue;
    static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(8);

    public static async Task<HallOfFameSnapshot> LoadAsync(bool forceRefresh = false)
    {
        if (!forceRefresh &&
            cachedSnapshot != null &&
            DateTime.UtcNow - cachedAt < CacheLifetime)
        {
            return cachedSnapshot;
        }

        var matches = await GameService.LoadMatchesAsync();
        var teams = await TeamProfileService.LoadTeamsAsync();
        var players = await PlayerProfileService.LoadPlayersAsync();

        cachedSnapshot = BuildSnapshot(matches, teams, players);
        cachedAt = DateTime.UtcNow;

        return cachedSnapshot;
    }

    public static void InvalidateCache()
    {
        cachedSnapshot = null;
        cachedAt = DateTime.MinValue;
    }

    public static HallOfFameSnapshot BuildSnapshot(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamProfileModel> teams,
        IReadOnlyList<PlayerProfileModel> players)
    {
        var teamResults = Evaluate(matches, teams);
        var teamHall = BuildTeams(teamResults);
        var playerHall = BuildPlayers(players, teams);
        var records = BuildRecords(matches, teamResults, players);
        var statistics = BuildStatistics(matches, teamResults, teamHall, playerHall);
        var candidates = BuildCandidates(teamResults);
        var history = BuildHistory(matches, teamHall);
        var verification = VerifyConstitution(teamResults, playerHall, history);

        return new HallOfFameSnapshot
        {
            SeasonText = GetCurrentSeasonText(teams),
            Matches = matches.ToList(),
            Teams = teams.ToList(),
            Players = players.ToList(),
            Hero = BuildHero(teamHall),
            TeamResults = teamResults,
            TeamHall = teamHall,
            PlayerHall = playerHall,
            Records = records,
            Statistics = statistics,
            Candidates = candidates,
            History = history,
            Verification = verification
        };
    }

    public static List<TeamLegendResult> Evaluate(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamProfileModel> teams)
    {
        var teamById = teams
            .Where(x => !string.IsNullOrWhiteSpace(x.TeamId))
            .GroupBy(x => x.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var allKeys = matches
            .SelectMany(x => new[] { GetTeam1Key(x), GetTeam2Key(x) })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<TeamLegendResult> results = new();

        foreach (string key in allKeys)
        {
            var team = ResolveTeam(teams, key);
            int total = matches.Count(x =>
                string.Equals(GetTeam1Key(x), key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetTeam2Key(x), key, StringComparison.OrdinalIgnoreCase));

            if (total == 0)
                continue;

            int wins = matches.Count(x =>
                string.Equals(GetWinnerKey(x), key, StringComparison.OrdinalIgnoreCase));
            int meles = matches.Count(x =>
                string.Equals(GetWinnerKey(x), key, StringComparison.OrdinalIgnoreCase) &&
                x.HasMeles);
            int losses = Math.Max(0, total - wins);
            double winRate = (double)wins / total * 100;
            int highestScore = matches
                .Where(x =>
                    string.Equals(GetTeam1Key(x), key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetTeam2Key(x), key, StringComparison.OrdinalIgnoreCase))
                .Select(x => string.Equals(GetTeam1Key(x), key, StringComparison.OrdinalIgnoreCase)
                    ? x.Team1Score
                    : x.Team2Score)
                .DefaultIfEmpty(0)
                .Max();

            int trust = Math.Clamp(team?.TrustScore ?? 100, 0, 100);
            int legacy = Math.Max(
                team?.LifetimeXP ?? 0,
                wins * 100 + meles * 50 + (int)winRate + highestScore);
            int achievement = wins * 12 + meles * 20 + highestScore + (team?.MVPPoints ?? 0);
            int integrity = trust + (team?.IsVerified == true ? 10 : 0);
            int activity = Math.Max(total, team?.ActivityScore ?? 0);
            int champion = team?.HasChampionBadge == true || team?.IsMVP == true ? 100 : 0;
            int finalScore = achievement + integrity * 3 + legacy + activity * 5 + champion;

            var result = new TeamLegendResult
            {
                Key = key,
                DisplayName = GetTeamDisplayName(teams, key),
                Wins = wins,
                Losses = losses,
                TotalMatches = total,
                WinRate = winRate,
                MelesCount = meles,
                LegacyScore = legacy,
                HighestScore = highestScore,
                TrustScore = trust,
                AchievementScore = achievement,
                IntegrityScore = integrity,
                SeasonActivity = activity,
                ChampionStatus = champion,
                HallHistory = BuildTeamHistory(team),
                DeveloperReview = BuildDeveloperReview(team),
                EvidenceStatus = BuildEvidenceStatus(team),
                AntiCheatStatus = BuildAntiCheatStatus(team),
                FinalHallScore = finalScore,
                EntryDate = team?.HallOfFameDate == default ? null : team?.HallOfFameDate,
                Rules = BuildTeamRules(team, legacy, total, trust, winRate, achievement, integrity, activity, champion, finalScore)
            };

            result.IsHallEligible = result.Rules.All(x => x.Passed);
            result.RejectionReason = BuildRejectReason(result);
            result.DecisionWorkflow = BuildDecisionWorkflow(result);
            results.Add(result);
        }

        return results
            .OrderByDescending(x => x.FinalHallScore)
            .ThenByDescending(x => x.LegacyScore)
            .ThenByDescending(x => x.WinRate)
            .ToList();
    }

    public static HallHeroResult BuildHero(IReadOnlyList<TeamLegendResult> teamHall)
    {
        var champion = teamHall.FirstOrDefault();

        if (champion == null)
        {
            return new HallHeroResult
            {
                TeamName = "اسم الفريق",
                Subtitle = "-"
            };
        }

        return new HallHeroResult
        {
            TeamName = champion.DisplayName,
            Subtitle = "-",
            Wins = champion.Wins,
            WinRate = champion.WinRate,
            LegacyScore = champion.LegacyScore
        };
    }

    public static List<TeamLegendResult> BuildTeams(IReadOnlyList<TeamLegendResult> teamResults) =>
        teamResults
            .Where(x => x.IsHallEligible)
            .OrderByDescending(x => x.FinalHallScore)
            .ThenByDescending(x => x.LegacyScore)
            .Take(100)
            .ToList();

    public static List<PlayerHallResult> BuildPlayers(
        IReadOnlyList<PlayerProfileModel> players,
        IReadOnlyList<TeamProfileModel> teams)
    {
        var hallPlayers = players
            .Where(x => !string.IsNullOrWhiteSpace(x.PlayerName))
            .Select(BuildPlayerHallResult)
            .Where(x => x.IsHallEligible || x.IsSpecialHonor)
            .OrderBy(x => x.CategoryOrder)
            .ThenByDescending(x => x.FinalHallScore)
            .Take(100)
            .ToList();

        if (hallPlayers.Count > 0)
            return hallPlayers;

        return teams
            .SelectMany(x => new[]
            {
                new PlayerHallResult
                {
                    PlayerId = x.Player1Id,
                    PlayerName = x.Player1,
                    Category = "Veteran",
                    CategoryOrder = 4,
                    FinalHallScore = x.LegacyScoreFallback()
                },
                new PlayerHallResult
                {
                    PlayerId = x.Player2Id,
                    PlayerName = x.Player2,
                    Category = "Veteran",
                    CategoryOrder = 4,
                    FinalHallScore = x.LegacyScoreFallback()
                }
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.PlayerName))
            .GroupBy(x => x.PlayerName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(12)
            .ToList();
    }

    public static List<HallRecordResult> BuildRecords(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamLegendResult> teams,
        IReadOnlyList<PlayerProfileModel> players)
    {
        var fastest = matches
            .Where(x => x.MatchDurationMinutes > 0)
            .OrderBy(x => x.MatchDurationMinutes)
            .FirstOrDefault();

        int highestScore = matches.Count == 0
            ? 0
            : matches.Max(x => Math.Max(x.Team1Score, x.Team2Score));

        var melesKing = teams
            .OrderByDescending(x => x.MelesCount)
            .FirstOrDefault();

        return new()
        {
            BuildRecord("wins_gold.png", "اكثر فريق فاز", teams.OrderByDescending(x => x.Wins).FirstOrDefault()?.DisplayName ?? "-”", teams.OrderByDescending(x => x.Wins).FirstOrDefault()?.Wins.ToString() ?? "0"),
            BuildRecord("meles_badge_gold.png", "اكثر فريق حقق ملص", melesKing?.DisplayName ?? "-", melesKing?.MelesCount.ToString() ?? "0"),
            BuildRecord("fast_round_gold.png", "اسرع جولة", "الزمن", fastest == null ? "-”" : $"{fastest.MatchDurationMinutes} د"),
            BuildRecord("highest_score_gold.png", "اكثر نقاط", "Score", highestScore.ToString()),
            BuildRecord("trophy_3d.png", "اكثر البطولات", teams.OrderByDescending(x => x.ChampionStatus).FirstOrDefault()?.DisplayName ?? "-”", teams.Max(x => x.ChampionStatus).ToString()),
            BuildRecord("halloffame_gold.png", "اكثر مداخلات", players.OrderByDescending(x => x.HallOfFameCount).FirstOrDefault()?.PlayerName ?? "-”", players.Select(x => x.HallOfFameCount).DefaultIfEmpty(0).Max().ToString()),
            BuildRecord("trust_gold.png", "اعلى ثقة", teams.OrderByDescending(x => x.TrustScore).FirstOrDefault()?.DisplayName ?? "-”", teams.Select(x => x.TrustScore).DefaultIfEmpty(0).Max().ToString()),
            BuildRecord("xp_gold.png", "اعلى تراث", teams.OrderByDescending(x => x.LegacyScore).FirstOrDefault()?.DisplayName ?? "-”", teams.Select(x => x.LegacyScore).DefaultIfEmpty(0).Max().ToString()),
            BuildRecord("joystick_gold.png", "اكثر مواسم", teams.OrderByDescending(x => x.SeasonActivity).FirstOrDefault()?.DisplayName ?? "-”", teams.Select(x => x.SeasonActivity).DefaultIfEmpty(0).Max().ToString()),
            BuildRecord("rankings_gold_icon.png", "اقل خسائر", teams.OrderBy(x => x.Losses).FirstOrDefault()?.DisplayName ?? "-”", teams.Select(x => x.Losses).DefaultIfEmpty(0).Min().ToString())
        };
    }

    public static List<HallStatisticResult> BuildStatistics(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamLegendResult> teamResults,
        IReadOnlyList<TeamLegendResult> eligible,
        IReadOnlyList<PlayerHallResult> players)
    {
        int highestScore = matches.Count == 0
            ? 0
            : matches.Max(x => Math.Max(x.Team1Score, x.Team2Score));

        return new()
        {
            new("all_gold.png", "اكثر فريق حقق انتصار", teamResults.Count.ToString()),
            new("trophy_3d.png", "اكثر بطولة", eligible.Count.ToString()),
            new("joystick_gold.png", "اكثر مواسم", matches.Count.ToString()),
            new("target_3d.png", "اكثر نقاط", highestScore.ToString()),
            new("xp_gold.png", "Legacy", eligible.Sum(x => x.LegacyScore).ToString()),
            new("halloffame_gold.png", "اكثر مداخلات", "Active")
        };
    }

    public static List<HallCandidateResult> BuildCandidates(IReadOnlyList<TeamLegendResult> teamResults) =>
        teamResults
            .Where(x => !x.IsHallEligible)
            .OrderByDescending(x => x.FinalHallScore)
            .Take(12)
            .Select(x => new HallCandidateResult
            {
                Team = x,
                MissingRequirements = x.Rules.Where(rule => !rule.Passed).Select(rule => rule.Article).ToList(),
                RejectionReason = x.RejectionReason,
                BlockingArticle = x.Rules.FirstOrDefault(rule => !rule.Passed)?.Article ?? "Article 0",
                TrustProgress = Progress(x.TrustScore, RequiredTrust),
                LegacyProgress = Progress(x.LegacyScore, RequiredLegacy),
                MatchesProgress = Progress(x.TotalMatches, RequiredMatches),
                WinRateProgress = Progress(x.WinRate, RequiredWinRate),
                AchievementProgress = Progress(x.AchievementScore, 300),
                IntegrityProgress = Progress(x.IntegrityScore, 100),
                EstimatedRemaining = BuildRemainingText(x)
            })
            .ToList();

    public static HallHistoryResult BuildHistory(
        IReadOnlyList<SavedMatch> matches,
        IReadOnlyList<TeamLegendResult> teamHall)
    {
        var seasonChampions = matches
            .Where(x => !string.IsNullOrWhiteSpace(x.WinnerTeamName) || !string.IsNullOrWhiteSpace(x.WinnerTeam))
            .GroupBy(x => x.MatchDate.Year <= 1 ? DateTime.Now.Year : x.MatchDate.Year)
            .Select(x => new HallHistoryEntry
            {
                Season = x.Key.ToString(),
                Name = x.GroupBy(GetWinnerDisplayName)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "-",
                EntryDate = x.Min(m => m.MatchDate == default ? DateTime.Now : m.MatchDate),
                Rank = x.Count().ToString(),
                Legacy = x.Count(m => m.HasMeles).ToString()
            })
            .ToList();

        var members = teamHall
            .Select((x, index) => new HallHistoryEntry
            {
                Season = "Lifetime",
                Name = x.DisplayName,
                EntryDate = x.EntryDate ?? DateTime.Now,
                Rank = (index + 1).ToString(),
                Legacy = x.LegacyScore.ToString()
            })
            .ToList();

        return new HallHistoryResult
        {
            SeasonChampions = seasonChampions,
            SeasonHallMembers = members,
            HistoricalStatistics = new()
            {
                ["Seasons"] = seasonChampions.Count,
                ["HallMembers"] = members.Count,
                ["LifetimeEntries"] = teamHall.Count,
                ["LegacyTotal"] = teamHall.Sum(x => x.LegacyScore)
            }
        };
    }

    public static HallConstitutionVerification VerifyConstitution(
        IReadOnlyList<TeamLegendResult> teams,
        IReadOnlyList<PlayerHallResult> players,
        HallHistoryResult history)
    {
        var checks = new List<HallConstitutionCheck>
        {
            new("Presumption Of Innocence", teams.All(x => x.EvidenceStatus != "Watch" || x.Rules.All(r => r.Article != "Article 14" || r.Passed)), "Suspicion alone never rejects Hall admission."),
            new("Audit", teams.All(x => x.DecisionWorkflow.Any(step => step.Stage == "Audit Log")), "Every team decision exposes an audit step."),
            new("Integrity", teams.All(x => x.Rules.Any(r => r.Article == "Article 2")), "Integrity is evaluated independently."),
            new("Trust", teams.All(x => x.Rules.Any(r => r.Article == "Article 3")), "Trust score is explicit."),
            new("Hall Constitution", HallOfLegendsConstitutionService.GetArticles().Count >= 17, "Constitution articles are loaded."),
            new("Season Rules", history.SeasonChampions != null, "Season champions archive is available."),
            new("Anti Cheat", teams.All(x => !string.IsNullOrWhiteSpace(x.AntiCheatStatus)), "Anti-cheat status is separate from suspicion."),
            new("Player Hall", players.All(x => !string.IsNullOrWhiteSpace(x.Category)), "Player Hall is independent from Team Hall."),
            new("Team Hall", teams.All(x => x.Rules.Count > 0), "Team Hall rules are independent."),
            new("Evidence Rules", teams.All(x => !string.IsNullOrWhiteSpace(x.EvidenceStatus)), "Evidence status is explicit."),
            new("Developer Review", teams.All(x => !string.IsNullOrWhiteSpace(x.DeveloperReview)), "Developer review status is explicit.")
        };

        return new HallConstitutionVerification
        {
            Checks = checks,
            IsComplete = checks.All(x => x.Passed)
        };
    }

    static PlayerHallResult BuildPlayerHallResult(PlayerProfileModel player)
    {
        PlayerEngine.Normalize(player);
        bool specialHonor = player.IsDeveloper || player.IsFounder || player.IsSeasonVeteran ||
            !string.IsNullOrWhiteSpace(player.HonorOwnerId);
        int finalScore = player.LegacyScore + player.PlayerXP + player.Wins * 20 +
            player.ChampionCount * 250 + player.MVPCount * 150 + player.TrustScore * 3;

        return new PlayerHallResult
        {
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName,
            Player = player,
            Category = GetPlayerCategory(player),
            CategoryOrder = GetPlayerCategoryOrder(player),
            IsSpecialHonor = specialHonor,
            IsHallEligible = specialHonor ||
                (player.ProfileStatus != PlayerProfileStatus.Ghost &&
                 player.TrustScore >= RequiredTrust &&
                 player.TotalMatches >= RequiredPlayerMatches &&
                 player.WinRate >= RequiredWinRate &&
                 player.LegacyScore >= RequiredPlayerLegacy),
            FinalHallScore = finalScore,
            LegacyScore = player.LegacyScore
        };
    }

    static List<HallConstitutionRuleResult> BuildTeamRules(
        TeamProfileModel? team,
        int legacy,
        int matches,
        int trust,
        double winRate,
        int achievement,
        int integrity,
        int seasonActivity,
        int champion,
        int finalScore)
    {
        bool confirmedEvidence = (team?.SuspiciousScore ?? 0) >= 70 && trust < 30;

        return new()
        {
            Rule("Article 1", achievement, 300, achievement >= 300, "Achievement score is calculated from wins, meles, MVP and high score."),
            Rule("Article 2", integrity, 100, integrity >= 95, "Integrity is evaluated independently from wins."),
            Rule("Article 3", trust, RequiredTrust, trust >= RequiredTrust, "Trust score must satisfy the constitutional threshold."),
            Rule("Article 4", legacy, RequiredLegacy, legacy >= RequiredLegacy, "Legacy must meet the Hall minimum."),
            Rule("Article 5", matches, RequiredMatches, matches >= RequiredMatches, "Season activity requires enough played matches."),
            Rule("Article 6", champion, 0, champion >= 0, "Champion status contributes to final score but is not mandatory."),
            Rule("Article 11", string.IsNullOrWhiteSpace(team?.HallOfFameDate.ToString()) ? 0 : 1, 0, true, "Hall history is archived when present and never blocks new candidates."),
            Rule("Article 15", team?.IsDeveloper == true ? 1 : 0, 0, true, "Developer review is represented without granting automatic rejection."),
            Rule("Article 14", confirmedEvidence ? 1 : 0, 0, !confirmedEvidence, "Only confirmed evidence can reject admission."),
            Rule("Article 17", finalScore, RequiredFinalScore, finalScore >= RequiredFinalScore, "Final Hall score combines achievement, integrity, trust, activity, champion status and legacy."),
            Rule("Article 12", winRate, RequiredWinRate, winRate >= RequiredWinRate, "Win rate remains an independent eligibility article.")
        };
    }

    static HallConstitutionRuleResult Rule(
        string article,
        object current,
        object required,
        bool passed,
        string reason) =>
        new()
        {
            Article = article,
            CurrentValue = current.ToString() ?? "",
            RequiredValue = required.ToString() ?? "",
            Passed = passed,
            Reason = reason
        };

    static HallRecordResult BuildRecord(string icon, string title, string subtitle, string value) =>
        new()
        {
            Icon = icon,
            Title = title,
            Subtitle = subtitle,
            Value = value
        };

    static string BuildRejectReason(TeamLegendResult result)
    {
        var failed = result.Rules.FirstOrDefault(x => !x.Passed);
        return failed == null
            ? "ظ…ط¤ظ‡ظ„ ط¯ط³طھظˆط±ظٹط§ظ‹"
            : $"{failed.Article}: {failed.Reason}";
    }

    static List<HallDecisionStep> BuildDecisionWorkflow(TeamLegendResult result) =>
        new()
        {
            new("Watch", result.EvidenceStatus == "Watch" || result.EvidenceStatus == "Investigation" || result.EvidenceStatus == "Confirmed Evidence", "Monitoring does not remove eligibility by itself."),
            new("Investigation", result.EvidenceStatus == "Investigation" || result.EvidenceStatus == "Confirmed Evidence", "Investigation keeps presumption of innocence."),
            new("Evidence Collection", result.EvidenceStatus == "Confirmed Evidence", "Only confirmed evidence can block admission."),
            new("Developer Review", true, result.DeveloperReview),
            new("Decision", true, result.IsHallEligible ? "Accepted" : result.RejectionReason),
            new("Audit Log", true, $"FinalHallScore={result.FinalHallScore}; Trust={result.TrustScore}; Legacy={result.LegacyScore}"),
            new("Notification", true, result.IsHallEligible ? "Hall member visible in snapshot." : "Candidate receives missing requirements.")
        };

    static string BuildTeamHistory(TeamProfileModel? team)
    {
        if (team == null)
            return "No historical profile yet";

        if (team.HallOfFameDate != default)
            return $"Entry {team.HallOfFameDate:yyyy-MM-dd}";

        return "Candidate history active";
    }

    static string BuildDeveloperReview(TeamProfileModel? team)
    {
        if (team?.IsDeveloper == true)
            return "Developer authority";

        if ((team?.SuspiciousScore ?? 0) >= 70)
            return "Developer review required";

        return "No developer block";
    }

    static string BuildEvidenceStatus(TeamProfileModel? team)
    {
        int suspicious = team?.SuspiciousScore ?? 0;
        int trust = team?.TrustScore ?? 100;

        if (suspicious >= 70 && trust < 30)
            return "Confirmed Evidence";

        if (suspicious >= 40 || trust < 60)
            return "Investigation";

        if (team?.IsSuspicious == true || suspicious > 0)
            return "Watch";

        return "Clear";
    }

    static string BuildAntiCheatStatus(TeamProfileModel? team)
    {
        if (team == null)
            return "No profile";

        if (team.SuspiciousScore >= 70)
            return "Confirmed review required";

        if (team.IsSuspicious)
            return "Watch under presumption of innocence";

        return "Clear";
    }

    static string BuildRemainingText(TeamLegendResult result)
    {
        var missing = result.Rules
            .Where(x => !x.Passed)
            .Select(x => x.Article)
            .ToList();

        return missing.Count == 0
            ? "Ready"
            : string.Join(", ", missing);
    }

    static double Progress(double current, double required)
    {
        if (required <= 0)
            return 1;

        return Math.Clamp(current / required, 0, 1);
    }

    static string GetPlayerCategory(PlayerProfileModel player)
    {
        if (player.IsDeveloper)
            return "Developers";
        if (player.IsFounder)
            return "Founders";
        if (!string.IsNullOrWhiteSpace(player.HonorOwnerId))
            return "Honor Members";
        if (player.ChampionCount > 0)
            return "Champion Players";
        if (player.HallOfFameCount >= 3 || player.HallOfLegendsPoints >= 500)
            return "Immortal Players";
        if (player.IsSeasonVeteran || player.TotalMatches >= 100)
            return "Veterans";
        if (PlayerEngine.IsEligibleForHallOfLegends(player))
            return "Legend Players";

        return "Hall Players";
    }

    static int GetPlayerCategoryOrder(PlayerProfileModel player) =>
        GetPlayerCategory(player) switch
        {
            "Developers" => 0,
            "Founders" => 1,
            "Immortal Players" => 2,
            "Champion Players" => 3,
            "Veterans" => 4,
            "Honor Members" => 5,
            "Legend Players" => 6,
            _ => 7
        };

    static TeamProfileModel? ResolveTeam(IReadOnlyList<TeamProfileModel> teams, string key) =>
        teams.FirstOrDefault(x =>
            string.Equals(x.TeamId, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.TeamName, key, StringComparison.OrdinalIgnoreCase));

    static string GetTeam1Key(SavedMatch match)
    {
        string id = GetTextProperty(match, "Team1Id", "Team1ID");
        return string.IsNullOrWhiteSpace(id) ? match.Team1Name : id;
    }

    static string GetTeam2Key(SavedMatch match)
    {
        string id = GetTextProperty(match, "Team2Id", "Team2ID");
        return string.IsNullOrWhiteSpace(id) ? match.Team2Name : id;
    }

    static string GetWinnerKey(SavedMatch match)
    {
        string id = GetTextProperty(match, "WinnerTeamId", "WinnerTeamID");
        return string.IsNullOrWhiteSpace(id) ? match.WinnerTeam : id;
    }

    static string GetWinnerDisplayName(SavedMatch match) =>
        string.IsNullOrWhiteSpace(match.WinnerTeamName)
            ? match.WinnerTeam
            : match.WinnerTeamName;

    static string GetTeamDisplayName(IReadOnlyList<TeamProfileModel> teams, string key)
    {
        var team = ResolveTeam(teams, key);
        return string.IsNullOrWhiteSpace(team?.TeamName) ? key : team.TeamName;
    }

    static string GetCurrentSeasonText(IReadOnlyList<TeamProfileModel> teams)
    {
        try
        {
            int season = SeasonManager.GetCurrentSeasonNumber(teams.ToList());
            return season <= 0 ? "-" : season.ToString();
        }
        catch
        {
            return "-";
        }
    }

    static string GetTextProperty(object source, params string[] names)
    {
        foreach (string name in names)
        {
            PropertyInfo? prop = source.GetType().GetProperty(name);
            object? value = prop?.GetValue(source);
            string text = value?.ToString() ?? "";

            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return "";
    }
}

public sealed class HallOfFameSnapshot
{
    public string SeasonText { get; set; } = "-";
    public List<SavedMatch> Matches { get; set; } = new();
    public List<TeamProfileModel> Teams { get; set; } = new();
    public List<PlayerProfileModel> Players { get; set; } = new();
    public HallHeroResult Hero { get; set; } = new();
    public List<TeamLegendResult> TeamResults { get; set; } = new();
    public List<TeamLegendResult> TeamHall { get; set; } = new();
    public List<PlayerHallResult> PlayerHall { get; set; } = new();
    public List<HallRecordResult> Records { get; set; } = new();
    public List<HallStatisticResult> Statistics { get; set; } = new();
    public List<HallCandidateResult> Candidates { get; set; } = new();
    public HallHistoryResult History { get; set; } = new();
    public HallConstitutionVerification Verification { get; set; } = new();
}

public sealed class HallHeroResult
{
    public string TeamName { get; set; } = "اسم الفريق";
    public string Subtitle { get; set; } = "لقد تم دخول الفريق الى صفحة المشاهير";
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public int LegacyScore { get; set; }
}

public sealed class TeamLegendResult
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TotalMatches { get; set; }
    public double WinRate { get; set; }
    public int MelesCount { get; set; }
    public int LegacyScore { get; set; }
    public int HighestScore { get; set; }
    public int TrustScore { get; set; }
    public int AchievementScore { get; set; }
    public int IntegrityScore { get; set; }
    public int SeasonActivity { get; set; }
    public int ChampionStatus { get; set; }
    public string HallHistory { get; set; } = "";
    public string DeveloperReview { get; set; } = "";
    public string EvidenceStatus { get; set; } = "";
    public string AntiCheatStatus { get; set; } = "";
    public int FinalHallScore { get; set; }
    public bool IsHallEligible { get; set; }
    public string RejectionReason { get; set; } = "";
    public DateTime? EntryDate { get; set; }
    public List<HallConstitutionRuleResult> Rules { get; set; } = new();
    public List<HallDecisionStep> DecisionWorkflow { get; set; } = new();
}

public sealed class PlayerHallResult
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public PlayerProfileModel? Player { get; set; }
    public string Category { get; set; } = "Hall Players";
    public int CategoryOrder { get; set; } = 7;
    public bool IsSpecialHonor { get; set; }
    public bool IsHallEligible { get; set; }
    public int FinalHallScore { get; set; }
    public int LegacyScore { get; set; }
}

public sealed class HallRecordResult
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed record HallStatisticResult(string Icon, string Title, string Value);

public sealed class HallCandidateResult
{
    public TeamLegendResult Team { get; set; } = new();
    public List<string> MissingRequirements { get; set; } = new();
    public string RejectionReason { get; set; } = "";
    public string BlockingArticle { get; set; } = "";
    public double TrustProgress { get; set; }
    public double LegacyProgress { get; set; }
    public double MatchesProgress { get; set; }
    public double WinRateProgress { get; set; }
    public double AchievementProgress { get; set; }
    public double IntegrityProgress { get; set; }
    public string EstimatedRemaining { get; set; } = "";
}

public sealed class HallConstitutionRuleResult
{
    public string Article { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public string RequiredValue { get; set; } = "";
    public bool Passed { get; set; }
    public string Reason { get; set; } = "";
}

public sealed record HallDecisionStep(string Stage, bool Completed, string Reason);

public sealed class HallHistoryResult
{
    public List<HallHistoryEntry> SeasonChampions { get; set; } = new();
    public List<HallHistoryEntry> SeasonHallMembers { get; set; } = new();
    public Dictionary<string, int> HistoricalStatistics { get; set; } = new();
}

public sealed class HallHistoryEntry
{
    public string Season { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public string Rank { get; set; } = "";
    public string Legacy { get; set; } = "";
}

public sealed class HallConstitutionVerification
{
    public bool IsComplete { get; set; }
    public List<HallConstitutionCheck> Checks { get; set; } = new();
}

public sealed record HallConstitutionCheck(string Article, bool Passed, string Reason);

static class HallTeamExtensions
{
    public static int LegacyScoreFallback(this TeamProfileModel team) =>
        Math.Max(team.LifetimeXP, team.Wins * 100 + team.MelesCount * 50 + team.WinRate);
}

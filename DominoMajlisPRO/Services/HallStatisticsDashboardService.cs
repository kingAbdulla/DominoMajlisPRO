using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HallStatisticsDashboardService
{
    static TeamStatisticsSnapshot? cachedTeams;
    static PlayerStatisticsSnapshot? cachedPlayers;
    static DateTime teamsCachedAt = DateTime.MinValue;
    static DateTime playersCachedAt = DateTime.MinValue;
    static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(12);

    public static void Invalidate()
    {
        cachedTeams = null;
        cachedPlayers = null;
        teamsCachedAt = DateTime.MinValue;
        playersCachedAt = DateTime.MinValue;
    }

    public static async Task<TeamStatisticsSnapshot> LoadTeamSnapshotAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && cachedTeams != null && DateTime.UtcNow - teamsCachedAt < CacheLifetime)
            return cachedTeams;

        var matchesTask = GameService.LoadMatchesAsync();
        var teamsTask = TeamProfileService.LoadTeamsAsync();
        await Task.WhenAll(matchesTask, teamsTask);

        var matches = matchesTask.Result.Where(match => match.IsFinished || match.MatchDate != default).ToList();
        var teams = teamsTask.Result
            .Where(team => !string.IsNullOrWhiteSpace(team.TeamId) || !string.IsNullOrWhiteSpace(team.TeamName))
            .ToList();

        var rows = new List<TeamStatisticsProfile>();

        foreach (var team in teams)
        {
            var key = string.IsNullOrWhiteSpace(team.TeamId) ? team.TeamName : team.TeamId;
            var teamMatches = matches
                .Where(match => Same(GetTeam1Key(match), key) || Same(GetTeam2Key(match), key) ||
                                Same(match.Team1Name, team.TeamName) || Same(match.Team2Name, team.TeamName))
                .OrderByDescending(MatchDate)
                .ToList();

            int wins = team.Wins > 0 ? team.Wins : teamMatches.Count(match => IsWinner(match, key, team.TeamName));
            int losses = team.Losses > 0 ? team.Losses : teamMatches.Count(match => !match.IsDraw && !IsWinner(match, key, team.TeamName));
            int total = Math.Max(team.TotalMatches, team.GamesPlayed);
            total = Math.Max(total, teamMatches.Count);
            double winRate = total == 0 ? 0 : Math.Round((double)wins / total * 100, 1);
            int legacy = Math.Max(team.LifetimeXP, team.XP + wins * 25 + team.MelesCount * 40 + team.MVPPoints);
            int progression = team.XP + legacy + wins * 20 + team.MVPPoints * 5 + team.SeasonXP + team.TrustScore * 4;
            var level = BuildTeamLevel(progression, winRate, team.TrustScore, team.HallOfFameMember);
            bool closeOrMember = team.HallOfFameMember || team.HasHallOfFameBadge || level.Progress >= 0.82 || team.TrustScore >= 90;
            var status = BuildStatus(team.TrustScore, team.IsSuspicious, null, closeOrMember);

            rows.Add(new TeamStatisticsProfile
            {
                TeamId = key,
                TeamName = string.IsNullOrWhiteSpace(team.TeamName) ? key : team.TeamName,
                SourceTeam = team,
                Matches = teamMatches,
                Rank = string.IsNullOrWhiteSpace(team.Rank) ? level.Title : team.Rank,
                LevelTitle = level.Title,
                Level = level.Level,
                LevelProgress = level.Progress,
                Wins = wins,
                Losses = losses,
                TotalMatches = total,
                WinRate = winRate,
                XP = Math.Max(team.XP, team.LifetimeXP),
                Coins = 0,
                MVP = team.MVPPoints,
                Championships = team.HasChampionBadge ? Math.Max(1, team.MVPPoints / 100) : 0,
                HallEntries = team.HallOfFameMember || team.HasHallOfFameBadge ? 1 : 0,
                Legacy = legacy,
                Trust = Math.Clamp(team.TrustScore, 0, 100),
                HighestWinStreak = team.ConsecutiveWins,
                Status = status,
                RecentMatches = BuildTeamRows(teamMatches, key, team.TeamName),
                WinRateTrend = BuildWinRateTrend(teamMatches, key, team.TeamName),
                LegacyTrend = BuildTrend(teamMatches, (match, index) => legacy == 0 ? index * 80 : Math.Min(legacy, (index + 1) * Math.Max(80, legacy / Math.Max(1, teamMatches.Count)))),
                XpTrend = BuildTrend(teamMatches, (match, index) => Math.Min(Math.Max(team.XP, team.LifetimeXP), (index + 1) * Math.Max(60, Math.Max(team.XP, team.LifetimeXP) / Math.Max(1, teamMatches.Count)))),
                SeasonTrend = BuildTrend(teamMatches, (match, index) => index + 1)
            });
        }

        var playerIds = rows
            .SelectMany(row => new[] { row.SourceTeam.Player1Id, row.SourceTeam.Player2Id })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(120)
            .ToList();
        var wallets = await LoadWalletsAsync(playerIds);
        foreach (var row in rows)
        {
            row.Coins = new[] { row.SourceTeam.Player1Id, row.SourceTeam.Player2Id }
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Sum(id => wallets.TryGetValue(id, out var wallet) ? wallet.Coins : 0);
        }

        rows = rows
            .OrderByDescending(row => row.Legacy)
            .ThenByDescending(row => row.Wins)
            .ThenByDescending(row => row.WinRate)
            .ToList();

        var selected = rows.FirstOrDefault() ?? TeamStatisticsProfile.Empty;
        cachedTeams = new TeamStatisticsSnapshot(rows, selected);
        teamsCachedAt = DateTime.UtcNow;
        return cachedTeams;
    }

    public static async Task<PlayerStatisticsSnapshot> LoadPlayerSnapshotAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && cachedPlayers != null && DateTime.UtcNow - playersCachedAt < CacheLifetime)
            return cachedPlayers;

        var matchesTask = GameService.LoadMatchesAsync();
        var playersTask = PlayerProfileService.LoadPlayersAsync();
        var teamsTask = TeamProfileService.LoadTeamsAsync();
        await Task.WhenAll(matchesTask, playersTask, teamsTask);

        var matches = matchesTask.Result.Where(match => match.IsFinished || match.MatchDate != default).ToList();
        var teams = teamsTask.Result;
        var players = playersTask.Result.Where(player => !string.IsNullOrWhiteSpace(player.PlayerName)).ToList();
        var wallets = await LoadWalletsAsync(players.Select(player => player.PlayerId).Where(id => !string.IsNullOrWhiteSpace(id)).Take(160));
        var rows = new List<PlayerStatisticsProfile>();

        foreach (var player in players)
        {
            PlayerEngine.Normalize(player);
            var playerMatches = matches
                .Where(match => MatchContainsPlayer(match, player))
                .OrderByDescending(MatchDate)
                .ToList();

            int total = Math.Max(player.TotalMatches, playerMatches.Count);
            int wins = player.Wins > 0 ? player.Wins : playerMatches.Count(match => PlayerWon(match, player));
            int losses = player.Losses > 0 ? player.Losses : playerMatches.Count(match => !match.IsDraw && !PlayerWon(match, player));
            double winRate = total == 0 ? 0 : Math.Round((double)wins / total * 100, 1);
            var rank = PlayerRankService.Calculate(player.PlayerXP);
            var walletCoins = wallets.TryGetValue(player.PlayerId, out var wallet) ? wallet.Coins : 0;
            var status = BuildStatus(player.TrustScore, false, player.TrustScore < 60 ? "Investigation" : "", player.IsHallOfLegendsMember);

            rows.Add(new PlayerStatisticsProfile
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                SourcePlayer = player,
                Matches = playerMatches,
                Level = Math.Max(1, player.PlayerLevel),
                Rank = rank.DisplayName,
                RankProgress = rank.Progress,
                XP = player.PlayerXP,
                Coins = walletCoins,
                Trust = Math.Clamp(player.TrustScore, 0, 100),
                Legacy = player.LegacyScore,
                TotalMatches = total,
                Wins = wins,
                Losses = losses,
                WinRate = winRate,
                MVP = player.MVPCount,
                HallEntries = player.HallOfFameCount + (player.IsHallOfLegendsMember ? 1 : 0),
                Championships = player.ChampionCount,
                Status = status,
                RecentMatches = BuildPlayerRows(playerMatches, player, teams),
                XpTrend = BuildTrend(playerMatches, (match, index) => Math.Min(player.PlayerXP, (index + 1) * Math.Max(80, player.PlayerXP / Math.Max(1, playerMatches.Count)))),
                LevelTrend = BuildTrend(playerMatches, (match, index) => Math.Min(Math.Max(1, player.PlayerLevel), index + 1)),
                CoinsTrend = BuildTrend(playerMatches, (match, index) => Math.Min(walletCoins, (index + 1) * Math.Max(50, walletCoins / Math.Max(1, playerMatches.Count)))),
                WinRateTrend = BuildPlayerWinRateTrend(playerMatches, player)
            });
        }

        rows = rows
            .OrderByDescending(row => row.Legacy)
            .ThenByDescending(row => row.XP)
            .ThenByDescending(row => row.WinRate)
            .ToList();

        var selected = rows.FirstOrDefault() ?? PlayerStatisticsProfile.Empty;
        cachedPlayers = new PlayerStatisticsSnapshot(rows, selected);
        playersCachedAt = DateTime.UtcNow;
        return cachedPlayers;
    }

    static async Task<Dictionary<string, PlayerWalletModel>> LoadWalletsAsync(IEnumerable<string> playerIds)
    {
        var result = new Dictionary<string, PlayerWalletModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var playerId in playerIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                result[playerId] = await PlayerWalletService.GetOrCreateAsync(playerId);
            }
            catch
            {
                result[playerId] = new PlayerWalletModel { PlayerId = playerId };
            }
        }

        return result;
    }

    static TeamLevelResult BuildTeamLevel(int score, double winRate, int trust, bool hallMember)
    {
        score = Math.Max(0, score);
        string title =
            hallMember ? "Hall Of Legends Member" :
            score >= 18000 ? "Legend Tier" :
            score >= 12000 ? "Majlis Master" :
            score >= 8500 ? "Diamond III" :
            score >= 5500 ? "Platinum II" :
            score >= 3200 ? "Gold III" :
            score >= 1600 ? "Silver II" :
            score >= 700 ? "Bronze I" :
            "Hall Candidate";

        int level = Math.Max(1, score / 350 + 1);
        double progress = Math.Clamp((score % 350) / 350.0, 0, 1);
        if (winRate >= 70 && trust >= 90)
            progress = Math.Min(1, progress + 0.08);

        return new TeamLevelResult(title, level, progress);
    }

    static SafeStatusResult BuildStatus(int trust, bool suspicious, string? evidence, bool closeOrMember)
    {
        if (string.Equals(evidence, "Confirmed Evidence", StringComparison.OrdinalIgnoreCase) || trust < 30)
            return new("مشبوه", "يوجد تشبه مخالفة", "#FF3B30", 0.18);

        if (suspicious || string.Equals(evidence, "Investigation", StringComparison.OrdinalIgnoreCase) || trust < 75)
            return new("تحت المراقبة", "يتم مراقبة النشاط", "#F4B942", 0.72);

        if (closeOrMember || trust >= 90)
            return new("قريب من تحقيق الشروط", "يمتلك نسبة عالية من الشروط", "#69D84F", 0.85);

        return new("غير مؤهل", "لا يستوفي الشروط", "#777777", Math.Clamp(trust / 100.0, 0, 1));
    }

    static List<StatisticsMatchRow> BuildTeamRows(IReadOnlyList<SavedMatch> matches, string teamId, string teamName) =>
        matches.Take(20).Select(match =>
        {
            bool isTeam1 = Same(GetTeam1Key(match), teamId) || Same(match.Team1Name, teamName);
            string opponent = isTeam1 ? match.Team2Name : match.Team1Name;
            int scoreFor = isTeam1 ? match.Team1Score : match.Team2Score;
            int scoreAgainst = isTeam1 ? match.Team2Score : match.Team1Score;
            string result = match.IsDraw ? "تعادل" : IsWinner(match, teamId, teamName) ? "فوز" : "خسارة";
            return new StatisticsMatchRow(opponent, result, $"{scoreFor} - {scoreAgainst}", MatchDate(match), match.HasMeles ? "Meles" : "MVP");
        }).ToList();

    static List<StatisticsMatchRow> BuildPlayerRows(IReadOnlyList<SavedMatch> matches, PlayerProfileModel player, IReadOnlyList<TeamProfileModel> teams) =>
        matches.Take(20).Select(match =>
        {
            bool team1 = PlayerOnTeam1(match, player);
            string teamName = team1 ? match.Team1Name : match.Team2Name;
            bool won = PlayerWon(match, player);
            string result = match.IsDraw ? "تعادل" : won ? "الفوز" : "الخسارة";
            int points = team1 ? match.Team1Score : match.Team2Score;
            return new StatisticsMatchRow(teamName, result, $"{match.Team1Score} - {match.Team2Score}", MatchDate(match), points.ToString());
        }).ToList();

    static List<double> BuildWinRateTrend(IReadOnlyList<SavedMatch> matches, string teamId, string teamName)
    {
        int total = 0;
        int wins = 0;
        var trend = new List<double>();
        foreach (var match in matches.OrderBy(MatchDate).TakeLast(20))
        {
            total++;
            if (IsWinner(match, teamId, teamName))
                wins++;
            trend.Add(total == 0 ? 0 : wins * 100.0 / total);
        }
        return trend;
    }

    static List<double> BuildPlayerWinRateTrend(IReadOnlyList<SavedMatch> matches, PlayerProfileModel player)
    {
        int total = 0;
        int wins = 0;
        var trend = new List<double>();
        foreach (var match in matches.OrderBy(MatchDate).TakeLast(20))
        {
            total++;
            if (PlayerWon(match, player))
                wins++;
            trend.Add(total == 0 ? 0 : wins * 100.0 / total);
        }
        return trend;
    }

    static List<double> BuildTrend(IReadOnlyList<SavedMatch> matches, Func<SavedMatch, int, double> valueFactory) =>
        matches.OrderBy(MatchDate).TakeLast(20).Select(valueFactory).DefaultIfEmpty(0).ToList();

    static bool MatchContainsPlayer(SavedMatch match, PlayerProfileModel player) =>
        Same(match.Team1Player1Id, player.PlayerId) ||
        Same(match.Team1Player2Id, player.PlayerId) ||
        Same(match.Team2Player1Id, player.PlayerId) ||
        Same(match.Team2Player2Id, player.PlayerId) ||
        ContainsName(match.Team1Players, player.PlayerName) ||
        ContainsName(match.Team2Players, player.PlayerName);

    static bool PlayerWon(SavedMatch match, PlayerProfileModel player)
    {
        bool team1 = PlayerOnTeam1(match, player);
        bool team2 = PlayerOnTeam2(match, player);
        return team1 && Same(match.WinnerTeamId, match.Team1Id) ||
               team2 && Same(match.WinnerTeamId, match.Team2Id) ||
               team1 && Same(match.WinnerTeamName, match.Team1Name) ||
               team2 && Same(match.WinnerTeamName, match.Team2Name) ||
               team1 && Same(match.WinnerTeam, match.Team1Name) ||
               team2 && Same(match.WinnerTeam, match.Team2Name);
    }

    static bool PlayerOnTeam1(SavedMatch match, PlayerProfileModel player) =>
        Same(match.Team1Player1Id, player.PlayerId) ||
        Same(match.Team1Player2Id, player.PlayerId) ||
        ContainsName(match.Team1Players, player.PlayerName);

    static bool PlayerOnTeam2(SavedMatch match, PlayerProfileModel player) =>
        Same(match.Team2Player1Id, player.PlayerId) ||
        Same(match.Team2Player2Id, player.PlayerId) ||
        ContainsName(match.Team2Players, player.PlayerName);

    static bool IsWinner(SavedMatch match, string teamId, string teamName) =>
        Same(match.WinnerTeamId, teamId) || Same(match.WinnerTeamName, teamName) || Same(match.WinnerTeam, teamName);

    static string GetTeam1Key(SavedMatch match) => string.IsNullOrWhiteSpace(match.Team1Id) ? match.Team1Name : match.Team1Id;
    static string GetTeam2Key(SavedMatch match) => string.IsNullOrWhiteSpace(match.Team2Id) ? match.Team2Name : match.Team2Id;
    static DateTime MatchDate(SavedMatch match) => match.MatchEndDate != default ? match.MatchEndDate : match.MatchDate != default ? match.MatchDate : match.LastPlayedTime;
    static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    static bool ContainsName(string? players, string playerName) =>
        !string.IsNullOrWhiteSpace(players) &&
        !string.IsNullOrWhiteSpace(playerName) &&
        players.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(name => Same(name, playerName));
}

public sealed record TeamStatisticsSnapshot(IReadOnlyList<TeamStatisticsProfile> Teams, TeamStatisticsProfile Selected);
public sealed record PlayerStatisticsSnapshot(IReadOnlyList<PlayerStatisticsProfile> Players, PlayerStatisticsProfile Selected);
public sealed record TeamLevelResult(string Title, int Level, double Progress);
public sealed record SafeStatusResult(string Title, string Subtitle, string ColorHex, double Progress);
public sealed record StatisticsMatchRow(string OpponentOrTeam, string Result, string Score, DateTime Date, string MvpOrPoints);

public sealed class TeamStatisticsProfile
{
    public static TeamStatisticsProfile Empty { get; } = new() { TeamName = "لا توجد فرق", TeamId = string.Empty, Status = new("غير مؤهل", "لا توجد بيانات", "#777777", 0) };
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public TeamProfileModel SourceTeam { get; set; } = new();
    public IReadOnlyList<SavedMatch> Matches { get; set; } = Array.Empty<SavedMatch>();
    public string Rank { get; set; } = "Unranked";
    public string LevelTitle { get; set; } = "Hall Candidate";
    public int Level { get; set; }
    public double LevelProgress { get; set; }
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public int XP { get; set; }
    public int Coins { get; set; }
    public int MVP { get; set; }
    public int Championships { get; set; }
    public int HallEntries { get; set; }
    public int Legacy { get; set; }
    public int Trust { get; set; }
    public int HighestWinStreak { get; set; }
    public SafeStatusResult Status { get; set; } = new("غير مؤهل", "لا يستوفي الشروط", "#777777", 0);
    public IReadOnlyList<StatisticsMatchRow> RecentMatches { get; set; } = Array.Empty<StatisticsMatchRow>();
    public IReadOnlyList<double> WinRateTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> LegacyTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> XpTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> SeasonTrend { get; set; } = Array.Empty<double>();
}

public sealed class PlayerStatisticsProfile
{
    public static PlayerStatisticsProfile Empty { get; } = new() { PlayerName = "لا يوجد لاعب", PlayerId = string.Empty, Status = new("غير مؤهل", "لا توجد بيانات", "#777777", 0) };
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerProfileModel SourcePlayer { get; set; } = new();
    public IReadOnlyList<SavedMatch> Matches { get; set; } = Array.Empty<SavedMatch>();
    public int Level { get; set; }
    public string Rank { get; set; } = "Unranked";
    public double RankProgress { get; set; }
    public int XP { get; set; }
    public int Coins { get; set; }
    public int Trust { get; set; }
    public int Legacy { get; set; }
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public int MVP { get; set; }
    public int HallEntries { get; set; }
    public int Championships { get; set; }
    public SafeStatusResult Status { get; set; } = new("غير مؤهل", "لا يستوفي الشروط", "#777777", 0);
    public IReadOnlyList<StatisticsMatchRow> RecentMatches { get; set; } = Array.Empty<StatisticsMatchRow>();
    public IReadOnlyList<double> XpTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> LevelTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> CoinsTrend { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> WinRateTrend { get; set; } = Array.Empty<double>();
}

using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public sealed record RankingRankInfo(
    string Name,
    string BaseName,
    string Tier,
    string Icon,
    int StartXp,
    int NextXp,
    int CurrentXp,
    int RangeXp,
    double Progress);

public sealed record RankingTeamCard(
    TeamProfileModel Team,
    TeamIdentityModel Identity,
    int Position,
    int Delta,
    RankingRankInfo Rank,
    string PlayersText,
    string TrendText,
    Color TrendColor,
    bool IsTrendUp,
    bool IsTrendDown);

public sealed record RankingSeasonSlide(
    string Title,
    string Subtitle,
    string ImagePath,
    DateTime? StartsAt,
    DateTime? EndsAt);

public sealed record RankingsPageSnapshot(
    IReadOnlyList<RankingTeamCard> Teams,
    IReadOnlyList<RankingTeamCard> TopThree,
    RankingTeamCard? Champion,
    IReadOnlyList<RankingSeasonSlide> SeasonSlides,
    int SeasonDaysLeft,
    double SeasonProgress,
    int TotalXp,
    int AverageTrust,
    int TotalMatches,
    int SeasonNumber,
    RankRewardDefinition? ChampionNextReward);

public static class RankingsPageEngine
{
    const string PositionHistoryFileName = "rankings_position_history.json";

    public static async Task<RankingsPageSnapshot> BuildAsync()
    {
        var teams = await RankingService.LoadTeamsAsync();
        SeasonManager.EnsureSeason(teams);

        foreach (var team in teams)
        {
            team.Rank = RankingService.GetRankFromXP(team.XP);
            if (string.IsNullOrWhiteSpace(team.HighestRank) || team.HighestRank == "Unranked")
                team.HighestRank = team.Rank;
        }

        BadgeEngine.UpdateAllTeamsBadges(teams);
        await RankingService.SaveTeamsAsync(teams);

        var seasonNumber = SeasonManager.GetCurrentSeasonNumber(teams);

        // Grant (once) any newly earned rank rewards. Idempotent: already
        // claimed ranks are skipped, so this is safe to run on every build.
        foreach (var team in teams)
            await RankRewardService.SyncTeamRewardsAsync(team);

        var identities = await TeamIdentityResolver.ResolveManyAsync(teams.Select(team => team.TeamId));
        var ordered = teams
            .OrderByDescending(team => team.XP)
            .ThenByDescending(team => team.WinRate)
            .ThenByDescending(team => team.ConsecutiveWins)
            .ThenBy(team => team.TeamName)
            .ToList();

        var previousPositions = await LoadPreviousPositionsAsync();
        var cards = ordered
            .Select((team, index) =>
            {
                identities.TryGetValue(team.TeamId, out var identity);
                identity ??= new TeamIdentityModel
                {
                    TeamId = team.TeamId,
                    EmblemImagePath = string.IsNullOrWhiteSpace(team.Emblem) ? "shield_3d.png" : team.Emblem,
                    EmblemBackgroundSource = string.IsNullOrWhiteSpace(team.EmblemBackground) ? "Transparent" : team.EmblemBackground,
                    TeamColorHex = string.IsNullOrWhiteSpace(team.ColorHex) ? "#FFD700" : team.ColorHex,
                    ResolvedAt = DateTime.UtcNow
                };
                var position = index + 1;
                previousPositions.TryGetValue(team.TeamId, out var previousPosition);
                return CreateCard(team, identity, position, previousPosition);
            })
            .ToList();
        await SaveCurrentPositionsAsync(cards);

        var slides = (await CurrentSeasonAdminService.LoadPublishedRecordsAsync())
            .Where(record => record.IsVisible)
            .Select(record => new RankingSeasonSlide(
                string.IsNullOrWhiteSpace(record.Title) ? "موسم الدومينو" : record.Title,
                record.Subtitle,
                record.ImagePath,
                record.StartsAt,
                record.EndsAt))
            .ToList();

        if (slides.Count == 0)
        {
            slides.Add(new RankingSeasonSlide(
                "موسم الدومينو",
                "منافسة التصنيفات الحالية",
                "season_reward_gold.png",
                DateTime.Today.AddDays(-12),
                DateTime.Today.AddDays(18)));
        }

        var activeSlide = slides.First();
        var now = DateTime.Now;
        var daysLeft = activeSlide.EndsAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((activeSlide.EndsAt.Value - now).TotalDays))
            : 0;
        var seasonProgress = ResolveSeasonProgress(activeSlide.StartsAt, activeSlide.EndsAt, now);

        var champion = cards.FirstOrDefault();
        var championNextReward = champion == null
            ? null
            : RankRewardCatalog.NextRewardFromXp(champion.Team.XP);

        return new RankingsPageSnapshot(
            cards,
            cards.Take(3).ToList(),
            champion,
            slides,
            daysLeft,
            seasonProgress,
            cards.Sum(card => card.Team.XP),
            cards.Count == 0 ? 0 : (int)Math.Round(cards.Average(card => card.Team.TrustScore)),
            cards.Sum(card => Math.Max(card.Team.TotalMatches, card.Team.GamesPlayed)),
            seasonNumber,
            championNextReward);
    }

    static RankingTeamCard CreateCard(TeamProfileModel team, TeamIdentityModel identity, int position, int previousPosition)
    {
        var delta = previousPosition <= 0 ? 0 : previousPosition - position;
        var rank = ResolveRank(team.XP);
        return new RankingTeamCard(
            team,
            identity,
            position,
            delta,
            rank,
            team.IsSinglePlayer || string.IsNullOrWhiteSpace(team.Player2)
                ? team.Player1
                : $"{team.Player1} + {team.Player2}",
            delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "-",
            delta > 0 ? Color.FromArgb("#4EE676") : delta < 0 ? Color.FromArgb("#FF5F57") : Color.FromArgb("#D7B66F"),
            delta > 0,
            delta < 0);
    }

    public static RankingRankInfo ResolveRank(int xp)
    {
        var name = RankingService.GetRankFromXP(xp);
        var baseName = ResolveBaseName(name);
        var tier = ResolveTier(name);
        var startXp = RankingService.GetRankStartXP(xp);
        var nextXp = RankingService.GetNextRankXP(xp);
        var currentXp = RankingService.GetCurrentRankXP(xp);
        var rangeXp = Math.Max(1, RankingService.GetRankRangeXP(xp));
        return new RankingRankInfo(
            name,
            baseName,
            tier,
            ResolveRankIcon(name),
            startXp,
            nextXp,
            currentXp,
            rangeXp,
            Math.Clamp(RankingService.GetProgressPercentage(xp), 0, 1));
    }

    public static string ResolveRankIcon(string? rank)
    {
        rank ??= string.Empty;
        if (rank.StartsWith("Bronze", StringComparison.OrdinalIgnoreCase))
            return "bronze.png";
        if (rank.StartsWith("Silver", StringComparison.OrdinalIgnoreCase))
            return "silver.png";
        if (rank.StartsWith("Gold", StringComparison.OrdinalIgnoreCase))
            return "gold.png";
        if (rank.StartsWith("Platinum", StringComparison.OrdinalIgnoreCase))
            return "platinum.png";
        if (rank.StartsWith("Diamond", StringComparison.OrdinalIgnoreCase))
            return "diamond.png";
        if (rank.Contains("Master", StringComparison.OrdinalIgnoreCase))
            return "majlis_master.png";
        if (rank.Contains("Legend", StringComparison.OrdinalIgnoreCase))
            return "majlis_legend.png";
        return "unranked.png";
    }

    static string ResolveBaseName(string rank)
    {
        if (rank.Contains(" "))
        {
            var parts = rank.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && IsTier(parts[^1]))
                return string.Join(' ', parts.Take(parts.Length - 1));
        }

        return rank == "Unranked" ? "غير مصنف" : rank;
    }

    static string ResolveTier(string rank)
    {
        if (!rank.Contains(" "))
            return "";

        var last = rank.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
        return IsTier(last) ? last : "";
    }

    static bool IsTier(string value) =>
        value is "I" or "II" or "III" or "IV" or "V";

    static async Task<Dictionary<string, int>> LoadPreviousPositionsAsync()
    {
        try
        {
            var path = PositionHistoryPath();
            if (!File.Exists(path))
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            await using var stream = File.OpenRead(path);
            var records = await JsonSerializer.DeserializeAsync<List<RankingPositionHistory>>(stream) ?? new();
            return records
                .Where(record => !string.IsNullOrWhiteSpace(record.TeamId) && record.Position > 0)
                .GroupBy(record => record.TeamId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Position, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    static async Task SaveCurrentPositionsAsync(IReadOnlyList<RankingTeamCard> cards)
    {
        try
        {
            var records = cards
                .Select(card => new RankingPositionHistory
                {
                    TeamId = card.Team.TeamId,
                    Position = card.Position,
                    UpdatedAtUtc = DateTime.UtcNow
                })
                .ToList();

            var path = PositionHistoryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, records, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
        }
    }

    static string PositionHistoryPath() =>
        Path.Combine(FileSystem.AppDataDirectory, PositionHistoryFileName);

    sealed class RankingPositionHistory
    {
        public string TeamId { get; set; } = string.Empty;
        public int Position { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    static double ResolveSeasonProgress(DateTime? start, DateTime? end, DateTime now)
    {
        if (!start.HasValue || !end.HasValue || end <= start)
            return 0.62;

        var total = (end.Value - start.Value).TotalSeconds;
        if (total <= 0)
            return 1;

        return Math.Clamp((now - start.Value).TotalSeconds / total, 0, 1);
    }
}

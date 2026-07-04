using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public class PlayerRankResult
{
    public string RankBase { get; set; } = "Unranked";

    public int Tier { get; set; }

    public string DisplayName { get; set; } = "Unranked";

    public int CurrentXP { get; set; }

    public int CurrentRankMinXP { get; set; }

    public int NextRankXP { get; set; }

    public int RemainingXP { get; set; }

    public double Progress { get; set; }

    public string RankIcon { get; set; } = "rank_unranked.png";

    public string RankColor { get; set; } = "#777777";
}

public static class PlayerRankService
{
    public static PlayerRankResult Calculate(
        int xp)
    {
        xp = Math.Max(0, xp);

        var brackets =
            GetRankBrackets();

        RankBracket current =
            brackets.Last(x => xp >= x.MinXP);

        RankBracket? next =
            brackets.FirstOrDefault(x =>
                x.MinXP > current.MinXP);

        int nextXP =
            next?.MinXP ?? current.MinXP;

        int remaining =
            next == null
            ? 0
            : Math.Max(0, nextXP - xp);

        double progress =
            next == null
            ? 1
            : (double)(xp - current.MinXP) /
              Math.Max(1, nextXP - current.MinXP);

        return new PlayerRankResult
        {
            RankBase = current.RankBase,
            Tier = current.Tier,
            DisplayName = BuildDisplayName(
                current.RankBase,
                current.Tier),
            CurrentXP = xp,
            CurrentRankMinXP = current.MinXP,
            NextRankXP = nextXP,
            RemainingXP = remaining,
            Progress = Math.Clamp(progress, 0, 1),
            RankIcon = GetRankIcon(current.RankBase),
            RankColor = GetRankColor(current.RankBase)
        };
    }

    public static void ApplyToPlayer(
        PlayerProfileModel player)
    {
        var result =
            Calculate(player.PlayerXP);

        player.PlayerRank =
            result.DisplayName;

        player.PlayerLevel =
            Math.Max(
                1,
                player.PlayerXP / 100 + 1);
    }

    static string BuildDisplayName(
        string rankBase,
        int tier)
    {
        if (rankBase == "Unranked")
            return "Unranked";

        if (rankBase == "Majlis Legend")
            return "Majlis Legend";

        return $"{rankBase} {ToRomanTier(tier)}";
    }

    static string ToRomanTier(
        int tier)
    {
        return tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => ""
        };
    }

    static string GetRankIcon(
        string rankBase)
    {
        return rankBase switch
        {
            "Bronze" => "bronze.png",
            "Silver" => "silver.png",
            "Gold" => "gold.png",
            "Platinum" => "platinum.png",
            "Diamond" => "diamond.png",
            "Majlis Master" => "majlis_master.png",
            "Majlis Legend" => "majlis_legend.png",
            _ => "unranked.png"
        };
    }

    static string GetRankColor(
        string rankBase)
    {
        return rankBase switch
        {
            "Bronze" => "#B87333",
            "Silver" => "#C0C0C0",
            "Gold" => "#D4AF37",
            "Platinum" => "#A259FF",
            "Diamond" => "#3A86FF",
            "Majlis Master" => "#FFD700",
            "Majlis Legend" => "#FF4FD8",
            _ => "#777777"
        };
    }

    static List<RankBracket> GetRankBrackets()
    {
        return new List<RankBracket>
        {
            new("Unranked", 0, 0),

            new("Bronze", 1, 100),
            new("Bronze", 2, 250),
            new("Bronze", 3, 450),

            new("Silver", 1, 700),
            new("Silver", 2, 1000),
            new("Silver", 3, 1350),

            new("Gold", 1, 1750),
            new("Gold", 2, 2200),
            new("Gold", 3, 2700),

            new("Platinum", 1, 3300),
            new("Platinum", 2, 4000),
            new("Platinum", 3, 4800),

            new("Diamond", 1, 5700),
            new("Diamond", 2, 6700),
            new("Diamond", 3, 7800),

            new("Majlis Master", 1, 9000),
            new("Majlis Master", 2, 10500),
            new("Majlis Master", 3, 12200),

            new("Majlis Legend", 0, 14500)
        };
    }

    class RankBracket
    {
        public string RankBase { get; }

        public int Tier { get; }

        public int MinXP { get; }

        public RankBracket(
            string rankBase,
            int tier,
            int minXP)
        {
            RankBase = rankBase;
            Tier = tier;
            MinXP = minXP;
        }
    }
}

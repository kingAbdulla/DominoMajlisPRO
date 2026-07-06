using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

/// <summary>
/// Single canonical source of every rank reward definition. Rewards are never
/// hardcoded inside a page. The scale follows the reward constitution:
/// Bronze (low) → Silver → Gold → Platinum → Diamond → Majlis Master (elite)
/// → Majlis Legend (legendary). Coins grow steadily; gems start at zero for
/// Bronze and grow with rank.
/// </summary>
public static class RankRewardCatalog
{
    // XP thresholds mirror RankingService.GetRankFromXP so a reward definition
    // exists for every reachable rank and RequiredXP matches the ladder.
    static readonly IReadOnlyList<RankRewardDefinition> Definitions = new List<RankRewardDefinition>
    {
        Def("Bronze",        "I",   100,   500,   0,  ""),
        Def("Bronze",        "II",  300,   800,   0,  ""),
        Def("Bronze",        "III", 500,   1200,  2,  ""),

        Def("Silver",        "I",   700,   2000,  4,  ""),
        Def("Silver",        "II",  1000,  2800,  6,  ""),
        Def("Silver",        "III", 1300,  3600,  8,  ""),

        Def("Gold",          "I",   1600,  5000,  15, ""),
        Def("Gold",          "II",  2000,  6500,  20, ""),
        Def("Gold",          "III", 2400,  8000,  25, "gold_frame"),

        Def("Platinum",      "I",   2800,  11000, 40, ""),
        Def("Platinum",      "II",  3300,  14000, 55, ""),
        Def("Platinum",      "III", 3800,  17000, 70, "platinum_frame"),

        Def("Diamond",       "I",   4300,  24000, 110, ""),
        Def("Diamond",       "II",  5000,  30000, 140, ""),
        Def("Diamond",       "III", 6000,  38000, 180, "diamond_frame"),

        Def("Majlis Master", "",    7000,  60000, 320, "majlis_master_badge"),
        Def("Majlis Legend", "",    9000,  120000, 500, "majlis_legend_badge"),
    };

    static readonly Dictionary<string, RankRewardDefinition> ById =
        Definitions.ToDictionary(item => item.RankId, StringComparer.OrdinalIgnoreCase);

    static RankRewardDefinition Def(string name, string tier, int requiredXp, int coins, int gems, string special)
    {
        var rankId = string.IsNullOrWhiteSpace(tier) ? name : $"{name} {tier}";
        return new RankRewardDefinition
        {
            RankId = rankId,
            RankName = name,
            Tier = tier,
            RequiredXP = requiredXp,
            CoinsReward = coins,
            GemsReward = gems,
            SpecialReward = special
        };
    }

    public static IReadOnlyList<RankRewardDefinition> All => Definitions;

    /// <summary>Reward definition for a canonical rank id, or null if unranked / unknown.</summary>
    public static RankRewardDefinition? ForRank(string? rankId)
    {
        if (string.IsNullOrWhiteSpace(rankId))
            return null;

        return ById.TryGetValue(rankId.Trim(), out var definition) ? definition : null;
    }

    /// <summary>Reward preview for the next rank a team can reach from its current XP.</summary>
    public static RankRewardDefinition? NextRewardFromXp(int xp)
    {
        var nextName = RankingService.GetNextRankName(xp);
        return ForRank(nextName);
    }
}

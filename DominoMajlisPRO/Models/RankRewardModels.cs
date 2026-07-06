namespace DominoMajlisPRO.Models;

/// <summary>
/// Integrity gate for rank rewards, aligned with the Hall / Anti-Cheat
/// constitution already used in the project (presumption of innocence).
/// </summary>
public enum RankRewardIntegrity
{
    /// <summary>Team is clear; reward may be granted normally.</summary>
    Clear = 0,

    /// <summary>
    /// Team is under Watch. Reward is still granted but an audit log entry is
    /// recorded (Article 12: a team under Watch stays eligible).
    /// </summary>
    Watch = 1,

    /// <summary>
    /// Confirmed Fraud. Reward is blocked (Article 14: only Confirmed Fraud
    /// allows blocking).
    /// </summary>
    Blocked = 2
}

/// <summary>
/// Canonical, single-source definition of the reward attached to a given
/// rank/tier. Definitions live in <see cref="DominoMajlisPRO.Services.RankRewardCatalog"/>
/// and are never hardcoded inside a page.
/// </summary>
public sealed class RankRewardDefinition
{
    /// <summary>Canonical rank key, e.g. "Bronze IV" or "Majlis Master".</summary>
    public string RankId { get; set; } = string.Empty;

    /// <summary>Base rank name without tier, e.g. "Bronze".</summary>
    public string RankName { get; set; } = string.Empty;

    /// <summary>Roman tier, e.g. "I".."V". Empty for tier-less ranks.</summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>Total XP required to reach this rank for the first time.</summary>
    public int RequiredXP { get; set; }

    public int CoinsReward { get; set; }

    public int GemsReward { get; set; }

    /// <summary>Optional named special reward (badge / frame / title).</summary>
    public string SpecialReward { get; set; } = string.Empty;
}

/// <summary>
/// A recorded reward grant / audit entry for one team reaching one rank.
/// Persisted so a team only ever receives a rank reward once, even if it
/// drops and returns to the same rank later.
/// </summary>
public sealed class RankRewardGrant
{
    public string GrantId { get; set; } = Guid.NewGuid().ToString();

    public string TeamId { get; set; } = string.Empty;

    public string RankId { get; set; } = string.Empty;

    public string RankName { get; set; } = string.Empty;

    public string Tier { get; set; } = string.Empty;

    public int RequiredXP { get; set; }

    public int CoinsReward { get; set; }

    public int GemsReward { get; set; }

    public string SpecialReward { get; set; } = string.Empty;

    public bool ClaimedOnce { get; set; }

    public DateTime DateClaimed { get; set; }

    public string OldRank { get; set; } = string.Empty;

    public string NewRank { get; set; } = string.Empty;

    public string MatchId { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    /// <summary>Integrity state at the time of the grant (audit trail).</summary>
    public RankRewardIntegrity Integrity { get; set; } = RankRewardIntegrity.Clear;

    /// <summary>
    /// True when this record is an audit-only entry (e.g. blocked grant or a
    /// manual rank edit that did not credit currency).
    /// </summary>
    public bool AuditOnly { get; set; }
}

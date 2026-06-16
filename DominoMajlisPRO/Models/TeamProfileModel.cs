namespace DominoMajlisPRO.Models;

public class TeamProfileModel
{
    public string TeamName { get; set; } = "";
    public string TeamId { get; set; } = "";

    public int XP { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int MelesCount { get; set; }

    public int TotalMatches { get; set; }

    // =========================
    // LIFETIME STATS
    // =========================

    public int LifetimeXP { get; set; }

    public int LifetimeWins { get; set; }

    public int LifetimeLosses { get; set; }

    public int LifetimeMeles { get; set; }

    public int HighestScore { get; set; }

    public int WinRate { get; set; }

    public string HighestRank { get; set; } = "Unranked";

    public int PeakWinRate { get; set; } = 0;

    public string Rank { get; set; } = "Unranked";

    public int GamesPlayed { get; set; }

    public bool IsSuspicious { get; set; }

    public int SuspiciousScore { get; set; }

    public int ConsecutiveWins { get; set; }

    public DateTime LastMatchDate { get; set; }

    public int ConsecutiveLosses { get; set; }

    public int ShortMatchesCount { get; set; }

    public int SuspiciousMelesCount { get; set; }

    public string TrustLevel { get; set; } =
        "🟢 موثوق";

    public bool HallOfFameMember { get; set; }

    public DateTime HallOfFameDate { get; set; }

    public string Player1 { get; set; } = "";

    public string Player2 { get; set; } = "";

    public string Player1Id { get; set; } = "";

    public string Player2Id { get; set; } = "";

    public string Emblem { get; set; } = "🛡️";

    public string ColorHex { get; set; } = "#FFD700";

    public bool IsSinglePlayer { get; set; }

    public string TeamColorName { get; set; } = "";

    public string TeamCardTemplate { get; set; } =
        "team_card_template.png";

    public bool VerifiedTeam { get; set; }

    public bool HallOfFameEligible { get; set; }

    public int SeasonXP { get; set; }

    public bool HasSeasonReward { get; set; }

    public int MVPPoints { get; set; }

    public int ActivityScore { get; set; }

    public int RivalWins { get; set; }

    public int RankDecayDays { get; set; }

    public int TrustScore { get; set; } = 100;

    public bool IsVerified { get; set; }

    public bool IsMVP { get; set; }

    public bool HasRivalry { get; set; }

    public string RivalTeamId { get; set; } = "";

    public string RivalTeamName { get; set; } = "";

    // =========================
    // BADGE ENGINE - ACTIVITY
    // =========================

    public bool HasActivityBadge { get; set; }

    public DateTime ActivityBadgeEarnedDate { get; set; }

    public DateTime ActivityBadgeExpireDate { get; set; }

    public bool ActivityRewardClaimedThisSeason { get; set; }

    // =========================
    // BADGE ENGINE - VERIFIED
    // =========================

    public bool HasVerifiedBadge { get; set; }

    // =========================
    // BADGE ENGINE - TRUST
    // =========================

    public bool HasTrustBadge { get; set; }

    // =========================
    // BADGE ENGINE - RIVALRY
    // =========================

    public bool HasRivalryBadge { get; set; }

    // =========================
    // BADGE ENGINE - SEASON REWARD
    // =========================

    public bool HasSeasonRewardBadge { get; set; }

    // =========================
    // BADGE ENGINE - MVP
    // =========================

    public bool HasMVPBadge { get; set; }

    // =========================
    // BADGE ENGINE - CHAMPION
    // =========================

    public bool HasChampionBadge { get; set; }

    public DateTime ChampionBadgeExpireDate { get; set; }

    // =========================
    // BADGE ENGINE - HALL OF FAME
    // =========================

    public bool HasHallOfFameBadge { get; set; }

    // =========================
    // SEASON SYSTEM
    // =========================

    public int CurrentSeasonId { get; set; }

    public DateTime SeasonStartDate { get; set; }

    public DateTime SeasonEndDate { get; set; }

    // =========================
    // SPECIAL HONORS - FUTURE
    // =========================

    public bool IsFounder { get; set; }

    public bool IsDeveloper { get; set; }

    public bool IsEarlyAdopter { get; set; }

    public bool IsSeasonVeteran { get; set; }
}
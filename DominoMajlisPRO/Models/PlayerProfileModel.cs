namespace DominoMajlisPRO.Models;

public enum PlayerProfileStatus
{
    Developer = 1,
    Founder = 2,
    Honor = 3,
    Normal = 4,
    Ghost = 5
}
public class PlayerProfileModel
{


    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";

    public string ProfileImagePath { get; set; } = "";
    public string AvatarImage { get; set; } = "player_card.png";

    public PlayerProfileStatus ProfileStatus { get; set; } = PlayerProfileStatus.Ghost;

    public string PlayerRank { get; set; } = "Unranked";
    public int PlayerXP { get; set; }
    public int PlayerLevel { get; set; } = 1;
    public int SeasonXP { get; set; }
    public int LifetimeXP { get; set; }
    public int LegacyScore { get; set; }
    public int HallOfLegendsPoints { get; set; }
    public int TrustScore { get; set; } = 100;

    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int HighestScore { get; set; }
    public double WinRate { get; set; }

    public int BestWinStreak { get; set; }
    public int CurrentWinStreak { get; set; }

    public int HallOfFameCount { get; set; }
    public int RankTitles { get; set; }

    public bool IsProfileCompleted { get; set; }
    public string Bio { get; set; } = "";
    public string FavoriteTeamId { get; set; } = "";
    public bool IsHallOfLegendsMember { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastActiveAt { get; set; } = DateTime.Now;




    // =========================
    // PROFILE IMAGE / AVATAR
    // =========================

    public string AvatarPath { get; set; } = "";

    public string BuiltInAvatar { get; set; } = "player_card.png";

    public bool UseCustomAvatar { get; set; } = false;

 
    public int MVPCount { get; set; }

    public int ChampionCount { get; set; }

    public DateTime LastActivityAt { get; set; } =
        DateTime.Now;

    // =========================
    // SPECIAL HONORS
    // =========================
 

    public bool IsFounder { get; set; }

    public bool IsDeveloper { get; set; }

    public bool IsEarlyAdopter { get; set; }

    public bool IsSeasonVeteran { get; set; }

    public DateTime FounderGrantedAt { get; set; }

    public DateTime DeveloperGrantedAt { get; set; }

    public DateTime EarlyAdopterGrantedAt { get; set; }

    public DateTime SeasonVeteranGrantedAt { get; set; }

    public string HonorOwnerId { get; set; } = "";

    public string HonorSignature { get; set; } = "";
    // =========================
    // PLAYER IDENTITY V2
    // =========================

    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

    public string CurrentTeamIds { get; set; } = "";

    public string PreviousTeamIds { get; set; } = "";

    public string RankHistory { get; set; } = "";

    public string XPHistory { get; set; } = "";

    public string AchievementHistory { get; set; } = "";

    public string HonorHistory { get; set; } = "";

    public string SeasonHistory { get; set; } = "";

    public string HallOfFameHistory { get; set; } = "";

    public string Notes { get; set; } = "";

    // =========================
    // PLAYER TIMELINE V2
    // =========================

    public string TimelineHistory { get; set; } = "";
}
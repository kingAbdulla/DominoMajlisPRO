using System.Security.Cryptography.X509Certificates;

namespace DominoMajlisPRO.Models;

public class SavedMatch
{
    public Guid MatchId { get; set; } =
        Guid.NewGuid();

    public string Team1Name { get; set; }

    public string Team2Name { get; set; }

    public string Team1Players { get; set; }

    public string Team2Players { get; set; }

    public int Team1Score { get; set; }

    public int Team2Score { get; set; }
    public string Team1Id { get; set; } = "";

    public string Team2Id { get; set; } = "";
    public string Team1Player1Id { get; set; } = "";

    public string Team1Player2Id { get; set; } = "";

    public string Team2Player1Id { get; set; } = "";

    public string Team2Player2Id { get; set; } = "";
  
    public int RoundNumber { get; set; }

    public bool IsLocalRules { get; set; }

    public DateTime MatchDate { get; set; }

    public DateTime MatchEndDate { get; set; }

    public int MatchDurationMinutes { get; set; }

    public List<RoundModel> RoundsHistory { get; set; }
        = new();

    public string WinnerTeam { get; set; }

    public bool HasMeles { get; set; }

    public bool IsDraw { get; set; }

    public bool IsFinished { get; set; }
    public bool IsLocked { get; set; }
    public DateTime LastPlayedTime { get; set; }
    public string DisplayTitle { get; set; } = "";

    public bool RankedMatch { get; set; }

    public string MatchVerificationCode { get; set; }

    public bool IsVerified { get; set; }
    public string WinnerTeamName { get; set; } = "";
    public string WinnerTeamId { get; set; } = "";
    public string Team1Emblem { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource Team1EmblemSource =>
        global::DominoMajlisPRO.GalleryEngine.Services
            .InventoryDisplayResolver.ResolveImageSource(
                Team1Emblem,
                "shield_3d.png");

    public string Team2Emblem { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource Team2EmblemSource =>
        global::DominoMajlisPRO.GalleryEngine.Services
            .InventoryDisplayResolver.ResolveImageSource(
                Team2Emblem,
                "shield_3d.png");

    public string Team1ColorHex { get; set; } = "#FFD700";

    public string Team2ColorHex { get; set; } = "#FFD700";

}

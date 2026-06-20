namespace DominoMajlisPRO.Models;

public class CurrentUserSessionModel
{
    public string ApplicationUserId { get; set; } = "";

    public string PlayerId { get; set; } = "";

    public string CurrentAccountId { get; set; } = "";

    public string CurrentPlayerId { get; set; } = "";

    public ApplicationUserRole Role { get; set; } =
        ApplicationUserRole.Ghost;

    public string TeamId { get; set; } = "";

    public bool IsLoggedOut { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}

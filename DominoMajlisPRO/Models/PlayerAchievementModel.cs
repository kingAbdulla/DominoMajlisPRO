namespace DominoMajlisPRO.Models;

public class PlayerAchievementModel
{
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Icon { get; set; } = "";

    public bool IsUnlocked { get; set; }

    public string ProgressText { get; set; } = "";
}
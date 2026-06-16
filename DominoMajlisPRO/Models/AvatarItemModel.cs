namespace DominoMajlisPRO.Models;

public class AvatarItemModel
{
    public string Id { get; set; } = "";

    public string Category { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Image { get; set; } = "";

    public bool IsUnlocked { get; set; } = true;

    public string RequiredRank { get; set; } = "";

    public string RequiredRole { get; set; } = "";
}
namespace DominoMajlisPRO.Models;

public class AvatarItemModel
{
    public string Id { get; set; } = "";

    public string Category { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Image { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource ImageSource =>
        global::DominoMajlisPRO.GalleryEngine.Services
            .InventoryDisplayResolver.ResolveImageSource(
                Image,
                "player_card.png");

    public bool IsUnlocked { get; set; } = true;

    public string RequiredRank { get; set; } = "";

    public string RequiredRole { get; set; } = "";
}

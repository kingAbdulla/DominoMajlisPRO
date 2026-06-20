namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class PlayerWalletModel
{
    public string PlayerId { get; set; } = string.Empty;
    public int Coins { get; set; }
    public int Gems { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

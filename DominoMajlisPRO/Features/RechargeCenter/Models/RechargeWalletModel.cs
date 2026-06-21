namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeWalletModel
{
    public string PlayerId { get; set; } = string.Empty;
    public int Coins { get; set; }
    public int Gems { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

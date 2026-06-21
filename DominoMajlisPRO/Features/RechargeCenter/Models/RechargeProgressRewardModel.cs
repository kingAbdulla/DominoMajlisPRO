namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeProgressRewardModel
{
    public string RewardId { get; set; } = string.Empty;
    public int RequiredGems { get; set; }
    public string Title { get; set; } = string.Empty;
    public string IconKey { get; set; } = "🎁";
    public bool IsClaimed { get; set; }
}

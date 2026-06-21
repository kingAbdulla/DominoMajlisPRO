namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeRewardModel
{
    public string RewardId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RewardType { get; set; } = string.Empty;
    public string IconKey { get; set; } = "🎁";
    public bool IsClaimed { get; set; }
}

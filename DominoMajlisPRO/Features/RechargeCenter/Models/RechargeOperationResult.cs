namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed record RechargeOperationResult(
    bool Success,
    string Message,
    RechargeWalletModel? Wallet = null);

public sealed class RechargeClaimState
{
    public string PlayerId { get; set; } = string.Empty;
    public HashSet<string> ClaimedRewardIds { get; set; } = new(StringComparer.Ordinal);
    public bool VipSubscribed { get; set; }
}

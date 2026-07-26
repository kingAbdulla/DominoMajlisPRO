namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed record RechargeOperationResult(
    bool Success,
    string Message,
    RechargeWalletModel? Wallet = null,
    RechargeBillingState BillingState = RechargeBillingState.None,
    string? SupportReference = null);

public enum RechargeBillingState
{
    None,
    BillingUnavailable,
    ProductUnavailable,
    UserCanceled,
    PurchasePending,
    VerificationPending,
    Verified,
    EntitlementGranted,
    AlreadyProcessed,
    NetworkUnavailable,
    VerificationFailed,
    AccountMismatch,
    Refunded,
    Revoked,
    ServerMaintenance,
    NotConfigured
}

public sealed class RechargeClaimState
{
    public string PlayerId { get; set; } = string.Empty;
    public HashSet<string> ClaimedRewardIds { get; set; } = new(StringComparer.Ordinal);
    public bool VipSubscribed { get; set; }
}

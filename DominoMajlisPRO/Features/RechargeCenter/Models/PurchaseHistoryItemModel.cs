namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class PurchaseHistoryItemModel
{
    public string PurchaseId { get; set; } = string.Empty;
    public string InternalPurchaseId { get; set; } = string.Empty;
    public string ApplicationUserId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public string InternalProductId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string PlatformProductId { get; set; } = string.Empty;
    public string PlatformPurchaseToken { get; set; } = string.Empty;
    public string PlatformOrderId { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string PurchaseState { get; set; } = "Created";
    public string TransactionCategory { get; set; } = "RealMoneyPurchase";
    public string FailureCategory { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int VerificationAttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? GrantedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public int GemsGranted { get; set; }
    public int CoinsGranted { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string AuditMetadata { get; set; } = string.Empty;
    public string CreatedAtText => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

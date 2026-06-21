namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class PurchaseHistoryItemModel
{
    public string PurchaseId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string Status { get; set; } = "مكتمل";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int GemsGranted { get; set; }
    public int CoinsGranted { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CreatedAtText => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

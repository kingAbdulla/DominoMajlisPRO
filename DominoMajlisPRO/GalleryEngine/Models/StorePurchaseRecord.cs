namespace DominoMajlisPRO.GalleryEngine.Models;

public enum StoreItemType { Avatar = 0, Background = 1 }
public enum StorePurchaseCurrencyType { Coins = 0, Gems = 1, Free = 2 }

public sealed class StorePurchaseRecord
{
    public string PurchaseId { get; set; } = Guid.NewGuid().ToString();
    public string PlayerId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public StoreItemType ItemType { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public StorePurchaseCurrencyType CurrencyType { get; set; }
    public int PricePaid { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public string SourceSection { get; set; } = string.Empty;
    public bool IsRefunded { get; set; }
    public DateTime? RefundedAt { get; set; }
}

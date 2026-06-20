namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class StorePurchaseResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public StoreItemType ItemType { get; set; }
    public StorePurchaseCurrencyType CurrencyType { get; set; }
    public int PricePaid { get; set; }
    public int NewCoinsBalance { get; set; }
    public int NewGemsBalance { get; set; }
}

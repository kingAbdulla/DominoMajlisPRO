namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public enum StoreCmsCurrency { Gems = 0, Coins = 1 }
public sealed record StoreCmsPricing(int Price, StoreCmsCurrency Currency, bool IsFree);

public static class StoreCmsPricingEngine
{
    public static StoreCmsPricing Normalize(int price, StoreCmsCurrency currency, bool isFree) => new(isFree ? 0 : price, currency, isFree);
    public static StoreCmsValidationResult Validate(StoreCmsValidationResult result, int price, bool isFree, string field = "Price")
    {
        if (price < 0) return result.Add(field, "السعر لا يمكن أن يكون سالباً");
        if (!isFree && price <= 0) return result.Add(field, "سعر العنصر المدفوع يجب أن يكون أكبر من صفر");
        return result;
    }
    public static string Format(int price, StoreCmsCurrency currency, bool isFree) => isFree ? "مجاني" : currency == StoreCmsCurrency.Gems ? $"💎 {price}" : $"🪙 {price}";
}

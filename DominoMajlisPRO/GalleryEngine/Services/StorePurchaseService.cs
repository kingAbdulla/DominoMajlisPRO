using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class StorePurchaseService
{
    private const string FileName = "store_purchases.json";
    private static readonly SemaphoreSlim PurchaseGate = new(1, 1);

    public static async Task<StorePurchaseResult> PurchaseAsync(string playerId, string itemId, StoreItemType itemType)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return Failure("يجب اختيار لاعب قبل الشراء.", playerId, itemId, itemType);
        if (string.IsNullOrWhiteSpace(itemId)) return Failure("معرف العنصر غير صالح.", playerId, itemId, itemType);

        await PurchaseGate.WaitAsync();
        try
        {
            var item = await ResolvePublishedItemAsync(itemId, itemType);
            if (item == null) return Failure("العنصر غير منشور أو غير موجود.", playerId, itemId, itemType);
            if (await PlayerInventoryService.IsOwnedAsync(playerId, itemId, itemType))
                return Failure("العنصر مملوك بالفعل.", playerId, itemId, itemType, item.CurrencyType, item.Price);

            var debit = await PlayerWalletService.TryDebitAsync(playerId, item.CurrencyType, item.Price);
            if (!debit.Success)
                return Failure("الرصيد غير كافٍ.", playerId, itemId, itemType, item.CurrencyType, item.Price, debit.Wallet);

            var purchase = new StorePurchaseRecord
            {
                PlayerId = playerId,
                ItemId = itemId,
                ItemType = itemType,
                ItemTitle = item.Title,
                CurrencyType = item.CurrencyType,
                PricePaid = item.Price,
                SourceSection = item.SourceSection
            };
            // Resolve ApplicationUserId before acquiring the inventory gate.
            var appUserId = (await DominoMajlisPRO.Services.ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;
            var owned = new PlayerOwnedStoreItem
            {
                ApplicationUserId = appUserId,
                PlayerId = playerId,
                ItemId = itemId,
                ItemType = itemType,
                SourcePurchaseId = purchase.PurchaseId,
                IsOwned = true,
                PurchasedAt = DateTime.UtcNow,
                AcquiredAt = DateTime.UtcNow
            };

            var added = await PlayerInventoryService.AddOwnedAsync(owned);

            if (!added) return Failure("العنصر مملوك بالفعل.", playerId, itemId, itemType, item.CurrencyType, item.Price, debit.Wallet);

            var purchases = await LoadAsync();
            purchases.Add(purchase);
            await SaveAsync(purchases);
            // Raise events only after inventory and purchases persisted.
            DominoMajlisPRO.Services.AppEvents.RaiseStoreEconomyChanged(playerId);
            return new StorePurchaseResult
            {
                IsSuccess = true,
                Message = item.Price == 0 ? "تم اقتناء العنصر." : "تم الشراء بنجاح.",
                PlayerId = playerId,
                ItemId = itemId,
                ItemType = itemType,
                CurrencyType = item.CurrencyType,
                PricePaid = item.Price,
                NewCoinsBalance = debit.Wallet.Coins,
                NewGemsBalance = debit.Wallet.Gems
            };
        }
        finally { PurchaseGate.Release(); }
    }

    public static async Task<IReadOnlyList<StorePurchaseRecord>> LoadPlayerPurchasesAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return Array.Empty<StorePurchaseRecord>();
        return (await LoadAsync()).Where(item => item.PlayerId == playerId).OrderByDescending(item => item.PurchasedAt).ToList();
    }

    private static async Task<PublishedStoreItem?> ResolvePublishedItemAsync(string itemId, StoreItemType itemType)
    {
        if (itemType == StoreItemType.Avatar)
        {
            var item = (await AvatarsAdminService.LoadPublishedAsync()).FirstOrDefault(record => record.Id == itemId);
            if (item == null) return null;
            var free = item.IsFree || item.CurrencyType == AvatarCurrencyType.Free;
            return new(item.Id, DisplayTitle(item.NameAr, item.NameEn), free ? StorePurchaseCurrencyType.Free : item.CurrencyType == AvatarCurrencyType.Coins ? StorePurchaseCurrencyType.Coins : StorePurchaseCurrencyType.Gems, free ? 0 : item.Price, "Avatars");
        }

        var background = (await BackgroundsAdminService.LoadPublishedAsync()).FirstOrDefault(record => record.Id == itemId);
        if (background == null) return null;
        var isFree = background.IsFree || background.CurrencyType == BackgroundCurrencyType.Free;
        return new(background.Id, DisplayTitle(background.NameAr, background.NameEn), isFree ? StorePurchaseCurrencyType.Free : background.CurrencyType == BackgroundCurrencyType.Coins ? StorePurchaseCurrencyType.Coins : StorePurchaseCurrencyType.Gems, isFree ? 0 : background.Price, "Backgrounds");
    }

    private static string DisplayTitle(string arabic, string english) => !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
    private static StorePurchaseResult Failure(string message, string playerId, string itemId, StoreItemType itemType, StorePurchaseCurrencyType currency = StorePurchaseCurrencyType.Free, int price = 0, PlayerWalletModel? wallet = null) => new() { Message = message, PlayerId = playerId ?? string.Empty, ItemId = itemId ?? string.Empty, ItemType = itemType, CurrencyType = currency, PricePaid = price, NewCoinsBalance = wallet?.Coins ?? 0, NewGemsBalance = wallet?.Gems ?? 0 };
    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);
    private static Task<List<StorePurchaseRecord>> LoadAsync() => StoreCmsJsonRepository.LoadListAsync<StorePurchaseRecord>(StoragePath);
    private static Task SaveAsync(IReadOnlyList<StorePurchaseRecord> records) => StoreCmsJsonRepository.SaveListAsync(StoragePath, records);
    private sealed record PublishedStoreItem(string Id, string Title, StorePurchaseCurrencyType CurrencyType, int Price, string SourceSection);
}

using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerStoreIdentityService
{
    public static Task<PlayerWalletModel> GetWalletAsync(string playerId) => PlayerWalletService.GetOrCreateAsync(playerId);
    public static Task<StorePurchaseResult> PurchaseAsync(string playerId, string itemId, StoreItemType itemType) => StorePurchaseService.PurchaseAsync(playerId, itemId, itemType);
    public static Task<IReadOnlyList<PlayerOwnedStoreItem>> GetInventoryAsync(string playerId) => PlayerInventoryService.LoadOwnedAsync(playerId);
    public static Task<bool> EquipAsync(string playerId, string itemId, StoreItemType itemType) => PlayerInventoryService.EquipAsync(playerId, itemId, itemType);
    public static Task<PlayerOwnedStoreItem?> GetEquippedAsync(string playerId, StoreItemType itemType) => PlayerInventoryService.GetEquippedAsync(playerId, itemType);
    public static Task<PlayerStoreProgressModel> GetCollectionProgressAsync(string playerId) => PlayerStoreProgressService.CalculateAsync(playerId);

    public static async Task<string?> GetDeviceIdentityPlayerIdAsync()
    {
        var currentUser = await ApplicationUserService.EnsureCurrentSessionAsync();
        return string.IsNullOrWhiteSpace(currentUser.PlayerId) ? null : currentUser.PlayerId.Trim();
    }

    public static async Task<PlayerWalletModel?> GetDeviceWalletAsync()
    {
        var playerId = await GetDeviceIdentityPlayerIdAsync();
        return playerId == null ? null : await GetWalletAsync(playerId);
    }
}

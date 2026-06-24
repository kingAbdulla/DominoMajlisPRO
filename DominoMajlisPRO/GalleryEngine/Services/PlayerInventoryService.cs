using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerInventoryService
{
    private const string FileName = "player_owned_assets.json";
    private const string LegacyFileName = "player_owned_store_items.json";
    private const string UnknownStoreType = "Unknown";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<IReadOnlyList<PlayerOwnedStoreItem>> GetInventoryForPlayerAsync(string playerId)
    {
        ValidatePlayerId(playerId);
        return (await LoadAsync()).Where(x => Same(x.PlayerId, playerId)).OrderByDescending(x => x.PurchasedAt).ToList();
    }

    public static Task<IReadOnlyList<PlayerOwnedStoreItem>> GetInventoryForPlayer(string playerId) => GetInventoryForPlayerAsync(playerId);
    public static async Task<IReadOnlyList<PlayerOwnedStoreItem>> LoadOwnedAsync(string playerId) => (await GetInventoryForPlayerAsync(playerId)).Where(IsActiveOwned).ToList();

    public static async Task<bool> IsOwnedAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        return (await LoadAsync()).Any(x => Same(x.PlayerId, playerId) && Same(x.AssetId, assetId) && IsActiveOwned(x));
    }

    public static Task<bool> IsOwned(string playerId, string assetId) => IsOwnedAsync(playerId, assetId);
    public static Task<bool> IsOwnedAsync(string playerId, string itemId, StoreItemType itemType) { _ = itemType; return IsOwnedAsync(playerId, itemId); }

    public static Task<bool> AddOwnedItemAsync(string playerId, string assetId, string storeTypeId = UnknownStoreType, string source = "Store", DateTime? expireAt = null, string? seasonId = null, string? collectionId = null) =>
        AddOwnedItemCoreAsync(playerId, assetId, storeTypeId, source, expireAt, seasonId, collectionId, true);

    internal static Task<bool> AddOwnedItemWithoutNotificationAsync(string playerId, string assetId, string storeTypeId, string source, DateTime? expireAt = null, string? seasonId = null, string? collectionId = null) =>
        AddOwnedItemCoreAsync(playerId, assetId, storeTypeId, source, expireAt, seasonId, collectionId, false);

    private static async Task<bool> AddOwnedItemCoreAsync(string playerId, string assetId, string storeTypeId, string source, DateTime? expireAt, string? seasonId, string? collectionId, bool raiseEvent)
    {
        ValidateIdentity(playerId, assetId);
        
        // Resolve ApplicationUserId from current session for identity isolation
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;
        
        var added = await AddOwnedAsync(new PlayerOwnedStoreItem
        {
            ApplicationUserId = appUserId,
            PlayerId = playerId,
            AssetId = assetId,
            ItemId = assetId,
            StoreTypeId = NormalizeStoreType(storeTypeId),
            AssetType = NormalizeStoreType(storeTypeId),
            PurchasedAt = DateTime.UtcNow,
            AcquiredAt = DateTime.UtcNow,
            Source = source,
            IsOwned = true,
            ExpireAt = expireAt,
            IsExpired = expireAt.HasValue && expireAt.Value <= DateTime.UtcNow,
            SeasonId = seasonId,
            CollectionId = collectionId
        });
        if (added && raiseEvent) AppEvents.RaiseStoreEconomyChanged(playerId);
        return added;
    }

    public static Task<bool> AddOwnedItem(string playerId, string assetId) => AddOwnedItemAsync(playerId, assetId);

    internal static async Task<bool> AddOwnedAsync(PlayerOwnedStoreItem owned)
    {
        Normalize(owned);
        ValidateIdentity(owned.PlayerId, owned.AssetId);
        
        // Validate ApplicationUserId for identity isolation
        if (owned.IsOwned && string.IsNullOrWhiteSpace(owned.ApplicationUserId))
        {
            throw new InvalidOperationException("ApplicationUserId is required for owned inventory items.");
        }
        
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            if (records.Any(x => Same(x.PlayerId, owned.PlayerId) && Same(x.AssetId, owned.AssetId))) return false;
            records.Add(owned);
            await SaveAsync(records);
            return true;
        }
        finally { Gate.Release(); }
    }

    public static Task<bool> EquipItemAsync(string playerId, string assetId) => EquipCoreAsync(playerId, assetId, null, true);
    internal static Task<bool> EquipItemWithoutNotificationAsync(string playerId, string assetId) => EquipCoreAsync(playerId, assetId, null, false);
    public static Task<bool> EquipItem(string playerId, string assetId) => EquipItemAsync(playerId, assetId);
    public static Task<bool> EquipAsync(string playerId, string itemId, StoreItemType itemType) => EquipCoreAsync(playerId, itemId, itemType.ToString(), true);

    private static async Task<bool> EquipCoreAsync(string playerId, string assetId, string? requestedStoreType, bool raiseEvent)
    {
        ValidateIdentity(playerId, assetId);
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            var target = records.FirstOrDefault(x => Same(x.PlayerId, playerId) && Same(x.AssetId, assetId) && IsActiveOwned(x));
            if (target == null) return false;
            if (target.StoreTypeId == UnknownStoreType && !string.IsNullOrWhiteSpace(requestedStoreType))
            {
                target.StoreTypeId = NormalizeStoreType(requestedStoreType);
                target.AssetType = target.StoreTypeId;
            }
            foreach (var x in records.Where(x => Same(x.PlayerId, playerId) && string.Equals(x.StoreTypeId, target.StoreTypeId, StringComparison.OrdinalIgnoreCase)))
            {
                x.IsEquipped = false;
                x.EquippedAt = null;
            }
            target.IsEquipped = true;
            target.EquippedAt = DateTime.UtcNow;
            await SaveAsync(records);
            if (raiseEvent) AppEvents.RaiseStoreEconomyChanged(playerId);
            return true;
        }
        finally { Gate.Release(); }
    }

    public static async Task<bool> UnequipItemAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            var target = records.FirstOrDefault(x => Same(x.PlayerId, playerId) && Same(x.AssetId, assetId));
            if (target == null || !target.IsEquipped) return false;
            target.IsEquipped = false;
            target.EquippedAt = null;
            await SaveAsync(records);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return true;
        }
        finally { Gate.Release(); }
    }

    public static Task<bool> UnequipItem(string playerId, string assetId) => UnequipItemAsync(playerId, assetId);

    public static async Task<PlayerOwnedStoreItem?> GetEquippedAsync(string playerId, StoreItemType itemType)
    {
        ValidatePlayerId(playerId);
        var storeTypeId = itemType.ToString();
        return (await LoadAsync()).FirstOrDefault(x => Same(x.PlayerId, playerId) && string.Equals(x.StoreTypeId, storeTypeId, StringComparison.OrdinalIgnoreCase) && x.IsEquipped && IsActiveOwned(x));
    }

    private static async Task<List<PlayerOwnedStoreItem>> LoadAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<PlayerOwnedStoreItem>(StoragePath);
        records.AddRange(await StoreCmsJsonRepository.LoadListAsync<PlayerOwnedStoreItem>(LegacyStoragePath));
        foreach (var x in records) Normalize(x);
        var deduped = records.GroupBy(OwnershipKey, StringComparer.OrdinalIgnoreCase).Select(MergeOwnership).ToList();
        foreach (var group in deduped.Where(x => x.IsEquipped).GroupBy(EquippedTypeKey, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var extra in group.OrderByDescending(x => x.EquippedAt).Skip(1))
            {
                extra.IsEquipped = false;
                extra.EquippedAt = null;
            }
        }
        return deduped;
    }

    private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
    {
        var records = group.OrderByDescending(x => x.IsEquipped).ThenByDescending(x => x.PurchasedAt).ToList();
        var merged = records[0];
        
        // Preserve ApplicationUserId from source records for identity isolation
        merged.ApplicationUserId = records.Select(r => r.ApplicationUserId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        
        merged.IsOwned = records.Any(x => x.IsOwned);
        merged.IsEquipped = records.Any(x => x.IsEquipped) && !records.All(x => x.IsExpired);
        merged.EquippedAt = records.Where(x => x.IsEquipped).Select(x => x.EquippedAt).OrderByDescending(x => x).FirstOrDefault();
        merged.ExpireAt = records.Max(x => x.ExpireAt);
        merged.IsExpired = merged.ExpireAt.HasValue && merged.ExpireAt.Value <= DateTime.UtcNow;
        merged.Source = records.Select(x => x.Source).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Store";
        merged.StoreTypeId = records.Select(x => x.StoreTypeId).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x != UnknownStoreType) ?? UnknownStoreType;
        merged.AssetType = merged.StoreTypeId;
        merged.SeasonId ??= records.Select(x => x.SeasonId).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        merged.CollectionId ??= records.Select(x => x.CollectionId).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (merged.IsExpired) { merged.IsEquipped = false; merged.EquippedAt = null; }
        return merged;
    }

    private static bool Normalize(PlayerOwnedStoreItem item)
    {
        var before = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|{item.AssetId}|{item.StoreTypeId}|{item.AssetType}|{item.PurchasedAt:O}|{item.Source}|{item.ItemId}|{item.AcquiredAt:O}|{item.IsExpired}";
        item.InventoryItemId = string.IsNullOrWhiteSpace(item.InventoryItemId) ? Guid.NewGuid().ToString() : item.InventoryItemId.Trim();
        
        // Preserve ApplicationUserId if present for identity isolation
        item.ApplicationUserId = item.ApplicationUserId?.Trim() ?? string.Empty;
        
        item.PlayerId = item.PlayerId?.Trim() ?? string.Empty;
        item.AssetId = !string.IsNullOrWhiteSpace(item.AssetId) ? item.AssetId.Trim() : !string.IsNullOrWhiteSpace(item.ItemId) ? item.ItemId.Trim() : Guid.NewGuid().ToString();
        item.ItemId = item.AssetId;
        item.StoreTypeId = NormalizeStoreType(!string.IsNullOrWhiteSpace(item.StoreTypeId) ? item.StoreTypeId : !string.IsNullOrWhiteSpace(item.AssetType) ? item.AssetType : item.ItemType.ToString());
        item.AssetType = item.StoreTypeId;
        item.PurchasedAt = item.PurchasedAt == default ? item.AcquiredAt == default ? DateTime.UtcNow : item.AcquiredAt : item.PurchasedAt;
        item.AcquiredAt = item.PurchasedAt;
        item.Source = string.IsNullOrWhiteSpace(item.Source) ? string.IsNullOrWhiteSpace(item.SourcePurchaseId) ? "Store" : "Purchase" : item.Source.Trim();
        item.IsExpired = item.IsExpired || item.ExpireAt.HasValue && item.ExpireAt.Value <= DateTime.UtcNow;
        if (item.IsExpired) { item.IsEquipped = false; item.EquippedAt = null; }
        var after = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|{item.AssetId}|{item.StoreTypeId}|{item.AssetType}|{item.PurchasedAt:O}|{item.Source}|{item.ItemId}|{item.AcquiredAt:O}|{item.IsExpired}";
        return !string.Equals(before, after, StringComparison.Ordinal);
    }

    private static bool IsActiveOwned(PlayerOwnedStoreItem item) => item.IsOwned && !item.IsExpired;
    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static string OwnershipKey(PlayerOwnedStoreItem item) => $"{item.PlayerId}|{item.AssetId}";
    private static string EquippedTypeKey(PlayerOwnedStoreItem item) => $"{item.PlayerId}|{item.StoreTypeId}";
    private static string NormalizeStoreType(string? value) => string.IsNullOrWhiteSpace(value) ? UnknownStoreType : value.Trim();
    private static void ValidateIdentity(string playerId, string assetId) { ValidatePlayerId(playerId); if (string.IsNullOrWhiteSpace(assetId)) throw new ArgumentException("AssetId is required.", nameof(assetId)); }
    private static void ValidatePlayerId(string playerId) { if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("PlayerId is required.", nameof(playerId)); }
    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);
    private static string LegacyStoragePath => Path.Combine(FileSystem.AppDataDirectory, LegacyFileName);
    private static Task SaveAsync(IReadOnlyList<PlayerOwnedStoreItem> records) => StoreCmsJsonRepository.SaveListAsync(StoragePath, records);
}

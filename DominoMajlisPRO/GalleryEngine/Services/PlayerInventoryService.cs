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
        var session = await ApplicationUserService.EnsureCurrentSessionAsync();
        var appUserId = session.ApplicationUserId ?? string.Empty;
        return (await LoadAsync())
            .Where(item => SamePlayer(item.PlayerId, playerId) && string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.PurchasedAt)
            .ToList();
    }

    public static Task<IReadOnlyList<PlayerOwnedStoreItem>> GetInventoryForPlayer(string playerId) =>
        GetInventoryForPlayerAsync(playerId);

    public static async Task<IReadOnlyList<PlayerOwnedStoreItem>> LoadOwnedAsync(string playerId) =>
        (await GetInventoryForPlayerAsync(playerId))
            .Where(IsActiveOwned)
            .ToList();

    public static async Task<bool> IsOwnedAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        var session = await ApplicationUserService.EnsureCurrentSessionAsync();
        var appUserId = session.ApplicationUserId ?? string.Empty;
        return (await LoadAsync()).Any(item =>
            string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase) &&
            SamePlayer(item.PlayerId, playerId) && SameAsset(item.AssetId, assetId) && IsActiveOwned(item));
    }

    public static Task<bool> IsOwned(string playerId, string assetId) => IsOwnedAsync(playerId, assetId);

    public static Task<bool> IsOwnedAsync(string playerId, string itemId, StoreItemType itemType)
    {
        _ = itemType;
        return IsOwnedAsync(playerId, itemId);
    }

    public static async Task<bool> AddOwnedItemAsync(
        string playerId,
        string assetId,
        string storeTypeId = UnknownStoreType,
        string source = "Store",
        DateTime? expireAt = null,
        string? seasonId = null,
        string? collectionId = null)
        => await AddOwnedItemCoreAsync(playerId, assetId, storeTypeId, source, expireAt, seasonId, collectionId, true);

    internal static Task<bool> AddOwnedItemWithoutNotificationAsync(
        string playerId,
        string assetId,
        string storeTypeId,
        string source,
        DateTime? expireAt = null,
        string? seasonId = null,
        string? collectionId = null) =>
        AddOwnedItemCoreAsync(playerId, assetId, storeTypeId, source, expireAt, seasonId, collectionId, false);

    private static async Task<bool> AddOwnedItemCoreAsync(
        string playerId,
        string assetId,
        string storeTypeId,
        string source,
        DateTime? expireAt,
        string? seasonId,
        string? collectionId,
        bool raiseEvent)
    {
        ValidateIdentity(playerId, assetId);
        var added = await AddOwnedAsync(new PlayerOwnedStoreItem
        {
            ApplicationUserId = (await DominoMajlisPRO.Services.ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty,
            PlayerId = playerId,
            AssetId = assetId,
            ItemId = assetId,
            StoreTypeId = NormalizeStoreType(storeTypeId),
            PurchasedAt = DateTime.UtcNow,
            AcquiredAt = DateTime.UtcNow,
            Source = source,
            IsOwned = true,
            ExpireAt = expireAt,
            IsExpired = expireAt.HasValue && expireAt.Value <= DateTime.UtcNow,
            SeasonId = seasonId,
            CollectionId = collectionId
        });

        if (added && raiseEvent)
            DominoMajlisPRO.Services.AppEvents.RaiseStoreEconomyChanged(playerId);
        return added;
    }

    public static Task<bool> AddOwnedItem(string playerId, string assetId) =>
        AddOwnedItemAsync(playerId, assetId);

    internal static async Task<bool> AddOwnedAsync(PlayerOwnedStoreItem owned)
    {
        Normalize(owned);
        ValidateIdentity(owned.PlayerId, owned.AssetId);
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            foreach (var item in records)
            {
                if (await SameOwnershipAsync(item, owned.PlayerId, owned.AssetId))
                    return false;
            }
            records.Add(owned);
            await SaveAsync(records);
            return true;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static Task<bool> EquipItemAsync(string playerId, string assetId) =>
        EquipCoreAsync(playerId, assetId, null, true);

    internal static Task<bool> EquipItemWithoutNotificationAsync(string playerId, string assetId) =>
        EquipCoreAsync(playerId, assetId, null, false);

    public static Task<bool> EquipItem(string playerId, string assetId) => EquipItemAsync(playerId, assetId);

    public static Task<bool> EquipAsync(string playerId, string itemId, StoreItemType itemType) =>
        EquipCoreAsync(playerId, itemId, itemType.ToString(), true);

    private static async Task<bool> EquipCoreAsync(string playerId, string assetId, string? requestedStoreType, bool raiseEvent)
    {
        ValidateIdentity(playerId, assetId);
        // Resolve current application user id outside the inventory gate to avoid potential cross-lock deadlocks.
        var appUserId = await GetCurrentApplicationUserId();
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            PlayerOwnedStoreItem? target = null;
            foreach (var item in records)
            {
                if (string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase) &&
                    SamePlayer(item.PlayerId, playerId) &&
                    SameAsset(item.AssetId, assetId) &&
                    IsActiveOwned(item))
                {
                    target = item;
                    break;
                }
            }

            if (target == null)
                return false;

            if (target.StoreTypeId == UnknownStoreType && !string.IsNullOrWhiteSpace(requestedStoreType))
                target.StoreTypeId = NormalizeStoreType(requestedStoreType);

            foreach (var item in records.Where(item =>
                         SamePlayer(item.PlayerId, playerId) &&
                         string.Equals(item.StoreTypeId, target.StoreTypeId, StringComparison.OrdinalIgnoreCase)))
            {
                item.IsEquipped = false;
                item.EquippedAt = null;
            }

            target.IsEquipped = true;
            target.EquippedAt = DateTime.UtcNow;
            await SaveAsync(records);
            if (raiseEvent)
                DominoMajlisPRO.Services.AppEvents.RaiseStoreEconomyChanged(playerId);
            return true;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> UnequipItemAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        var appUserId = await GetCurrentApplicationUserId();
        await Gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            PlayerOwnedStoreItem? target = null;
            foreach (var item in records)
            {
                if (string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase) &&
                    SamePlayer(item.PlayerId, playerId) &&
                    SameAsset(item.AssetId, assetId))
                {
                    target = item;
                    break;
                }
            }

            if (target == null || !target.IsEquipped)
                return false;
            target.IsEquipped = false;
            target.EquippedAt = null;
            await SaveAsync(records);
            DominoMajlisPRO.Services.AppEvents.RaiseStoreEconomyChanged(playerId);
            return true;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static Task<bool> UnequipItem(string playerId, string assetId) => UnequipItemAsync(playerId, assetId);

    public static async Task<PlayerOwnedStoreItem?> GetEquippedAsync(string playerId, StoreItemType itemType)
    {
        ValidatePlayerId(playerId);
        var storeTypeId = itemType.ToString();
        return (await LoadAsync()).FirstOrDefault(item =>
            SamePlayer(item.PlayerId, playerId) &&
            string.Equals(item.StoreTypeId, storeTypeId, StringComparison.OrdinalIgnoreCase) &&
            item.IsEquipped && IsActiveOwned(item));
    }

    private static async Task<List<PlayerOwnedStoreItem>> LoadAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<PlayerOwnedStoreItem>(StoragePath);
        var legacyRecords =
            await StoreCmsJsonRepository.LoadListAsync<PlayerOwnedStoreItem>(LegacyStoragePath);
        records.AddRange(legacyRecords);
        var changed = false;
        foreach (var record in records)
            changed |= Normalize(record);

        var deduplicated = records
            .GroupBy(OwnershipKey, StringComparer.OrdinalIgnoreCase)
            .Select(MergeOwnership)
            .ToList();
        changed |= deduplicated.Count != records.Count;
        foreach (var equippedGroup in deduplicated
                     .Where(item => item.IsEquipped)
                     .GroupBy(EquippedTypeKey, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var extra in equippedGroup.OrderByDescending(item => item.EquippedAt).Skip(1))
            {
                extra.IsEquipped = false;
                extra.EquippedAt = null;
                changed = true;
            }
        }

        // Loading is intentionally read-only. Normalized records are persisted
        // by the next explicit inventory mutation, avoiding write races while
        // profile and inventory pages are rendering.
        _ = changed;
        return deduplicated;
    }

    private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
    {
        var records = group.OrderByDescending(item => item.IsEquipped).ThenByDescending(item => item.PurchasedAt).ToList();
        var merged = records[0];
        merged.ApplicationUserId = records.Select(r => r.ApplicationUserId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        merged.IsOwned = records.Any(item => item.IsOwned);
        merged.IsEquipped = records.Any(item => item.IsEquipped) && !records.All(item => item.IsExpired);
        merged.EquippedAt = records.Where(item => item.IsEquipped).Select(item => item.EquippedAt).OrderByDescending(value => value).FirstOrDefault();
        merged.ExpireAt = records.Max(item => item.ExpireAt);
        merged.IsExpired = merged.ExpireAt.HasValue && merged.ExpireAt.Value <= DateTime.UtcNow;
        merged.Source = records.Select(item => item.Source).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "Store";
        merged.StoreTypeId = records.Select(item => item.StoreTypeId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value != UnknownStoreType) ?? UnknownStoreType;
        merged.SeasonId ??= records.Select(item => item.SeasonId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        merged.CollectionId ??= records.Select(item => item.CollectionId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (merged.IsExpired)
        {
            merged.IsEquipped = false;
            merged.EquippedAt = null;
        }
        return merged;
    }

    private static bool Normalize(PlayerOwnedStoreItem item)
    {
        var before = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|{item.AssetId}|{item.StoreTypeId}|{item.AssetType}|{item.PurchasedAt:O}|{item.Source}|{item.ItemId}|{item.AcquiredAt:O}|{item.IsExpired}";
        item.InventoryItemId = string.IsNullOrWhiteSpace(item.InventoryItemId) ? Guid.NewGuid().ToString() : item.InventoryItemId.Trim();
        item.ApplicationUserId = item.ApplicationUserId?.Trim() ?? string.Empty;
        item.PlayerId = item.PlayerId?.Trim() ?? string.Empty;
        item.AssetId = !string.IsNullOrWhiteSpace(item.AssetId)
            ? item.AssetId.Trim()
            : !string.IsNullOrWhiteSpace(item.ItemId)
                ? item.ItemId.Trim()
                : Guid.NewGuid().ToString();
        item.ItemId = item.AssetId;
        item.StoreTypeId = NormalizeStoreType(
            !string.IsNullOrWhiteSpace(item.StoreTypeId)
                ? item.StoreTypeId
                : !string.IsNullOrWhiteSpace(item.AssetType)
                    ? item.AssetType
                    : item.ItemType.ToString());
        item.AssetType = item.StoreTypeId;
        item.PurchasedAt = item.PurchasedAt == default
            ? item.AcquiredAt == default ? DateTime.UtcNow : item.AcquiredAt
            : item.PurchasedAt;
        item.AcquiredAt = item.PurchasedAt;
        item.Source = string.IsNullOrWhiteSpace(item.Source)
            ? string.IsNullOrWhiteSpace(item.SourcePurchaseId) ? "Store" : "Purchase"
            : item.Source.Trim();
        item.IsExpired = item.IsExpired || item.ExpireAt.HasValue && item.ExpireAt.Value <= DateTime.UtcNow;
        if (item.IsExpired)
        {
            item.IsEquipped = false;
            item.EquippedAt = null;
        }
        var after = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|{item.AssetId}|{item.StoreTypeId}|{item.AssetType}|{item.PurchasedAt:O}|{item.Source}|{item.ItemId}|{item.AcquiredAt:O}|{item.IsExpired}";
        return !string.Equals(before, after, StringComparison.Ordinal);
    }

    private static bool IsActiveOwned(PlayerOwnedStoreItem item) => item.IsOwned && !item.IsExpired;

    private static async Task<bool> SameOwnershipAsync(PlayerOwnedStoreItem item, string playerId, string assetId)
    {
        try
        {
            var session = await ApplicationUserService.EnsureCurrentSessionAsync();
            var appUserId = session?.ApplicationUserId ?? string.Empty;
            return string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase) &&
                   SamePlayer(item.PlayerId, playerId) && SameAsset(item.AssetId, assetId);
        }
        catch
        {
            return false;
        }
    }

    private static bool SamePlayer(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static bool SameAsset(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static string OwnershipKey(PlayerOwnedStoreItem item) => $"{item.ApplicationUserId}\u001F{item.PlayerId}\u001F{item.AssetId}";
    private static string EquippedTypeKey(PlayerOwnedStoreItem item) => $"{item.ApplicationUserId}\u001F{item.PlayerId}\u001F{item.StoreTypeId}";
    private static string NormalizeStoreType(string? value) => string.IsNullOrWhiteSpace(value) ? UnknownStoreType : value.Trim();

    private static void ValidateIdentity(string playerId, string assetId)
    {
        ValidatePlayerId(playerId);
        if (string.IsNullOrWhiteSpace(assetId))
            throw new ArgumentException("AssetId is required.", nameof(assetId));
    }

    private static async Task<string> GetCurrentApplicationUserId()
    {
        try
        {
            var session = await ApplicationUserService.EnsureCurrentSessionAsync();
            return session.ApplicationUserId ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void ValidatePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("PlayerId is required.", nameof(playerId));
    }

    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);
    private static string LegacyStoragePath =>
        Path.Combine(FileSystem.AppDataDirectory, LegacyFileName);
    private static Task SaveAsync(IReadOnlyList<PlayerOwnedStoreItem> records) =>
        StoreCmsJsonRepository.SaveListAsync(StoragePath, records);
}

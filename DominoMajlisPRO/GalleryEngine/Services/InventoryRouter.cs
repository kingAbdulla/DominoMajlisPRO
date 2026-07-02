using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public enum InventoryOwnerScope
{
    Player,
    Team,
    Unsupported
}

public enum InventoryEquipTarget
{
    None,
    Avatar,
    ProfileBackground,
    Frame,
    Effect,
    Title,
    Emblem,
    TeamColor,
    EmblemBackground,
    TeamEffect,
    PlayerNameEffect,
    PlayerNameFrame,
    TeamNameEffect,
    TeamNameFrame
}

public sealed record InventoryProductContext(
    string ProductId,
    string AssetId,
    string StoreTypeId,
    int? Price,
    bool? IsFree,
    string? CurrencyMetadata,
    string? DisplayPrice,
    string? SeasonId = null,
    string? CollectionId = null,
    DateTime? AvailableFrom = null,
    DateTime? AvailableUntil = null);

public sealed record InventoryRoute(
    InventoryOwnerScope OwnerScope,
    string StoreTypeId,
    InventoryEquipTarget EquipTarget,
    bool Equipable);

public sealed record InventoryState(
    InventoryRoute Route,
    bool IsFree,
    bool IsOwned,
    bool IsEquipped,
    bool RequiresIdentity,
    bool RequiresPlayer,
    bool RequiresTeam,
    string? PlayerId,
    string? TeamId,
    bool IsAvailable = true,
    string? AvailabilityMessage = null);

public sealed record InventoryActionResult(
    InventoryState State,
    bool WasAdded,
    bool Changed,
    bool PaidActionRequired);

public static class InventoryRouter
{
    public static bool IsAvailable(InventoryProductContext product)
    {
        ArgumentNullException.ThrowIfNull(product);
        var now = DateTime.Now;
        return (!product.AvailableFrom.HasValue ||
                product.AvailableFrom.Value <= now) &&
               (!product.AvailableUntil.HasValue ||
                product.AvailableUntil.Value >= now);
    }

    public static string? GetAvailabilityMessage(
        InventoryProductContext product)
    {
        ArgumentNullException.ThrowIfNull(product);
        var now = DateTime.Now;
        if (product.AvailableFrom.HasValue &&
            product.AvailableFrom.Value > now)
        {
            return "هذا العرض لم يبدأ بعد.";
        }
        if (product.AvailableUntil.HasValue &&
            product.AvailableUntil.Value < now)
        {
            return "انتهت صلاحية هذا العرض.";
        }
        return null;
    }

    public static bool IsFree(InventoryProductContext product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return product.Price == 0 ||
               product.IsFree == true ||
               IsFreeToken(product.CurrencyMetadata) ||
               IsFreeToken(product.DisplayPrice);
    }

    public static InventoryRoute Resolve(InventoryProductContext product)
    {
        ValidateProduct(product);
        if (!StoreProductAssetTypeCatalog.TryResolve(product.StoreTypeId, out var assetType))
        {
            return new InventoryRoute(
                InventoryOwnerScope.Unsupported,
                product.StoreTypeId.Trim(),
                InventoryEquipTarget.None,
                false);
        }

        var canonicalTypeId = assetType.ToString();
        if (assetType == StoreProductAssetType.Emblem)
            return TeamRoute(canonicalTypeId, InventoryEquipTarget.Emblem);
        if (assetType == StoreProductAssetType.TeamColor)
            return TeamRoute(canonicalTypeId, InventoryEquipTarget.TeamColor);
        if (assetType == StoreProductAssetType.EmblemBackground)
            return TeamRoute(canonicalTypeId, InventoryEquipTarget.EmblemBackground);
        if (assetType == StoreProductAssetType.TeamNameEffect)
            return TeamRoute(canonicalTypeId, InventoryEquipTarget.TeamNameEffect);
        if (assetType == StoreProductAssetType.TeamNameFrame)
            return TeamRoute(canonicalTypeId, InventoryEquipTarget.TeamNameFrame);
        if (assetType == StoreProductAssetType.Avatar)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.Avatar, true);
        if (assetType == StoreProductAssetType.ProfileBackground)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.ProfileBackground, true);
        if (assetType == StoreProductAssetType.Frame)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.Frame, true);
        if (assetType == StoreProductAssetType.Effect)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.Effect, true);
        if (assetType == StoreProductAssetType.PlayerNameEffect)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.PlayerNameEffect, true);
        if (assetType == StoreProductAssetType.PlayerNameFrame)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.PlayerNameFrame, true);
        if (assetType == StoreProductAssetType.TeamEffect)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.TeamEffect, false);
        if (assetType == StoreProductAssetType.Title)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.Title, true);
        if (assetType is StoreProductAssetType.Badge or StoreProductAssetType.SeasonReward)
            return PlayerRoute(canonicalTypeId, InventoryEquipTarget.None, false);

        return new InventoryRoute(
            InventoryOwnerScope.Unsupported,
            canonicalTypeId,
            InventoryEquipTarget.None,
            false);
    }

    public static async Task<InventoryState> GetStateAsync(
        InventoryProductContext product)
    {
        var route = Resolve(product);
        var free = IsFree(product);
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (owner.IsGhost)
            return EmptyState(route, free, requiresIdentity: true);
        if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
            return EmptyState(route, free, requiresPlayer: true);

        if (route.OwnerScope == InventoryOwnerScope.Team)
        {
            var teamId = await ResolveActiveTeamIdAsync(owner.PlayerId);
            if (string.IsNullOrWhiteSpace(teamId))
                return new InventoryState(
                    route, free, false, false,
                    false, false, true, owner.PlayerId, null,
                    IsAvailable(product),
                    GetAvailabilityMessage(product));

            var owned = await TeamAssetInventoryService.GetEquippedAsync(
                teamId,
                route.StoreTypeId);
            var isOwned = await TeamAssetInventoryService.IsOwnedAsync(
                teamId,
                product.AssetId,
                route.StoreTypeId);
            return new InventoryState(
                route, free, isOwned, SameId(owned?.TeamAssetId, product.AssetId),
                false, false, false, owner.PlayerId, teamId,
                IsAvailable(product),
                GetAvailabilityMessage(product));
        }

        if (route.OwnerScope == InventoryOwnerScope.Player)
        {
            var inventory = await PlayerInventoryService.GetInventoryForPlayerAsync(owner.PlayerId);
            var owned = inventory.FirstOrDefault(item =>
                item.IsOwned && !item.IsExpired &&
                SameId(item.AssetId, product.AssetId));
            return new InventoryState(
                route, free, owned != null, owned?.IsEquipped == true,
                false, false, false, owner.PlayerId, null,
                IsAvailable(product),
                GetAvailabilityMessage(product));
        }

        return new InventoryState(
            route, free, false, false, false, false, false,
            owner.PlayerId, null);
    }

    public static async Task<InventoryActionResult> AcquireOrEquipAsync(
        InventoryProductContext product)
    {
        var before = await GetStateAsync(product);
        if (before.RequiresIdentity || before.RequiresPlayer ||
            before.RequiresTeam ||
            before.Route.OwnerScope == InventoryOwnerScope.Unsupported)
        {
            return new InventoryActionResult(before, false, false, false);
        }

        if (!before.IsAvailable && !before.IsOwned)
            return new InventoryActionResult(before, false, false, false);

        if (before.IsEquipped)
            return new InventoryActionResult(before, false, false, false);

        if (!before.IsOwned && !before.IsFree)
            return new InventoryActionResult(before, false, false, true);

        var wasAdded = false;
        var changed = false;
        if (before.Route.OwnerScope == InventoryOwnerScope.Team)
        {
            if (!before.IsOwned)
            {
                wasAdded = await TeamAssetInventoryService.AddOwnedAssetAsync(
                    before.TeamId!,
                    product.AssetId,
                    before.Route.StoreTypeId,
                    "FreeAcquire",
                    seasonId: product.SeasonId,
                    collectionId: product.CollectionId);
            }

            var acquired = before.IsOwned || wasAdded ||
                await TeamAssetInventoryService.IsOwnedAsync(
                    before.TeamId!,
                    product.AssetId,
                    before.Route.StoreTypeId);

            changed = acquired && await TeamAssetInventoryService.EquipAsync(
                before.TeamId!,
                product.AssetId,
                before.Route.StoreTypeId);

            if (acquired)
            {
                AppEvents.RaiseStoreEconomyChanged(before.PlayerId!);
                AppEvents.RaiseTeamIdentityChanged(before.TeamId!);
            }
        }
        else
        {
            if (!before.IsOwned)
            {
                wasAdded = await PlayerInventoryService.AddOwnedItemWithoutNotificationAsync(
                    before.PlayerId!,
                    product.AssetId,
                    before.Route.StoreTypeId,
                    "FreeAcquire",
                    seasonId: product.SeasonId,
                    collectionId: product.CollectionId);
            }

            var acquired = before.IsOwned || wasAdded ||
                await PlayerInventoryService.IsOwnedAsync(
                    before.PlayerId!,
                    product.AssetId);
            if (acquired && before.Route.Equipable)
            {
                changed = await EquipPlayerAssetAsync(
                    before.PlayerId!,
                    product.AssetId,
                    before.Route);
            }

            if (!changed)
                AppEvents.RaiseStoreEconomyChanged(before.PlayerId!);
        }

        var after = await GetStateAsync(product);
        return new InventoryActionResult(
            after,
            wasAdded,
            wasAdded || changed,
            false);
    }

    private static async Task<bool> EquipPlayerAssetAsync(
        string playerId,
        string assetId,
        InventoryRoute route)
    {
        if (route.EquipTarget is InventoryEquipTarget.Avatar or
            InventoryEquipTarget.ProfileBackground or
            InventoryEquipTarget.Frame or
            InventoryEquipTarget.Effect or
            InventoryEquipTarget.PlayerNameEffect or
            InventoryEquipTarget.PlayerNameFrame)
        {
            return await StoreEquipService.EquipAsync(playerId, assetId);
        }

        return await PlayerInventoryService.EquipItemAsync(playerId, assetId);
    }

    private static InventoryState EmptyState(
        InventoryRoute route,
        bool free,
        bool requiresIdentity = false,
        bool requiresPlayer = false) =>
        new(
            route, free, false, false,
            requiresIdentity, requiresPlayer, false,
            null, null);

    private static InventoryRoute PlayerRoute(
        string typeId,
        InventoryEquipTarget target,
        bool equipable) =>
        new(InventoryOwnerScope.Player, typeId, target, equipable);

    private static InventoryRoute TeamRoute(
        string typeId,
        InventoryEquipTarget target) =>
        new(InventoryOwnerScope.Team, typeId, target, true);

    private static async Task<string?> ResolveActiveTeamIdAsync(string playerId)
    {
        var player = await PlayerProfileService.GetPlayerByIdAsync(playerId);
        var currentTeamIds = (player?.CurrentTeamIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        if (currentTeamIds.Count == 1)
            return currentTeamIds[0];
        if (currentTeamIds.Count > 1)
            return null;

        var team = await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
        return string.IsNullOrWhiteSpace(team?.TeamId) ? null : team.TeamId.Trim();
    }

    private static bool IsFreeToken(string? value)
    {
        var token = value?.Trim();
        return string.Equals(token, "Free", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(token, "مجاني", StringComparison.Ordinal);
    }

    private static bool SameId(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void ValidateProduct(InventoryProductContext product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (string.IsNullOrWhiteSpace(product.ProductId))
            throw new ArgumentException("ProductId is required.", nameof(product));
        if (string.IsNullOrWhiteSpace(product.AssetId))
            throw new ArgumentException("AssetId is required.", nameof(product));
        if (string.IsNullOrWhiteSpace(product.StoreTypeId))
            throw new ArgumentException("StoreTypeId is required.", nameof(product));
    }
}

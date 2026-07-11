using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class InventoryDisplayResolver
{
    public const string FallbackImage = "ss.png";

    public static async Task<InventoryCollectionSnapshot> ResolveAsync(
        string? playerId,
        string? teamId = null)
    {
        var catalogTask = StoreAssetCatalogService.LoadAsync();
        var productReferencesTask =
            StoreAssetCatalogService.LoadProductReferencesAsync();
        var session = await DominoMajlisPRO.Services.ApplicationUserService.EnsureCurrentSessionAsync();
                var appUserId = session.ApplicationUserId ?? string.Empty;
                var playerInventoryTask = string.IsNullOrWhiteSpace(playerId)
                    ? Task.FromResult<IReadOnlyList<PlayerOwnedStoreItem>>( 
                        Array.Empty<PlayerOwnedStoreItem>())
                    : PlayerAssetInventoryService.GetInventoryForPlayerAsync(playerId); // PlayerAssetInventoryService will scope by ApplicationUserId internally.
                var teamInventoryTask = string.IsNullOrWhiteSpace(teamId)
                    ? Task.FromResult<IReadOnlyList<TeamOwnedAssetItem>>(
                        Array.Empty<TeamOwnedAssetItem>())
                    : TeamAssetInventoryService.GetInventoryForTeamAsync(teamId);
        await Task.WhenAll(
            catalogTask,
            productReferencesTask,
            playerInventoryTask,
            teamInventoryTask);

        var catalog = catalogTask.Result;
        var productReferences = productReferencesTask.Result;
        var items = new List<ResolvedInventoryDisplay>();
        items.AddRange(playerInventoryTask.Result
            .Where(item => item.IsOwned && !item.IsExpired)
            .GroupBy(
                item => $"{StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId)}\u001F{item.AssetId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.IsEquipped)
                .ThenByDescending(item => item.PurchasedAt)
                .First())
            .Select(item => ResolvePlayer(
                item,
                catalog,
                productReferences)));
        items.AddRange(teamInventoryTask.Result
            .Where(item => item.IsOwned)
            .GroupBy(
                item => $"{StoreAssetCatalogService.CanonicalTypeId(item.TeamAssetTypeId)}\u001F{item.TeamAssetId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.IsEquipped)
                .ThenByDescending(item => item.AcquiredAt)
                .First())
            .Select(item => ResolveTeam(
                item,
                catalog,
                productReferences)));

        items = items
            .Where(item => item.HasCatalogDisplayMetadata)
            .ToList();

        var supportedCatalog = catalog
            .Where(item =>
                IsProgressType(item.AssetType) &&
                !TeamAssetPayloadCatalog.IsDefaultTeamAsset(item.AssetId) &&
                (item.OwnerScope == StoreProductOwnerScope.Player ||
                 item.ProductIds.Count > 0))
            .ToList();
        var typeIds = supportedCatalog
            .Select(item => item.AssetType.ToString())
            .Concat(items.Select(item => item.AssetType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => TypeOrder(item))
            .ToList();
        var counts = typeIds.Select(typeId =>
        {
            var catalogIds = supportedCatalog
                .Where(item => Same(item.AssetType.ToString(), typeId))
                .Select(item => item.AssetId);
            var ownedIds = items
                .Where(item => Same(item.AssetType, typeId))
                .Select(item => item.AssetId);
            var availableIds = catalogIds
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var owned = ownedIds
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(item => availableIds.Contains(item));
            var total = availableIds.Count;
            owned = Math.Min(owned, total);
            return new StoreProgressCount
            {
                Key = typeId,
                Owned = owned,
                Total = total
            };
        }).ToList();

        var missing = items
            .Where(item => !item.HasCatalogDisplayMetadata)
            .SelectMany(item =>
            {
                var productIds = string.IsNullOrWhiteSpace(item.ProductId)
                    ? new[] { string.Empty }
                    : item.ProductId.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);
                return productIds.Select(productId =>
                    new MissingCatalogDisplayMetadata(
                        productId,
                        item.AssetId,
                        item.AssetType));
            })
            .Distinct()
            .ToList();
        var totalOwned = counts.Sum(item => item.Owned);
        var totalAvailable = counts.Sum(item => item.Total);
        return new InventoryCollectionSnapshot(
            items
                .OrderBy(item => TypeOrder(item.AssetType))
                .ThenByDescending(item => item.IsEquipped)
                .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            counts,
            totalOwned,
            totalAvailable,
            totalAvailable == 0 ? 0 : totalOwned * 100d / totalAvailable,
            missing);
    }

    private static ResolvedInventoryDisplay ResolvePlayer(
        PlayerOwnedStoreItem item,
        IReadOnlyList<CatalogAssetDisplay> catalog,
        IReadOnlyList<StoreProductAssetReference> productReferences)
    {
        var typeId = StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId);
        if (Same(item.Source, "Default") &&
            Same(typeId, StoreProductAssetType.Avatar.ToString()) &&
            AvatarService.GetById(item.AssetId) is { } defaultAvatar)
        {
            return new ResolvedInventoryDisplay(
                string.Empty,
                item.AssetId,
                typeId,
                defaultAvatar.DisplayName,
                string.Empty,
                ResolveImagePath(defaultAvatar.Image, "player_card.png"),
                string.Empty,
                true,
                item.IsEquipped,
                false,
                true);
        }

        var asset = StoreAssetCatalogService.Resolve(catalog, item.AssetId, typeId);
        return Resolve(
            item.AssetId,
            typeId,
            item.IsEquipped,
            false,
            asset,
            productReferences);
    }

    private static ResolvedInventoryDisplay ResolveTeam(
        TeamOwnedAssetItem item,
        IReadOnlyList<CatalogAssetDisplay> catalog,
        IReadOnlyList<StoreProductAssetReference> productReferences)
    {
        var typeId = StoreAssetCatalogService.CanonicalTypeId(
            ResolveTeamTypeId(item, catalog));
        var asset = StoreAssetCatalogService.Resolve(
            catalog,
            item.TeamAssetId,
            typeId);
        return Resolve(
            item.TeamAssetId,
            typeId,
            item.IsEquipped,
            true,
            asset,
            productReferences);
    }

    private static string ResolveTeamTypeId(
        TeamOwnedAssetItem item,
        IReadOnlyList<CatalogAssetDisplay> catalog)
    {
        var canonical = StoreAssetCatalogService.CanonicalType(
            item.TeamAssetTypeId);
        if (canonical.HasValue)
            return canonical.Value.ToString();

        var catalogMatches = catalog
            .Where(asset => Same(asset.AssetId, item.TeamAssetId))
            .Select(asset => asset.AssetType)
            .Distinct()
            .ToList();
        if (catalogMatches.Count == 1)
            return catalogMatches[0].ToString();

        var payload = TeamAssetPayloadCatalog.Resolve(item.TeamAssetId);
        return payload?.TeamAssetTypeId ?? item.TeamAssetTypeId;
    }

    private static ResolvedInventoryDisplay Resolve(
        string assetId,
        string typeId,
        bool isEquipped,
        bool isTeamAsset,
        CatalogAssetDisplay? asset,
        IReadOnlyList<StoreProductAssetReference> productReferences)
    {
        var hasMetadata = asset?.HasDisplayMetadata == true;
        var productIds = asset?.ProductIds ??
            productReferences
                .Where(item =>
                    Same(item.AssetId, assetId) &&
                    Same(item.AssetType, typeId))
                .Select(item => item.ProductId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        return new ResolvedInventoryDisplay(
            string.Join(",", productIds),
            assetId,
            typeId,
            hasMetadata
                ? asset!.DisplayName
                : StoreAssetCatalogService.IncompleteDisplayName,
            hasMetadata ? asset!.ArabicDisplayName : string.Empty,
            hasMetadata
                ? ResolveImagePath(asset!.PreviewImage)
                : FallbackImage,
            asset?.ColorHex ?? string.Empty,
            true,
            isEquipped,
            isTeamAsset,
            hasMetadata);
    }

    private static bool IsProgressType(
        StoreProductAssetType type) =>
        type is
            StoreProductAssetType.Avatar or
            StoreProductAssetType.ProfileBackground or
            StoreProductAssetType.Frame or
            StoreProductAssetType.Effect or
            StoreProductAssetType.Title or
            StoreProductAssetType.Emblem or
            StoreProductAssetType.TeamColor or
            StoreProductAssetType.EmblemBackground or
            StoreProductAssetType.Badge or
            StoreProductAssetType.SeasonReward;

    private static int TypeOrder(string typeId) => typeId switch
    {
        "Avatar" => 0,
        "ProfileBackground" => 1,
        "Frame" => 2,
        "Effect" => 3,
        "Title" => 4,
        "Emblem" => 5,
        "TeamColor" => 6,
        "EmblemBackground" => 7,
        "Badge" => 8,
        "SeasonReward" => 9,
        _ => 100
    };

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static string ResolveImagePath(
        string? imagePath,
        string fallback = FallbackImage)
    {
        var resolved = ResolveOptionalImagePath(imagePath);
        return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
    }

    public static string? ResolveOptionalImagePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        var candidate = imagePath.Trim();
        try
        {
            if (File.Exists(candidate))
                return candidate;
        }
        catch
        {
            return null;
        }

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return candidate;
        }

        const string resourcePrefix = "Resources/Images/";
        var normalized = candidate.Replace('\\', '/');
        if (normalized.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase))
            normalized = Path.GetFileName(normalized);

        if (Path.IsPathRooted(candidate) ||
            normalized.Contains('/') ||
            normalized.Contains(':'))
        {
            return null;
        }

        return HasSupportedImageExtension(normalized)
            ? normalized
            : null;
    }

    public static ImageSource ResolveImageSource(
        string? imagePath,
        string fallback = FallbackImage)
    {
        var resolved = ResolveImagePath(imagePath, fallback);
        return File.Exists(resolved)
            ? ImageSource.FromStream(() => File.OpenRead(resolved))
            : resolved;
    }

    public static ImageSource? ResolveOptionalImageSource(string? imagePath)
    {
        var resolved = ResolveOptionalImagePath(imagePath);
        if (string.IsNullOrWhiteSpace(resolved))
            return null;

        return File.Exists(resolved)
            ? ImageSource.FromStream(() => File.OpenRead(resolved))
            : resolved;
    }

    private static bool HasSupportedImageExtension(string value)
    {
        var extension = Path.GetExtension(value);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }
}

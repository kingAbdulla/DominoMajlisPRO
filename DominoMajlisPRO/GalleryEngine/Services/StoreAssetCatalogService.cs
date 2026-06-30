using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class StoreAssetCatalogService
{
    public const string LivingProductionDefaultEmblemAssetId = "team-emblem-living-production-default";
    private const string LegacyLivingFilamentBackendProbeAssetId = "team-emblem-living-filament-backend-probe";

    public const string IncompleteDisplayName = "عنصر غير مكتمل البيانات";

    public static async Task<IReadOnlyList<CatalogAssetDisplay>> LoadAsync()
    {
        var avatarsTask = AvatarsAdminService.LoadPublishedAsync();
        var backgroundsTask = BackgroundsAdminService.LoadPublishedAsync();
        var arrivalsTask = NewArrivalsAdminService.LoadPublishedAsync();
        var offersTask = LimitedOffersAdminService.LoadPublishedAsync();
        await Task.WhenAll(avatarsTask, backgroundsTask, arrivalsTask, offersTask);

        var productIds = arrivalsTask.Result
            .Select(item => new ProductLink(item.ProductId, item.AssetId, item.StoreTypeId))
            .Concat(offersTask.Result.Select(item => new ProductLink(item.ProductId, item.AssetId, item.StoreTypeId)))
            .Where(item => !string.IsNullOrWhiteSpace(item.AssetId))
            .ToList();

        var assets = new List<CatalogAssetDisplay>();
        foreach (var item in arrivalsTask.Result)
        {
            if (!StoreProductAssetTypeCatalog.TryResolve(item.StoreTypeId, out var type) ||
                !StoreProductAssetTypeCatalog.IsInventory(type) ||
                type is StoreProductAssetType.Avatar or StoreProductAssetType.ProfileBackground)
            {
                continue;
            }

            assets.Add(Create(
                item.AssetId,
                type,
                ResolveOwnerScope(type, item.OwnerScope),
                item.Title,
                item.Title,
                item.ImagePath,
                item.ColorHex,
                productIds,
                item.EffectType,
                item.AnimationType,
                item.DurationMilliseconds,
                item.EquipTarget,
                item.PrimaryColorPresetId,
                item.SecondaryColorPresetId,
                item.CustomPrimaryColorHex,
                item.CustomSecondaryColorHex,
                item.EffectLayerIds,
                item.EffectOpacity,
                item.EffectScale,
                item.EffectSpeed,
                item.EffectIntensity,
                item.TypographyPreset,
                item.LivingVisualScope,
                item.LivingVisualKind,
                item.LivingPackageId,
                item.LivingPackageManifestPath,
                item.LivingPackagePath,
                item.PreferredBackend,
                item.FallbackPolicy,
                item.LivingVisualVersion,
                item.LivingPackageVersion,
                item.Rarity));
        }

        assets.AddRange(avatarsTask.Result.Select(item => Create(
            item.Id,
            StoreProductAssetType.Avatar,
            StoreProductOwnerScope.Player,
            DisplayName(item.NameAr, item.NameEn),
            item.NameAr,
            PreferredImage(item.ThumbnailPath, item.ImagePath),
            string.Empty,
            productIds)));
        assets.AddRange(backgroundsTask.Result.Select(item => Create(
            item.Id,
            StoreProductAssetType.ProfileBackground,
            StoreProductOwnerScope.Player,
            DisplayName(item.NameAr, item.NameEn),
            item.NameAr,
            PreferredImage(item.ThumbnailPath, item.ImagePath),
            string.Empty,
            productIds)));
        assets.AddRange(TeamAssetPayloadCatalog.GetAllPayloads().Select(item => Create(
            item.TeamAssetId,
            TeamType(item.TeamAssetTypeId),
            StoreProductOwnerScope.Team,
            DisplayName(item.ArabicDisplayName, item.EnglishDisplayName),
            item.ArabicDisplayName ?? string.Empty,
            PreferredImage(item.ImagePath, item.BackgroundImagePath),
            item.ColorHex ?? item.BackgroundColorHex ?? string.Empty,
            productIds)));
        return assets
            .Where(item => !string.IsNullOrWhiteSpace(item.AssetId))
            .GroupBy(item => $"{item.AssetType}\u001F{item.AssetId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.AssetType)
            .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static async Task<IReadOnlyList<StoreProductAssetReference>> LoadProductReferencesAsync()
    {
        var arrivalsTask = NewArrivalsAdminService.LoadPublishedAsync();
        var offersTask = LimitedOffersAdminService.LoadPublishedAsync();
        await Task.WhenAll(arrivalsTask, offersTask);
        return arrivalsTask.Result
            .Select(item => new StoreProductAssetReference(item.ProductId, item.AssetId, CanonicalTypeId(item.StoreTypeId)))
            .Concat(offersTask.Result.Select(item => new StoreProductAssetReference(item.ProductId, item.AssetId, CanonicalTypeId(item.StoreTypeId))))
            .Where(item => !string.IsNullOrWhiteSpace(item.ProductId) && !string.IsNullOrWhiteSpace(item.AssetId))
            .Distinct()
            .ToList();
    }

    public static async Task<CatalogAssetDisplay?> ResolveAsync(string? assetId, string? assetType)
    {
        var catalog = await LoadAsync();
        return Resolve(catalog, assetId, assetType);
    }

    public static CatalogAssetDisplay? Resolve(IReadOnlyList<CatalogAssetDisplay> catalog, string? assetId, string? assetType)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return null;

        var canonicalType = CanonicalType(assetType);
        var matches = catalog
            .Where(item => Same(item.AssetId, assetId) && (canonicalType == null || item.AssetType == canonicalType))
            .ToList();
        return matches.Count > 0 ? matches[0] : null;
    }

    public static bool IsLivingEmblemAsset(CatalogAssetDisplay? asset) =>
        (asset?.AssetType == StoreProductAssetType.TeamLivingEmblem || asset?.AssetType == StoreProductAssetType.Emblem) &&
        (Same(asset.AssetId, LivingProductionDefaultEmblemAssetId) ||
         Same(asset.AssetId, LegacyLivingFilamentBackendProbeAssetId) ||
         !string.IsNullOrWhiteSpace(asset.LivingVisualKind) ||
         !string.IsNullOrWhiteSpace(asset.LivingPackagePath) ||
         !string.IsNullOrWhiteSpace(asset.PreferredBackend));

    public static bool IsLivingEmblemAsset(string? assetId, string? assetType) =>
        (string.Equals(CanonicalTypeId(assetType), StoreProductAssetType.TeamLivingEmblem.ToString(), StringComparison.OrdinalIgnoreCase) ||
         string.Equals(CanonicalTypeId(assetType), StoreProductAssetType.Emblem.ToString(), StringComparison.OrdinalIgnoreCase)) &&
        (Same(assetId, LivingProductionDefaultEmblemAssetId) || Same(assetId, LegacyLivingFilamentBackendProbeAssetId));

    public static StoreProductAssetType? CanonicalType(string? assetType)
    {
        if (StoreProductAssetTypeCatalog.TryResolve(assetType, out var canonical))
            return canonical;
        if (Same(assetType, "Background"))
            return StoreProductAssetType.ProfileBackground;
        if (Same(assetType, TeamAssetTypes.Emblem.TeamAssetTypeId))
            return StoreProductAssetType.Emblem;
        if (Same(assetType, "TeamEmblem") || Same(assetType, "LivingTeamEmblem") || Same(assetType, "TeamLivingEmblem"))
            return StoreProductAssetType.TeamLivingEmblem;
        if (Same(assetType, TeamAssetPayloadCatalog.TeamColorTypeId))
            return StoreProductAssetType.TeamColor;
        if (Same(assetType, TeamAssetTypes.EmblemBackground.TeamAssetTypeId))
            return StoreProductAssetType.EmblemBackground;
        if (Same(assetType, TeamAssetTypes.Effect.TeamAssetTypeId))
            return StoreProductAssetType.TeamEffect;
        return null;
    }

    public static string CanonicalTypeId(string? assetType) =>
        CanonicalType(assetType)?.ToString() ??
        (string.IsNullOrWhiteSpace(assetType) ? "Unknown" : assetType.Trim());

    private static CatalogAssetDisplay Create(
        string assetId,
        StoreProductAssetType assetType,
        StoreProductOwnerScope ownerScope,
        string displayName,
        string arabicDisplayName,
        string previewImage,
        string colorHex,
        IReadOnlyList<ProductLink> products,
        string effectType = "",
        string animationType = "",
        int durationMilliseconds = 0,
        string equipTarget = "",
        string primaryColorPresetId = "",
        string secondaryColorPresetId = "",
        string customPrimaryColorHex = "",
        string customSecondaryColorHex = "",
        IReadOnlyList<string>? effectLayerIds = null,
        float effectOpacity = 1,
        float effectScale = 1,
        float effectSpeed = 1,
        float effectIntensity = 1,
        TypographyIdentityPreset? typographyPreset = null,
        string livingVisualScope = "",
        string livingVisualKind = "",
        string livingPackageId = "",
        string livingPackageManifestPath = "",
        string livingPackagePath = "",
        string preferredBackend = "",
        string fallbackPolicy = "",
        string livingVisualVersion = "",
        string livingPackageVersion = "",
        string rarity = "") =>
        new(
            assetId.Trim(),
            assetType,
            ownerScope,
            displayName,
            arabicDisplayName?.Trim() ?? string.Empty,
            previewImage?.Trim() ?? string.Empty,
            colorHex?.Trim() ?? string.Empty,
            products
                .Where(item => Same(item.AssetId, assetId) && CanonicalType(item.AssetType) == assetType)
                .Select(item => item.ProductId?.Trim() ?? string.Empty)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            effectType?.Trim() ?? string.Empty,
            animationType?.Trim() ?? string.Empty,
            durationMilliseconds,
            equipTarget?.Trim() ?? string.Empty,
            primaryColorPresetId?.Trim() ?? string.Empty,
            secondaryColorPresetId?.Trim() ?? string.Empty,
            customPrimaryColorHex?.Trim() ?? string.Empty,
            customSecondaryColorHex?.Trim() ?? string.Empty,
            effectLayerIds?.ToList() ?? new List<string>(),
            effectOpacity,
            effectScale,
            effectSpeed,
            effectIntensity,
            typographyPreset,
            livingVisualScope?.Trim() ?? string.Empty,
            livingVisualKind?.Trim() ?? string.Empty,
            livingPackageId?.Trim() ?? string.Empty,
            livingPackageManifestPath?.Trim() ?? string.Empty,
            livingPackagePath?.Trim() ?? string.Empty,
            preferredBackend?.Trim() ?? string.Empty,
            fallbackPolicy?.Trim() ?? string.Empty,
            livingVisualVersion?.Trim() ?? string.Empty,
            livingPackageVersion?.Trim() ?? string.Empty,
            rarity?.Trim() ?? string.Empty);

    private static StoreProductAssetType TeamType(string typeId) =>
        Same(typeId, TeamAssetTypes.Emblem.TeamAssetTypeId)
            ? StoreProductAssetType.Emblem
            : Same(typeId, TeamAssetPayloadCatalog.TeamColorTypeId)
                ? StoreProductAssetType.TeamColor
                : Same(typeId, TeamAssetTypes.Effect.TeamAssetTypeId)
                    ? StoreProductAssetType.TeamEffect
                    : StoreProductAssetType.EmblemBackground;

    private static StoreProductOwnerScope ResolveOwnerScope(StoreProductAssetType type, string? ownerScope) =>
        Enum.TryParse<StoreProductOwnerScope>(ownerScope?.Trim(), false, out var parsed)
            ? parsed
            : StoreProductAssetTypeCatalog.GetOwnerScope(type);

    private static string DisplayName(string? arabic, string? english) =>
        !string.IsNullOrWhiteSpace(arabic)
            ? arabic.Trim()
            : !string.IsNullOrWhiteSpace(english)
                ? english.Trim()
                : string.Empty;

    private static string PreferredImage(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : fallback?.Trim() ?? string.Empty;

    private static bool Same(string? left, string? right) =>
        CanonicalAssetIdentityService.SameAssetId(left, right);

    private sealed record ProductLink(string ProductId, string AssetId, string AssetType);
}

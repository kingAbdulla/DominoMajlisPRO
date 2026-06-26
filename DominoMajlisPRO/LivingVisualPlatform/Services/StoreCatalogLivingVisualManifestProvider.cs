using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class StoreCatalogLivingVisualManifestProvider : ILivingVisualManifestProvider
{
    public async Task<LivingVisualAssetManifest?> GetManifestAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return null;
        }

        var catalog = await StoreAssetCatalogService.LoadAsync();
        var asset = catalog.FirstOrDefault(item =>
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));

        return asset == null ? null : ToManifest(asset);
    }

    private static LivingVisualAssetManifest? ToManifest(CatalogAssetDisplay asset)
    {
        if (asset.AssetType != StoreProductAssetType.TeamEffect)
        {
            return null;
        }

        var version = string.IsNullOrWhiteSpace(asset.AnimationType)
            ? "catalog-team-effect-static-1"
            : $"catalog-team-effect-{asset.AnimationType.Trim()}-1";

        return new LivingVisualAssetManifest
        {
            AssetId = asset.AssetId,
            DisplayName = asset.DisplayName,
            Scope = LivingVisualAssetScope.TeamEmblem,
            Kind = LivingVisualAssetKind.LivingLegendaryEmblem,
            StaticFallbackImage = asset.PreviewImage,
            LivingPackagePath = string.Empty,
            PreferredBackend = LivingRendererBackend.StaticFallback,
            Capabilities = ResolveCapabilities(asset),
            MinimumDeviceProfile = string.Empty,
            BehaviorProfileId = string.Empty,
            Version = version,
            IsPublished = true,
            Rarity = string.Empty,
            FallbackPolicy = "StaticFallback",
            AllowedDisplayLocations = LivingVisualDisplayLocationCatalog.TeamEmblemLocations.ToList()
        };
    }

    private static LivingVisualCapability ResolveCapabilities(CatalogAssetDisplay asset)
    {
        var capabilities = LivingVisualCapability.FallbackStatic;

        if (!string.IsNullOrWhiteSpace(asset.EffectType) ||
            !string.IsNullOrWhiteSpace(asset.AnimationType) ||
            asset.EffectLayerIds.Count > 0)
        {
            capabilities |= LivingVisualCapability.Materials |
                LivingVisualCapability.Particles;
        }

        return capabilities;
    }
}

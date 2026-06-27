using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class StoreCatalogLivingVisualManifestProvider : ILivingVisualManifestProvider
{
    private const string FilamentBackendProbeAssetId = "team-emblem-living-filament-backend-probe";
    private const string LegacyTeamEffectFilamentBackendProbeAssetId = "teameffect_living_filament_backend_probe";

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

        if (asset == null && IsFilamentBackendProbe(assetId))
        {
            return CreateFilamentBackendProbeManifest();
        }

        return asset == null ? null : ToManifest(asset);
    }

    private static LivingVisualAssetManifest? ToManifest(CatalogAssetDisplay asset)
    {
        if (asset.AssetType != StoreProductAssetType.Emblem)
        {
            return null;
        }

        if (IsFilamentBackendProbe(asset.AssetId))
        {
            return CreateFilamentBackendProbeManifest(asset);
        }

        if (!IsLivingLegendaryEmblem(asset))
        {
            return null;
        }

        var version = string.IsNullOrWhiteSpace(asset.LivingVisualVersion)
            ? $"catalog-living-emblem-{asset.AssetId.Trim()}-1"
            : asset.LivingVisualVersion.Trim();

        return new LivingVisualAssetManifest
        {
            AssetId = asset.AssetId,
            DisplayName = asset.DisplayName,
            Scope = ParseEnum(asset.LivingVisualScope, LivingVisualAssetScope.TeamEmblem),
            Kind = ParseEnum(asset.LivingVisualKind, LivingVisualAssetKind.LivingLegendaryEmblem),
            StaticFallbackImage = asset.PreviewImage,
            LivingPackagePath = asset.LivingPackagePath,
            PreferredBackend = ParseEnum(asset.PreferredBackend, LivingRendererBackend.StaticFallback),
            Capabilities = ResolveCapabilities(asset),
            MinimumDeviceProfile = string.Empty,
            BehaviorProfileId = string.Empty,
            Version = version,
            IsPublished = true,
            Rarity = asset.Rarity,
            FallbackPolicy = string.IsNullOrWhiteSpace(asset.FallbackPolicy)
                ? "StaticFallback"
                : asset.FallbackPolicy,
            AllowedDisplayLocations = LivingVisualDisplayLocationCatalog.TeamEmblemLocations.ToList()
        };
    }

    private static bool IsLivingLegendaryEmblem(CatalogAssetDisplay asset) =>
        string.Equals(asset.LivingVisualKind, LivingVisualAssetKind.LivingLegendaryEmblem.ToString(), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(asset.EffectType, "LivingVisual", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(asset.LivingPackagePath);

    private static bool IsFilamentBackendProbe(string? assetId) =>
        CanonicalAssetIdentityService.SameAssetId(assetId, FilamentBackendProbeAssetId) ||
        CanonicalAssetIdentityService.SameAssetId(assetId, LegacyTeamEffectFilamentBackendProbeAssetId);

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

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value?.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : fallback;

    private static LivingVisualAssetManifest CreateFilamentBackendProbeManifest(CatalogAssetDisplay? asset = null) => new()
    {
        AssetId = FilamentBackendProbeAssetId,
        DisplayName = asset?.DisplayName ?? "Living Filament Backend Probe",
        Scope = LivingVisualAssetScope.TeamEmblem,
        Kind = LivingVisualAssetKind.LivingLegendaryEmblem,
        StaticFallbackImage = string.IsNullOrWhiteSpace(asset?.PreviewImage) ? "shield_3d.png" : asset.PreviewImage,
        LivingPackagePath = "living_visual_backend_probe.glb",
        PreferredBackend = LivingRendererBackend.Filament,
        Capabilities = LivingVisualCapability.Bones |
            LivingVisualCapability.Materials |
            LivingVisualCapability.Lighting |
            LivingVisualCapability.FallbackStatic,
        MinimumDeviceProfile = string.Empty,
        BehaviorProfileId = "filament-backend-probe",
        Version = "filament-backend-probe-1",
        IsPublished = true,
        Rarity = string.IsNullOrWhiteSpace(asset?.Rarity) ? "BackendProbe" : asset.Rarity,
        FallbackPolicy = "StaticFallback",
        AllowedDisplayLocations =
        {
            LivingVisualDisplayLocation.StorePreview,
            LivingVisualDisplayLocation.StoreActionSheet,
            LivingVisualDisplayLocation.Inventory,
            LivingVisualDisplayLocation.CreateTeamPreview
        }
    };
}

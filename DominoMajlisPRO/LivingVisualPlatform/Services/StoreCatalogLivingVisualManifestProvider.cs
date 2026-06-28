using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using System.Collections.Concurrent;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class StoreCatalogLivingVisualManifestProvider : ILivingVisualManifestProvider
{
    private const string ProductionDefaultAssetId = "team-emblem-living-production-default";
    private const string LegacyLivingFilamentBackendProbeAssetId = "team-emblem-living-filament-backend-probe";
    private const string LegacyTeamEffectFilamentBackendProbeAssetId = "teameffect_living_filament_backend_probe";
    private static readonly ConcurrentDictionary<string, LivingEmblemPackage> DeveloperPreviewPackages = new(StringComparer.OrdinalIgnoreCase);
    private readonly LivingEmblemPackageLoader _packageLoader = new();

    public static void RegisterDeveloperPreviewPackage(LivingEmblemPackage package)
    {
        var assetId = package.Manifest.AssetId?.Trim();
        if (!string.IsNullOrWhiteSpace(assetId))
            DeveloperPreviewPackages[assetId] = package;
    }

    public async Task<LivingVisualAssetManifest?> GetManifestAsync(string assetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return null;

        if (DeveloperPreviewPackages.TryGetValue(assetId.Trim(), out var previewPackage))
            return ToManifest(previewPackage);

        var catalog = await StoreAssetCatalogService.LoadAsync();
        var asset = catalog.FirstOrDefault(item => CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));

        if (asset == null && IsDefaultLivingEmblemOrLegacy(assetId))
            return await CreateDefaultPackageManifestAsync(cancellationToken);

        return asset == null ? null : await ToManifestAsync(asset, cancellationToken);
    }

    private async Task<LivingVisualAssetManifest?> ToManifestAsync(CatalogAssetDisplay asset, CancellationToken cancellationToken)
    {
        if (asset.AssetType != StoreProductAssetType.Emblem)
            return null;

        if (IsDefaultLivingEmblemOrLegacy(asset.AssetId))
            return await CreateDefaultPackageManifestAsync(cancellationToken, asset);

        if (!IsLivingLegendaryEmblem(asset))
            return null;

        var packageRoot = string.IsNullOrWhiteSpace(asset.LivingPackagePath) || IsDirectModelPath(asset.LivingPackagePath)
            ? LivingEmblemPackagePaths.DefaultProductionPackagePath
            : asset.LivingPackagePath;
        var import = await _packageLoader.LoadAsync(packageRoot, cancellationToken);
        if (import.Package != null)
            return ToManifest(import.Package, asset);

        return null;
    }

    private static bool IsLivingLegendaryEmblem(CatalogAssetDisplay asset) =>
        string.Equals(asset.LivingVisualKind, LivingVisualAssetKind.LivingLegendaryEmblem.ToString(), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(asset.EffectType, "LivingVisual", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(asset.LivingPackageId) ||
        !string.IsNullOrWhiteSpace(asset.LivingPackagePath);

    private static bool IsDirectModelPath(string? path)
    {
        var normalized = LivingEmblemPackagePaths.Normalize(path);
        return normalized.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDefaultLivingEmblemOrLegacy(string? assetId) =>
        CanonicalAssetIdentityService.SameAssetId(assetId, ProductionDefaultAssetId) ||
        CanonicalAssetIdentityService.SameAssetId(assetId, LegacyLivingFilamentBackendProbeAssetId) ||
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

    private async Task<LivingVisualAssetManifest?> CreateDefaultPackageManifestAsync(CancellationToken cancellationToken, CatalogAssetDisplay? asset = null)
    {
        var import = await _packageLoader.LoadAsync(
            string.IsNullOrWhiteSpace(asset?.LivingPackagePath)
                ? LivingEmblemPackagePaths.DefaultProductionPackagePath
                : asset.LivingPackagePath,
            cancellationToken);

        return import.Package == null ? null : ToManifest(import.Package, asset);
    }

    private static LivingVisualAssetManifest ToManifest(LivingEmblemPackage package, CatalogAssetDisplay? asset = null) => new()
    {
        PackageId = string.IsNullOrWhiteSpace(asset?.LivingPackageId) ? package.Manifest.PackageId : asset.LivingPackageId,
        PackageManifestPath = string.IsNullOrWhiteSpace(asset?.LivingPackageManifestPath) ? package.ManifestPath : asset.LivingPackageManifestPath,
        AssetId = string.IsNullOrWhiteSpace(asset?.AssetId) ? package.Manifest.AssetId : asset.AssetId,
        DisplayName = string.IsNullOrWhiteSpace(asset?.DisplayName) ? package.Manifest.DisplayName : asset.DisplayName,
        Scope = LivingVisualAssetScope.TeamEmblem,
        Kind = LivingVisualAssetKind.LivingLegendaryEmblem,
        ThumbnailImage = package.ResolvedThumbnailPath,
        StaticFallbackImage = string.IsNullOrWhiteSpace(asset?.PreviewImage) ? package.ResolvedFallbackPath : asset.PreviewImage,
        LivingPackagePath = package.ResolvedGlbPath,
        PackageRootPath = package.PackageRootPath,
        BehaviorPath = package.ResolvedBehaviorPath,
        PreferredBackend = ParseEnum(package.Manifest.Backend, LivingRendererBackend.Filament),
        Capabilities = LivingVisualCapability.Bones |
            LivingVisualCapability.Materials |
            LivingVisualCapability.Lighting |
            LivingVisualCapability.BehaviorBrain |
            LivingVisualCapability.FallbackStatic,
        MinimumDeviceProfile = package.Manifest.MinimumDeviceTier,
        BehaviorProfileId = package.Behavior.ProfileId,
        CameraPreset = package.Manifest.CameraPreset,
        LightingPreset = package.Manifest.LightingPreset,
        Version = string.IsNullOrWhiteSpace(asset?.LivingPackageVersion) ? package.Manifest.Version : asset.LivingPackageVersion,
        IsPublished = true,
        Rarity = string.IsNullOrWhiteSpace(asset?.Rarity) ? "Legendary" : asset.Rarity,
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

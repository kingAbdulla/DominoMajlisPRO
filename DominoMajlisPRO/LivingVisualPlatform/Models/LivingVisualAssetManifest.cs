namespace DominoMajlisPRO.LivingVisualPlatform.Models;

public sealed class LivingVisualAssetManifest
{
    public string AssetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public LivingVisualAssetScope Scope { get; set; } = LivingVisualAssetScope.Unknown;
    public LivingVisualAssetKind Kind { get; set; } = LivingVisualAssetKind.Unknown;
    public string StaticFallbackImage { get; set; } = string.Empty;
    public string LivingPackagePath { get; set; } = string.Empty;
    public LivingRendererBackend PreferredBackend { get; set; } = LivingRendererBackend.StaticFallback;
    public LivingVisualCapability Capabilities { get; set; } = LivingVisualCapability.FallbackStatic;
    public string MinimumDeviceProfile { get; set; } = string.Empty;
    public string BehaviorProfileId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public bool IsPublished { get; set; }
    public string Rarity { get; set; } = string.Empty;
    public string FallbackPolicy { get; set; } = "StaticFallback";
    public List<LivingVisualDisplayLocation> AllowedDisplayLocations { get; set; } = new();
}

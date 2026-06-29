namespace DominoMajlisPRO.LivingVisualPlatform.Models;

public sealed class LivingRenderEligibilityResult
{
    public LivingRenderEligibilityStatus Status { get; set; } = LivingRenderEligibilityStatus.Unknown;
    public string AssetId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public LivingVisualDisplayLocation DisplayLocation { get; set; } = LivingVisualDisplayLocation.Unknown;
    public bool ShouldUseStaticFallback { get; set; } = true;
    public string Reason { get; set; } = string.Empty;
    public LivingVisualAssetManifest? Manifest { get; set; }
}

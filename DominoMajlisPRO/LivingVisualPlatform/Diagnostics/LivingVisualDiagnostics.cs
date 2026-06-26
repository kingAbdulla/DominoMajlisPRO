using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Diagnostics;

public sealed class LivingVisualDiagnostics
{
    public string RequestedAssetId { get; set; } = string.Empty;
    public LivingVisualDisplayLocation DisplayLocation { get; set; } = LivingVisualDisplayLocation.Unknown;
    public LivingRenderEligibilityStatus EligibilityStatus { get; set; } = LivingRenderEligibilityStatus.Unknown;
    public string FallbackReason { get; set; } = string.Empty;
    public LivingRendererBackend SelectedBackend { get; set; } = LivingRendererBackend.None;
    public LivingVisualCapability Capabilities { get; set; } = LivingVisualCapability.None;
    public string ManifestVersion { get; set; } = string.Empty;

    public static LivingVisualDiagnostics FromEligibility(
        LivingRenderEligibilityResult result,
        LivingRendererBackend selectedBackend)
    {
        return new LivingVisualDiagnostics
        {
            RequestedAssetId = result.AssetId,
            DisplayLocation = result.DisplayLocation,
            EligibilityStatus = result.Status,
            FallbackReason = result.ShouldUseStaticFallback ? result.Reason : string.Empty,
            SelectedBackend = selectedBackend,
            Capabilities = result.Manifest?.Capabilities ?? LivingVisualCapability.None,
            ManifestVersion = result.Manifest?.Version ?? string.Empty
        };
    }
}

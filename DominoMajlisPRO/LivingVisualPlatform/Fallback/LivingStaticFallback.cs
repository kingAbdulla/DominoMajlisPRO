using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Fallback;

public sealed class LivingStaticFallback
{
    public string AssetId { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public static LivingStaticFallback FromEligibility(LivingRenderEligibilityResult result)
    {
        return new LivingStaticFallback
        {
            AssetId = result.AssetId,
            ImageSource = result.Manifest?.StaticFallbackImage ?? string.Empty,
            Reason = string.IsNullOrWhiteSpace(result.Reason)
                ? result.Status.ToString()
                : result.Reason
        };
    }
}

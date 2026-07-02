using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class LivingVisualCapabilityService : ILivingVisualCapabilityService
{
    public bool Supports(LivingVisualAssetManifest? manifest, LivingVisualCapability capability)
    {
        return manifest != null &&
            capability != LivingVisualCapability.None &&
            manifest.Capabilities.HasFlag(capability);
    }

    public bool CanRenderAt(LivingVisualAssetManifest? manifest, LivingVisualDisplayLocation displayLocation)
    {
        return manifest != null &&
            displayLocation != LivingVisualDisplayLocation.Unknown &&
            manifest.AllowedDisplayLocations.Contains(displayLocation);
    }

    public bool SupportsJaw(LivingVisualAssetManifest? manifest) => Supports(manifest, LivingVisualCapability.Jaw);
    public bool SupportsBlink(LivingVisualAssetManifest? manifest) => Supports(manifest, LivingVisualCapability.Blink);
    public bool SupportsFire(LivingVisualAssetManifest? manifest) => Supports(manifest, LivingVisualCapability.Fire);
    public bool SupportsReflection(LivingVisualAssetManifest? manifest) => Supports(manifest, LivingVisualCapability.Reflection);
    public bool SupportsBehaviorBrain(LivingVisualAssetManifest? manifest) => Supports(manifest, LivingVisualCapability.BehaviorBrain);
}

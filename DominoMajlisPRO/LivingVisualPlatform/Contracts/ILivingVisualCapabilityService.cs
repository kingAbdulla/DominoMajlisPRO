using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Contracts;

public interface ILivingVisualCapabilityService
{
    bool Supports(LivingVisualAssetManifest? manifest, LivingVisualCapability capability);
    bool CanRenderAt(LivingVisualAssetManifest? manifest, LivingVisualDisplayLocation displayLocation);
}

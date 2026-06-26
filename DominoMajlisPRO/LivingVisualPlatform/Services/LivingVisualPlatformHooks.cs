using DominoMajlisPRO.LivingVisualPlatform.Diagnostics;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public static class LivingVisualPlatformHooks
{
    public static event EventHandler<LivingVisualDiagnostics>? EligibilityResolved;
    public static event EventHandler<LivingVisualAssetManifest>? ManifestPublished;

    public static void PublishEligibilityResolved(LivingVisualDiagnostics diagnostics)
    {
        var handler = EligibilityResolved;
        handler?.Invoke(null, diagnostics);
    }

    public static void PublishManifestAvailable(LivingVisualAssetManifest manifest)
    {
        var handler = ManifestPublished;
        handler?.Invoke(null, manifest);
    }
}

using DominoMajlisPRO.LivingVisualPlatform.Diagnostics;
using DominoMajlisPRO.LivingVisualPlatform.Fallback;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Rendering;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed record LivingVisualPreviewFallbackResult(
    LivingRenderEligibilityResult Eligibility,
    LivingStaticFallback StaticFallback,
    LivingVisualDiagnostics Diagnostics);

public sealed class LivingVisualPreviewFallbackService
{
    private readonly LivingRenderEligibilityResolver _resolver;

    public LivingVisualPreviewFallbackService()
        : this(new LivingRenderEligibilityResolver(
            new StoreCatalogLivingVisualManifestProvider(),
            new PlayerInventoryLivingVisualOwnershipService(),
            new LivingVisualCapabilityService(),
            new LivingVisualRendererAdapterFactory()))
    {
    }

    public LivingVisualPreviewFallbackService(
        LivingRenderEligibilityResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<LivingVisualPreviewFallbackResult> ResolveForCurrentOwnerAsync(
        string assetId,
        LivingVisualDisplayLocation displayLocation =
            LivingVisualDisplayLocation.CreateTeamPreview,
        string? teamId = null,
        bool requireOwnership = true,
        CancellationToken cancellationToken = default)
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        var request = new LivingRenderRequest
        {
            ApplicationUserId = owner.ApplicationUserId,
            PlayerId = owner.PlayerId,
            TeamId = string.IsNullOrWhiteSpace(teamId) ? null : teamId.Trim(),
            AssetId = assetId,
            DisplayLocation = displayLocation,
            DeviceProfile = string.Empty,
            IsPreview = !requireOwnership,
            IsDeveloperPreview = false
        };

        var eligibility = await _resolver.ResolveAsync(request, cancellationToken);
        var fallback = LivingStaticFallback.FromEligibility(eligibility);
        var diagnostics = LivingVisualDiagnostics.FromEligibility(
            eligibility,
            LivingRendererBackend.StaticFallback);

        LivingVisualPlatformHooks.PublishEligibilityResolved(diagnostics);

        return new LivingVisualPreviewFallbackResult(
            eligibility,
            fallback,
            diagnostics);
    }
}

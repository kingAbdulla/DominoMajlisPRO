using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Rendering;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class LivingRenderEligibilityResolver
{
    private readonly ILivingVisualManifestProvider _manifestProvider;
    private readonly ILivingVisualOwnershipService _ownershipService;
    private readonly ILivingVisualCapabilityService _capabilityService;
    private readonly ILivingVisualRendererAdapterFactory _adapterFactory;

    public LivingRenderEligibilityResolver(
        ILivingVisualManifestProvider manifestProvider,
        ILivingVisualOwnershipService ownershipService,
        ILivingVisualCapabilityService capabilityService,
        ILivingVisualRendererAdapterFactory adapterFactory)
    {
        _manifestProvider = manifestProvider;
        _ownershipService = ownershipService;
        _capabilityService = capabilityService;
        _adapterFactory = adapterFactory;
    }

    public async Task<LivingRenderEligibilityResult> ResolveAsync(
        LivingRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = BaseResult(request);

        if (string.IsNullOrWhiteSpace(request.AssetId))
        {
            return Deny(result, LivingRenderEligibilityStatus.DeniedAsset, "AssetId is required.");
        }

        var manifest = await _manifestProvider.GetManifestAsync(request.AssetId, cancellationToken);
        result.Manifest = manifest;

        if (manifest == null || !manifest.IsPublished)
        {
            return Deny(result, LivingRenderEligibilityStatus.DeniedAsset, "Published living visual manifest was not found.");
        }

        if (!_capabilityService.CanRenderAt(manifest, request.DisplayLocation))
        {
            return Deny(result, LivingRenderEligibilityStatus.DeniedLocation, "Asset is not approved for this display location.");
        }

        if (!request.IsDeveloperPreview && !request.IsPreview)
        {
            var ownsAsset = await _ownershipService.PlayerOwnsAssetAsync(
                request.ApplicationUserId,
                request.PlayerId,
                request.AssetId,
                cancellationToken);

            if (!ownsAsset)
            {
                return Deny(result, LivingRenderEligibilityStatus.DeniedOwnership, "Ownership could not be verified for ApplicationUserId + PlayerId + AssetId.");
            }
        }

        if (manifest.PreferredBackend == LivingRendererBackend.StaticFallback ||
            manifest.PreferredBackend == LivingRendererBackend.None)
        {
            result.Status = LivingRenderEligibilityStatus.StaticOnly;
            result.ShouldUseStaticFallback = true;
            result.Reason = "Manifest requests static fallback rendering.";
            return result;
        }

        if (!_adapterFactory.IsBackendAvailable(manifest.PreferredBackend))
        {
            result.Status = LivingRenderEligibilityStatus.RendererUnavailable;
            result.ShouldUseStaticFallback = true;
            result.Reason = "Preferred living renderer backend is not available.";
            return result;
        }

        result.Status = LivingRenderEligibilityStatus.LivingAllowed;
        result.ShouldUseStaticFallback = false;
        result.Reason = "Living rendering is eligible.";
        return result;
    }

    private static LivingRenderEligibilityResult BaseResult(LivingRenderRequest request) => new()
    {
        AssetId = request.AssetId?.Trim() ?? string.Empty,
        PlayerId = request.PlayerId?.Trim() ?? string.Empty,
        TeamId = string.IsNullOrWhiteSpace(request.TeamId) ? null : request.TeamId.Trim(),
        DisplayLocation = request.DisplayLocation,
        ShouldUseStaticFallback = true
    };

    private static LivingRenderEligibilityResult Deny(
        LivingRenderEligibilityResult result,
        LivingRenderEligibilityStatus status,
        string reason)
    {
        result.Status = status;
        result.ShouldUseStaticFallback = true;
        result.Reason = reason;
        return result;
    }
}

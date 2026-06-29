using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.LivingVisualPlatform.Contracts;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class PlayerInventoryLivingVisualOwnershipService : ILivingVisualOwnershipService
{
    public async Task<bool> PlayerOwnsAssetAsync(
        string applicationUserId,
        string playerId,
        string assetId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId) ||
            string.IsNullOrWhiteSpace(playerId) ||
            string.IsNullOrWhiteSpace(assetId))
        {
            return false;
        }

        IReadOnlyList<PlayerOwnedStoreItem> owned;
        try
        {
            owned = await PlayerInventoryService.LoadOwnedAsync(playerId.Trim());
        }
        catch
        {
            return false;
        }

        return owned.Any(item =>
            item.IsOwned &&
            !item.IsExpired &&
            Same(item.ApplicationUserId, applicationUserId) &&
            CanonicalAssetIdentityService.SameAssetId(item.PlayerId, playerId) &&
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}

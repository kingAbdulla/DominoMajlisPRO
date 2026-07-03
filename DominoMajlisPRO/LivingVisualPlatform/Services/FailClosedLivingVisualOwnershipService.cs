using DominoMajlisPRO.LivingVisualPlatform.Contracts;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class FailClosedLivingVisualOwnershipService : ILivingVisualOwnershipService
{
    public Task<bool> PlayerOwnsAssetAsync(
        string applicationUserId,
        string playerId,
        string assetId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}

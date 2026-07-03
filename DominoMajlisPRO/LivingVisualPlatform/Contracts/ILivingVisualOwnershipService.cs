namespace DominoMajlisPRO.LivingVisualPlatform.Contracts;

public interface ILivingVisualOwnershipService
{
    Task<bool> PlayerOwnsAssetAsync(
        string applicationUserId,
        string playerId,
        string assetId,
        CancellationToken cancellationToken = default);
}

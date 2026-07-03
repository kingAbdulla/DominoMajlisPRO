using DominoMajlisPRO.LivingVisualPlatform.Contracts;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class ConfigurableLivingVisualOwnershipService : ILivingVisualOwnershipService
{
    private readonly Func<string, string, string, CancellationToken, Task<bool>> _resolver;

    public ConfigurableLivingVisualOwnershipService(
        Func<string, string, string, CancellationToken, Task<bool>>? resolver = null)
    {
        _resolver = resolver ?? ((_, _, _, _) => Task.FromResult(false));
    }

    public Task<bool> PlayerOwnsAssetAsync(
        string applicationUserId,
        string playerId,
        string assetId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId) ||
            string.IsNullOrWhiteSpace(playerId) ||
            string.IsNullOrWhiteSpace(assetId))
        {
            return Task.FromResult(false);
        }

        return _resolver(
            applicationUserId.Trim(),
            playerId.Trim(),
            assetId.Trim(),
            cancellationToken);
    }
}

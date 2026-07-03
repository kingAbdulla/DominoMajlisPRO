using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Contracts;

public interface ILivingVisualManifestProvider
{
    Task<LivingVisualAssetManifest?> GetManifestAsync(
        string assetId,
        CancellationToken cancellationToken = default);
}

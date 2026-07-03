using DominoMajlisPRO.LivingVisualPlatform.Contracts;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public sealed class InMemoryLivingVisualManifestProvider : ILivingVisualManifestProvider
{
    private readonly Dictionary<string, LivingVisualAssetManifest> _manifests;

    public InMemoryLivingVisualManifestProvider(IEnumerable<LivingVisualAssetManifest>? manifests = null)
    {
        _manifests = (manifests ?? Array.Empty<LivingVisualAssetManifest>())
            .Where(manifest => !string.IsNullOrWhiteSpace(manifest.AssetId))
            .GroupBy(manifest => manifest.AssetId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public Task<LivingVisualAssetManifest?> GetManifestAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return Task.FromResult<LivingVisualAssetManifest?>(null);
        }

        _manifests.TryGetValue(assetId.Trim(), out var manifest);
        return Task.FromResult<LivingVisualAssetManifest?>(manifest);
    }
}

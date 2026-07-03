using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public interface ILivingVisualRendererAdapter : IAsyncDisposable
{
    LivingRendererBackend Backend { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task LoadAssetAsync(
        LivingVisualAssetManifest manifest,
        CancellationToken cancellationToken = default);

    Task AttachToHostAsync(
        object host,
        CancellationToken cancellationToken = default);

    Task ApplyMotionCommandAsync(
        LivingMotionCommand command,
        CancellationToken cancellationToken = default);

    void Pause();
    void Resume();
}

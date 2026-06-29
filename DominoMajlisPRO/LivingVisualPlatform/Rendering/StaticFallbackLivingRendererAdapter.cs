using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class StaticFallbackLivingRendererAdapter : ILivingVisualRendererAdapter
{
    private LivingVisualAssetManifest? _manifest;

    public LivingRendererBackend Backend => LivingRendererBackend.StaticFallback;
    public string StaticFallbackImage => _manifest?.StaticFallbackImage ?? string.Empty;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAssetAsync(
        LivingVisualAssetManifest manifest,
        CancellationToken cancellationToken = default)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        return Task.CompletedTask;
    }

    public Task AttachToHostAsync(
        object host,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ApplyMotionCommandAsync(
        LivingMotionCommand command,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Pause()
    {
    }

    public void Resume()
    {
    }

    public ValueTask DisposeAsync()
    {
        _manifest = null;
        return ValueTask.CompletedTask;
    }
}

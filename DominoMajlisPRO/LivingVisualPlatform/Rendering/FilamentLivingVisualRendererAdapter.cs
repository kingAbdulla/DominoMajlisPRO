using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class FilamentLivingVisualRendererAdapter : ILivingVisualRendererAdapter
{
    private LivingVisualAssetManifest? _manifest;
    private bool _isPaused = true;
    private string _lastMotionCommand = string.Empty;
    private int _lastMotionCommandVersion;

#if ANDROID
    private FilamentLivingVisualView? _surface;
#endif

    public LivingRendererBackend Backend => LivingRendererBackend.Filament;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if ANDROID
        return Task.CompletedTask;
#else
        throw new PlatformNotSupportedException("Filament living rendering is only available on Android in this backend proof.");
#endif
    }

    public Task LoadAssetAsync(LivingVisualAssetManifest manifest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));

        if (string.IsNullOrWhiteSpace(_manifest.LivingPackagePath))
            throw new InvalidOperationException("Filament living rendering requires a GLB/glTF package path in the manifest.");

        return Task.CompletedTask;
    }

    public Task AttachToHostAsync(object host, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (host is not ContentView contentHost)
            throw new InvalidOperationException("Filament living rendering can only attach through LivingVisualHost.");

#if ANDROID
        var manifest = _manifest ?? throw new InvalidOperationException("A Filament manifest must be loaded before attaching.");
        _surface = new FilamentLivingVisualView
        {
            AssetPath = manifest.LivingPackagePath,
            IsPaused = _isPaused,
            LastMotionCommand = _lastMotionCommand,
            LastMotionCommandVersion = _lastMotionCommandVersion,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true
        };

        var hostGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true
        };
        hostGrid.Children.Add(_surface);

        MainThread.BeginInvokeOnMainThread(() => contentHost.Content = hostGrid);
        return Task.CompletedTask;
#else
        throw new PlatformNotSupportedException("Filament living rendering is only available on Android in this backend proof.");
#endif
    }

    public Task ApplyMotionCommandAsync(LivingMotionCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clamped = LivingMotionLimits.ClampDragonMasterCommand(command);
        _lastMotionCommand = LivingMotionCommandSerializer.Serialize(clamped);
        _lastMotionCommandVersion++;

#if ANDROID
        if (_surface != null)
        {
            var commandText = _lastMotionCommand;
            var commandVersion = _lastMotionCommandVersion;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_surface == null)
                    return;

                _surface.LastMotionCommand = commandText;
                _surface.LastMotionCommandVersion = commandVersion;
            });
        }
#endif

        return Task.CompletedTask;
    }

    public void Pause()
    {
        _isPaused = true;
#if ANDROID
        if (_surface != null)
            MainThread.BeginInvokeOnMainThread(() => _surface.IsPaused = true);
#endif
    }

    public void Resume()
    {
        _isPaused = false;
#if ANDROID
        if (_surface != null)
            MainThread.BeginInvokeOnMainThread(() => _surface.IsPaused = false);
#endif
    }

    public ValueTask DisposeAsync()
    {
        _manifest = null;
        _isPaused = true;
        _lastMotionCommand = string.Empty;
        _lastMotionCommandVersion = 0;
#if ANDROID
        _surface = null;
#endif
        return ValueTask.CompletedTask;
    }
}

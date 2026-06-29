using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;
using DominoMajlisPRO.LivingVisualPlatform.Skeleton;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class FilamentLivingVisualRendererAdapter : ILivingVisualRendererAdapter
{
    private LivingVisualAssetManifest? _manifest;
    private bool _isPaused = true;
    private int _touchStimulusVersion;

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
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Black,
            InputTransparent = true
        };

        var hostGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Black,
            InputTransparent = false
        };
        hostGrid.Children.Add(_surface);

        var hasMouthBurstOverlay = !IsTManDeveloperPreview(manifest) &&
            (manifest.Capabilities.HasFlag(LivingVisualCapability.Fire) ||
            manifest.Capabilities.HasFlag(LivingVisualCapability.Smoke));
        if (hasMouthBurstOverlay)
            hostGrid.Children.Add(new LivingVisualPulseOverlay());

        if (IsTManDeveloperPreview(manifest))
            hostGrid.Children.Add(CreateTManTouchLayer(hostGrid));

        MainThread.BeginInvokeOnMainThread(() => contentHost.Content = hostGrid);
        return Task.CompletedTask;
#else
        throw new PlatformNotSupportedException("Filament living rendering is only available on Android in this backend proof.");
#endif
    }

    public Task ApplyMotionCommandAsync(LivingMotionCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
#if ANDROID
        _surface = null;
#endif
        return ValueTask.CompletedTask;
    }

    private static bool IsTManDeveloperPreview(LivingVisualAssetManifest manifest)
    {
        var packagePath = manifest.LivingPackagePath?.Replace('\\', '/') ?? string.Empty;
        return string.Equals(manifest.AssetId, Services.StoreCatalogLivingVisualManifestProvider.TManSkeletonRuntimeAssetId, StringComparison.OrdinalIgnoreCase) ||
            packagePath.Contains("LivingEmblems/t_man/character.glb", StringComparison.OrdinalIgnoreCase);
    }

    private View CreateTManTouchLayer(Grid hostGrid)
    {
        var layer = new BoxView
        {
            BackgroundColor = Colors.Transparent,
            Opacity = 0.01,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = false
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, args) =>
        {
            if (_surface == null)
                return;

            var position = args.GetPosition(hostGrid);
            var width = Math.Max(1.0, hostGrid.Width);
            var height = Math.Max(1.0, hostGrid.Height);
            var x = Math.Clamp((position?.X ?? width * 0.5) / width, 0, 1);
            var y = Math.Clamp((position?.Y ?? height * 0.5) / height, 0, 1);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            _surface.LastTouchStimulus = LivingTouchStimulus.Create(x, y, 1.0, timestamp).Serialize();
            _surface.LastTouchStimulusVersion = ++_touchStimulusVersion;
        };

        layer.GestureRecognizers.Add(tap);
        return layer;
    }
}

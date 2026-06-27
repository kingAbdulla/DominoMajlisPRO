using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Diagnostics;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Performance;
using DominoMajlisPRO.LivingVisualPlatform.Rendering;
using DominoMajlisPRO.LivingVisualPlatform.Services;

namespace DominoMajlisPRO.LivingVisualPlatform.Controls;

public sealed class LivingVisualHost : ContentView
{
    public static readonly BindableProperty AssetIdProperty =
        BindableProperty.Create(nameof(AssetId), typeof(string), typeof(LivingVisualHost), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty StaticFallbackImageProperty =
        BindableProperty.Create(nameof(StaticFallbackImage), typeof(string), typeof(LivingVisualHost), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ApplicationUserIdProperty =
        BindableProperty.Create(nameof(ApplicationUserId), typeof(string), typeof(LivingVisualHost), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PlayerIdProperty =
        BindableProperty.Create(nameof(PlayerId), typeof(string), typeof(LivingVisualHost), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TeamIdProperty =
        BindableProperty.Create(nameof(TeamId), typeof(string), typeof(LivingVisualHost), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty DisplayLocationProperty =
        BindableProperty.Create(nameof(DisplayLocation), typeof(LivingVisualDisplayLocation), typeof(LivingVisualHost), LivingVisualDisplayLocation.Unknown, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IsDeveloperPreviewProperty =
        BindableProperty.Create(nameof(IsDeveloperPreview), typeof(bool), typeof(LivingVisualHost), false, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IsStorePreviewProperty =
        BindableProperty.Create(nameof(IsStorePreview), typeof(bool), typeof(LivingVisualHost), false, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IsInventoryPreviewProperty =
        BindableProperty.Create(nameof(IsInventoryPreview), typeof(bool), typeof(LivingVisualHost), false, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IsPausedProperty =
        BindableProperty.Create(nameof(IsPaused), typeof(bool), typeof(LivingVisualHost), false, propertyChanged: OnPausedPropertyChanged);

    private readonly Image _fallbackImage = new()
    {
        Aspect = Aspect.AspectFit,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill
    };

    private readonly LivingRenderEligibilityResolver _eligibilityResolver;
    private readonly LivingVisualRendererAdapterFactory _adapterFactory = new();
    private readonly LivingVisualPerformanceService _performanceService = new();
    private ILivingVisualRendererAdapter? _adapter;
    private int _reloadVersion;
    private bool _isLoaded;
    private bool _isDisposed;

    public LivingVisualHost()
    {
        _eligibilityResolver = new LivingRenderEligibilityResolver(
            new StoreCatalogLivingVisualManifestProvider(),
            new PlayerInventoryLivingVisualOwnershipService(),
            new LivingVisualCapabilityService(),
            _adapterFactory);

        Content = _fallbackImage;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string AssetId
    {
        get => (string)GetValue(AssetIdProperty);
        set => SetValue(AssetIdProperty, value);
    }

    public string StaticFallbackImage
    {
        get => (string)GetValue(StaticFallbackImageProperty);
        set => SetValue(StaticFallbackImageProperty, value);
    }

    public string ApplicationUserId
    {
        get => (string)GetValue(ApplicationUserIdProperty);
        set => SetValue(ApplicationUserIdProperty, value);
    }

    public string PlayerId
    {
        get => (string)GetValue(PlayerIdProperty);
        set => SetValue(PlayerIdProperty, value);
    }

    public string TeamId
    {
        get => (string)GetValue(TeamIdProperty);
        set => SetValue(TeamIdProperty, value);
    }

    public LivingVisualDisplayLocation DisplayLocation
    {
        get => (LivingVisualDisplayLocation)GetValue(DisplayLocationProperty);
        set => SetValue(DisplayLocationProperty, value);
    }

    public bool IsDeveloperPreview
    {
        get => (bool)GetValue(IsDeveloperPreviewProperty);
        set => SetValue(IsDeveloperPreviewProperty, value);
    }

    public bool IsStorePreview
    {
        get => (bool)GetValue(IsStorePreviewProperty);
        set => SetValue(IsStorePreviewProperty, value);
    }

    public bool IsInventoryPreview
    {
        get => (bool)GetValue(IsInventoryPreviewProperty);
        set => SetValue(IsInventoryPreviewProperty, value);
    }

    public bool IsPaused
    {
        get => (bool)GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }

    public LivingVisualDiagnostics? Diagnostics { get; private set; }
    public LivingRenderEligibilityResult? Eligibility { get; private set; }
    public LivingVisualPerformanceDecision? PerformanceDecision { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        ReloadAsync(cancellationToken);

    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        var version = Interlocked.Increment(ref _reloadVersion);
        return ReloadCoreAsync(version, cancellationToken);
    }

    public void Pause()
    {
        IsPaused = true;
        _adapter?.Pause();
    }

    public void Resume()
    {
        IsPaused = false;
        _adapter?.Resume();
        QueueReload();
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;

        if (_adapter != null)
        {
            await _adapter.DisposeAsync();
            _adapter = null;
        }
    }

    private async Task ReloadCoreAsync(int version, CancellationToken cancellationToken)
    {
        if (_isDisposed || !_isLoaded || IsPaused)
        {
            SetFallback(StaticFallbackImage);
            return;
        }

        try
        {
            var request = new LivingRenderRequest
            {
                ApplicationUserId = ApplicationUserId?.Trim() ?? string.Empty,
                PlayerId = PlayerId?.Trim() ?? string.Empty,
                TeamId = string.IsNullOrWhiteSpace(TeamId) ? null : TeamId.Trim(),
                AssetId = AssetId?.Trim() ?? string.Empty,
                DisplayLocation = DisplayLocation,
                DeviceProfile = string.Empty,
                IsPreview = IsStorePreview,
                IsDeveloperPreview = IsDeveloperPreview
            };

            SetFallback(StaticFallbackImage);

            var eligibility = await _eligibilityResolver.ResolveAsync(request, cancellationToken);
            if (version != _reloadVersion || _isDisposed)
                return;

            Eligibility = eligibility;

            var requestedBackend = eligibility.Manifest?.PreferredBackend ?? LivingRendererBackend.StaticFallback;
            var backendAvailable = _adapterFactory.IsBackendAvailable(requestedBackend);
            var performance = _performanceService.Decide(request.DeviceProfile, !_isLoaded || IsPaused, requestedBackend, backendAvailable);
            PerformanceDecision = performance;

            var selectedBackend = backendAvailable && !eligibility.ShouldUseStaticFallback && !performance.ShouldUseStaticFallback
                ? requestedBackend
                : LivingRendererBackend.StaticFallback;

            Diagnostics = LivingVisualDiagnostics.FromEligibility(eligibility, selectedBackend);
            Diagnostics.ApplicationUserId = request.ApplicationUserId;
            Diagnostics.PlayerId = request.PlayerId;
            Diagnostics.TeamId = request.TeamId ?? string.Empty;
            Diagnostics.OwnershipVerified = eligibility.Status is not LivingRenderEligibilityStatus.DeniedOwnership;
            if (performance.ShouldUseStaticFallback && string.IsNullOrWhiteSpace(Diagnostics.FallbackReason))
                Diagnostics.FallbackReason = performance.Reason;

            LivingVisualPlatformHooks.PublishEligibilityResolved(Diagnostics);

            if (performance.ShouldPause)
            {
                _adapter?.Pause();
                SetFallback(eligibility.Manifest?.StaticFallbackImage ?? StaticFallbackImage);
                return;
            }

            if (eligibility.Manifest == null)
            {
                SetFallback(StaticFallbackImage);
                return;
            }

            SetFallback(eligibility.Manifest.StaticFallbackImage);

            await ReplaceAdapterAsync(selectedBackend, eligibility.Manifest, cancellationToken);
        }
        catch (Exception ex)
        {
            Diagnostics = new LivingVisualDiagnostics
            {
                RequestedAssetId = AssetId?.Trim() ?? string.Empty,
                ApplicationUserId = ApplicationUserId?.Trim() ?? string.Empty,
                PlayerId = PlayerId?.Trim() ?? string.Empty,
                TeamId = TeamId?.Trim() ?? string.Empty,
                DisplayLocation = DisplayLocation,
                EligibilityStatus = LivingRenderEligibilityStatus.RendererFailed,
                FallbackReason = ex.Message,
                SelectedBackend = LivingRendererBackend.StaticFallback,
                OwnershipVerified = false
            };
            LivingVisualPlatformHooks.PublishEligibilityResolved(Diagnostics);
            SetFallback(StaticFallbackImage);
        }
    }

    private async Task ReplaceAdapterAsync(
        LivingRendererBackend backend,
        LivingVisualAssetManifest manifest,
        CancellationToken cancellationToken)
    {
        if (_adapter != null)
        {
            await _adapter.DisposeAsync();
            _adapter = null;
        }

        var adapter = _adapterFactory.CreateAdapter(backend);
        await adapter.InitializeAsync(cancellationToken);
        await adapter.LoadAssetAsync(manifest, cancellationToken);
        await adapter.AttachToHostAsync(this, cancellationToken);

        if (IsPaused)
            adapter.Pause();
        else
            adapter.Resume();

        _adapter = adapter;
    }

    private void SetFallback(string? imagePath)
    {
        var source = InventoryDisplayResolver.ResolveImageSource(
            string.IsNullOrWhiteSpace(imagePath) ? "shield_3d.png" : imagePath,
            "shield_3d.png");

        void Apply()
        {
            _fallbackImage.Source = source;
            Content = _fallbackImage;
        }

        if (MainThread.IsMainThread)
            Apply();
        else
            MainThread.BeginInvokeOnMainThread(Apply);
    }

    private void QueueReload()
    {
        if (!_isLoaded || _isDisposed)
            return;

        _ = ReloadAsync();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _isLoaded = true;
        if (!IsPaused)
            QueueReload();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isLoaded = false;
        _adapter?.Pause();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LivingVisualHost host)
            host.QueueReload();
    }

    private static void OnPausedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not LivingVisualHost host)
            return;

        if (newValue is bool paused && paused)
            host._adapter?.Pause();
        else
            host._adapter?.Resume();

        host.QueueReload();
    }
}

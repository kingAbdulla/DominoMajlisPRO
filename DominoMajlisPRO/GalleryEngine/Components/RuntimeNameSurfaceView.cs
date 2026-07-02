using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public abstract class RuntimeNameSurfaceView : ContentView
{
    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(RuntimeNameSurfaceView), "اسم اللاعب", propertyChanged: OnChanged);

    private readonly IdentityPlateView _plate = new()
    {
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Center
    };

    private int _refreshVersion;

    protected RuntimeNameSurfaceView()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        Content = _plate;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    protected async Task RefreshAsync()
    {
        var version = ++_refreshVersion;
        try
        {
            var identity = await ResolveIdentityAsync();
            if (version != _refreshVersion)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (version == _refreshVersion)
                    _plate.Bind(DisplayText, identity?.ResolvePreset());
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() => _plate.Bind(DisplayText, null));
        }
    }

    protected virtual void OnLoaded(object? sender, EventArgs e)
    {
        AppEvents.PlayerProfileChanged -= OnRefresh;
        AppEvents.PlayerProfileChanged += OnRefresh;
        AppEvents.TeamsChanged -= OnRefresh;
        AppEvents.TeamsChanged += OnRefresh;
        AppEvents.StoreEconomyChanged -= OnStoreRefresh;
        AppEvents.StoreEconomyChanged += OnStoreRefresh;
        AppEvents.StoreProgressChanged -= OnStoreRefresh;
        AppEvents.StoreProgressChanged += OnStoreRefresh;
        AppEvents.TeamAssetsChanged -= OnStoreRefresh;
        AppEvents.TeamAssetsChanged += OnStoreRefresh;
        _ = RefreshAsync();
    }

    protected virtual void OnUnloaded(object? sender, EventArgs e)
    {
        _refreshVersion++;
        AppEvents.PlayerProfileChanged -= OnRefresh;
        AppEvents.TeamsChanged -= OnRefresh;
        AppEvents.StoreEconomyChanged -= OnStoreRefresh;
        AppEvents.StoreProgressChanged -= OnStoreRefresh;
        AppEvents.TeamAssetsChanged -= OnStoreRefresh;
    }

    protected abstract Task<NameTypographyIdentity?> ResolveIdentityAsync();

    private void OnRefresh() => _ = RefreshAsync();
    private void OnStoreRefresh(string id) => _ = RefreshAsync();

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((RuntimeNameSurfaceView)bindable).RefreshAsync();
}

public sealed class RuntimePlayerNameSurfaceView : RuntimeNameSurfaceView
{
    public static readonly BindableProperty PlayerIdProperty =
        BindableProperty.Create(nameof(PlayerId), typeof(string), typeof(RuntimePlayerNameSurfaceView), string.Empty, propertyChanged: OnChanged);

    public string PlayerId
    {
        get => (string)GetValue(PlayerIdProperty);
        set => SetValue(PlayerIdProperty, value);
    }

    protected override Task<NameTypographyIdentity?> ResolveIdentityAsync() =>
        NameTypographyResolver.ResolvePlayerAsync(PlayerId);

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((RuntimePlayerNameSurfaceView)bindable).RefreshAsync();
}

public sealed class RuntimeTeamNameSurfaceView : RuntimeNameSurfaceView
{
    public static readonly BindableProperty TeamIdProperty =
        BindableProperty.Create(nameof(TeamId), typeof(string), typeof(RuntimeTeamNameSurfaceView), string.Empty, propertyChanged: OnChanged);

    public string TeamId
    {
        get => (string)GetValue(TeamIdProperty);
        set => SetValue(TeamIdProperty, value);
    }

    protected override Task<NameTypographyIdentity?> ResolveIdentityAsync() =>
        NameTypographyResolver.ResolveTeamAsync(TeamId);

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((RuntimeTeamNameSurfaceView)bindable).RefreshAsync();
}

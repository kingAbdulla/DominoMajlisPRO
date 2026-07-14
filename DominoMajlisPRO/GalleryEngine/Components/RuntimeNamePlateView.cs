using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class RuntimeNamePlateView : ContentView
{
    public static readonly BindableProperty OwnerIdProperty =
        BindableProperty.Create(
            nameof(OwnerId),
            typeof(string),
            typeof(RuntimeNamePlateView),
            string.Empty,
            propertyChanged: (bindable, _, _) => ((RuntimeNamePlateView)bindable).Refresh());

    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(
            nameof(DisplayText),
            typeof(string),
            typeof(RuntimeNamePlateView),
            string.Empty,
            propertyChanged: (bindable, _, _) => ((RuntimeNamePlateView)bindable).Refresh());

    public static readonly BindableProperty OwnerKindProperty =
        BindableProperty.Create(
            nameof(OwnerKind),
            typeof(string),
            typeof(RuntimeNamePlateView),
            "Team",
            propertyChanged: (bindable, _, _) => ((RuntimeNamePlateView)bindable).Refresh());

    public static readonly BindableProperty RenderingContextProperty =
        BindableProperty.Create(
            nameof(RenderingContext),
            typeof(NameSurfaceRenderingContext),
            typeof(RuntimeNamePlateView),
            NameSurfaceRenderingContext.TeamProfile,
            propertyChanged: (bindable, _, value) =>
                ((RuntimeNamePlateView)bindable)._plate.RenderingContext = (NameSurfaceRenderingContext)value);

    private readonly Label _fallback;
    private readonly IdentityPlateView _plate;
    private int _version;
    private bool _eventsHooked;

    public RuntimeNamePlateView()
    {
        IsClippedToBounds = true;
        FlowDirection = FlowDirection.RightToLeft;
        _fallback = new Label
        {
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        _plate = new IdentityPlateView
        {
            IsVisible = false,
            HeightRequest = 36,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        Content = new Grid
        {
            Children = { _fallback, _plate }
        };
        Loaded += (_, _) =>
        {
            HookEvents();
            Refresh();
        };
        Unloaded += (_, _) => UnhookEvents();
    }

    public string OwnerId
    {
        get => (string)GetValue(OwnerIdProperty);
        set => SetValue(OwnerIdProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public string OwnerKind
    {
        get => (string)GetValue(OwnerKindProperty);
        set => SetValue(OwnerKindProperty, value);
    }

    public NameSurfaceRenderingContext RenderingContext
    {
        get => (NameSurfaceRenderingContext)GetValue(RenderingContextProperty);
        set => SetValue(RenderingContextProperty, value);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null)
            Refresh();
    }

    private void HookEvents()
    {
        if (_eventsHooked)
            return;
        _eventsHooked = true;
        AppEvents.StoreProgressChanged += OnPlayerIdentityChanged;
        AppEvents.TeamAssetsChanged += OnTeamIdentityChanged;
    }

    private void UnhookEvents()
    {
        if (!_eventsHooked)
            return;
        _eventsHooked = false;
        AppEvents.StoreProgressChanged -= OnPlayerIdentityChanged;
        AppEvents.TeamAssetsChanged -= OnTeamIdentityChanged;
    }

    private void OnPlayerIdentityChanged(string playerId)
    {
        if (string.Equals(OwnerKind, "Player", StringComparison.OrdinalIgnoreCase) && SameOwner(playerId))
            Refresh();
    }

    private void OnTeamIdentityChanged(string teamId)
    {
        if (string.Equals(OwnerKind, "Team", StringComparison.OrdinalIgnoreCase) && SameOwner(teamId))
            Refresh();
    }

    private bool SameOwner(string ownerId) =>
        string.Equals(OwnerId?.Trim(), ownerId?.Trim(), StringComparison.OrdinalIgnoreCase);

    private void Refresh()
    {
        var version = Interlocked.Increment(ref _version);
        var text = DisplayText?.Trim() ?? string.Empty;
        _fallback.Text = text;
        _fallback.IsVisible = true;
        _plate.IsVisible = false;
        if (string.IsNullOrWhiteSpace(OwnerId) ||
            string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _ = ResolveAsync(version, OwnerId, text, OwnerKind);
    }

    private async Task ResolveAsync(
        int version,
        string ownerId,
        string text,
        string ownerKind)
    {
        var identity = string.Equals(ownerKind, "Player", StringComparison.OrdinalIgnoreCase)
            ? await PlayerNameTypographyResolver.ResolveAsync(ownerId)
            : await TeamNameTypographyResolver.ResolveAsync(ownerId);
        if (version != _version)
            return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (version != _version)
                return;
            IdentityPlateBinder.Apply(_fallback, _plate, text, identity);
        });
    }
}

using DominoMajlisPRO.GalleryEngine.Services;

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

    private readonly Label _fallback;
    private readonly IdentityPlateView _plate;
    private int _version;

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

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null)
            Refresh();
    }

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

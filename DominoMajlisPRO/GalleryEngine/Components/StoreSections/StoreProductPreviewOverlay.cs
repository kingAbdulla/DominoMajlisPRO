using Microsoft.Maui.Controls.Shapes;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Services;

using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

internal sealed record StoreProductPreviewRequest(
    string ImagePath,
    string Name,
    string Description,
    string Rarity,
    string Price,
    string State,
    StoreProductPreviewKind Kind,
    Color Accent,
    CatalogAssetDisplay? Effect = null);

internal sealed class StoreProductPreviewOverlay : Grid
{
    private static readonly Color Black = Color.FromArgb("#070707");
    private static readonly Color Gold = Color.FromArgb("#FFD76A");
    private static readonly Color GoldDark = Color.FromArgb("#8A642E");
    private static readonly Color Primary = Color.FromArgb("#FFF4D2");
    private static readonly Color Secondary = Color.FromArgb("#C8B58A");

    private readonly Border _panel;
    private readonly ContentView _visualHost;
    private readonly Label _name;
    private readonly Label _description;
    private readonly Label _rarity;
    private readonly Border _rarityBadge;
    private readonly Label _priceState;
    private double _visualScale = 1;
    private int _animationVersion;
    private bool _isClosing;

    public StoreProductPreviewOverlay()
    {
        IsVisible = false;
        InputTransparent = false;
        CascadeInputTransparent = false;
        FlowDirection = FlowDirection.RightToLeft;
        Padding = new Thickness(14, 26);
        BackgroundColor = Color.FromArgb("#F2000000");
        var blocker = new TapGestureRecognizer();
        blocker.Tapped += (_, _) => { };
        GestureRecognizers.Add(blocker);

        _visualHost = new ContentView { HeightRequest = 350, HorizontalOptions = LayoutOptions.Fill };
        _name = Label(24, Primary, true, TextAlignment.Center);
        _description = Label(13, Secondary, false, TextAlignment.Center);
        _description.MaxLines = 4;
        _description.LineBreakMode = LineBreakMode.TailTruncation;
        _rarity = Label(12, Black, true, TextAlignment.Center);
        _rarityBadge = new Border
        {
            Padding = new Thickness(13, 5),
            HorizontalOptions = LayoutOptions.Center,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = _rarity
        };
        _priceState = Label(14, Gold, true, TextAlignment.Center);

        var close = Button("✕", Color.FromArgb("#211B10"), Primary);
        close.WidthRequest = 42;
        close.HorizontalOptions = LayoutOptions.Start;
        close.Clicked += async (_, _) => await HideAsync();
        var title = Label(11, Gold, true, TextAlignment.Center);
        title.Text = "PREMIUM PREVIEW";
        title.CharacterSpacing = 2;
        title.VerticalTextAlignment = TextAlignment.Center;
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(close, 0, 0);
        header.Add(title, 1, 0);
        header.Add(new BoxView { WidthRequest = 42, Opacity = 0 }, 2, 0);

        var temporary = Label(14, Gold, true, TextAlignment.Center);
        temporary.Text = "👁 تجربة مؤقتة";
        var zoomIn = Button("تكبير", Color.FromArgb("#211B10"), Primary);
        zoomIn.Clicked += (_, _) => SetScale(Math.Min(1.6, _visualScale + 0.15));
        var zoomOut = Button("تصغير", Color.FromArgb("#211B10"), Primary);
        zoomOut.Clicked += (_, _) => SetScale(Math.Max(0.7, _visualScale - 0.15));
        var reset = Button("إعادة ضبط", Color.FromArgb("#171717"), Secondary);
        reset.Clicked += (_, _) => SetScale(1);
        var controls = new Grid { ColumnSpacing = 8 };
        for (var i = 0; i < 3; i++) controls.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        controls.Add(zoomIn, 0, 0);
        controls.Add(zoomOut, 1, 0);
        controls.Add(reset, 2, 0);
        var dismiss = Button("إغلاق", Gold, Black);
        dismiss.FontAttributes = FontAttributes.Bold;
        dismiss.Clicked += async (_, _) => await HideAsync();

        var content = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { header, temporary, _visualHost, _name, _rarityBadge, _description, _priceState, controls, dismiss }
        };
        _panel = new Border
        {
            MaximumWidthRequest = 680,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(18, 14, 18, 18),
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#18130B"), 0),
                    new GradientStop(Black, 0.62f),
                    new GradientStop(Color.FromArgb("#211406"), 1)
                }
            },
            Stroke = GoldDark,
            StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#CC000000")), Offset = new Point(0, 8), Radius = 28, Opacity = 0.95f },
            Content = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never, Content = content }
        };
        Children.Add(_panel);
    }

    public bool IsOpen => IsVisible && !_isClosing;

    public void Show(StoreProductPreviewRequest request)
    {
        _animationVersion++;
        _isClosing = false;
        _visualScale = 1;
        _name.Text = request.Name;
        _description.Text = request.Description;
        _description.IsVisible = !string.IsNullOrWhiteSpace(request.Description);
        _rarity.Text = string.IsNullOrWhiteSpace(request.Rarity) ? "COMMON" : request.Rarity.ToUpperInvariant();
        _rarityBadge.Background = new SolidColorBrush(request.Accent);
        _rarityBadge.Stroke = request.Accent;
        _priceState.Text = $"{request.Price}   •   {request.State}";
        _panel.Stroke = request.Accent;
        _visualHost.Content = BuildVisual(request);
        _visualHost.Scale = 1;
        this.CancelAnimations();
        _panel.CancelAnimations();
        Opacity = 0;
        _panel.Scale = 0.92;
        IsVisible = true;
        _ = AnimateOpenAsync(_animationVersion, request.Kind);
    }

    public async Task HideAsync()
    {
        if (!IsVisible || _isClosing) return;
        _isClosing = true;
        var version = ++_animationVersion;
        this.CancelAnimations();
        _panel.CancelAnimations();
        await Task.WhenAll(this.FadeToAsync(0, 150, Easing.CubicIn), _panel.ScaleToAsync(0.94, 150, Easing.CubicIn));
        if (version == _animationVersion) HideImmediately();
    }

    public void HideImmediately()
    {
        _animationVersion++;
        this.CancelAnimations();
        _panel.CancelAnimations();
        IsVisible = false;
        Opacity = 0;
        _panel.Scale = 1;
        _visualHost.Content = null;
        _visualScale = 1;
        _isClosing = false;
    }

    private async Task AnimateOpenAsync(int version, StoreProductPreviewKind kind)
    {
        await Task.WhenAll(this.FadeToAsync(1, 180, Easing.CubicOut), _panel.ScaleToAsync(1, 220, Easing.CubicOut));
        if (version != _animationVersion ||
            _isClosing ||
            kind is not (StoreProductPreviewKind.Effect or StoreProductPreviewKind.NameTypography))
            return;
        await _visualHost.ScaleToAsync(1.04, 220, Easing.SinInOut);
        await _visualHost.ScaleToAsync(1, 220, Easing.SinInOut);
    }

    private void SetScale(double scale)
    {
        _visualScale = scale;
        _visualHost.CancelAnimations();
        _ = _visualHost.ScaleToAsync(scale, 140, Easing.CubicOut);
    }

    private static View BuildVisual(StoreProductPreviewRequest request)
    {
        var image = new Image
        {
            Source = InventoryDisplayResolver.ResolveOptionalImageSource(request.ImagePath),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        image.IsVisible = image.Source != null;
        return request.Kind switch
        {
            StoreProductPreviewKind.Avatar => AvatarVisual(image, request.Accent),
            StoreProductPreviewKind.Background => BackgroundVisual(image, request),
            StoreProductPreviewKind.Frame => FrameVisual(image, request.Accent),
            StoreProductPreviewKind.Badge => IdentityVisual(image, request),
            StoreProductPreviewKind.Season => BackgroundVisual(image, request),
            StoreProductPreviewKind.NameTypography when request.Effect != null =>
                NameTypographyVisual(request, request.Effect),
            StoreProductPreviewKind.Effect when request.Effect != null =>
                EffectVisual(image, request.Effect, request.Accent),
            _ => PreviewCard(image, request.Accent)
        };
    }

    private static View NameTypographyVisual(
        StoreProductPreviewRequest request,
        CatalogAssetDisplay effect)
    {
        var preset = new NameTypographyIdentity(string.Empty, effect, null).ResolvePreset() ??
            effect.TypographyPreset.Normalized();
        if (effect.AssetType is StoreProductAssetType.PlayerNameEffect or
            StoreProductAssetType.TeamNameEffect)
            preset.FrameStylePreset = "None";

        var plate = new IdentityPlateView
        {
            HeightRequest = 70,
            RenderingContext = NameSurfaceRenderingContext.Store,
            MaximumWidthRequest = 420,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        plate.Bind(request.Name, preset);
        return PreviewCard(plate, request.Accent);
    }

    private static View EffectVisual(Image image, CatalogAssetDisplay effect, Color accent)
    {
        image.Source = InventoryDisplayResolver.ResolveOptionalImageSource(effect.PreviewImage);
        image.IsVisible = image.Source != null;
        image.WidthRequest = 190;
        image.HeightRequest = 190;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        var layers = new Grid();
        layers.Children.Add(IdentityEffectRenderer.Create(effect, 1.28));
        layers.Children.Add(image);
        return PreviewCard(layers, accent);
    }

    private static View AvatarVisual(Image image, Color accent)
    {
        image.Aspect = Aspect.AspectFill;
        return new Border
        {
            WidthRequest = 300, HeightRequest = 300, HorizontalOptions = LayoutOptions.Center, Padding = 8,
            Background = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.45), Radius = 0.7,
                GradientStops = { new GradientStop(Color.FromArgb("#493618"), 0), new GradientStop(Black, 1) }
            },
            Stroke = accent, StrokeThickness = 3, StrokeShape = new Ellipse(), Shadow = Glow(accent),
            Content = new Border { StrokeShape = new Ellipse(), Stroke = Gold, StrokeThickness = 1, Content = image }
        };
    }

    private static View BackgroundVisual(Image image, StoreProductPreviewRequest request)
    {
        image.Aspect = Aspect.AspectFill;
        var title = Label(16, Primary, true, TextAlignment.End);
        title.Text = request.Name;
        var grid = new Grid();
        grid.Children.Add(image);
        grid.Children.Add(new Border
        {
            VerticalOptions = LayoutOptions.End, Margin = 12, Padding = new Thickness(14, 10),
            Background = new SolidColorBrush(Color.FromArgb("#CC080808")), Stroke = GoldDark,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, Content = title
        });
        return PreviewCard(grid, request.Accent);
    }

    private static View FrameVisual(Image frame, Color accent)
    {
        frame.Aspect = Aspect.AspectFit;
        var portrait = Label(100, Secondary, true, TextAlignment.Center);
        portrait.Text = "👤";
        portrait.VerticalTextAlignment = TextAlignment.Center;
        var layers = new Grid();
        layers.Children.Add(portrait);
        layers.Children.Add(frame);
        return PreviewCard(layers, accent);
    }

    private static View IdentityVisual(Image image, StoreProductPreviewRequest request)
    {
        image.Aspect = Aspect.AspectFit;
        image.HeightRequest = 200;
        var title = Label(18, Primary, true, TextAlignment.Center);
        title.Text = request.Name;
        return PreviewCard(new VerticalStackLayout { Spacing = 12, Padding = 20, Children = { image, title } }, request.Accent);
    }

    private static Border PreviewCard(View content, Color accent) => new()
    {
        Margin = 4, Padding = 8, Background = new SolidColorBrush(Color.FromArgb("#171109")),
        Stroke = accent, StrokeThickness = 1.5, StrokeShape = new RoundRectangle { CornerRadius = 24 },
        Shadow = Glow(accent), Content = content
    };

    private static Shadow Glow(Color accent) => new() { Brush = new SolidColorBrush(accent), Offset = new Point(0, 3), Radius = 22, Opacity = 0.38f };
    private static Label Label(double size, Color color, bool bold, TextAlignment alignment) => new()
    {
        FontFamily = "Tajawal-Regular", FontSize = size, TextColor = color,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, HorizontalTextAlignment = alignment
    };
    private static Button Button(string text, Color background, Color foreground) => new()
    {
        Text = text, FontFamily = "Tajawal-Regular", FontSize = 13, HeightRequest = 44, Padding = new Thickness(8, 0),
        CornerRadius = 14, BackgroundColor = background, TextColor = foreground, BorderColor = GoldDark, BorderWidth = 1
    };
}

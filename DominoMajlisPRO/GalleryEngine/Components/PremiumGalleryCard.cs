using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Components;

public class PremiumGalleryCard : ContentView
{
    private readonly Border _root;
    private readonly BoxView _fullBackground;
    private readonly Image _image;
    private readonly Label _badge;
    private readonly Label _name;
    private readonly Label _price;
    private readonly Label _currencyIcon;
    private readonly Grid _contentGrid;
    private IdentityPlateView? _namePlateView;

    public PremiumGalleryCard()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var theme = GalleryThemeEngine.Current;

        _fullBackground = new BoxView
        {
            Background = CreateFallbackGradient()
        };

        var diagonalShade = new BoxView
        {
            Opacity = 0.22,
            Rotation = -28,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Fill,
            WidthRequest = 82,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#00FFFFFF"), 0f),
                    new GradientStop(Color.FromArgb("#20FFFFFF"), 0.45f),
                    new GradientStop(Color.FromArgb("#00000000"), 1f)
                }
            }
        };

        _image = new Image
        {
            Source = "gallery_lion.png",
            Aspect = Aspect.AspectFit,
            WidthRequest = 90,
            HeightRequest = 90,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(4, 6, 4, 0)
        };

        _badge = new Label
        {
            Text = "جديد",
            FontFamily = "Tajawal-Regular",
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Padding = new Thickness(8, 2),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var badgeBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#E50922"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(7, 7, 0, 0),
            Content = _badge
        };

        _name = new Label
        {
            Text = "أسد الصحراء",
            FontFamily = "Tajawal-Regular",
            FontSize = 12.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(5, 2, 5, 0)
        };

        _price = new Label
        {
            Text = "250",
            FontFamily = "CinzelDecorative-Bold",
            FontSize = 13.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.Gold,
            VerticalTextAlignment = TextAlignment.Center
        };

        _currencyIcon = new Label
        {
            Text = "💎",
            FontSize = 13,
            VerticalTextAlignment = TextAlignment.Center
        };

        var priceRow = new HorizontalStackLayout
        {
            FlowDirection = FlowDirection.LeftToRight,
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { _currencyIcon, _price }
        };

        _contentGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children = { _fullBackground, diagonalShade }
        };

        _contentGrid.Add(_image, 0, 0);
        _contentGrid.Add(badgeBorder, 0, 0);
        _contentGrid.Add(_name, 0, 1);
        _contentGrid.Add(priceRow, 0, 2);

        _root = new Border
        {
            Background = theme.CardBackground,
            Stroke = theme.Stroke,
            StrokeThickness = 1.05,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(0),
            Content = _contentGrid,
            Shadow = CreateShadow(theme)
        };

        Content = _root;

        ApplyTheme();
        ApplyResponsive();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Bind(GalleryItem item)
    {
        if (item == null)
            return;

        var imageName = string.IsNullOrWhiteSpace(item.Image) ? "gallery_lion.png" : item.Image;

        _image.Source = InventoryDisplayResolver.ResolveImageSource(imageName, "gallery_lion.png");
        _image.IsVisible = true;

        _name.Text = string.IsNullOrWhiteSpace(item.Name) ? "عنصر المتجر" : item.Name;

        var isFree = string.Equals(item.Currency, "Free", StringComparison.OrdinalIgnoreCase);
        _currencyIcon.IsVisible = !isFree;
        _currencyIcon.Text = string.Equals(item.Currency, "Coins", StringComparison.OrdinalIgnoreCase) ? "🪙" : "💎";
        _price.Text = isFree ? "مجاني" : item.Price.ToString();

        _badge.Text = item.IsLimited ? "محدود" : item.IsNew ? "جديد" : "جديد";

        _ = ApplyDynamicBackgroundAsync(imageName);
        _ = ApplyEffectPreviewAsync(item.Id);

        ApplyResponsive();
    }

    private async Task ApplyEffectPreviewAsync(string assetId)
    {
        var asset = await StoreAssetCatalogService.ResolveAsync(assetId, null);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ClearRuntimePreview();
            ResetImagePreviewSize();
            _image.IsVisible = true;

            if (asset == null)
                return;

            if (IsNameTypographyAsset(asset.AssetType))
            {
                _image.IsVisible = false;
                _namePlateView = new IdentityPlateView
                {
                    WidthRequest = 112,
                    HeightRequest = 50,
                    MaximumWidthRequest = 130,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(8, 18, 8, 0)
                };
                _namePlateView.Bind(string.IsNullOrWhiteSpace(asset.DisplayName) ? _name.Text : asset.DisplayName, asset.TypographyPreset);
                _contentGrid.Add(_namePlateView, 0, 0);
                return;
            }

            var isLivingEmblem = StoreAssetCatalogService.IsLivingEmblemAsset(asset);
            if (asset.AssetType is not (StoreProductAssetType.Effect or StoreProductAssetType.TeamEffect) && !isLivingEmblem)
                return;

            var fallback = asset.AssetType == StoreProductAssetType.TeamEffect || isLivingEmblem
                ? "shield_3d.png"
                : "gallery_lion.png";
            _image.Source = InventoryDisplayResolver.ResolveImageSource(
                string.IsNullOrWhiteSpace(asset.PreviewImage) ? fallback : asset.PreviewImage,
                fallback);
            _image.IsVisible = true;
        });
    }

    private void ClearRuntimePreview()
    {
        if (_namePlateView != null)
        {
            _contentGrid.Children.Remove(_namePlateView);
            _namePlateView = null;
        }
    }


    private void ResetImagePreviewSize()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            _image.WidthRequest = 92;
            _image.HeightRequest = 92;
        }
        else
        {
            _image.WidthRequest = 132;
            _image.HeightRequest = 132;
        }
    }
    private static bool IsNameTypographyAsset(StoreProductAssetType assetType) =>
        assetType is StoreProductAssetType.PlayerNameEffect or
            StoreProductAssetType.TeamNameEffect or
            StoreProductAssetType.PlayerNameFrame or
            StoreProductAssetType.TeamNameFrame;

    public void Bind(GalleryItem item, object? theme)
    {
        Bind(item);
    }

    private async Task ApplyDynamicBackgroundAsync(string imageName)
    {
        var brush = await ImageColorExtractor.CreateSoftGradientAsync(imageName);
        MainThread.BeginInvokeOnMainThread(() => _fullBackground.Background = brush);
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        _root.Background = theme.CardBackground;
        _root.Stroke = theme.Stroke;
        _root.Shadow = CreateShadow(theme);
        _name.TextColor = theme.TextPrimary;
        _price.TextColor = theme.Gold;
    }

    private static Shadow CreateShadow(DominoMajlisPRO.GalleryEngine.Services.GalleryTheme theme)
    {
        return new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 16,
            Opacity = 0.25f,
            Offset = new Point(0, 4)
        };
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        ClearRuntimePreview();
    }

    private void OnThemeChanged(object? sender, DominoMajlisPRO.GalleryEngine.Services.GalleryTheme theme)
    {
        ApplyTheme();
    }

    private void ApplyResponsive()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            HeightRequest = 146;
            _image.WidthRequest = 92;
            _image.HeightRequest = 92;
            _name.FontSize = 12.5;
            _price.FontSize = 13.5;
            _badge.FontSize = 9;
        }
        else
        {
            HeightRequest = 198;
            _image.WidthRequest = 132;
            _image.HeightRequest = 132;
            _name.FontSize = 15;
            _price.FontSize = 16;
            _badge.FontSize = 11;
        }
    }

    private static Brush CreateFallbackGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb("#070707"), 0f),
                new GradientStop(Color.FromArgb("#14100A"), 0.45f),
                new GradientStop(Color.FromArgb("#5E401B").WithAlpha(0.50f), 1f)
            }
        };
    }
}

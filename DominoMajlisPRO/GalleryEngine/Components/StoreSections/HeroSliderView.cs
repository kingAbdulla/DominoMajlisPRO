using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Admin;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class HeroSliderView : ContentView
{
    private readonly Border _root;
    private readonly CarouselView _carousel;
    private readonly IndicatorView _indicator;
    private readonly Border _developerButton;
    private Label _developerButtonLabel = null!;
    private readonly List<HeroSlide> _slides;
    private bool _timerStarted;

    public HeroSliderView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        _slides = new List<HeroSlide>();

        _carousel = new CarouselView
        {
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 244 : 292,
            Loop = true,
            IsBounceEnabled = true,
            ItemsSource = _slides,
            ItemTemplate = new DataTemplate(CreateSlide)
        };

        _indicator = new IndicatorView
        {
            IndicatorColor = GalleryThemeEngine.Current.Stroke,
            SelectedIndicatorColor = GalleryThemeEngine.Current.Gold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _carousel.IndicatorView = _indicator;

        _developerButton = CreateDeveloperAdminButton();

        var root = new Grid
        {
            Children =
            {
                _carousel,
                _indicator,
                _developerButton
            }
        };

        _root = new Border
        {
            Background = GalleryThemeEngine.Current.CardBackground,
            Stroke = GalleryThemeEngine.Current.Stroke,
            StrokeThickness = 1.25,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = root
        };

        Content = _root;

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void BindSeason(GallerySeason season)
    {
        _ = season;
        _ = ApplyPublishedSeasonHeroAsync();
    }

    private async Task ApplyPublishedSeasonHeroAsync()
    {
        var published = await StoreAssetQueryService.LoadPublishedSeasonsAsync();
        ApplyPublishedSeasonHeroes(published);
    }

    private void OnPublishedSeasonChanged(CurrentSeasonRecord? model)
    {
        _ = model;
        _ = ApplyPublishedSeasonHeroAsync();
    }

    private void ApplyPublishedSeasonHeroes(IReadOnlyList<CurrentSeasonRecord> published)
    {
        _slides.Clear();
        _slides.AddRange(published.Select(record => new HeroSlide
        {
            Badge = record.Title,
            Title = record.Title,
            Subtitle = record.Subtitle,
            Description = record.Description,
            ButtonText = string.IsNullOrWhiteSpace(record.ButtonText) ? "عرض التفاصيل" : record.ButtonText.Trim(),
            Image = record.ImagePath
        }));

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _carousel.ItemsSource = null;
            _carousel.ItemsSource = _slides;
            if (_slides.Count > 0) _carousel.Position = 0;
        });
    }
    private void ResetFirstSlideToFallback()
    {
        _slides.Clear();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _carousel.ItemsSource = null;
            _carousel.ItemsSource = _slides;
            _carousel.Position = 0;
        });
    }
    private Border CreateDeveloperAdminButton()
    {
        _developerButtonLabel = new Label
        {
            Text = "+",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = GalleryThemeEngine.Current.Gold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var button = new Border
        {
            AutomationId = "DeveloperStoreAdminButton",
            WidthRequest = 36,
            HeightRequest = 36,
            Margin = new Thickness(10),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Stroke = GalleryThemeEngine.Current.Accent,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            Background = GalleryThemeEngine.Current.CardBackground,
            IsVisible = false,
            Content = _developerButtonLabel
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OpenDeveloperStoreManagerAsync();
        button.GestureRecognizers.Add(tap);

        return button;
    }
    private async Task OpenDeveloperStoreManagerAsync()
    {
        if (!await IsDeveloperAsync())
            return;

        var page = new DeveloperStoreManagerPage();
        var navigation = Application.Current?.MainPage?.Navigation;

        if (navigation != null)
        {
            await navigation.PushAsync(page);
            return;
        }

        await Shell.Current.Navigation.PushAsync(page);
    }

    private async Task RefreshDeveloperAdminButtonVisibilityAsync()
    {
        var isDeveloper = await IsDeveloperAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
            _developerButton.IsVisible = isDeveloper);
    }

    private static async Task<bool> IsDeveloperAsync()
    {
        var applicationUser = await ApplicationUserService.GetCurrentUserAsync();
        if (applicationUser.Role == ApplicationUserRole.Developer)
            return true;
        if (applicationUser.Role != ApplicationUserRole.Ghost)
            return false;

        var role = await HonorIdentityService.GetCurrentRoleAsync();
        return role == HonorRoleType.Developer;
    }


    private void OnLoaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        CurrentSeasonAdminService.PublishedChanged -= OnPublishedSeasonChanged;
        CurrentSeasonAdminService.PublishedChanged += OnPublishedSeasonChanged;
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        AppEvents.CurrentUserChanged += OnCurrentUserChanged;
        _ = RefreshDeveloperAdminButtonVisibilityAsync();
        _ = ApplyPublishedSeasonHeroAsync();

        if (_timerStarted)
            return;

        _timerStarted = true;

        Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            if (!_timerStarted || _slides.Count == 0)
                return false;

            _carousel.Position = (_carousel.Position + 1) % _slides.Count;
            return true;
        });
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        CurrentSeasonAdminService.PublishedChanged -= OnPublishedSeasonChanged;
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        _timerStarted = false;
    }

    private void OnCurrentUserChanged() =>
        _ = RefreshDeveloperAdminButtonVisibilityAsync();

    private void OnThemeChanged(
        object? sender,
        DominoMajlisPRO.GalleryEngine.Services.GalleryTheme theme)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        _root.Background = theme.CardBackground;
        _root.Stroke = theme.Stroke;
        _root.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 22,
            Opacity = 0.26f,
            Offset = new Point(0, 6)
        };

        _indicator.IndicatorColor = theme.Stroke;
        _indicator.SelectedIndicatorColor = theme.Gold;

        _developerButton.Background = theme.CardBackground;
        _developerButton.Stroke = theme.Accent;
        _developerButton.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 10,
            Opacity = 0.22f,
            Offset = new Point(0, 2)
        };
        _developerButtonLabel.TextColor = theme.Gold;
    }

    private static View CreateSlide()
    {
        var theme = GalleryThemeEngine.Current;
        var root = new Grid();

        var background = new BoxView
        {
            Background = CreateFallbackGradient()
        };

        var darkOverlay = new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#08000000"), 0f),
                    new GradientStop(Color.FromArgb("#32000000"), 0.52f),
                    new GradientStop(Color.FromArgb("#90000000"), 1f)
                }
            }
        };

        var contentGrid = new Grid
        {
            Padding = new Thickness(16, 10, 14, 18),
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(0.57, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(0.43, GridUnitType.Star) }
            }
        };

        var badgeLabel = CreateLabel(11, theme.Gold, true, 1);
        badgeLabel.HorizontalTextAlignment = TextAlignment.Center;
        badgeLabel.SetBinding(Label.TextProperty, nameof(HeroSlide.Badge));

        var badge = new Border
        {
            Padding = new Thickness(12, 4),
            HorizontalOptions = LayoutOptions.Start,
            Stroke = theme.Stroke,
            StrokeThickness = 0.7,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Background = new SolidColorBrush(theme.Glow.WithAlpha(0.22f)),
            Content = badgeLabel
        };

        var title = CreateLabel(DeviceInfo.Idiom == DeviceIdiom.Phone ? 28 : 36, theme.TextPrimary, true, 2);
        title.SetBinding(Label.TextProperty, nameof(HeroSlide.Title));

        var subtitle = CreateLabel(DeviceInfo.Idiom == DeviceIdiom.Phone ? 15 : 18, Colors.White, true, 1);
        subtitle.SetBinding(Label.TextProperty, nameof(HeroSlide.Subtitle));

        var description = CreateLabel(DeviceInfo.Idiom == DeviceIdiom.Phone ? 11 : 13, theme.TextSecondary, false, 2);
        description.SetBinding(Label.TextProperty, nameof(HeroSlide.Description));

        var buttonLabel = new Label
        {
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 : 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#2B1600"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        buttonLabel.SetBinding(Label.TextProperty, nameof(HeroSlide.ButtonText));

        var button = new Border
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 144 : 168,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 36 : 42,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(theme.TextPrimary, 0f),
                    new GradientStop(theme.Gold, 1f)
                }
            },
            Content = buttonLabel
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = DeviceInfo.Idiom == DeviceIdiom.Phone ? 5 : 7,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                badge,
                title,
                subtitle,
                description,
                button
            }
        };

        var image = new Image
        {
            Aspect = Aspect.AspectFit,
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 158 : 218,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 196 : 246,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        image.SetBinding(Image.SourceProperty, nameof(HeroSlide.ImageSource));

        contentGrid.Add(textStack, 0, 0);
        contentGrid.Add(image, 1, 0);

        root.Children.Add(background);
        root.Children.Add(darkOverlay);
        root.Children.Add(contentGrid);

        root.BindingContextChanged += async (_, _) =>
        {
            if (root.BindingContext is not HeroSlide slide)
                return;

            var brush = await ImageColorExtractor.CreateSoftGradientAsync(slide.Image);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                background.Background = brush;
            });
        };

        return root;
    }

    private static Label CreateLabel(double fontSize, Color color, bool bold, int maxLines)
    {
        return new Label
        {
            FontFamily = "Tajawal-Regular",
            FontSize = fontSize,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = color,
            MaxLines = maxLines,
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center
        };
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
                new GradientStop(Color.FromArgb("#14100A"), 0.52f),
                new GradientStop(Color.FromArgb("#5E401B").WithAlpha(0.58f), 1f)
            }
        };
    }

    private static ImageSource ResolveHeroImageSource(string image)
        => InventoryDisplayResolver.ResolveImageSource(image);
    private sealed class HeroSlide
    {
        public string Badge { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Description { get; set; } = "";
        public string ButtonText { get; set; } = "عرض التفاصيل";
        public string Image { get; set; } = "";
        public ImageSource ImageSource => ResolveHeroImageSource(Image);
    }
}










using DominoMajlisPRO.GalleryEngine.Helpers;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Components;

public class HeroBannerView : ContentView
{
    private readonly PremiumCard _card;
    private readonly Grid _root;

    private readonly BoxView _generatedBackground;
    private readonly Image _optionalBackgroundImage;
    private readonly Image _characterImage;

    private readonly BoxView _baseOverlay;
    private readonly BoxView _textSafetyGradient;
    private readonly BoxView _rightGlow;
    private readonly BoxView _topVignette;
    private readonly BoxView _bottomVignette;
    private readonly BoxView _depthGlow;

    private readonly HeroContentView _content;
    private readonly CountdownView _countdown;
    private readonly VerticalStackLayout _textStack;

    public HeroBannerView()
    {
        FlowDirection = FlowDirection.LeftToRight;

        _generatedBackground = new BoxView
        {
            InputTransparent = true,
            Background = CreateBackgroundBrush("#2A1A08", "#050505", "#7A3E12")
        };

        _optionalBackgroundImage = new Image
        {
            Aspect = Aspect.AspectFill,
            Opacity = 0.32,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            IsVisible = false
        };

        _rightGlow = new BoxView
        {
            HorizontalOptions = LayoutOptions.End,
            InputTransparent = true,
            Background = CreateRightGlowBrush(Color.FromArgb("#FFB84A"))
        };

        _depthGlow = new BoxView
        {
            InputTransparent = true,
            Background = new RadialGradientBrush
            {
                Center = new Point(0.78, 0.42),
                Radius = 0.78,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#44FFB84A"), 0.00f),
                    new GradientStop(Color.FromArgb("#1C7A3E12"), 0.42f),
                    new GradientStop(Color.FromArgb("#00000000"), 1.00f)
                }
            }
        };

        _baseOverlay = new BoxView
        {
            Color = Colors.Black,
            Opacity = 0.08,
            InputTransparent = true
        };

        _textSafetyGradient = new BoxView
        {
            HorizontalOptions = LayoutOptions.Start,
            InputTransparent = true,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#C8050505"), 0.00f),
                    new GradientStop(Color.FromArgb("#99050505"), 0.45f),
                    new GradientStop(Color.FromArgb("#22050505"), 0.78f),
                    new GradientStop(Color.FromArgb("#00050505"), 1.00f)
                }
            }
        };

        _topVignette = new BoxView
        {
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#66050505"), 0.00f),
                    new GradientStop(Color.FromArgb("#22050505"), 0.58f),
                    new GradientStop(Color.FromArgb("#00050505"), 1.00f)
                }
            }
        };

        _bottomVignette = new BoxView
        {
            VerticalOptions = LayoutOptions.End,
            InputTransparent = true,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(0, 0),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#AA050505"), 0.00f),
                    new GradientStop(Color.FromArgb("#44050505"), 0.48f),
                    new GradientStop(Color.FromArgb("#00050505"), 1.00f)
                }
            }
        };

        _characterImage = new Image
        {
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0.98,
            InputTransparent = true
        };

        _content = new HeroContentView();
        _countdown = new CountdownView();

        _textStack = new VerticalStackLayout
        {
            FlowDirection = FlowDirection.RightToLeft,
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            Children =
            {
                _content,
                _countdown
            }
        };

        _root = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            Children =
            {
                _generatedBackground,
                _optionalBackgroundImage,
                _rightGlow,
                _depthGlow,
                _baseOverlay,
                _textSafetyGradient,
                _topVignette,
                _bottomVignette,
                _characterImage,
                _textStack
            }
        };

        _card = new PremiumCard
        {
            Padding = 0,
            Content = _root
        };

        Content = _card;
        SizeChanged += OnSizeChanged;
    }

    public void Bind(GallerySeason season)
    {
        if (season == null)
            return;

        var theme = season.Theme;

        _generatedBackground.Background = CreateBackgroundBrush(
            theme.CardBackgroundStart,
            theme.CardBackgroundEnd,
            theme.SecondaryColor);

        var backgroundImage = !string.IsNullOrWhiteSpace(theme.BackgroundImage)
            ? theme.BackgroundImage
            : season.BackgroundImage;

        _optionalBackgroundImage.Source = string.IsNullOrWhiteSpace(backgroundImage)
            ? null
            : ImageSource.FromFile(backgroundImage);

        _optionalBackgroundImage.IsVisible = !string.IsNullOrWhiteSpace(backgroundImage);

        var characterImage = !string.IsNullOrWhiteSpace(theme.CharacterImage)
            ? theme.CharacterImage
            : season.CharacterImage;

        _characterImage.Source = string.IsNullOrWhiteSpace(characterImage)
            ? null
            : ImageSource.FromFile(characterImage);

        _rightGlow.Background = CreateRightGlowBrush(
            ParseColor(theme.GlowColor, "#FFB84A"));

        _baseOverlay.Opacity = Math.Clamp(
            season.HeroLayout.OverlayOpacity * 0.20,
            0.06,
            0.12);

        _content.Bind(
            season.BadgeText,
            season.Title,
            season.Chapter,
            season.Description,
            season.ButtonText);

        _countdown.SetText(GalleryTimeHelper.GetCountdownFull(season.EndDate));

        ApplyResponsiveLayout();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (Width <= 0)
            return;

        var phone = DeviceInfo.Idiom == DeviceIdiom.Phone;
        var tablet = DeviceInfo.Idiom == DeviceIdiom.Tablet;
        var width = Width;

        HeightRequest =
            phone
                ? Math.Clamp(width * 0.70, 260, 330)
                : tablet
                    ? Math.Clamp(width * 0.43, 330, 470)
                    : Math.Clamp(width * 0.37, 340, 500);

        var height = HeightRequest;

        _rightGlow.WidthRequest =
            phone ? width * 0.48 :
            tablet ? width * 0.46 :
            width * 0.42;

        _textSafetyGradient.WidthRequest =
            phone ? width * 0.64 :
            tablet ? width * 0.58 :
            width * 0.55;

        _topVignette.HeightRequest = height * 0.28;
        _bottomVignette.HeightRequest = height * 0.38;

        _textStack.WidthRequest =
            phone ? width * 0.55 :
            tablet ? width * 0.50 :
            width * 0.48;

        _textStack.Margin =
            phone
                ? new Thickness(18, 14, 0, 14)
                : tablet
                    ? new Thickness(30, 24, 0, 24)
                    : new Thickness(38, 28, 0, 28);

        _characterImage.WidthRequest =
            phone
                ? Math.Clamp(width * 0.34, 130, 185)
                : tablet
                    ? Math.Clamp(width * 0.30, 240, 360)
                    : Math.Clamp(width * 0.28, 260, 410);

        _characterImage.HeightRequest =
            phone ? height * 0.90 :
            tablet ? height * 0.90 :
            height * 0.92;

        _characterImage.Margin =
            phone
                ? new Thickness(0, 0, 10, 0)
                : tablet
                    ? new Thickness(0, 0, 22, 0)
                    : new Thickness(0, 0, 30, 0);
    }

    private static LinearGradientBrush CreateBackgroundBrush(
        string start,
        string end,
        string accent)
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(ParseColor(start, "#2A1A08"), 0.00f),
                new GradientStop(ParseColor(accent, "#7A3E12").WithAlpha(0.85f), 0.48f),
                new GradientStop(ParseColor(end, "#050505"), 1.00f)
            }
        };
    }

    private static LinearGradientBrush CreateRightGlowBrush(Color color)
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(1, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(color.WithAlpha(0.42f), 0.00f),
                new GradientStop(color.WithAlpha(0.20f), 0.44f),
                new GradientStop(Color.FromArgb("#00000000"), 1.00f)
            }
        };
    }

    private static Color ParseColor(string value, string fallback)
    {
        try
        {
            return Color.FromArgb(
                string.IsNullOrWhiteSpace(value)
                    ? fallback
                    : value);
        }
        catch
        {
            return Color.FromArgb(fallback);
        }
    }
}
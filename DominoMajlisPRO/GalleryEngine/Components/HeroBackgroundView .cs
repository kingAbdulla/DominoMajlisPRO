namespace DominoMajlisPRO.GalleryEngine.Components;

public class HeroBackgroundView : ContentView
{
    private readonly Image _backgroundImage;
    private readonly BoxView _darkOverlay;
    private readonly BoxView _textProtectionShade;
    private readonly BoxView _rightGlow;

    public HeroBackgroundView()
    {
        _backgroundImage = new Image
        {
            Aspect = Aspect.AspectFill,
            Opacity = 1,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        _darkOverlay = new BoxView
        {
            Color = Colors.Black,
            Opacity = 0.22
        };

        _textProtectionShade = new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#F2050505"), 0.00f),
                    new GradientStop(Color.FromArgb("#CC050505"), 0.35f),
                    new GradientStop(Color.FromArgb("#66050505"), 0.62f),
                    new GradientStop(Color.FromArgb("#00050505"), 1.00f)
                }
            }
        };

        _rightGlow = new BoxView
        {
            HorizontalOptions = LayoutOptions.End,
            WidthRequest = 260,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(1, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#55FFB84A"), 0.00f),
                    new GradientStop(Color.FromArgb("#22FFB84A"), 0.45f),
                    new GradientStop(Color.FromArgb("#00000000"), 1.00f)
                }
            }
        };

        Content = new Grid
        {
            Children =
            {
                _backgroundImage,
                _darkOverlay,
                _textProtectionShade,
                _rightGlow
            }
        };

        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0)
            return;

        _rightGlow.WidthRequest =
            DeviceInfo.Idiom == DeviceIdiom.Phone
                ? Width * 0.42
                : Width * 0.36;
    }

    public void SetBackground(string image)
    {
        _backgroundImage.Source = string.IsNullOrWhiteSpace(image)
            ? null
            : ImageSource.FromFile(image);
    }

    public void SetCharacter(string image)
    {
        // الشخصية لم تعد هنا.
        // سيتم وضعها داخل عمود مستقل في HeroBannerView لمنع تداخلها مع النص.
    }

    public void SetOverlayOpacity(double opacity)
    {
        _darkOverlay.Opacity = Math.Clamp(opacity, 0.14, 0.34);
    }

    public void SetCharacterWidth(double width)
    {
        // لم يعد مطلوبًا هنا.
    }

    public void SetGlowColor(Color color)
    {
        _rightGlow.Background = new LinearGradientBrush
        {
            StartPoint = new Point(1, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(color.WithAlpha(0.34f), 0.00f),
                new GradientStop(color.WithAlpha(0.16f), 0.45f),
                new GradientStop(Color.FromArgb("#00000000"), 1.00f)
            }
        };
    }
}
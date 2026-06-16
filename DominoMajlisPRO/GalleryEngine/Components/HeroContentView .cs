namespace DominoMajlisPRO.GalleryEngine.Components;

public class HeroContentView : ContentView
{
    private readonly Label _badge;
    private readonly Label _title;
    private readonly Label _chapter;
    private readonly Label _description;
    private readonly PremiumButton _button;
    private readonly VerticalStackLayout _layout;

    public HeroContentView()
    {
        _badge = new Label
        {
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD76A"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _title = new Label
        {
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFE8A3"),
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 2
        };

        _chapter = new Label
        {
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _description = new Label
        {
            TextColor = Color.FromArgb("#F2DFA8"),
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 3
        };

        _button = new PremiumButton
        {
            Text = "استكشف الموسم",
            HorizontalOptions = LayoutOptions.Start
        };

        _layout = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                _badge,
                _title,
                _chapter,
                _description,
                _button
            }
        };

        Content = _layout;

        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveTypography(Width);
    }

    private void ApplyResponsiveTypography(double width)
    {
        var phone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        _badge.FontSize = phone ? 12 : 15;
        _title.FontSize = phone ? 28 : 42;
        _chapter.FontSize = phone ? 16 : 22;
        _description.FontSize = phone ? 13 : 17;

        _layout.Spacing = phone ? 8 : 12;

        _button.WidthRequest = phone ? 150 : 210;
        _button.HeightRequest = phone ? 44 : 54;
    }

    public void Bind(
        string badge,
        string title,
        string chapter,
        string description,
        string buttonText)
    {
        _badge.Text = badge;
        _title.Text = title;
        _chapter.Text = chapter;
        _description.Text = description;
        _button.Text = buttonText;

        ApplyResponsiveTypography(Width);
    }

    public PremiumButton Button => _button;
}
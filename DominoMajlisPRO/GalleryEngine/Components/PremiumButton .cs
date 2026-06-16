using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public class PremiumButton : ContentView
{
    private readonly Label _label;

    public event EventHandler? Clicked;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(PremiumButton),
            "استكشف",
            propertyChanged: OnTextChanged);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public PremiumButton()
    {
        _label = new Label
        {
            Text = Text,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1206"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var border = new Border
        {
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#F7D06A"),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 18
            },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#FFE08A"), 0.0f),
                    new GradientStop(Color.FromArgb("#B8860B"), 1.0f)
                }
            },
            Padding = new Thickness(18, 10),
            Content = _label
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        border.GestureRecognizers.Add(tap);

        Content = border;
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PremiumButton button && newValue is string text)
        {
            button._label.Text = text;
        }
    }
}
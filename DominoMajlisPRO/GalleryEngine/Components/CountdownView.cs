using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public class CountdownView : ContentView
{
    private readonly Label _countdownLabel;

    public CountdownView()
    {
        _countdownLabel = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD76A"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = new Border
        {
            Padding = new Thickness(12, 6),
            Stroke = Color.FromArgb("#8A6A2A"),
            StrokeThickness = 1,
            Background = Color.FromArgb("#1A1A1A"),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 14
            },
            Content = _countdownLabel
        };
    }

    public void SetText(string text)
    {
        _countdownLabel.Text = text;
    }
}
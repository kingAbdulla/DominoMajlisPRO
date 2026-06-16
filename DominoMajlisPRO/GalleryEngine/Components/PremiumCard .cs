using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public class PremiumCard : Border
{
    public PremiumCard()
    {
        StrokeShape = new RoundRectangle
        {
            CornerRadius = 22
        };

        Stroke = Color.FromArgb("#6E5525");
        StrokeThickness = 1;

        Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb("#242424"), 0),
                new GradientStop(Color.FromArgb("#171717"), 1)
            },
            new Point(0, 0),
            new Point(1, 1));

        Padding = 18;

        Shadow = new Shadow
        {
            Brush = Brush.Black,
            Offset = new Point(0, 8),
            Radius = 20,
            Opacity = 0.35f
        };
    }
}
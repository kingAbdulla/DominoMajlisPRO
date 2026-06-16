using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.Services;

public class TrustRingDrawable : IDrawable
{
    public float Percentage { get; set; }

    public Color RingColor { get; set; } =
        Colors.Green;

    public void Draw(
        ICanvas canvas,
        RectF dirtyRect)
    {
        float stroke = 12;

        float size =
            Math.Min(
                dirtyRect.Width,
                dirtyRect.Height)
            - stroke;

        float x =
            (dirtyRect.Width - size) / 2;

        float y =
            (dirtyRect.Height - size) / 2;

      

        // الحلقة الفعلية

        canvas.StrokeColor =
            RingColor;

        canvas.StrokeSize =
            stroke;

        canvas.DrawArc(
            x,
            y,
            size,
            size,
            270,
            270 + (360f * Percentage / 100f),
            false,
            false);
    }
}
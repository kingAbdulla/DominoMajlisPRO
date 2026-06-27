using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class LivingEmblemLivePreviewView : GraphicsView, IDrawable, IDisposable
{
    private bool _running;
    private double _time;

    public LivingEmblemLivePreviewView()
    {
        Drawable = this;
        HeightRequest = 220;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        BackgroundColor = Colors.Transparent;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        var cx = w / 2f;
        var cy = h / 2f + 10f;
        var size = MathF.Min(w, h) * 0.62f;
        var pulse = (float)((Math.Sin(_time * 2.2) + 1.0) * 0.5);
        var yaw = (float)Math.Sin(_time * 0.85) * 0.16f;
        var floatY = (float)Math.Sin(_time * 1.4) * 5f;

        canvas.SaveState();
        canvas.Translate(cx, cy + floatY);
        canvas.Scale(1f + yaw, 1f - Math.Abs(yaw) * 0.08f);

        DrawAura(canvas, size, pulse);
        DrawWings(canvas, size, pulse);
        DrawShield(canvas, size, pulse);
        DrawCrown(canvas, size, pulse);
        DrawLivingCore(canvas, size, pulse);

        canvas.RestoreState();

        DrawOrbitSparks(canvas, cx, cy, size, pulse);
    }

    public void Dispose()
    {
        _running = false;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (_running)
            return;

        _running = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(33), () =>
        {
            if (!_running)
                return false;

            _time += 0.033;
            Invalidate();
            return true;
        });
    }

    private void OnUnloaded(object? sender, EventArgs e) => _running = false;

    private static void DrawAura(ICanvas canvas, float size, float pulse)
    {
        canvas.StrokeSize = 3f + pulse * 2f;
        canvas.StrokeColor = Color.FromRgba(255, 210, 65, (int)(70 + pulse * 70));
        canvas.DrawEllipse(-size * 0.62f, -size * 0.58f, size * 1.24f, size * 1.1f);

        canvas.StrokeSize = 1f;
        canvas.StrokeColor = Color.FromRgba(255, 155, 10, 70);
        canvas.DrawEllipse(-size * 0.72f, -size * 0.66f, size * 1.44f, size * 1.24f);
    }

    private static void DrawWings(ICanvas canvas, float size, float pulse)
    {
        canvas.FillColor = Color.FromArgb("#D4AF37");
        canvas.StrokeColor = Color.FromArgb("#5A3A06");
        canvas.StrokeSize = 1.5f;

        for (var side = -1; side <= 1; side += 2)
        {
            for (var i = 0; i < 5; i++)
            {
                var y = -size * 0.28f + i * size * 0.105f;
                var outer = size * (0.52f + i * 0.03f + pulse * 0.015f);
                var inner = size * 0.22f;
                var path = new PathF();
                path.MoveTo(side * inner, y);
                path.CurveTo(side * outer, y - size * 0.12f, side * outer, y + size * 0.06f, side * inner, y + size * 0.13f);
                path.CurveTo(side * (inner + size * 0.08f), y + size * 0.06f, side * (inner + size * 0.08f), y - size * 0.02f, side * inner, y);
                path.Close();
                canvas.FillPath(path);
                canvas.DrawPath(path);
            }
        }
    }

    private static void DrawShield(ICanvas canvas, float size, float pulse)
    {
        var path = new PathF();
        path.MoveTo(0, -size * 0.48f);
        path.LineTo(size * 0.36f, -size * 0.33f);
        path.LineTo(size * 0.32f, size * 0.18f);
        path.LineTo(0, size * 0.52f);
        path.LineTo(-size * 0.32f, size * 0.18f);
        path.LineTo(-size * 0.36f, -size * 0.33f);
        path.Close();

        canvas.FillColor = Color.FromArgb("#151515");
        canvas.FillPath(path);
        canvas.StrokeSize = 6f;
        canvas.StrokeColor = Color.FromArgb("#FFD966");
        canvas.DrawPath(path);
        canvas.StrokeSize = 2f;
        canvas.StrokeColor = Color.FromRgba(255, 255, 210, (int)(110 + pulse * 90));
        canvas.DrawPath(path);

        canvas.StrokeSize = 1f;
        canvas.StrokeColor = Color.FromArgb("#5F5F5F");
        canvas.DrawLine(0, -size * 0.42f, 0, size * 0.4f);
    }

    private static void DrawCrown(ICanvas canvas, float size, float pulse)
    {
        var crown = new PathF();
        crown.MoveTo(-size * 0.22f, -size * 0.55f);
        crown.LineTo(-size * 0.11f, -size * 0.68f);
        crown.LineTo(0, -size * 0.53f);
        crown.LineTo(size * 0.11f, -size * 0.68f);
        crown.LineTo(size * 0.22f, -size * 0.55f);
        crown.LineTo(size * 0.20f, -size * 0.48f);
        crown.LineTo(-size * 0.20f, -size * 0.48f);
        crown.Close();
        canvas.FillColor = Color.FromArgb("#FFD966");
        canvas.FillPath(crown);
        canvas.StrokeSize = 1.5f;
        canvas.StrokeColor = Color.FromRgba(255, 255, 230, (int)(140 + pulse * 80));
        canvas.DrawPath(crown);
    }

    private static void DrawLivingCore(ICanvas canvas, float size, float pulse)
    {
        var r = size * (0.07f + pulse * 0.025f);
        canvas.FillColor = Color.FromRgba(255, 98, 0, 190);
        canvas.FillEllipse(-r, -size * 0.04f - r, r * 2, r * 2);
        canvas.StrokeColor = Color.FromRgba(255, 220, 40, 210);
        canvas.StrokeSize = 2f;
        canvas.DrawEllipse(-r, -size * 0.04f - r, r * 2, r * 2);
    }

    private void DrawOrbitSparks(ICanvas canvas, float cx, float cy, float size, float pulse)
    {
        canvas.FillColor = Color.FromRgba(255, 220, 60, 180);
        for (var i = 0; i < 10; i++)
        {
            var a = _time * 1.4 + i * Math.PI * 0.2;
            var rx = size * (0.58f + 0.05f * (float)Math.Sin(_time + i));
            var ry = size * 0.42f;
            var x = cx + (float)Math.Cos(a) * rx;
            var y = cy + (float)Math.Sin(a) * ry;
            var s = 2.0f + pulse * 2.5f;
            canvas.FillEllipse(x - s / 2f, y - s / 2f, s, s);
        }
    }
}

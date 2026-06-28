using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class LivingVisualPulseOverlay : GraphicsView, IDrawable
{
    private IDispatcherTimer? _timer;
    private DateTimeOffset _startedAt;

    public LivingVisualPulseOverlay()
    {
        Drawable = this;
        InputTransparent = true;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        BackgroundColor = Colors.Transparent;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width <= 1 || dirtyRect.Height <= 1)
            return;

        var seconds = (DateTimeOffset.UtcNow - _startedAt).TotalSeconds;
        var cycle = seconds % 3.2;
        if (cycle > 1.2)
            return;

        var amount = (float)(1.0 - cycle / 1.2);
        var x = dirtyRect.Center.X;
        var y = dirtyRect.Top + dirtyRect.Height * 0.58f;
        var size = MathF.Min(dirtyRect.Width, dirtyRect.Height);

        canvas.Alpha = 0.35f * amount;
        canvas.FillColor = Color.FromArgb("#FF8A00");
        canvas.FillEllipse(x - size * 0.08f, y - size * 0.30f, size * 0.16f, size * 0.32f);

        canvas.Alpha = 0.25f * amount;
        canvas.FillColor = Color.FromArgb("#D0D0D0");
        canvas.FillCircle(x - size * 0.08f, y - size * 0.20f, size * 0.05f);
        canvas.FillCircle(x + size * 0.09f, y - size * 0.25f, size * 0.06f);
        canvas.Alpha = 1f;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _startedAt = DateTimeOffset.UtcNow;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_timer == null)
            return;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e) => Invalidate();
}

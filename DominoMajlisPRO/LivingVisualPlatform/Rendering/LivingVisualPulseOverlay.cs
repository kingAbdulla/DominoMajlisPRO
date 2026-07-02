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

        var seconds = (float)(DateTimeOffset.UtcNow - _startedAt).TotalSeconds;
        var cycle = seconds % 3.4f;
        if (cycle > 1.35f)
            return;

        var progress = cycle / 1.35f;
        var amount = 1f - progress;
        var x = dirtyRect.Center.X;
        var y = dirtyRect.Top + dirtyRect.Height * 0.705f;
        var size = MathF.Min((float)dirtyRect.Width, (float)dirtyRect.Height);
        var length = size * (0.22f + 0.08f * amount);
        var width = size * (0.13f + 0.035f * amount);
        var wave = MathF.Sin(seconds * 24f) * size * 0.018f;

        var outer = new PathF();
        outer.MoveTo(x - width * 0.44f, y - width * 0.10f);
        outer.CurveTo(x - width * 0.95f, y + length * 0.22f, x - width * 0.25f, y + length * 0.68f, x + wave, y + length);
        outer.CurveTo(x + width * 0.55f, y + length * 0.62f, x + width * 0.92f, y + length * 0.18f, x + width * 0.42f, y - width * 0.10f);
        outer.Close();

        canvas.Alpha = 0.78f * amount;
        canvas.FillColor = Color.FromArgb("#FF3A00");
        canvas.FillPath(outer);

        var inner = new PathF();
        inner.MoveTo(x - width * 0.18f, y - width * 0.06f);
        inner.CurveTo(x - width * 0.42f, y + length * 0.18f, x - width * 0.10f, y + length * 0.46f, x + wave * 0.45f, y + length * 0.70f);
        inner.CurveTo(x + width * 0.28f, y + length * 0.42f, x + width * 0.38f, y + length * 0.12f, x + width * 0.16f, y - width * 0.06f);
        inner.Close();

        canvas.Alpha = 0.92f * amount;
        canvas.FillColor = Color.FromArgb("#FFD75A");
        canvas.FillPath(inner);

        var smokeAmount = Math.Clamp(progress * 1.4f, 0f, 1f) * amount;
        canvas.FillColor = Color.FromArgb("#BDBDBD");
        for (var i = 0; i < 4; i++)
        {
            var side = (i - 1.5f) * size * 0.045f;
            var driftDown = size * (0.12f + i * 0.025f) * progress;
            var radius = size * (0.028f + i * 0.007f) * (0.8f + progress);
            canvas.Alpha = 0.26f * smokeAmount;
            canvas.FillCircle(x + side + wave * 0.4f, y + driftDown, radius);
        }

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

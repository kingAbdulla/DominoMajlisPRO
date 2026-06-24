using System.Runtime.CompilerServices;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record IdentityEffectRenderProfile(
    string AssetId,
    EffectPresetId PresetId,
    EffectAnimationId AnimationId,
    Color PrimaryColor,
    Color SecondaryColor,
    IReadOnlyList<EffectLayerId> Layers,
    float Opacity,
    float Scale,
    float Speed,
    float Intensity,
    uint Duration,
    bool UseLegacyImage,
    string LegacyImagePath)
{
    public static IdentityEffectRenderProfile From(CatalogAssetDisplay effect, double baseScale = 1.18)
    {
        var definition = PlayerEffectEngine.CreateDefinition(effect, baseScale);
        var render = PlayerEffectEngine.CreateRenderProfile(definition);
        return new(
            effect.AssetId,
            definition.PresetId,
            definition.AnimationId,
            render.PrimaryColor,
            render.SecondaryColor,
            definition.Layers,
            (float)render.Opacity,
            (float)render.Scale,
            (float)Math.Clamp(definition.Speed, 0.10, 4.00),
            (float)Math.Clamp(definition.Intensity, 0.10, 3.00),
            render.Duration,
            render.UseLegacyImage,
            render.LegacyImagePath);
    }
}

public sealed class IdentityEffectView : GraphicsView
{
    private readonly IdentityEffectDrawable _drawable = new();
    private bool _running;
    private long _started;

    public IdentityEffectView()
    {
        Drawable = _drawable;
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => _running = false;
    }

    public string EffectKey { get; private set; } = string.Empty;

    public void SetEffect(IdentityEffectRenderProfile profile, double baseScale = 1.18, bool lightweight = false)
    {
        var key = $"{profile.AssetId}|{profile.PresetId}|{profile.AnimationId}|{profile.PrimaryColor}|{profile.SecondaryColor}|{profile.Scale}|{profile.Speed}|{profile.Intensity}|{baseScale}|{lightweight}";
        if (EffectKey == key && IsVisible)
            return;

        EffectKey = key;
        _drawable.Profile = profile;
        _drawable.BaseScale = (float)baseScale;
        _drawable.Lightweight = lightweight;
        _drawable.ElapsedSeconds = 0;
        _drawable.Phase = 0;
        _started = Environment.TickCount64;
        IsVisible = true;
        Start();
        Invalidate();
    }

    public void Clear()
    {
        _running = false;
        EffectKey = string.Empty;
        _drawable.Profile = null;
        _drawable.ElapsedSeconds = 0;
        _drawable.Phase = 0;
        IsVisible = false;
        Invalidate();
    }

    private void Start()
    {
        if (_running || _drawable.Profile == null || !IsLoaded)
            return;

        _running = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(_drawable.Lightweight ? 66 : 33), () =>
        {
            if (!_running || _drawable.Profile == null || !IsLoaded)
                return false;

            var elapsed = (Environment.TickCount64 - _started) / 1000f;
            _drawable.ElapsedSeconds = elapsed;
            _drawable.Phase = elapsed * _drawable.Profile.Speed;
            Invalidate();
            return true;
        });
    }
}

internal sealed class IdentityEffectDrawable : IDrawable
{
    public IdentityEffectRenderProfile? Profile { get; set; }
    public float Phase { get; set; }
    public float ElapsedSeconds { get; set; }
    public float BaseScale { get; set; } = 1.18f;
    public bool Lightweight { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var profile = Profile;
        if (profile == null || dirtyRect.Width <= 1 || dirtyRect.Height <= 1)
            return;

        canvas.SaveState();
        canvas.Alpha = Math.Clamp(profile.Opacity, 0.08f, 1.0f);

        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.34f * BaseScale * profile.Scale;
        var pulse = 0.72f + 0.28f * MathF.Abs(MathF.Sin(Phase * 2.2f));
        var intensity = Math.Clamp(profile.Intensity, 0.35f, 3.0f);

        DrawAura(canvas, cx, cy, radius * (0.98f + 0.10f * pulse), profile.PrimaryColor, 0.18f * intensity);
        DrawRing(canvas, cx, cy, radius * (1.02f + 0.08f * pulse), profile.SecondaryColor, 2.0f + intensity);

        switch (profile.PresetId)
        {
            case EffectPresetId.Fire:
                DrawFire(canvas, cx, cy, radius, profile, intensity);
                break;
            case EffectPresetId.Lightning:
                DrawLightning(canvas, cx, cy, radius, profile, intensity);
                break;
            case EffectPresetId.Ice:
            case EffectPresetId.Diamond:
                DrawSparkles(canvas, cx, cy, radius, profile.SecondaryColor, 10, 1.4f + intensity);
                break;
            case EffectPresetId.Royal:
                DrawSparkles(canvas, cx, cy, radius, profile.PrimaryColor, 16, 1.7f + intensity);
                DrawCrownPulse(canvas, cx, cy, radius, profile.SecondaryColor, intensity);
                break;
            case EffectPresetId.Shadow:
                DrawAura(canvas, cx, cy, radius * 0.95f, Colors.Black, 0.34f);
                DrawSparkles(canvas, cx, cy, radius, profile.PrimaryColor, 8, 1.0f + intensity);
                break;
            default:
                DrawSparkles(canvas, cx, cy, radius, profile.PrimaryColor, Lightweight ? 8 : 14, 1.2f + intensity);
                break;
        }

        canvas.RestoreState();
    }

    private static void DrawAura(ICanvas canvas, float cx, float cy, float radius, Color color, float alpha)
    {
        canvas.FillColor = color.WithAlpha(Math.Clamp(alpha, 0.02f, 0.75f));
        canvas.FillCircle(cx, cy, radius);
        canvas.FillColor = color.WithAlpha(Math.Clamp(alpha * 0.45f, 0.02f, 0.45f));
        canvas.FillCircle(cx, cy, radius * 1.22f);
    }

    private static void DrawRing(ICanvas canvas, float cx, float cy, float radius, Color color, float strokeSize)
    {
        canvas.StrokeColor = color.WithAlpha(0.90f);
        canvas.StrokeSize = strokeSize;
        canvas.DrawCircle(cx, cy, radius);
        canvas.StrokeColor = color.WithAlpha(0.32f);
        canvas.StrokeSize = Math.Max(1f, strokeSize * 2.1f);
        canvas.DrawCircle(cx, cy, radius * 1.07f);
    }

    private void DrawFire(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var count = Lightweight ? 10 : 18;
        for (var i = 0; i < count; i++)
        {
            var angle = (i / (float)count * MathF.Tau) + MathF.Sin(Phase * 3.1f + i) * 0.15f;
            var inner = PointOn(cx, cy, radius * 0.72f, angle);
            var outer = PointOn(cx, cy - radius * 0.10f, radius * (1.18f + 0.12f * MathF.Sin(Phase * 5.0f + i)), angle);
            canvas.StrokeColor = (i % 2 == 0 ? profile.PrimaryColor : profile.SecondaryColor).WithAlpha(0.62f);
            canvas.StrokeSize = 2.2f * intensity;
            canvas.DrawLine(inner, outer);
            canvas.FillColor = profile.SecondaryColor.WithAlpha(0.76f);
            canvas.FillCircle(outer, 1.8f * intensity);
        }
    }

    private void DrawLightning(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        if (((int)(Phase * 10f)) % 5 == 0)
            return;

        var count = Lightweight ? 4 : 7;
        for (var i = 0; i < count; i++)
        {
            var angle = (i / (float)count * MathF.Tau) + Phase * 0.32f;
            var a = PointOn(cx, cy, radius * 0.68f, angle);
            var b = PointOn(cx, cy, radius * 1.30f, angle + 0.12f * MathF.Sin(i + Phase));
            var mid = PointOn((a.X + b.X) / 2f, (a.Y + b.Y) / 2f, radius * 0.12f, angle + MathF.PI / 2f);
            var path = new PathF();
            path.MoveTo(a.X, a.Y);
            path.LineTo(mid.X, mid.Y);
            path.LineTo(b.X, b.Y);
            canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.38f);
            canvas.StrokeSize = 4.5f * intensity;
            canvas.DrawPath(path);
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.96f);
            canvas.StrokeSize = 1.4f * intensity;
            canvas.DrawPath(path);
        }
    }

    private void DrawSparkles(ICanvas canvas, float cx, float cy, float radius, Color color, int count, float size)
    {
        for (var i = 0; i < count; i++)
        {
            var seed = i * 2.39996f;
            var angle = seed + Phase * (0.28f + (i % 3) * 0.08f);
            var distance = radius * (0.78f + (i % 5) * 0.11f);
            var point = PointOn(cx, cy, distance, angle);
            var alpha = 0.30f + 0.65f * MathF.Abs(MathF.Sin(Phase * 2.6f + seed));
            canvas.FillColor = color.WithAlpha(alpha);
            canvas.FillCircle(point, size * (0.55f + alpha));
        }
    }

    private void DrawCrownPulse(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity)
    {
        var top = cy - radius * 1.18f;
        canvas.StrokeColor = color.WithAlpha(0.78f);
        canvas.StrokeSize = 1.2f * intensity;
        canvas.DrawLine(cx - radius * 0.24f, top + radius * 0.18f, cx, top);
        canvas.DrawLine(cx, top, cx + radius * 0.24f, top + radius * 0.18f);
    }

    private static PointF PointOn(float cx, float cy, float radius, float angle) =>
        new(cx + MathF.Cos(angle) * radius, cy + MathF.Sin(angle) * radius);
}

public static class IdentityEffectRenderer
{
    private sealed class Holder
    {
        public IdentityEffectView? View;
        public EventHandler? LoadedHandler;
    }

    private static readonly ConditionalWeakTable<Image, Holder> Views = new();
    private static readonly ConditionalWeakTable<Image, Holder> AroundViews = new();

    public static void Apply(Image slot, CatalogAssetDisplay? effect, double baseScale = 1.18, bool lightweight = false)
    {
        if (effect == null)
        {
            Clear(slot);
            return;
        }

        var holder = Views.GetOrCreateValue(slot);
        if (TryAttach(slot, holder, 1.28, out var view))
        {
            view.ZIndex = Math.Max(slot.ZIndex + 1, 2);
            view.SetEffect(IdentityEffectRenderProfile.From(effect, baseScale), baseScale, lightweight);
            slot.IsVisible = false;
        }
        else if (holder.LoadedHandler == null)
        {
            holder.LoadedHandler = (_, _) => Apply(slot, effect, baseScale, lightweight);
            slot.Loaded += holder.LoadedHandler;
        }
    }

    public static void Clear(Image slot)
    {
        if (!Views.TryGetValue(slot, out var holder))
            return;
        if (holder.LoadedHandler != null)
        {
            slot.Loaded -= holder.LoadedHandler;
            holder.LoadedHandler = null;
        }
        holder.View?.Clear();
        if (holder.View?.Parent is Layout layout)
            layout.Children.Remove(holder.View);
        holder.View = null;
        slot.IsVisible = false;
    }

    public static IdentityEffectView Create(CatalogAssetDisplay effect, double baseScale = 1.18, bool lightweight = false)
    {
        var view = new IdentityEffectView
        {
            WidthRequest = 54,
            HeightRequest = 54,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            ZIndex = 2
        };
        view.SetEffect(IdentityEffectRenderProfile.From(effect, baseScale), baseScale, lightweight);
        return view;
    }

    public static void ApplyAround(Image emblem, CatalogAssetDisplay? effect, double baseScale = 1.18, bool lightweight = false)
    {
        var holder = AroundViews.GetOrCreateValue(emblem);
        if (effect == null)
        {
            holder.View?.Clear();
            if (holder.View?.Parent is Layout oldLayout)
                oldLayout.Children.Remove(holder.View);
            holder.View = null;
            return;
        }

        if (!TryAttach(emblem, holder, 1.36, out var view))
            return;

        view.ZIndex = Math.Max(0, emblem.ZIndex - 1);
        emblem.ZIndex = view.ZIndex + 1;
        view.SetEffect(IdentityEffectRenderProfile.From(effect, baseScale), baseScale, lightweight);
    }

    private static bool TryAttach(Image slot, Holder holder, double visualScale, out IdentityEffectView view)
    {
        if (holder.View?.Parent != null)
        {
            view = holder.View;
            ApplyScaledSize(slot, view, visualScale);
            return true;
        }

        if (slot.Parent is not Layout parent)
        {
            view = null!;
            return false;
        }

        view = holder.View = new IdentityEffectView
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = slot.Margin,
            ZIndex = Math.Max(slot.ZIndex, 1),
            BackgroundColor = Colors.Transparent,
            InputTransparent = true
        };

        ApplyScaledSize(slot, view, visualScale);

        if (parent is Grid)
        {
            Grid.SetRow(view, Grid.GetRow(slot));
            Grid.SetColumn(view, Grid.GetColumn(slot));
            Grid.SetRowSpan(view, Grid.GetRowSpan(slot));
            Grid.SetColumnSpan(view, Grid.GetColumnSpan(slot));
        }

        parent.Children.Add(view);
        return true;
    }

    private static void ApplyScaledSize(Image source, IdentityEffectView view, double visualScale)
    {
        var width = source.WidthRequest > 0 ? source.WidthRequest : source.Width;
        var height = source.HeightRequest > 0 ? source.HeightRequest : source.Height;

        if (width <= 1)
            width = 64;
        if (height <= 1)
            height = 64;

        view.WidthRequest = width * visualScale;
        view.HeightRequest = height * visualScale;
        view.MinimumWidthRequest = view.WidthRequest;
        view.MinimumHeightRequest = view.HeightRequest;
        view.HorizontalOptions = LayoutOptions.Center;
        view.VerticalOptions = LayoutOptions.Center;
        view.InputTransparent = true;
        view.BackgroundColor = Colors.Transparent;
    }
}
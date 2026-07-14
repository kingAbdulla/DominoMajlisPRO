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
    private IDisposable? _clockSubscription;
    private double _clockEpoch = -1;
    private int _frame;

    public IdentityEffectView()
    {
        Drawable = _drawable;
        InputTransparent = true;
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => Stop();
    }

    public string EffectKey { get; private set; } = string.Empty;

    public void SetEffect(IdentityEffectRenderProfile profile, double baseScale = 1.18, bool lightweight = false)
    {
        var key = $"{profile.AssetId}|{profile.PresetId}|{profile.AnimationId}|{profile.PrimaryColor}|{profile.SecondaryColor}|{profile.Opacity}|{profile.Scale}|{profile.Speed}|{profile.Intensity}|{profile.Duration}|{baseScale}|{lightweight}";
        if (EffectKey == key)
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
        Stop();
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
        _clockEpoch = -1;
        _frame = 0;
        _clockSubscription = SharedAnimationClock.Subscribe(OnAnimationFrame);
    }

    private void OnAnimationFrame(double elapsed)
    {
        if (!_running || _drawable.Profile == null || !IsLoaded)
        {
            Stop();
            return;
        }
        if (_clockEpoch < 0) _clockEpoch = elapsed;
        if (_drawable.Lightweight && (++_frame & 1) == 1) return;
        var local = (float)(elapsed - _clockEpoch);
        _drawable.ElapsedSeconds = local;
        _drawable.Phase = local * _drawable.Profile.Speed;
        Invalidate();
    }

    private void Stop()
    {
        _running = false;
        _clockSubscription?.Dispose();
        _clockSubscription = null;
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
        canvas.Alpha = Math.Clamp(profile.Opacity, 0.05f, 1.0f);

        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var baseRadius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.30f * BaseScale * profile.Scale;
        var intro = ResolveIntroProgress(profile);
        var radius = baseRadius * (0.68f + 0.32f * EaseOutCubic(intro));

        DrawSignatureIntro(canvas, cx, cy, baseRadius, intro, profile);

        switch (profile.PresetId)
        {
            case EffectPresetId.Fire:
                DrawFire(canvas, cx, cy, radius, profile);
                break;
            case EffectPresetId.Lightning:
                DrawLightning(canvas, cx, cy, radius, profile);
                break;
            case EffectPresetId.Ice:
                DrawIce(canvas, cx, cy, radius, profile);
                break;
            case EffectPresetId.Royal:
            case EffectPresetId.Diamond:
                DrawRoyal(canvas, cx, cy, radius, profile);
                break;
            case EffectPresetId.Shadow:
                DrawShadow(canvas, cx, cy, radius, profile);
                break;
            default:
                DrawSimple(canvas, cx, cy, radius, profile);
                break;
        }

        canvas.RestoreState();
    }

    private float ResolveIntroProgress(IdentityEffectRenderProfile profile)
    {
        var duration = profile.PresetId switch
        {
            EffectPresetId.Lightning => 0.30f,
            EffectPresetId.Fire => 0.72f,
            EffectPresetId.Ice => 0.82f,
            EffectPresetId.Royal or EffectPresetId.Diamond => 0.95f,
            _ => 0.52f
        };
        return Math.Clamp(ElapsedSeconds / duration, 0f, 1f);
    }

    private static float EaseOutCubic(float value)
    {
        var t = Math.Clamp(value, 0f, 1f) - 1f;
        return (t * t * t) + 1f;
    }

    private void DrawSignatureIntro(ICanvas canvas, float cx, float cy, float radius, float intro, IdentityEffectRenderProfile profile)
    {
        if (intro >= 1f)
            return;

        var alpha = 1f - intro;
        var burst = 0.35f + 0.95f * EaseOutCubic(intro);
        switch (profile.PresetId)
        {
            case EffectPresetId.Fire:
                canvas.FillColor = profile.PrimaryColor.WithAlpha(0.20f * alpha);
                canvas.FillCircle(cx, cy + radius * (0.50f - intro), radius * burst);
                break;
            case EffectPresetId.Lightning:
                canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.90f * alpha);
                canvas.StrokeSize = 2.8f * profile.Intensity;
                DrawLightningBolt(canvas, cx, cy - radius * 1.1f, cx, cy + radius * 1.1f, radius * 0.16f);
                break;
            case EffectPresetId.Ice:
                canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.72f * alpha);
                canvas.StrokeSize = 1.2f;
                for (var i = 0; i < 10; i++)
                {
                    var angle = i / 10f * MathF.Tau;
                    canvas.DrawLine(PointOn(cx, cy, radius * 0.35f * burst, angle), PointOn(cx, cy, radius * 1.18f * burst, angle));
                }
                break;
            default:
                canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.55f * alpha);
                canvas.StrokeSize = 4f;
                canvas.DrawCircle(cx, cy, radius * burst);
                break;
        }
    }

    private void DrawFire(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        var count = Lightweight ? 10 : 18;
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.14f);
        for (var i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathF.Tau + MathF.Sin(Phase * 2.7f + i) * 0.10f;
            var flicker = 0.72f + 0.28f * MathF.Sin(Phase * 6.8f + i * 1.91f);
            var inner = PointOn(cx, cy, radius * 0.72f, angle);
            var outer = PointOn(cx, cy - radius * 0.14f, radius * (1.05f + 0.28f * flicker), angle);
            var sideA = PointOn(cx, cy, radius * 0.94f, angle + 0.14f);
            var sideB = PointOn(cx, cy, radius * 0.94f, angle - 0.14f);
            var path = new PathF();
            path.MoveTo(inner.X, inner.Y);
            path.QuadTo(sideA.X, sideA.Y, outer.X, outer.Y);
            path.QuadTo(sideB.X, sideB.Y, inner.X, inner.Y);
            path.Close();
            canvas.FillColor = (i % 3 == 0 ? profile.SecondaryColor : profile.PrimaryColor).WithAlpha((0.50f + 0.30f * flicker) * profile.Intensity / 1.4f);
            canvas.FillPath(path);
        }
        DrawParticles(canvas, cx, cy, radius, profile.SecondaryColor, profile, upward: true);
    }

    private void DrawLightning(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        if (((int)(Phase * 9f)) % 6 == 0)
            return;
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.10f);
        var count = Lightweight ? 3 : 7;
        for (var i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathF.Tau + Phase * 0.36f;
            var start = PointOn(cx, cy, radius * 0.70f, angle);
            var end = PointOn(cx, cy, radius * 1.28f, angle + 0.08f * MathF.Sin(i + Phase * 8.7f));
            canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.34f);
            canvas.StrokeSize = 5.2f * profile.Intensity;
            DrawLightningBolt(canvas, start.X, start.Y, end.X, end.Y, radius * 0.12f);
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.96f);
            canvas.StrokeSize = 1.4f * profile.Intensity;
            DrawLightningBolt(canvas, start.X, start.Y, end.X, end.Y, radius * 0.10f);
        }
    }

    private void DrawIce(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.12f);
        var count = Lightweight ? 9 : 16;
        for (var i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathF.Tau + Phase * 0.18f;
            var center = PointOn(cx, cy, radius * (1.02f + 0.08f * MathF.Sin(i + Phase)), angle);
            var length = radius * (0.15f + 0.08f * (i % 3) / 2f);
            var tip = PointOn(center.X, center.Y, length, angle);
            canvas.StrokeColor = (i % 3 == 0 ? profile.SecondaryColor : profile.PrimaryColor).WithAlpha(0.90f);
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(center, tip);
            canvas.DrawLine(PointOn(center.X, center.Y, length * 0.45f, angle), PointOn(center.X, center.Y, length * 0.45f, angle + 2.35f));
            canvas.DrawLine(PointOn(center.X, center.Y, length * 0.45f, angle), PointOn(center.X, center.Y, length * 0.45f, angle - 2.35f));
            canvas.FillColor = profile.SecondaryColor.WithAlpha(0.55f);
            canvas.FillCircle(center, i % 4 == 0 ? 2.5f : 1.2f);
        }
    }

    private void DrawRoyal(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.18f);
        var count = Lightweight ? 12 : 24;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 2.39996f;
            var angle = seed + Phase * (0.30f + (i % 4) * 0.08f);
            var point = PointOn(cx, cy, radius * (0.82f + (i % 5) * 0.10f), angle);
            var pulse = 0.25f + 0.75f * MathF.Abs(MathF.Sin(Phase * 3.2f + seed));
            canvas.FillColor = (i % 4 == 0 ? profile.SecondaryColor : profile.PrimaryColor).WithAlpha(0.42f + 0.46f * pulse);
            canvas.FillCircle(point, (1.1f + 2.1f * pulse) * profile.Intensity);
            if (!Lightweight && pulse > 0.72f)
            {
                canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.70f);
                canvas.StrokeSize = 1;
                canvas.DrawLine(point.X - 5, point.Y, point.X + 5, point.Y);
                canvas.DrawLine(point.X, point.Y - 5, point.X, point.Y + 5);
            }
        }
    }

    private void DrawShadow(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.22f);
        canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.50f);
        canvas.StrokeSize = 3f;
        canvas.DrawCircle(cx, cy, radius * (0.92f + 0.04f * MathF.Sin(Phase * 2.0f)));
    }

    private void DrawSimple(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile)
    {
        DrawAura(canvas, cx, cy, radius, profile.PrimaryColor, 0.10f);
        canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.56f);
        canvas.StrokeSize = 2.4f;
        canvas.DrawCircle(cx, cy, radius * (1.00f + 0.05f * MathF.Sin(Phase * 2.2f)));
    }

    private void DrawAura(ICanvas canvas, float cx, float cy, float radius, Color color, float alpha)
    {
        canvas.FillColor = color.WithAlpha(alpha);
        canvas.FillCircle(cx, cy, radius * 1.45f);
    }

    private void DrawParticles(ICanvas canvas, float cx, float cy, float radius, Color color, IdentityEffectRenderProfile profile, bool upward)
    {
        var count = Lightweight ? 5 : 10;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 1.73f;
            var progress = (Phase * (0.16f + (i % 3) * 0.03f) + i * 0.31f) % 1f;
            var angle = seed + MathF.Sin(Phase * 0.8f + i) * 0.28f;
            var distance = radius * (0.60f + progress * 0.78f);
            var point = PointOn(cx, cy + (upward ? radius * 0.28f : 0), distance, angle);
            canvas.FillColor = color.WithAlpha((1f - progress) * 0.62f);
            canvas.FillCircle(point, 1.2f + 1.8f * (1f - progress));
        }
    }

    private static PointF PointOn(float cx, float cy, float r, float angle) =>
        new(cx + MathF.Cos(angle) * r, cy + MathF.Sin(angle) * r);

    private static void DrawLightningBolt(ICanvas canvas, float x1, float y1, float x2, float y2, float jitter)
    {
        var p0 = new PointF(x1, y1);
        var p3 = new PointF(x2, y2);
        var p1 = Lerp(p0, p3, 0.35f, MathF.Sin(PhaseSeed(x1, y1) * 12.7f) * jitter);
        var p2 = Lerp(p0, p3, 0.72f, MathF.Cos(PhaseSeed(x2, y2) * 8.9f) * jitter);
        canvas.DrawLine(p0, p1);
        canvas.DrawLine(p1, p2);
        canvas.DrawLine(p2, p3);
    }

    private static float PhaseSeed(float x, float y) =>
        MathF.Abs(MathF.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f);

    private static PointF Lerp(PointF a, PointF b, float t, float offset)
    {
        var x = a.X + (b.X - a.X) * t;
        var y = a.Y + (b.Y - a.Y) * t;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = MathF.Max(1f, MathF.Sqrt(dx * dx + dy * dy));
        return new(x - dy / length * offset, y + dx / length * offset);
    }
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
        if (TryAttach(slot, holder, 1.00, out var view))
        {
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
        var view = new IdentityEffectView();
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

        if (!TryAttach(emblem, holder, 1.02, out var view))
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
            HorizontalOptions = slot.HorizontalOptions,
            VerticalOptions = slot.VerticalOptions,
            Margin = slot.Margin,
            ZIndex = Math.Max(slot.ZIndex, 1),
            Clip = new EllipseGeometry()
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

        if (width > 0)
            view.WidthRequest = width * visualScale;
        if (height > 0)
            view.HeightRequest = height * visualScale;

        view.HorizontalOptions = LayoutOptions.Center;
        view.VerticalOptions = LayoutOptions.Center;
        view.InputTransparent = true;
    }
}

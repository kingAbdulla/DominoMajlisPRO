using System.Runtime.CompilerServices;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record EffectRenderProfile(
    string AssetId,
    string EffectType,
    string AnimationType,
    Color PrimaryColor,
    Color SecondaryColor,
    IReadOnlyList<string> EffectLayerIds,
    float Opacity,
    float Scale,
    float Speed,
    float Intensity,
    int DurationMilliseconds)
{
    public static EffectRenderProfile From(CatalogAssetDisplay effect)
    {
        var key = $"{effect.AssetId} {effect.DisplayName} {effect.EffectType}".ToLowerInvariant();
        return new(
            effect.AssetId,
            effect.EffectType,
            effect.AnimationType,
            Parse(effect.CustomPrimaryColorHex, effect.ColorHex,
                key.Contains("ice") ? "#79DFFF" : key.Contains("lightning") ? "#E8FBFF" :
                key.Contains("gold") ? "#FFD45A" : "#FF6B18"),
            Parse(effect.CustomSecondaryColorHex, string.Empty,
                key.Contains("ice") ? "#FFFFFF" : key.Contains("lightning") ? "#69CFFF" :
                key.Contains("gold") ? "#FFF1A8" : "#FFD04A"),
            effect.EffectLayerIds,
            Math.Clamp(effect.EffectOpacity <= 0 ? 1f : effect.EffectOpacity, .05f, 1f),
            Math.Clamp(effect.EffectScale <= 0 ? 1f : effect.EffectScale, .5f, 2f),
            Math.Clamp(effect.EffectSpeed <= 0 ? 1f : effect.EffectSpeed, .1f, 4f),
            Math.Clamp(effect.EffectIntensity <= 0 ? 1f : effect.EffectIntensity, .1f, 3f),
            effect.DurationMilliseconds);
    }

    private static Color Parse(string? preferred, string? fallback, string defaultHex)
    {
        foreach (var value in new[] { preferred, fallback, defaultHex })
        {
            var token = value?.Trim();
            if (!string.IsNullOrWhiteSpace(token) && token[0] == '#' &&
                (token.Length == 7 || token.Length == 9) && token[1..].All(Uri.IsHexDigit))
                return Color.FromArgb(token);
        }
        return Colors.Gold;
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
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => _running = false;
    }

    public string EffectKey { get; private set; } = string.Empty;

    public void SetEffect(EffectRenderProfile profile, double baseScale = 1.18, bool lightweight = false)
    {
        var key = $"{profile.AssetId}|{profile.EffectType}|{profile.AnimationType}|{baseScale}|{lightweight}";
        if (EffectKey == key)
            return;
        EffectKey = key;
        _drawable.Profile = profile;
        _drawable.BaseScale = (float)baseScale;
        _drawable.Lightweight = lightweight;
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
            _drawable.Phase = (Environment.TickCount64 - _started) / 1000f * _drawable.Profile.Speed;
            Invalidate();
            return true;
        });
    }
}

internal sealed class IdentityEffectDrawable : IDrawable
{
    public EffectRenderProfile? Profile { get; set; }
    public float Phase { get; set; }
    public float BaseScale { get; set; } = 1.18f;
    public bool Lightweight { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var p = Profile;
        if (p == null || dirtyRect.Width <= 1 || dirtyRect.Height <= 1)
            return;
        canvas.SaveState();
        canvas.Alpha = p.Opacity;
        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * .34f * BaseScale * p.Scale;
        var type = (p.EffectType ?? string.Empty).Trim().ToLowerInvariant();
        if (type.Contains("fire") || type.Contains("flame")) DrawFire(canvas, cx, cy, radius, p);
        else if (type.Contains("lightning") || type.Contains("bolt")) DrawLightning(canvas, cx, cy, radius, p);
        else if (type.Contains("ice") || type.Contains("crystal") || type.Contains("snow")) DrawIce(canvas, cx, cy, radius, p);
        else if (type.Contains("gold") || type.Contains("spark")) DrawGold(canvas, cx, cy, radius, p);
        else DrawSimple(canvas, cx, cy, radius, p, type);
        canvas.RestoreState();
    }

    private void DrawFire(ICanvas c, float cx, float cy, float r, EffectRenderProfile p)
    {
        var count = Lightweight ? 9 : 16;
        for (var i = 0; i < count; i++)
        {
            var a = i / (float)count * MathF.Tau + MathF.Sin(Phase * 3 + i) * .08f;
            var flicker = .72f + .28f * MathF.Sin(Phase * 7 + i * 1.91f);
            var inner = PointOn(cx, cy, r * .78f, a);
            var outer = PointOn(cx, cy - r * .12f, r * (1.08f + .26f * flicker), a);
            var side = PointOn(cx, cy, r * .92f, a + .13f);
            var other = PointOn(cx, cy, r * .91f, a - .13f);
            var path = new PathF();
            path.MoveTo(inner.X, inner.Y);
            path.QuadTo(side.X, side.Y, outer.X, outer.Y);
            path.QuadTo(other.X, other.Y, inner.X, inner.Y);
            path.Close();
            c.FillColor = (i % 3 == 0 ? p.SecondaryColor : p.PrimaryColor).WithAlpha(.72f);
            c.FillPath(path);
        }
    }

    private void DrawLightning(ICanvas c, float cx, float cy, float r, EffectRenderProfile p)
    {
        if (((int)(Phase * 8)) % 5 == 0) return;
        var count = Lightweight ? 3 : 6;
        for (var i = 0; i < count; i++)
        {
            var a = i / (float)count * MathF.Tau + Phase * .35f;
            var start = PointOn(cx, cy, r * .75f, a);
            var end = PointOn(cx, cy, r * 1.28f, a + .08f * MathF.Sin(i + Phase * 9));
            var m1 = Lerp(start, end, .34f, ((i & 1) == 0 ? 1 : -1) * r * .12f);
            var m2 = Lerp(start, end, .68f, ((i & 1) == 0 ? -1 : 1) * r * .09f);
            c.StrokeColor = p.PrimaryColor.WithAlpha(.35f); c.StrokeSize = 5f * p.Intensity;
            Lines(c, start, m1, m2, end);
            c.StrokeColor = p.SecondaryColor; c.StrokeSize = 1.4f * p.Intensity;
            Lines(c, start, m1, m2, end);
            if (!Lightweight) c.DrawLine(m2, PointOn(m2.X, m2.Y, r * .22f, a + .7f));
        }
    }

    private void DrawIce(ICanvas c, float cx, float cy, float r, EffectRenderProfile p)
    {
        var count = Lightweight ? 8 : 14;
        for (var i = 0; i < count; i++)
        {
            var a = i / (float)count * MathF.Tau + Phase * .22f;
            var center = PointOn(cx, cy, r * (1.02f + .08f * MathF.Sin(i + Phase)), a);
            var len = r * (.16f + .07f * (i % 3) / 2f);
            var tip = PointOn(center.X, center.Y, len, a);
            c.StrokeColor = (i % 3 == 0 ? p.SecondaryColor : p.PrimaryColor).WithAlpha(.9f);
            c.StrokeSize = 1.5f;
            c.DrawLine(center, tip);
            c.DrawLine(PointOn(center.X, center.Y, len * .45f, a), PointOn(center.X, center.Y, len * .45f, a + 2.35f));
            c.DrawLine(PointOn(center.X, center.Y, len * .45f, a), PointOn(center.X, center.Y, len * .45f, a - 2.35f));
            c.FillColor = p.SecondaryColor.WithAlpha(.55f);
            c.FillCircle(center, i % 4 == 0 ? 2.5f : 1.2f);
        }
    }

    private void DrawGold(ICanvas c, float cx, float cy, float r, EffectRenderProfile p)
    {
        var count = Lightweight ? 10 : 20;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 2.39996f;
            var a = seed + Phase * (.35f + (i % 4) * .08f);
            var pt = PointOn(cx, cy, r * (.84f + (i % 5) * .1f), a);
            var pulse = .25f + .75f * MathF.Abs(MathF.Sin(Phase * 3.2f + seed));
            c.FillColor = (i % 4 == 0 ? p.SecondaryColor : p.PrimaryColor).WithAlpha(.45f + .5f * pulse);
            c.FillCircle(pt, (1.2f + 2.2f * pulse) * p.Intensity);
            if (!Lightweight && pulse > .72f)
            {
                c.StrokeColor = p.SecondaryColor.WithAlpha(.7f); c.StrokeSize = 1;
                c.DrawLine(pt.X - 5, pt.Y, pt.X + 5, pt.Y);
                c.DrawLine(pt.X, pt.Y - 5, pt.X, pt.Y + 5);
            }
        }
    }

    private void DrawSimple(ICanvas c, float cx, float cy, float r, EffectRenderProfile p, string type)
    {
        var pulse = .92f + .08f * MathF.Sin(Phase * MathF.Tau);
        c.StrokeColor = p.PrimaryColor.WithAlpha(type.Contains("glow") ? .35f : .85f);
        c.StrokeSize = (type.Contains("aura") ? 10 : 5) * p.Intensity;
        c.DrawCircle(cx, cy, r * pulse);
        if (type.Contains("pulse") || type.Contains("breath"))
        {
            c.StrokeColor = p.SecondaryColor.WithAlpha(.35f); c.StrokeSize = 2;
            c.DrawCircle(cx, cy, r * (1.05f + .18f * ((MathF.Sin(Phase * 3) + 1) / 2)));
        }
    }

    private static void Lines(ICanvas c, PointF a, PointF b, PointF d, PointF e)
    { c.DrawLine(a, b); c.DrawLine(b, d); c.DrawLine(d, e); }
    private static PointF PointOn(float x, float y, float radius, float angle) =>
        new(x + MathF.Cos(angle) * radius, y + MathF.Sin(angle) * radius);
    private static PointF Lerp(PointF a, PointF b, float t, float offset)
    {
        var x = a.X + (b.X - a.X) * t; var y = a.Y + (b.Y - a.Y) * t;
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var length = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        return new(x - dy / length * offset, y + dx / length * offset);
    }
}

public static class IdentityEffectRenderer
{
    private sealed class Holder { public IdentityEffectView? View; public EventHandler? LoadedHandler; }
    private static readonly ConditionalWeakTable<Image, Holder> Views = new();
    private static readonly ConditionalWeakTable<Image, Holder> AroundViews = new();

    public static void Apply(Image slot, CatalogAssetDisplay? effect, double baseScale = 1.18, bool lightweight = false)
    {
        if (effect == null) { Clear(slot); return; }
        var holder = Views.GetOrCreateValue(slot);
        if (TryAttach(slot, holder, out var view))
        {
            view.SetEffect(EffectRenderProfile.From(effect), baseScale, lightweight);
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
        if (!Views.TryGetValue(slot, out var holder)) return;
        if (holder.LoadedHandler != null) { slot.Loaded -= holder.LoadedHandler; holder.LoadedHandler = null; }
        holder.View?.Clear();
        if (holder.View?.Parent is Grid grid) grid.Children.Remove(holder.View);
        holder.View = null;
        slot.IsVisible = false;
    }

    public static IdentityEffectView Create(CatalogAssetDisplay effect, double baseScale = 1.18, bool lightweight = false)
    {
        var view = new IdentityEffectView();
        view.SetEffect(EffectRenderProfile.From(effect), baseScale, lightweight);
        return view;
    }

    public static void ApplyAround(Image emblem, CatalogAssetDisplay? effect, double baseScale = 1.18, bool lightweight = false)
    {
        var holder = AroundViews.GetOrCreateValue(emblem);
        if (effect == null)
        {
            holder.View?.Clear();
            if (holder.View?.Parent is Grid oldGrid) oldGrid.Children.Remove(holder.View);
            holder.View = null;
            return;
        }
        if (!TryAttach(emblem, holder, out var view))
            return;
        view.ZIndex = emblem.ZIndex + 1;
        emblem.ZIndex = view.ZIndex + 1;
        view.SetEffect(EffectRenderProfile.From(effect), baseScale, lightweight);
    }

    private static bool TryAttach(Image slot, Holder holder, out IdentityEffectView view)
    {
        if (holder.View?.Parent != null) { view = holder.View; return true; }
        if (slot.Parent is not Grid parent) { view = null!; return false; }
        view = holder.View = new IdentityEffectView
        {
            HorizontalOptions = slot.HorizontalOptions, VerticalOptions = slot.VerticalOptions,
            WidthRequest = slot.WidthRequest, HeightRequest = slot.HeightRequest,
            Margin = slot.Margin, ZIndex = Math.Max(slot.ZIndex, 1)
        };
        Grid.SetRow(view, Grid.GetRow(slot)); Grid.SetColumn(view, Grid.GetColumn(slot));
        Grid.SetRowSpan(view, Grid.GetRowSpan(slot)); Grid.SetColumnSpan(view, Grid.GetColumnSpan(slot));
        parent.Children.Add(view);
        return true;
    }
}

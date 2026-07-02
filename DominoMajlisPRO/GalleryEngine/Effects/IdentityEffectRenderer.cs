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
    public string VisualKey { get; init; } = string.Empty;

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
            render.LegacyImagePath)
        {
            VisualKey = BuildVisualKey(effect)
        };
    }

    private static string BuildVisualKey(CatalogAssetDisplay effect) =>
        $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.AnimationType} {effect.PreviewImage}"
            .ToLowerInvariant();
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
        canvas.Alpha = Math.Clamp(profile.Opacity, 0.08f, 0.82f);

        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.34f * BaseScale * profile.Scale;
        var intensity = Math.Clamp(profile.Intensity, 0.35f, 3.0f);

        if (HasVisualToken(profile, "dragon", "fire", "flame"))
            DrawDragonBehavior(canvas, cx, cy, radius, profile, intensity);
        else if (HasVisualToken(profile, "lion", "roar"))
            DrawLionBehavior(canvas, cx, cy, radius, profile, intensity);
        else if (HasVisualToken(profile, "eagle", "falcon", "hawk", "wing", "feather"))
            DrawEagleBehavior(canvas, cx, cy, radius, profile, intensity);
        else if (HasVisualToken(profile, "wolf", "frost", "ice", "cold", "snow"))
            DrawWolfBehavior(canvas, cx, cy, radius, profile, intensity);
        else if (HasVisualToken(profile, "shield", "reflect", "metal", "guardian"))
            DrawShieldBehavior(canvas, cx, cy, radius, profile, intensity);
        else if (HasVisualToken(profile, "crown", "royal", "gem"))
            DrawCrownBehavior(canvas, cx, cy, radius, profile, intensity);
        else
            DrawGenericEffect(canvas, cx, cy, radius, profile, intensity);

        canvas.RestoreState();
    }

    private void DrawGenericEffect(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var breath = 0.5f + 0.5f * MathF.Sin(Phase * 1.35f);
        DrawAura(canvas, cx, cy, radius * (0.82f + 0.04f * breath), profile.PrimaryColor, 0.055f * intensity);

        switch (profile.PresetId)
        {
            case EffectPresetId.Fire:
                DrawFireJets(canvas, cx, cy, radius, profile, intensity, 0.45f);
                break;
            case EffectPresetId.Lightning:
                DrawLightning(canvas, cx, cy, radius, profile, intensity);
                break;
            case EffectPresetId.Ice:
            case EffectPresetId.Diamond:
                DrawColdMist(canvas, cx, cy, radius, profile, intensity, 0.40f);
                break;
            case EffectPresetId.Royal:
                DrawJewelSparkles(canvas, cx, cy, radius, profile, intensity, 0.35f);
                break;
            case EffectPresetId.Shadow:
                DrawAura(canvas, cx, cy, radius * 0.78f, Colors.Black, 0.14f);
                break;
            default:
                DrawRing(canvas, cx, cy, radius * (0.88f + 0.05f * breath), profile.SecondaryColor, 1.4f + intensity * 0.35f);
                DrawJewelSparkles(canvas, cx, cy, radius, profile, intensity, 0.24f);
                break;
        }
    }

    private void DrawDragonBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(6.4f);
        var charge = Window01(cycle, 0.58f, 0.70f);
        var fire = Window01(cycle, 0.70f, 0.84f);
        var smoke = MathF.Max(Window01(cycle, 0.82f, 0.98f), 0.28f);
        var mouthOpen = MathF.Max(charge, fire);
        var mouth = new PointF(cx + radius * 0.23f, cy + radius * 0.06f);

        DrawBreathingBody(canvas, cx, cy, radius, profile.SecondaryColor, 0.08f, 0.018f);
        canvas.FillColor = Colors.Black.WithAlpha(0.34f + 0.30f * mouthOpen);
        canvas.FillEllipse(mouth.X - radius * 0.06f, mouth.Y - radius * 0.03f, radius * (0.10f + 0.08f * mouthOpen), radius * (0.06f + 0.06f * mouthOpen));

        canvas.FillColor = profile.SecondaryColor.WithAlpha(0.28f + 0.42f * charge);
        canvas.FillCircle(mouth, radius * (0.035f + 0.060f * charge));

        if (fire > 0.02f)
        {
            DrawFireBreath(canvas, mouth, radius, profile, intensity, fire);
            DrawHeatDistortion(canvas, mouth, radius, fire);
        }

        DrawDragonSmoke(canvas, mouth.X, mouth.Y, radius, profile, intensity * smoke);
    }

    private void DrawLionBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(6.8f);
        var focus = Window01(cycle, 0.44f, 0.58f);
        var roar = Window01(cycle, 0.58f, 0.72f);
        var dust = Window01(cycle, 0.70f, 0.92f);
        var headShift = MathF.Sin(Phase * 1.1f) * radius * 0.025f + focus * radius * 0.025f;

        DrawBreathingBody(canvas, cx, cy, radius, profile.PrimaryColor, 0.07f, 0.026f);
        DrawRoyalEyeGlints(canvas, cx + headShift, cy, radius, profile.SecondaryColor, intensity, 0.55f + 0.45f * focus);

        canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.26f);
        canvas.StrokeSize = Math.Clamp(1.3f * intensity, 1f, 3.4f);
        for (var i = -2; i <= 2; i++)
        {
            var y = cy - radius * 0.22f + i * radius * 0.09f;
            canvas.DrawLine(cx - radius * 0.46f, y, cx - radius * 0.28f + headShift, y + radius * 0.04f * MathF.Sin(Phase + i));
            canvas.DrawLine(cx + radius * 0.28f + headShift, y + radius * 0.04f * MathF.Sin(Phase + i), cx + radius * 0.46f, y);
        }

        if (roar > 0.02f)
        {
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.30f * roar);
            canvas.StrokeSize = Math.Clamp(2.4f * intensity, 1.6f, 5.2f);
            canvas.DrawArc(cx - radius * 0.52f, cy - radius * 0.42f, radius * 1.04f, radius * 0.90f, 205, 335, false, false);
        }

        DrawDustWave(canvas, cx, cy + radius * 0.50f, radius, profile.PrimaryColor, intensity, dust);
    }

    private void DrawEagleBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(6.0f);
        var flap = Window01(cycle, 0.54f, 0.76f);
        var release = Window01(cycle, 0.72f, 0.94f);
        var tracking = MathF.Sin(Phase * 0.85f) * radius * 0.055f;
        var wingLift = radius * (0.08f + 0.18f * flap) * MathF.Sin(Phase * 5.4f);

        DrawWing(canvas, cx, cy, radius, profile.PrimaryColor, intensity, -1f, wingLift);
        DrawWing(canvas, cx, cy, radius, profile.SecondaryColor, intensity, 1f, wingLift);

        canvas.FillColor = profile.SecondaryColor.WithAlpha(0.70f);
        canvas.FillCircle(cx + tracking, cy - radius * 0.16f, Math.Clamp(1.7f * intensity, 1.2f, 3.4f));
        if (flap > 0.42f)
        {
            canvas.StrokeColor = Colors.White.WithAlpha(0.40f);
            canvas.StrokeSize = Math.Clamp(1.1f * intensity, 1f, 2.8f);
            canvas.DrawLine(cx + tracking - radius * 0.08f, cy - radius * 0.16f, cx + tracking + radius * 0.08f, cy - radius * 0.17f);
        }

        DrawFeatherRelease(canvas, cx, cy, radius, profile, intensity, release);
    }

    private void DrawWolfBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(6.2f);
        var breath = Window01(cycle, 0.54f, 0.78f);
        var frost = Window01(cycle, 0.74f, 0.94f);
        var eye = 0.55f + 0.45f * MathF.Sin(Phase * 2.6f);
        var mouth = new PointF(cx + radius * 0.18f, cy + radius * 0.06f);

        DrawBreathingBody(canvas, cx, cy, radius, profile.SecondaryColor, 0.055f, 0.020f);
        DrawEyePair(canvas, cx, cy - radius * 0.15f, radius, profile.SecondaryColor, intensity, 0.38f + 0.36f * eye);
        DrawColdBreath(canvas, mouth, radius, profile, intensity, MathF.Max(breath, 0.22f));

        if (frost > 0.02f)
        {
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.28f * frost);
            canvas.StrokeSize = Math.Clamp(1.4f * intensity, 1f, 3.2f);
            canvas.DrawArc(cx - radius * 0.82f, cy - radius * 0.62f, radius * 1.64f, radius * 1.24f, 25, 155, false, false);
        }
    }

    private void DrawShieldBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(5.8f);
        var sweep = (cycle * 1.6f) % 1f;
        var pulse = Window01(cycle, 0.56f, 0.72f);
        var ripple = Window01(cycle, 0.70f, 0.90f);

        DrawReflectionSweep(canvas, cx, cy, radius, profile.SecondaryColor, intensity, sweep);

        canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.24f + 0.18f * pulse);
        canvas.StrokeSize = Math.Clamp(1.8f * intensity, 1.2f, 4.2f);
        canvas.DrawRoundedRectangle(cx - radius * 0.36f, cy - radius * 0.48f, radius * 0.72f, radius * 0.96f, radius * 0.10f);

        if (ripple > 0.02f)
        {
            canvas.StrokeColor = Colors.White.WithAlpha(0.22f * (1f - ripple));
            canvas.StrokeSize = Math.Clamp(1.2f * intensity, 1f, 3f);
            canvas.DrawCircle(cx, cy, radius * (0.62f + 0.42f * ripple));
        }
    }

    private void DrawCrownBehavior(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var cycle = Cycle01(7.0f);
        var sparkle = Window01(cycle, 0.58f, 0.78f);
        var floatOffset = MathF.Sin(Phase * 0.9f) * radius * 0.035f;

        DrawCrownOutline(canvas, cx, cy + floatOffset, radius, profile, intensity);
        DrawGoldSweep(canvas, cx, cy + floatOffset, radius, profile.SecondaryColor, intensity, cycle);
        DrawJewelSparkles(canvas, cx, cy + floatOffset, radius, profile, intensity, 0.32f + 0.58f * sparkle);
    }

    private static void DrawAura(ICanvas canvas, float cx, float cy, float radius, Color color, float alpha)
    {
        canvas.FillColor = color.WithAlpha(Math.Clamp(alpha, 0.01f, 0.24f));
        canvas.FillCircle(cx, cy, radius);
        canvas.FillColor = color.WithAlpha(Math.Clamp(alpha * 0.30f, 0.01f, 0.14f));
        canvas.FillCircle(cx, cy, radius * 1.22f);
    }

    private static void DrawRing(ICanvas canvas, float cx, float cy, float radius, Color color, float strokeSize)
    {
        canvas.StrokeColor = color.WithAlpha(0.32f);
        canvas.StrokeSize = Math.Max(1f, strokeSize * 0.62f);
        canvas.DrawCircle(cx, cy, radius);
        canvas.StrokeColor = color.WithAlpha(0.10f);
        canvas.StrokeSize = Math.Max(1f, strokeSize * 1.20f);
        canvas.DrawCircle(cx, cy, radius * 1.07f);
    }

    private void DrawFireJets(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity, float amount)
    {
        var count = Lightweight ? 8 : 14;
        for (var i = 0; i < count; i++)
        {
            var angle = (i / (float)count * MathF.Tau) + MathF.Sin(Phase * 3.1f + i) * 0.15f;
            var inner = PointOn(cx, cy, radius * 0.72f, angle);
            var outer = PointOn(cx, cy - radius * 0.10f, radius * (1.08f + 0.10f * MathF.Sin(Phase * 5.0f + i)), angle);
            canvas.StrokeColor = (i % 2 == 0 ? profile.PrimaryColor : profile.SecondaryColor).WithAlpha(0.34f * amount);
            canvas.StrokeSize = 1.6f * intensity;
            canvas.DrawLine(inner, outer);
            canvas.FillColor = profile.SecondaryColor.WithAlpha(0.42f * amount);
            canvas.FillCircle(outer, 1.2f * intensity);
        }
    }

    private void DrawFireBreath(ICanvas canvas, PointF mouth, float radius, IdentityEffectRenderProfile profile, float intensity, float action)
    {
        var length = radius * (0.48f + 0.46f * action);
        var width = radius * (0.10f + 0.10f * action);
        var tip = new PointF(mouth.X + length, mouth.Y - radius * 0.03f * MathF.Sin(Phase * 7f));
        var path = new PathF();
        path.MoveTo(mouth.X, mouth.Y);
        path.LineTo(mouth.X + length * 0.50f, mouth.Y - width);
        path.LineTo(tip.X, tip.Y);
        path.LineTo(mouth.X + length * 0.50f, mouth.Y + width);
        path.Close();
        canvas.FillColor = profile.PrimaryColor.WithAlpha(0.34f * action);
        canvas.FillPath(path);

        canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.62f * action);
        canvas.StrokeSize = Math.Clamp(1.7f * intensity, 1f, 4f);
        canvas.DrawLine(mouth.X + length * 0.10f, mouth.Y, tip.X, tip.Y);
    }

    private void DrawHeatDistortion(ICanvas canvas, PointF mouth, float radius, float action)
    {
        canvas.StrokeColor = Colors.White.WithAlpha(0.14f * action);
        canvas.StrokeSize = 1f;
        for (var i = 0; i < 4; i++)
        {
            var x = mouth.X + radius * (0.22f + i * 0.13f);
            var y = mouth.Y - radius * 0.13f + i * radius * 0.045f;
            canvas.DrawLine(x, y + MathF.Sin(Phase * 5f + i) * radius * 0.015f, x + radius * 0.07f, y + radius * 0.02f);
        }
    }

    private void DrawLightning(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        if (((int)(Phase * 10f)) % 5 == 0)
            return;

        var count = Lightweight ? 3 : 5;
        for (var i = 0; i < count; i++)
        {
            var angle = (i / (float)count * MathF.Tau) + Phase * 0.32f;
            var a = PointOn(cx, cy, radius * 0.68f, angle);
            var b = PointOn(cx, cy, radius * 1.16f, angle + 0.12f * MathF.Sin(i + Phase));
            var mid = PointOn((a.X + b.X) / 2f, (a.Y + b.Y) / 2f, radius * 0.10f, angle + MathF.PI / 2f);
            var path = new PathF();
            path.MoveTo(a.X, a.Y);
            path.LineTo(mid.X, mid.Y);
            path.LineTo(b.X, b.Y);
            canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.24f);
            canvas.StrokeSize = 3.0f * intensity;
            canvas.DrawPath(path);
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.74f);
            canvas.StrokeSize = 1.1f * intensity;
            canvas.DrawPath(path);
        }
    }

    private void DrawJewelSparkles(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity, float amount)
    {
        var count = Lightweight ? 4 : 8;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 2.39996f;
            var angle = seed + Phase * (0.18f + (i % 3) * 0.04f);
            var distance = radius * (0.52f + (i % 5) * 0.10f);
            var point = PointOn(cx, cy, distance, angle);
            var alpha = amount * (0.18f + 0.56f * MathF.Abs(MathF.Sin(Phase * 2.2f + seed)));
            canvas.FillColor = (i % 2 == 0 ? profile.SecondaryColor : profile.PrimaryColor).WithAlpha(Math.Clamp(alpha, 0.08f, 0.58f));
            canvas.FillCircle(point, Math.Clamp((0.8f + alpha * 2f) * intensity, 0.8f, 3.1f));
        }
    }

    private void DrawDragonSmoke(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var count = Lightweight ? 4 : 7;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 1.734f;
            var drift = (Phase * 0.34f + i * 0.17f) % 1f;
            var angle = -MathF.PI / 2f + MathF.Sin(Phase * 0.8f + seed) * 0.55f;
            var point = PointOn(cx + radius * 0.12f, cy + radius * 0.06f, radius * (0.24f + 0.68f * drift), angle + seed * 0.18f);
            var alpha = (0.18f + 0.14f * (1f - drift)) * Math.Clamp(intensity, 0.7f, 1.8f);
            canvas.FillColor = Colors.SlateGray.WithAlpha(Math.Clamp(alpha, 0.08f, 0.32f));
            canvas.FillCircle(point, radius * (0.045f + 0.035f * (1f - drift)));
        }
    }

    private void DrawRoyalEyeGlints(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float amount)
    {
        var flash = 0.55f + 0.45f * MathF.Abs(MathF.Sin(Phase * 2.8f));
        canvas.StrokeColor = color.WithAlpha((0.36f + 0.32f * flash) * amount);
        canvas.StrokeSize = Math.Clamp(1.2f * intensity, 1f, 3.2f);
        var y = cy - radius * 0.16f;
        var xOffset = radius * 0.20f;
        canvas.DrawLine(cx - xOffset - radius * 0.05f, y, cx - xOffset + radius * 0.05f, y - radius * 0.015f);
        canvas.DrawLine(cx + xOffset - radius * 0.05f, y - radius * 0.015f, cx + xOffset + radius * 0.05f, y);
    }

    private void DrawEyePair(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float amount)
    {
        canvas.FillColor = color.WithAlpha(Math.Clamp(amount, 0.12f, 0.72f));
        canvas.FillCircle(cx - radius * 0.16f, cy, Math.Clamp(1.5f * intensity, 1f, 3.2f));
        canvas.FillCircle(cx + radius * 0.16f, cy, Math.Clamp(1.5f * intensity, 1f, 3.2f));
    }

    private void DrawWing(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float side, float lift)
    {
        var count = Lightweight ? 3 : 5;
        canvas.StrokeColor = color.WithAlpha(0.30f);
        canvas.StrokeSize = Math.Clamp(1.2f * intensity, 1f, 3.0f);
        for (var i = 0; i < count; i++)
        {
            var root = new PointF(cx + side * radius * 0.12f, cy - radius * 0.10f + i * radius * 0.035f);
            var tip = new PointF(cx + side * radius * (0.48f + i * 0.055f), cy - radius * (0.30f - i * 0.12f) - lift);
            canvas.DrawLine(root, tip);
        }
    }

    private void DrawFeatherRelease(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity, float amount)
    {
        if (amount <= 0.02f)
            return;

        var count = Lightweight ? 3 : 6;
        for (var i = 0; i < count; i++)
        {
            var side = i % 2 == 0 ? -1f : 1f;
            var fall = (amount + i * 0.11f) % 1f;
            var x = cx + side * radius * (0.28f + i * 0.045f);
            var y = cy + radius * (-0.10f + fall * 0.62f);
            canvas.StrokeColor = profile.SecondaryColor.WithAlpha(0.30f * (1f - fall));
            canvas.StrokeSize = Math.Clamp(1.0f * intensity, 0.8f, 2.4f);
            canvas.DrawLine(x, y, x + side * radius * 0.08f, y + radius * 0.07f);
        }
    }

    private void DrawColdMist(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity, float amount)
    {
        var count = Lightweight ? 5 : 9;
        for (var i = 0; i < count; i++)
        {
            var seed = i * 2.119f;
            var angle = seed + Phase * 0.16f;
            var point = PointOn(cx, cy, radius * (0.28f + (i % 4) * 0.10f), angle);
            var alpha = 0.16f + 0.10f * MathF.Abs(MathF.Sin(Phase * 1.9f + seed));
            canvas.FillColor = (i % 2 == 0 ? profile.SecondaryColor : Colors.White).WithAlpha(Math.Clamp(alpha * intensity * amount, 0.05f, 0.24f));
            canvas.FillCircle(point, radius * (0.035f + 0.012f * (i % 3)));
        }
    }

    private void DrawColdBreath(ICanvas canvas, PointF mouth, float radius, IdentityEffectRenderProfile profile, float intensity, float amount)
    {
        var count = Lightweight ? 5 : 10;
        for (var i = 0; i < count; i++)
        {
            var drift = (Phase * 0.18f + i * 0.10f) % 1f;
            var x = mouth.X + radius * (0.10f + drift * 0.56f);
            var y = mouth.Y - radius * 0.06f + MathF.Sin(Phase * 1.7f + i) * radius * 0.08f;
            canvas.FillColor = (i % 2 == 0 ? profile.SecondaryColor : Colors.White).WithAlpha(Math.Clamp((0.22f - drift * 0.12f) * amount, 0.05f, 0.24f));
            canvas.FillCircle(x, y, radius * (0.030f + 0.035f * (1f - drift)) * Math.Clamp(intensity, 0.75f, 1.6f));
        }
    }

    private void DrawReflectionSweep(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float progress)
    {
        var sweep = (progress - 0.5f) * radius * 1.18f;
        canvas.StrokeColor = Colors.White.WithAlpha(0.24f);
        canvas.StrokeSize = Math.Clamp(1.8f * intensity, 1.1f, 4.2f);
        canvas.DrawLine(cx - radius * 0.32f + sweep, cy - radius * 0.48f, cx + radius * 0.10f + sweep, cy + radius * 0.48f);
        canvas.StrokeColor = color.WithAlpha(0.18f);
        canvas.StrokeSize = Math.Clamp(1.0f * intensity, 1f, 2.6f);
        canvas.DrawLine(cx - radius * 0.42f, cy + radius * 0.42f, cx + radius * 0.38f, cy + radius * 0.42f);
    }

    private void DrawDustWave(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float amount)
    {
        if (amount <= 0.02f)
            return;

        var count = Lightweight ? 5 : 10;
        for (var i = 0; i < count; i++)
        {
            var spread = (i - count / 2f) / count;
            var x = cx + spread * radius * (1.2f + amount);
            var y = cy + MathF.Sin(i + Phase) * radius * 0.025f;
            canvas.FillColor = color.WithAlpha(0.20f * amount);
            canvas.FillCircle(x, y, radius * 0.026f * Math.Clamp(intensity, 0.7f, 1.7f));
        }
    }

    private void DrawBreathingBody(ICanvas canvas, float cx, float cy, float radius, Color color, float alpha, float scale)
    {
        var breath = 0.5f + 0.5f * MathF.Sin(Phase * 1.15f);
        canvas.StrokeColor = color.WithAlpha(alpha);
        canvas.StrokeSize = 1.2f;
        canvas.DrawCircle(cx, cy, radius * (0.56f + scale * breath));
    }

    private void DrawCrownOutline(ICanvas canvas, float cx, float cy, float radius, IdentityEffectRenderProfile profile, float intensity)
    {
        var top = cy - radius * 1.18f;
        canvas.StrokeColor = profile.PrimaryColor.WithAlpha(0.38f);
        canvas.StrokeSize = Math.Clamp(1.3f * intensity, 1f, 3f);
        canvas.DrawLine(cx - radius * 0.36f, top + radius * 0.28f, cx - radius * 0.18f, top + radius * 0.04f);
        canvas.DrawLine(cx - radius * 0.18f, top + radius * 0.04f, cx, top - radius * 0.06f);
        canvas.DrawLine(cx, top - radius * 0.06f, cx + radius * 0.18f, top + radius * 0.04f);
        canvas.DrawLine(cx + radius * 0.18f, top + radius * 0.04f, cx + radius * 0.36f, top + radius * 0.28f);
        canvas.DrawLine(cx - radius * 0.38f, top + radius * 0.34f, cx + radius * 0.38f, top + radius * 0.34f);

        canvas.FillColor = profile.SecondaryColor.WithAlpha(0.42f);
        canvas.FillCircle(cx, top - radius * 0.06f, Math.Clamp(1.7f * intensity, 1.1f, 3.4f));
        canvas.FillCircle(cx - radius * 0.18f, top + radius * 0.04f, Math.Clamp(1.2f * intensity, 0.9f, 2.8f));
        canvas.FillCircle(cx + radius * 0.18f, top + radius * 0.04f, Math.Clamp(1.2f * intensity, 0.9f, 2.8f));
    }

    private void DrawGoldSweep(ICanvas canvas, float cx, float cy, float radius, Color color, float intensity, float progress)
    {
        var x = cx - radius * 0.42f + radius * 0.84f * progress;
        canvas.StrokeColor = color.WithAlpha(0.22f);
        canvas.StrokeSize = Math.Clamp(1.4f * intensity, 1f, 3.2f);
        canvas.DrawLine(x - radius * 0.08f, cy - radius * 0.76f, x + radius * 0.05f, cy - radius * 0.36f);
    }

    private static bool HasVisualToken(IdentityEffectRenderProfile profile, params string[] tokens) =>
        tokens.Any(token =>
            profile.VisualKey.Contains(token, StringComparison.OrdinalIgnoreCase) ||
            profile.AssetId.Contains(token, StringComparison.OrdinalIgnoreCase) ||
            profile.PresetId.ToString().Contains(token, StringComparison.OrdinalIgnoreCase) ||
            profile.AnimationId.ToString().Contains(token, StringComparison.OrdinalIgnoreCase));

    private float Cycle01(float seconds)
    {
        var scaled = ElapsedSeconds <= 0 ? Phase : ElapsedSeconds * Math.Clamp(Profile?.Speed ?? 1f, 0.55f, 1.45f);
        return scaled % seconds / seconds;
    }

    private static float Window01(float value, float start, float end)
    {
        if (value <= start || value >= end)
            return 0f;

        var midpoint = (start + end) * 0.5f;
        return value <= midpoint
            ? SmoothStep((value - start) / MathF.Max(0.001f, midpoint - start))
            : SmoothStep((end - value) / MathF.Max(0.001f, end - midpoint));
    }

    private static float SmoothStep(float value)
    {
        var t = Math.Clamp(value, 0f, 1f);
        return t * t * (3f - 2f * t);
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

        var profile = IdentityEffectRenderProfile.From(effect, baseScale);

        // Developer attached an image: show it as-is on the slot, no procedural
        // overlay, never an emblem/shield/default substitute.
        if (profile.UseLegacyImage)
        {
            var providedImage =
                InventoryDisplayResolver.ResolveOptionalImageSource(profile.LegacyImagePath);
            if (providedImage != null)
            {
                DetachView(slot);
                slot.Source = providedImage;
                slot.IsVisible = true;
                return;
            }
        }

        var holder = Views.GetOrCreateValue(slot);
        if (TryAttach(slot, holder, 1.28, out var view))
        {
            view.ZIndex = Math.Max(slot.ZIndex + 1, 2);
            view.SetEffect(profile, baseScale, lightweight);
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
        DetachView(slot);
        slot.IsVisible = false;
    }

    private static void DetachView(Image slot)
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

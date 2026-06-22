using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class ProceduralEffectDrawable : IDrawable
{
    public EffectDefinitionModel? Definition { get; private set; }
    public EffectRenderProfile? RenderProfile { get; private set; }
    public double AnimationProgress { get; set; }

    public void Configure(
        EffectDefinitionModel? definition,
        EffectRenderProfile? renderProfile)
    {
        Definition = definition;
        RenderProfile = renderProfile;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Definition == null || RenderProfile == null)
            return;

        var center = dirtyRect.Center;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f;
        var progress = (float)Math.Clamp(AnimationProgress, 0, 1);
        var intensity = (float)Math.Clamp(Definition.Intensity, 0.1, 3.0);
        var opacity = (float)Math.Clamp(RenderProfile.Opacity, 0.05, 1.0);
        var primary = RenderProfile.PrimaryColor;
        var secondary = RenderProfile.SecondaryColor;

        canvas.SaveState();
        canvas.Alpha = opacity;

        if (Definition.Layers.Contains(EffectLayerId.Shadow))
            DrawShadow(canvas, center, radius, primary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Aura))
            DrawAura(canvas, center, radius, secondary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Glow))
            DrawGlow(canvas, center, radius, primary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Ring))
            DrawRing(canvas, center, radius, primary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Border))
            DrawBorder(canvas, center, radius, secondary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Pulse))
            DrawPulse(canvas, center, radius, primary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Particle))
            DrawParticles(canvas, center, radius, primary, secondary, progress, intensity);

        canvas.RestoreState();
    }

    static void DrawGlow(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        var pulse = 1f + (0.10f * progress * intensity);
        canvas.StrokeColor = color.WithAlpha(0.75f);
        canvas.StrokeSize = 4f + (2f * intensity);
        canvas.DrawCircle(center.X, center.Y, radius * 0.74f * pulse);

        canvas.StrokeColor = color.WithAlpha(0.32f);
        canvas.StrokeSize = 12f + (4f * intensity);
        canvas.DrawCircle(center.X, center.Y, radius * 0.78f * pulse);
    }

    static void DrawAura(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        var auraRadius = radius * (0.82f + (0.12f * progress * intensity));
        canvas.FillColor = color.WithAlpha(0.12f);
        canvas.FillCircle(center.X, center.Y, auraRadius);
        canvas.StrokeColor = color.WithAlpha(0.22f);
        canvas.StrokeSize = 8f + (3f * intensity);
        canvas.DrawCircle(center.X, center.Y, auraRadius * 0.92f);
    }

    static void DrawRing(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        canvas.StrokeColor = color.WithAlpha(0.9f);
        canvas.StrokeSize = 3f + intensity;
        canvas.DrawCircle(center.X, center.Y, radius * 0.82f);

        canvas.StrokeColor = color.WithAlpha(0.42f);
        canvas.StrokeSize = 2f;
        canvas.DrawCircle(center.X, center.Y, radius * (0.68f + (0.05f * progress)));
    }

    static void DrawBorder(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        canvas.StrokeColor = color.WithAlpha(0.78f);
        canvas.StrokeSize = 2.5f + intensity;
        canvas.DrawCircle(center.X, center.Y, radius * (0.62f + (0.02f * progress)));
    }

    static void DrawPulse(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        var pulseRadius = radius * (0.52f + (0.30f * progress));
        var alpha = Math.Clamp(0.44f - (0.35f * progress), 0.05f, 0.44f);
        canvas.StrokeColor = color.WithAlpha(alpha);
        canvas.StrokeSize = 4f + (2f * intensity);
        canvas.DrawCircle(center.X, center.Y, pulseRadius);
    }

    static void DrawShadow(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        canvas.FillColor = color.WithAlpha(0.18f);
        canvas.FillCircle(center.X, center.Y + (4f * intensity), radius * (0.64f + (0.04f * progress)));
    }

    static void DrawParticles(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float progress,
        float intensity)
    {
        var count = 8;
        var orbit = radius * (0.72f + (0.05f * progress));
        for (var index = 0; index < count; index++)
        {
            var angle = ((MathF.PI * 2f) / count * index) + (MathF.PI * 2f * progress);
            var x = center.X + (MathF.Cos(angle) * orbit);
            var y = center.Y + (MathF.Sin(angle) * orbit);
            canvas.FillColor = (index % 2 == 0 ? primary : secondary).WithAlpha(0.80f);
            canvas.FillCircle(x, y, 2.5f + intensity);
        }
    }
}

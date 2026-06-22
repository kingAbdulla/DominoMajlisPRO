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
        if (radius <= 1)
            return;

        var progress = (float)Math.Clamp(AnimationProgress, 0, 1);
        var intensity = (float)Math.Clamp(Definition.Intensity, 0.35, 3.0);
        var opacity = (float)Math.Clamp(RenderProfile.Opacity, 0.08, 1.0);
        var primary = RenderProfile.PrimaryColor;
        var secondary = RenderProfile.SecondaryColor;
        var breathing = 0.5f + (0.5f * MathF.Sin((progress * MathF.PI * 2f) - (MathF.PI / 2f)));

        canvas.SaveState();
        canvas.Alpha = opacity;

        DrawSoftHalo(canvas, center, radius, primary, secondary, breathing, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Shadow))
            DrawShadow(canvas, center, radius, primary, breathing, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Aura))
            DrawAura(canvas, center, radius, secondary, progress, breathing, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Glow))
            DrawGlow(canvas, center, radius, primary, secondary, breathing, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Ring))
            DrawRing(canvas, center, radius, primary, secondary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Border))
            DrawArcHighlights(canvas, center, radius, secondary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Pulse))
            DrawPulse(canvas, center, radius, primary, secondary, progress, intensity);

        if (Definition.Layers.Contains(EffectLayerId.Particle))
            DrawParticles(canvas, center, radius, primary, secondary, progress, intensity);

        DrawSparkles(canvas, center, radius, primary, secondary, progress, intensity);
        canvas.RestoreState();
    }

    static void DrawSoftHalo(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float breathing,
        float intensity)
    {
        var outer = radius * (0.88f + (0.05f * breathing));
        canvas.FillColor = secondary.WithAlpha(0.035f + (0.025f * intensity));
        canvas.FillCircle(center.X, center.Y, outer);

        canvas.FillColor = primary.WithAlpha(0.025f + (0.015f * breathing));
        canvas.FillCircle(center.X, center.Y, radius * (0.72f + (0.04f * intensity)));
    }

    static void DrawGlow(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float breathing,
        float intensity)
    {
        var pulse = 1f + (0.055f * breathing * intensity);
        DrawCircleStroke(canvas, center, radius * 0.76f * pulse, primary.WithAlpha(0.92f), 2.2f + (0.65f * intensity));
        DrawCircleStroke(canvas, center, radius * 0.79f * pulse, secondary.WithAlpha(0.55f), 5.4f + (1.7f * intensity));
        DrawCircleStroke(canvas, center, radius * 0.84f * pulse, primary.WithAlpha(0.18f), 13f + (3.4f * intensity));
        DrawCircleStroke(canvas, center, radius * 0.91f * pulse, secondary.WithAlpha(0.11f), 22f + (4.5f * intensity));
    }

    static void DrawAura(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float breathing,
        float intensity)
    {
        var auraRadius = radius * (0.82f + (0.08f * breathing * intensity));
        canvas.FillColor = color.WithAlpha(0.08f + (0.025f * breathing));
        canvas.FillCircle(center.X, center.Y, auraRadius);

        DrawCircleStroke(canvas, center, auraRadius * 0.92f, color.WithAlpha(0.22f), 7f + (2.2f * intensity));
        DrawCircleStroke(canvas, center, auraRadius * (0.76f + (0.02f * progress)), color.WithAlpha(0.18f), 2f + intensity);
    }

    static void DrawRing(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float progress,
        float intensity)
    {
        var ring = radius * 0.82f;
        DrawCircleStroke(canvas, center, ring, primary.WithAlpha(0.88f), 2.4f + (0.7f * intensity));
        DrawCircleStroke(canvas, center, ring * 0.90f, secondary.WithAlpha(0.32f), 1.5f + (0.35f * intensity));
        DrawCircleStroke(canvas, center, radius * (0.62f + (0.05f * progress)), primary.WithAlpha(0.24f), 1.8f);
    }

    static void DrawArcHighlights(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float progress,
        float intensity)
    {
        var arcRadius = radius * 0.78f;
        canvas.StrokeColor = color.WithAlpha(0.95f);
        canvas.StrokeSize = 2.2f + (0.6f * intensity);
        canvas.StrokeLineCap = LineCap.Round;

        for (var i = 0; i < 4; i++)
        {
            var start = (progress * 360f) + (i * 90f) + 8f;
            canvas.DrawArc(
                center.X - arcRadius,
                center.Y - arcRadius,
                arcRadius * 2f,
                arcRadius * 2f,
                start,
                start + 32f,
                false,
                false);
        }
    }

    static void DrawPulse(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float progress,
        float intensity)
    {
        var pulseRadius = radius * (0.45f + (0.38f * progress));
        var alpha = Math.Clamp(0.38f - (0.31f * progress), 0.035f, 0.38f);
        DrawCircleStroke(canvas, center, pulseRadius, primary.WithAlpha(alpha), 3.2f + (1.3f * intensity));
        DrawCircleStroke(canvas, center, pulseRadius * 0.86f, secondary.WithAlpha(alpha * 0.65f), 1.4f + intensity);
    }

    static void DrawShadow(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float breathing,
        float intensity)
    {
        canvas.FillColor = color.WithAlpha(0.12f + (0.035f * breathing));
        canvas.FillCircle(
            center.X,
            center.Y + (3f * intensity),
            radius * (0.68f + (0.035f * breathing)));
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
        var count = 14;
        var orbit = radius * 0.86f;
        for (var index = 0; index < count; index++)
        {
            var phase = index / (float)count;
            var angle = (MathF.PI * 2f * phase) + (MathF.PI * 2f * progress);
            var wave = 0.5f + (0.5f * MathF.Sin((progress + phase) * MathF.PI * 2f));
            var x = center.X + (MathF.Cos(angle) * (orbit + (4f * wave * intensity)));
            var y = center.Y + (MathF.Sin(angle) * (orbit + (4f * wave * intensity)));
            var size = 1.5f + (2.1f * wave) + (0.45f * intensity);
            canvas.FillColor = (index % 2 == 0 ? primary : secondary).WithAlpha(0.38f + (0.42f * wave));
            canvas.FillCircle(x, y, size);
        }
    }

    static void DrawSparkles(
        ICanvas canvas,
        PointF center,
        float radius,
        Color primary,
        Color secondary,
        float progress,
        float intensity)
    {
        var count = 8;
        for (var index = 0; index < count; index++)
        {
            var phase = index / (float)count;
            var angle = (MathF.PI * 2f * phase) - (MathF.PI * 2f * progress * 0.7f);
            var wave = 0.5f + (0.5f * MathF.Sin((progress * 2f + phase) * MathF.PI * 2f));
            var orbit = radius * (0.60f + (0.28f * wave));
            var x = center.X + (MathF.Cos(angle) * orbit);
            var y = center.Y + (MathF.Sin(angle) * orbit);
            var sparkle = 3.5f + (2.2f * intensity * wave);
            canvas.StrokeColor = (index % 2 == 0 ? primary : secondary).WithAlpha(0.28f + (0.48f * wave));
            canvas.StrokeSize = 1.2f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(x - sparkle, y, x + sparkle, y);
            canvas.DrawLine(x, y - sparkle, x, y + sparkle);
        }
    }

    static void DrawCircleStroke(
        ICanvas canvas,
        PointF center,
        float radius,
        Color color,
        float strokeSize)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = strokeSize;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawCircle(center.X, center.Y, radius);
    }
}

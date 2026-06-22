using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class EffectPreviewHostView : ContentView
{
    const string AnimationName = "DominoEffectPreviewHostAnimation";

    readonly ProceduralEffectDrawable _drawable = new();
    readonly GraphicsView _graphicsView;
    int _animationVersion;

    public EffectPreviewHostView(double size = 168)
    {
        WidthRequest = size;
        HeightRequest = size;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        IsClippedToBounds = false;

        _graphicsView = new GraphicsView
        {
            Drawable = _drawable,
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            BackgroundColor = Colors.Transparent
        };

        Content = _graphicsView;
    }

    public void SetHostSize(double size)
    {
        var safeSize = Math.Clamp(size, 42, 360);
        WidthRequest = safeSize;
        HeightRequest = safeSize;
        _graphicsView.WidthRequest = safeSize;
        _graphicsView.HeightRequest = safeSize;
    }

    public void Apply(CatalogAssetDisplay? effect, double baseScale = 1.0)
    {
        _graphicsView.CancelAnimations();
        _animationVersion++;

        if (effect == null)
        {
            Clear();
            return;
        }

        var definition = PlayerEffectEngine.CreateDefinition(effect, baseScale);
        var render = PlayerEffectEngine.CreateRenderProfile(definition);
        var version = _animationVersion;

        IsVisible = true;
        Opacity = 1;
        Scale = 1;
        _graphicsView.IsVisible = true;
        _graphicsView.Opacity = render.Opacity;
        _graphicsView.Scale = render.Scale;
        _graphicsView.Rotation = 0;
        _graphicsView.BackgroundColor = Colors.Transparent;
        _drawable.Configure(definition, render);
        _drawable.AnimationProgress = 0;
        _graphicsView.Invalidate();

        if (definition.AnimationId == EffectAnimationId.None)
            return;

        new Animation(v =>
        {
            if (version != _animationVersion)
                return;

            _drawable.AnimationProgress = v;
            _graphicsView.Opacity = definition.AnimationId == EffectAnimationId.Flash && v >= 0.5
                ? 1
                : render.Opacity;
            _graphicsView.Rotation = definition.AnimationId is EffectAnimationId.Rotate or EffectAnimationId.Orbit
                ? 360 * v
                : definition.AnimationId == EffectAnimationId.Lightning
                    ? -6 + (12 * v)
                    : 0;
            _graphicsView.Scale = render.Scale + ResolveScaleBoost(definition, v);
            _graphicsView.Invalidate();
        }, 0, 1).Commit(
            _graphicsView,
            AnimationName,
            16,
            render.Duration,
            Easing.SinInOut,
            null,
            () => version == _animationVersion && IsVisible);
    }

    public void Clear()
    {
        _animationVersion++;
        _graphicsView.CancelAnimations();
        _drawable.Configure(null, null);
        _drawable.AnimationProgress = 0;
        _graphicsView.BackgroundColor = Colors.Transparent;
        _graphicsView.Opacity = 1;
        _graphicsView.Scale = 1;
        _graphicsView.Rotation = 0;
        _graphicsView.IsVisible = false;
        _graphicsView.Invalidate();
        IsVisible = false;
    }

    static double ResolveScaleBoost(EffectDefinitionModel definition, double progress)
    {
        return definition.AnimationId switch
        {
            EffectAnimationId.Lightning => progress < 0.5 ? 0.02 : 0.16 * definition.Intensity,
            EffectAnimationId.Pulse or EffectAnimationId.Breathing => 0.12 * progress * definition.Intensity,
            EffectAnimationId.Fade => 0.04 * progress * definition.Intensity,
            EffectAnimationId.Flash => 0.10 * progress * definition.Intensity,
            EffectAnimationId.Rotate or EffectAnimationId.Orbit => 0.05 * progress * definition.Intensity,
            _ => 0.08 * progress * definition.Intensity
        };
    }
}

using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

// Preview host that renders effects through the exact same production renderer
// (IdentityEffectView / IdentityEffectRenderer + SharedAnimationClock) used across
// the live app pages. No parallel preview renderer is used, so what a developer
// sees while designing is identical to what players see once the asset is equipped.
public sealed class EffectPreviewHostView : ContentView
{
    readonly IdentityEffectView _effectView;
    double _baseScale = 1.0;

    public EffectPreviewHostView(double size = 168)
    {
        WidthRequest = size;
        HeightRequest = size;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        IsClippedToBounds = false;

        _effectView = new IdentityEffectView
        {
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            BackgroundColor = Colors.Transparent
        };

        Content = _effectView;
    }

    public void SetHostSize(double size)
    {
        var safeSize = Math.Clamp(size, 42, 360);
        WidthRequest = safeSize;
        HeightRequest = safeSize;
        _effectView.WidthRequest = safeSize;
        _effectView.HeightRequest = safeSize;
    }

    public void Apply(CatalogAssetDisplay? effect, double baseScale = 1.0)
    {
        _baseScale = baseScale;

        if (effect == null)
        {
            Clear();
            return;
        }

        IsVisible = true;
        Opacity = 1;
        Scale = 1;
        _effectView.IsVisible = true;
        _effectView.SetEffect(IdentityEffectRenderProfile.From(effect, baseScale), baseScale);
    }

    public void Clear()
    {
        _effectView.Clear();
        IsVisible = false;
    }
}

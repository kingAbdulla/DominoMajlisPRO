using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class EffectsStudioPreviewView : ContentView
{
    const string ProceduralAnimationName = "DominoEffectsStudioProceduralPreview";

    readonly ProceduralEffectDrawable _proceduralDrawable = new();

    readonly Label _titleLabel = new()
    {
        Text = "معاينة التأثير",
        FontSize = 15,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center
    };

    readonly Label _subtitleLabel = new()
    {
        Text = "اضبط التأثير وشاهد النتيجة قبل النشر",
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center
    };

    readonly Border _avatarFrame = new()
    {
        WidthRequest = 112,
        HeightRequest = 112,
        StrokeThickness = 2,
        StrokeShape = new RoundRectangle { CornerRadius = 999 },
        HorizontalOptions = LayoutOptions.Center
    };

    readonly GraphicsView _proceduralOverlay;

    readonly Image _effectOverlay = new()
    {
        WidthRequest = 124,
        HeightRequest = 124,
        Aspect = Aspect.AspectFit,
        InputTransparent = true
    };

    readonly Label _avatarGlyph = new()
    {
        Text = "♛",
        FontSize = 44,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    readonly Label _metaLabel = new()
    {
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.WordWrap
    };

    public EffectsStudioPreviewView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        _proceduralOverlay = new GraphicsView
        {
            WidthRequest = 124,
            HeightRequest = 124,
            Drawable = _proceduralDrawable,
            InputTransparent = true,
            IsVisible = false
        };

        var previewGrid = new Grid
        {
            WidthRequest = 132,
            HeightRequest = 132,
            HorizontalOptions = LayoutOptions.Center
        };

        _avatarFrame.Content = _avatarGlyph;
        previewGrid.Add(_avatarFrame);
        previewGrid.Add(_proceduralOverlay);
        previewGrid.Add(_effectOverlay);

        Content = new Border
        {
            Padding = 14,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    _titleLabel,
                    _subtitleLabel,
                    previewGrid,
                    _metaLabel
                }
            }
        };

        ApplyTheme(GalleryThemeEngine.Current);
        GalleryThemeEngine.ThemeChanged += (_, theme) => ApplyTheme(theme);
    }

    public void Preview(NewArrivalRecord record)
    {
        if (record == null)
        {
            ClearPreview();
            _metaLabel.Text = string.Empty;
            return;
        }

        var effect = new CatalogAssetDisplay(
            string.IsNullOrWhiteSpace(record.AssetId) ? "preview-effect" : record.AssetId,
            StoreProductAssetType.Effect,
            StoreProductOwnerScope.Player,
            string.IsNullOrWhiteSpace(record.Title) ? "Preview Effect" : record.Title,
            string.IsNullOrWhiteSpace(record.Title) ? "معاينة التأثير" : record.Title,
            record.ImagePath ?? string.Empty,
            record.ColorHex ?? string.Empty,
            Array.Empty<string>(),
            record.EffectType ?? string.Empty,
            record.AnimationType ?? string.Empty,
            record.DurationMilliseconds,
            record.EquipTarget ?? string.Empty,
            record.PrimaryColorPresetId ?? string.Empty,
            record.SecondaryColorPresetId ?? string.Empty,
            record.CustomPrimaryColorHex ?? string.Empty,
            record.CustomSecondaryColorHex ?? string.Empty,
            record.EffectLayerIds ?? new List<string>(),
            record.EffectOpacity,
            record.EffectScale,
            record.EffectSpeed,
            record.EffectIntensity);

        var definition = PlayerEffectEngine.CreateDefinition(effect, 1.18);
        var render = PlayerEffectEngine.CreateRenderProfile(definition);

        if (render.UseLegacyImage)
            PreviewLegacyEffect(effect);
        else
            PreviewProceduralEffect(definition, render);

        _metaLabel.Text =
            $"{Display(record.EffectType, "Glow")} • {Display(record.AnimationType, "Breathing")} • " +
            $"{Display(record.PrimaryColorPresetId, "Gold")} / {Display(record.SecondaryColorPresetId, "Gold")}";
    }

    void PreviewLegacyEffect(CatalogAssetDisplay effect)
    {
        _proceduralOverlay.CancelAnimations();
        _proceduralOverlay.IsVisible = false;
        _proceduralDrawable.Configure(null, null);
        _proceduralOverlay.Invalidate();

        PlayerEffectEngine.Apply(_effectOverlay, effect, 1.18);
    }

    void PreviewProceduralEffect(
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        _effectOverlay.CancelAnimations();
        _effectOverlay.Source = null;
        _effectOverlay.BackgroundColor = Colors.Transparent;
        _effectOverlay.Shadow = null;
        _effectOverlay.IsVisible = false;
        _effectOverlay.Opacity = 1;
        _effectOverlay.Scale = 1;
        _effectOverlay.Rotation = 0;

        _proceduralDrawable.Configure(definition, render);
        _proceduralDrawable.AnimationProgress = 0;

        _proceduralOverlay.CancelAnimations();
        _proceduralOverlay.InputTransparent = true;
        _proceduralOverlay.IsVisible = true;
        _proceduralOverlay.BackgroundColor = PlayerEffectEngine.CreateBackgroundColor(definition, render);
        _proceduralOverlay.Opacity = render.Opacity;
        _proceduralOverlay.Scale = render.Scale;
        _proceduralOverlay.Rotation = 0;
        _proceduralOverlay.Invalidate();

        StartProceduralAnimation(definition, render);
    }

    void StartProceduralAnimation(
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        if (definition.AnimationId == EffectAnimationId.None)
            return;

        new Animation(v =>
        {
            _proceduralDrawable.AnimationProgress = v;
            _proceduralOverlay.Opacity = definition.AnimationId == EffectAnimationId.Flash && v >= 0.5
                ? 1
                : render.Opacity;
            _proceduralOverlay.Rotation = definition.AnimationId is EffectAnimationId.Rotate or EffectAnimationId.Orbit
                ? 360 * v
                : 0;
            _proceduralOverlay.Scale = render.Scale + ResolveProceduralScaleBoost(definition, v);
            _proceduralOverlay.Invalidate();
        }, 0, 1).Commit(
            _proceduralOverlay,
            ProceduralAnimationName,
            16,
            render.Duration,
            Easing.SinInOut,
            null,
            () => _proceduralOverlay.IsVisible);
    }

    static double ResolveProceduralScaleBoost(
        EffectDefinitionModel definition,
        double progress)
    {
        return definition.AnimationId switch
        {
            EffectAnimationId.Lightning => progress < 0.5
                ? 0.02
                : 0.16 * definition.Intensity,
            EffectAnimationId.Pulse or EffectAnimationId.Breathing => 0.12 * progress * definition.Intensity,
            EffectAnimationId.Fade => 0.04 * progress * definition.Intensity,
            EffectAnimationId.Flash => 0.10 * progress * definition.Intensity,
            EffectAnimationId.Rotate or EffectAnimationId.Orbit => 0.05 * progress * definition.Intensity,
            _ => 0.08 * progress * definition.Intensity
        };
    }

    void ClearPreview()
    {
        PlayerEffectEngine.Apply(_effectOverlay, null);

        _proceduralOverlay.CancelAnimations();
        _proceduralDrawable.Configure(null, null);
        _proceduralDrawable.AnimationProgress = 0;
        _proceduralOverlay.BackgroundColor = Colors.Transparent;
        _proceduralOverlay.IsVisible = false;
        _proceduralOverlay.Opacity = 1;
        _proceduralOverlay.Scale = 1;
        _proceduralOverlay.Rotation = 0;
        _proceduralOverlay.Invalidate();
    }

    void ApplyTheme(GalleryTheme theme)
    {
        if (Content is Border border)
        {
            border.Background = theme.ActionBackground;
            border.Stroke = theme.Stroke;
        }

        _titleLabel.TextColor = theme.TextPrimary;
        _subtitleLabel.TextColor = theme.TextMuted;
        _metaLabel.TextColor = theme.TextMuted;
        _avatarGlyph.TextColor = theme.Accent;
        _avatarFrame.BackgroundColor = Color.FromArgb("#151515");
        _avatarFrame.Stroke = theme.Accent;
    }

    static string Display(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

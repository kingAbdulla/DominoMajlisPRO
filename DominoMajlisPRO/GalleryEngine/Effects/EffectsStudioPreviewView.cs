using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class EffectsStudioPreviewView : ContentView
{
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

        var previewGrid = new Grid
        {
            WidthRequest = 132,
            HeightRequest = 132,
            HorizontalOptions = LayoutOptions.Center
        };

        _avatarFrame.Content = _avatarGlyph;
        previewGrid.Add(_avatarFrame);
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
            PlayerEffectEngine.Apply(_effectOverlay, null);
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

        PlayerEffectEngine.Apply(_effectOverlay, effect, 1.18);

        _metaLabel.Text =
            $"{Display(record.EffectType, "Glow")} • {Display(record.AnimationType, "Breathing")} • " +
            $"{Display(record.PrimaryColorPresetId, "Gold")} / {Display(record.SecondaryColorPresetId, "Gold")}";
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

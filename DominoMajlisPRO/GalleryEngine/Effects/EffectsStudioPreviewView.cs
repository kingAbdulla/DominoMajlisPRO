using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

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
        Text = "المعاينة تستخدم نفس محرك الظهور الفعلي",
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center
    };

    readonly Border _avatarFrame = new()
    {
        WidthRequest = 118,
        HeightRequest = 118,
        StrokeThickness = 2,
        StrokeShape = new RoundRectangle { CornerRadius = 999 },
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center
    };

    readonly Label _avatarGlyph = new()
    {
        Text = "●",
        FontSize = 46,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    readonly Grid _previewGrid = new()
    {
        WidthRequest = 176,
        HeightRequest = 176,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center
    };

    readonly Label _metaLabel = new()
    {
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.WordWrap
    };

    IdentityEffectView? _runtimeEffectView;
    Image? _imageEffectView;

    public EffectsStudioPreviewView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        _avatarFrame.Content = _avatarGlyph;
        _previewGrid.Add(_avatarFrame);

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
                    _previewGrid,
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

        var effect = BuildPreviewEffect(record);
        var contract = AvatarEffectRenderContract.ResolveFor(effect, AvatarEffectRenderSurface.DeveloperStudio);
        RenderRuntimeEffect(effect, contract);

        _metaLabel.Text =
            $"{Display(record.EffectType, "Glow")} • {Display(record.AnimationType, "Breathing")} • " +
            $"{Display(record.PrimaryColorPresetId, "Gold")} / {Display(record.SecondaryColorPresetId, "Gold")}\n" +
            $"{contract.DiagnosticName} • Scale {contract.BaseScale:0.##}";
    }

    CatalogAssetDisplay BuildPreviewEffect(NewArrivalRecord record) => new(
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
        record.EffectIntensity,
        Rarity: record.Rarity ?? string.Empty);

    void RenderRuntimeEffect(CatalogAssetDisplay effect, AvatarEffectRenderContract contract)
    {
        ClearEffectViewOnly();
        _previewGrid.WidthRequest = contract.HostSize;
        _previewGrid.HeightRequest = contract.HostSize;

        // Developer attached an image: preview it as-is — same contract as store
        // card and runtime. Otherwise render the procedural effect only.
        var providedImage =
            InventoryDisplayResolver.ResolveOptionalImageSource(effect.PreviewImage);
        if (providedImage != null)
        {
            _imageEffectView = new Image
            {
                Source = providedImage,
                Aspect = Aspect.AspectFit,
                WidthRequest = contract.HostSize,
                HeightRequest = contract.HostSize,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                ZIndex = 2
            };
            _avatarFrame.ZIndex = 1;
            _previewGrid.Add(_imageEffectView);
            return;
        }

        _runtimeEffectView = IdentityEffectRenderer.Create(effect, contract.BaseScale, contract.Lightweight);
        _runtimeEffectView.WidthRequest = contract.HostSize;
        _runtimeEffectView.HeightRequest = contract.HostSize;
        _runtimeEffectView.MinimumWidthRequest = contract.HostSize;
        _runtimeEffectView.MinimumHeightRequest = contract.HostSize;
        _runtimeEffectView.HorizontalOptions = LayoutOptions.Center;
        _runtimeEffectView.VerticalOptions = LayoutOptions.Center;
        _runtimeEffectView.InputTransparent = true;
        _runtimeEffectView.ZIndex = 2;

        _avatarFrame.ZIndex = 1;
        _previewGrid.Add(_runtimeEffectView);
    }

    void ClearPreview()
    {
        ClearEffectViewOnly();
        _previewGrid.WidthRequest = AvatarEffectRenderContract.DeveloperStudio.HostSize;
        _previewGrid.HeightRequest = AvatarEffectRenderContract.DeveloperStudio.HostSize;
    }

    void ClearEffectViewOnly()
    {
        if (_imageEffectView != null)
        {
            _previewGrid.Children.Remove(_imageEffectView);
            _imageEffectView = null;
        }

        if (_runtimeEffectView == null)
            return;

        _runtimeEffectView.Clear();
        _previewGrid.Children.Remove(_runtimeEffectView);
        _runtimeEffectView = null;
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

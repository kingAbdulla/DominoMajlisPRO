using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class IdentityPlateBinder
{
    public static void ApplyToLabel(Label label, string text, TypographyIdentityPreset? preset)
    {
        label.Text = text;
        if (preset == null)
            return;

        var normalized = preset.Normalized();
        label.FontFamily = normalized.FontFamily;
        label.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 11, Math.Max(11, label.FontSize <= 0 ? 34 : label.FontSize * 1.8));
        label.TextColor = Color.FromArgb(normalized.PrimaryColor);
        label.Opacity = normalized.Opacity;
    }

    public static void Apply(Label fallbackLabel, IdentityPlateView plate, string text, TypographyIdentityPreset? preset)
    {
        fallbackLabel.Text = text;
        if (preset == null)
        {
            fallbackLabel.IsVisible = true;
            plate.IsVisible = false;
            return;
        }

        var normalized = preset.Normalized();
        fallbackLabel.IsVisible = false;
        plate.IsVisible = true;
        plate.HorizontalOptions = fallbackLabel.HorizontalOptions;
        plate.VerticalOptions = fallbackLabel.VerticalOptions;
        plate.MaximumWidthRequest = fallbackLabel.MaximumWidthRequest;
        plate.HeightRequest = fallbackLabel.HeightRequest > 0 ? fallbackLabel.HeightRequest : Math.Max(30, fallbackLabel.FontSize + 14);
        plate.Bind(text, normalized);
    }

    public static View Create(string text, TypographyIdentityPreset? preset, double fontSize, Color textColor, bool bold = true, double maxWidth = -1)
    {
        if (preset == null)
        {
            return new Label
            {
                Text = text,
                TextColor = textColor,
                FontSize = fontSize,
                FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            };
        }

        var plate = new IdentityPlateView
        {
            HeightRequest = Math.Max(34, fontSize + 16),
            MaximumWidthRequest = maxWidth > 0 ? maxWidth : -1,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        plate.Bind(text, preset);
        return plate;
    }
}

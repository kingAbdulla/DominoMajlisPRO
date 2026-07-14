using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class IdentityPlateBinder
{
    public static void Apply(
        Label fallbackLabel,
        IdentityPlateView plate,
        string text,
        NameTypographyIdentity? identity)
    {
        fallbackLabel.Text = text;
        var preset = identity?.ResolvePreset();
        plate.IsVisible = false;
        fallbackLabel.IsVisible = true;

        if (preset == null)
            return;

        var normalized = preset.Normalized();
        plate.Bind(text, normalized);
        plate.IsVisible = true;
        fallbackLabel.IsVisible = false;
    }

    public static View Create(
        string text,
        NameTypographyIdentity? identity,
        double fontSize,
        Color textColor,
        bool bold = true,
        double maxWidth = -1)
    {
        var preset = identity?.ResolvePreset();
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

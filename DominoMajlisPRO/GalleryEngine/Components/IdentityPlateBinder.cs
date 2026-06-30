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
        var hasVisual = preset != null;
        fallbackLabel.IsVisible = !hasVisual;
        plate.IsVisible = hasVisual;
        if (!hasVisual)
            return;

        plate.Bind(text, preset);
        plate.FlowDirection = FlowDirection.RightToLeft;
        plate.HorizontalOptions = fallbackLabel.HorizontalOptions;
        plate.VerticalOptions = fallbackLabel.VerticalOptions;
        plate.MaximumWidthRequest = fallbackLabel.MaximumWidthRequest;
        plate.WidthRequest = fallbackLabel.WidthRequest;
        plate.HeightRequest = fallbackLabel.HeightRequest > 0
            ? fallbackLabel.HeightRequest
            : Math.Max(34, fallbackLabel.FontSize + 16);
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

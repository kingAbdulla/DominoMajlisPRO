namespace DominoMajlisPRO.GalleryEngine.Models;

public class GalleryTheme
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    // Background
    public string BackgroundImage { get; set; } = string.Empty;

    public string CharacterImage { get; set; } = string.Empty;

    // Colors
    public string PrimaryColor { get; set; } = "#D8A63A";

    public string SecondaryColor { get; set; } = "#7A3E12";

    public string AccentColor { get; set; } = "#FFB84A";

    public string TextColor { get; set; } = "#F7D98A";

    // Button
    public string ButtonStartColor { get; set; } = "#FFE08A";

    public string ButtonEndColor { get; set; } = "#B8860B";

    // Card
    public string CardBackgroundStart { get; set; } = "#252525";

    public string CardBackgroundEnd { get; set; } = "#171717";

    public string BorderColor { get; set; } = "#6E5525";

    // Effects
    public string GlowColor { get; set; } = "#FFB84A";

    public double GlowOpacity { get; set; } = 0.35;

    public double OverlayOpacity { get; set; } = 0.45;

    public double ShadowOpacity { get; set; } = 0.35;

    // Future
    public string Mood { get; set; } = "Royal";

    public string Lighting { get; set; } = "Warm";

    public bool EnableParticles { get; set; } = false;

    public bool EnableAnimatedGlow { get; set; } = true;
}
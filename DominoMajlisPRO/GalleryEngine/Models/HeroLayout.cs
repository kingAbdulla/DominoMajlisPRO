namespace DominoMajlisPRO.GalleryEngine.Models;

public class HeroLayout
{
    // Hero Size
    public double Height { get; set; } = 320;

    // Character
    public double CharacterWidth { get; set; } = 220;

    public double CharacterRightMargin { get; set; } = 10;

    public double CharacterBottomMargin { get; set; } = 0;

    // Content
    public double ContentLeftMargin { get; set; } = 24;

    public double ContentTopMargin { get; set; } = 24;

    public double ContentSpacing { get; set; } = 10;

    // Countdown
    public double CountdownTopMargin { get; set; } = 12;

    // Button
    public double ButtonTopMargin { get; set; } = 16;

    // Overlay
    public double OverlayOpacity { get; set; } = 0.45;
}
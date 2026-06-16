namespace DominoMajlisPRO.GalleryEngine.Models;

public class GallerySeason
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;

    public string Chapter { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string BadgeText { get; set; } = "الموسم الحالي";

    public string ButtonText { get; set; } = "استكشف الموسم";

    public string BackgroundImage { get; set; } = string.Empty;

    public string CharacterImage { get; set; } = string.Empty;

    public string PrimaryColor { get; set; } = "#D8A63A";

    public string SecondaryColor { get; set; } = "#7A3E12";

    public string GlowColor { get; set; } = "#FFB84A";

    public string TextColor { get; set; } = "#F7D98A";

    public string ButtonStartColor { get; set; } = "#F7D06A";

    public string ButtonEndColor { get; set; } = "#9C6A18";

    public double OverlayOpacity { get; set; } = 0.45;

    public DateTime StartDate { get; set; } = DateTime.Now;

    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
    public GalleryTheme Theme { get; set; } = new();
    public HeroLayout HeroLayout { get; set;} = new();
}
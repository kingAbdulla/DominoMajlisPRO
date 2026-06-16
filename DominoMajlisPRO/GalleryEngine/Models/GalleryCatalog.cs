namespace DominoMajlisPRO.GalleryEngine.Models;

public class GalleryCatalog
{
    public List<GallerySeason> Seasons { get; set; } = new();

    public List<GalleryItem> Items { get; set; } = new();

    public GallerySeason? CurrentSeason
    {
        get
        {
            var now = DateTime.Now;

            return Seasons
                .Where(s => s.StartDate <= now && s.EndDate >= now)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault()
                ?? Seasons.FirstOrDefault();
        }
    }

    public List<GalleryItem> FeaturedItems
    {
        get
        {
            return Items
                .Where(x => x.IsNew || x.IsLimited)
                .Take(10)
                .ToList();
        }
    }
}
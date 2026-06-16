using DominoMajlisPRO.GalleryEngine.Catalogs.Seasons;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class GalleryService
{
    private static GalleryCatalog? _cachedCatalog;

    public static GalleryCatalog GetCatalog()
    {
        _cachedCatalog ??= LoadCatalog();
        return _cachedCatalog;
    }

    public static GallerySeason? GetCurrentSeason()
    {
        return GetCatalog().CurrentSeason;
    }

    public static List<GallerySeason> GetSeasons()
    {
        return GetCatalog().Seasons;
    }

    public static List<GalleryItem> GetItems()
    {
        return GetCatalog().Items;
    }

    public static List<GalleryItem> GetFeaturedItems()
    {
        return GetCatalog().FeaturedItems;
    }

    public static GalleryItem? GetItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        return GetCatalog()
            .Items
            .FirstOrDefault(x => x.Id == itemId);
    }

    public static List<GalleryItem> GetItemsBySeason(string seasonId)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
            return new List<GalleryItem>();

        return GetCatalog()
            .Items
            .Where(x => x.SeasonId == seasonId)
            .ToList();
    }

    public static List<GalleryItem> GetItemsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<GalleryItem>();

        return GetCatalog()
            .Items
            .Where(x => x.Category == category)
            .ToList();
    }

    private static GalleryCatalog LoadCatalog()
    {
        return ArabianLegendsSeasonCatalog.Build();
    }
}
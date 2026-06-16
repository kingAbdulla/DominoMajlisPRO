using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Pages;

public partial class GalleryPage : ContentPage
{
    public GalleryPage()
    {
        InitializeComponent();

        LoadPage();
    }

    private void LoadPage()
    {
        var season = GalleryService.GetCurrentSeason();

        if (season != null)
        {
            HeroBanner.Bind(season);
        }
    }
}
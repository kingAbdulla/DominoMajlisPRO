namespace DominoMajlisPRO;

using DominoMajlisPRO.GalleryEngine.Pages;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage =
            new NavigationPage(
                new GalleryPage());
    }
}
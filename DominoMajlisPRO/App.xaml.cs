namespace DominoMajlisPRO;

using DominoMajlisPRO.GalleryEngine.Pages;
using DominoMajlisPRO.Localization;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        _ = ArabicTextRecoveryService.RepairAppDataJsonFilesOnceAsync();

        MainPage =
            new NavigationPage(
                new MainPage());
    }
}

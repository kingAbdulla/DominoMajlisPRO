namespace DominoMajlisPRO;

using DominoMajlisPRO.Localization;
using DominoMajlisPRO.Pages;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        _ = ArabicTextRecoveryService.RepairAppDataJsonFilesOnceAsync();

        MainPage =
            new NavigationPage(
                new PremiumAuthPage());
    }
}

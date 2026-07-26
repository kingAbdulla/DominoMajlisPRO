namespace DominoMajlisPRO;

using DominoMajlisPRO.Localization;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        _ = ArabicTextRecoveryService.RepairAppDataJsonFilesOnceAsync();
        SeasonExperienceService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(
            NavigationGuardService.CreateNavigationRoot(
                new AppStartupPage()));
        window.Resumed += (_, _) => SeasonExperienceService.RequestProgressRefresh();
        return window;
    }
}

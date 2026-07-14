namespace DominoMajlisPRO;

using DominoMajlisPRO.Localization;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.GalleryEngine.Services;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        _ = ArabicTextRecoveryService.RepairAppDataJsonFilesOnceAsync();
        SeasonExperienceService.Initialize();

        MainPage =
            new NavigationPage(
                new AppStartupPage());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Resumed += (_, _) => SeasonExperienceService.RequestProgressRefresh();
        return window;
    }
}

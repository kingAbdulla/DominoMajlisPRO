namespace DominoMajlisPRO;

using DominoMajlisPRO.GalleryEngine.Effects;
using DominoMajlisPRO.GalleryEngine.Pages;
using DominoMajlisPRO.Localization;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        _ = ArabicTextRecoveryService.RepairAppDataJsonFilesOnceAsync();

        // Seed official effect pack definitions once on first launch.
        // Idempotent — skips any AssetId already present in storage.
        // Fire-and-forget: never blocks UI, failure is logged safely.
        _ = SeedEffectPacksAsync();

        MainPage =
            new NavigationPage(
                new MainPage());
    }

    private static async Task SeedEffectPacksAsync()
    {
        try
        {
            await EffectBehaviorPackRegistry.SeedAllAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[App] EffectBehaviorPackRegistry.SeedAllAsync failed: {ex.Message}");
        }
    }
}

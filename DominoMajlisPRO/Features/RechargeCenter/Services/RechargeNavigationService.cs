using DominoMajlisPRO.Features.RechargeCenter.Pages;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargeNavigationService
{
    public static async Task OpenAsync(INavigation? navigation)
    {
        if (navigation == null)
            return;

        await NavigationGuardService.PushOnceAsync(navigation, new RechargeCenterPage());
    }
}

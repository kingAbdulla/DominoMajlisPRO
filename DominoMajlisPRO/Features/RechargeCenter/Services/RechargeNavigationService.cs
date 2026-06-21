using DominoMajlisPRO.Features.RechargeCenter.Pages;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargeNavigationService
{
    private static int _isNavigating;

    public static async Task OpenAsync(INavigation? navigation)
    {
        if (navigation == null || Interlocked.Exchange(ref _isNavigating, 1) == 1)
            return;
        try
        {
            await navigation.PushAsync(new RechargeCenterPage());
        }
        finally
        {
            Interlocked.Exchange(ref _isNavigating, 0);
        }
    }
}

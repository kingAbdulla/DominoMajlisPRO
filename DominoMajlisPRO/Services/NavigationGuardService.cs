namespace DominoMajlisPRO.Services;

public static class NavigationGuardService
{
    static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task PushOnceAsync(
        INavigation? navigation,
        Page page,
        bool animated = true)
    {
        if (navigation == null)
            throw new InvalidOperationException("Navigation stack is not available.");

        await Gate.WaitAsync();
        try
        {
            var current = navigation.NavigationStack.LastOrDefault();
            if (current?.GetType() == page.GetType())
                return;

            if (navigation.ModalStack.Any(modal => modal.GetType() == page.GetType()))
                return;

            await navigation.PushAsync(page, animated);
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task PopOrGoBackAsync(INavigation? navigation)
    {
        await Gate.WaitAsync();
        try
        {
            if (navigation?.NavigationStack.Count > 1)
            {
                await navigation.PopAsync();
                return;
            }

            if (Shell.Current != null)
                await Shell.Current.GoToAsync("..");
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ResetRootAsync(Page page)
    {
        await Gate.WaitAsync();
        try
        {
            var window = Application.Current?.Windows.FirstOrDefault()
                         ?? throw new InvalidOperationException("Application window is not available.");

            window.Page = new NavigationPage(page);
        }
        finally
        {
            Gate.Release();
        }
    }
}

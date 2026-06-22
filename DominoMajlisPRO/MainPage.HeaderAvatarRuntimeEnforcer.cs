namespace DominoMajlisPRO;

public partial class MainPage
{
    static MainPage()
    {
        Application.Current?.Dispatcher.StartTimer(
            TimeSpan.FromMilliseconds(250),
            () =>
            {
                var page = ResolveActivePage(Application.Current?.Windows.FirstOrDefault()?.Page);

                if (page is MainPage mainPage)
                {
                    mainPage.ApplyMainHeaderAvatarShape();
                    _ = mainPage.ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
                }

                return true;
            });
    }

    static Page? ResolveActivePage(Page? page)
    {
        return page switch
        {
            NavigationPage navigationPage => navigationPage.CurrentPage,
            TabbedPage tabbedPage => ResolveActivePage(tabbedPage.CurrentPage),
            FlyoutPage flyoutPage => ResolveActivePage(flyoutPage.Detail),
            Shell shell => shell.CurrentPage,
            _ => page
        };
    }
}

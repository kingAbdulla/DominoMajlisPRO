using System.Runtime.CompilerServices;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    const string UnifiedScrollAutomationId = "GamePageUnifiedScrollV1";

    internal void EnsureUnifiedScrollableLayout()
    {
        if (Content is not Grid root ||
            string.Equals(root.AutomationId, UnifiedScrollAutomationId, StringComparison.Ordinal))
        {
            return;
        }

        // The first four children are the header, hero cards, keypad and the
        // previous rounds-only ScrollView. Overlays must remain direct children
        // of the root Grid so they continue covering the full page.
        if (root.Children.Count < 6)
            return;

        var header = root.Children[0] as View;
        var hero = root.Children[2] as View;
        var keypad = root.Children[3] as View;
        var roundsScroll = root.Children[4] as ScrollView;

        if (header == null || hero == null || keypad == null || roundsScroll?.Content is not View roundsContent)
            return;

        root.Children.Remove(header);
        root.Children.Remove(hero);
        root.Children.Remove(keypad);
        root.Children.Remove(roundsScroll);

        Grid.SetRow(header, 0);
        Grid.SetRow(hero, 0);
        Grid.SetRow(keypad, 0);
        Grid.SetRow(roundsContent, 0);

        var pageStack = new VerticalStackLayout
        {
            Spacing = 7,
            Padding = new Thickness(0, 0, 0, 10),
            Children =
            {
                header,
                hero,
                keypad,
                roundsContent
            }
        };

        var unifiedScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            VerticalScrollBarVisibility = ScrollBarVisibility.Default,
            Content = pageStack
        };

        Grid.SetRow(unifiedScroll, 0);
        Grid.SetRowSpan(unifiedScroll, 4);
        root.Children.Insert(0, unifiedScroll);
        root.AutomationId = UnifiedScrollAutomationId;
    }
}

internal static class GamePageUnifiedScrollBootstrap
{
    static Timer? refreshTimer;

    [ModuleInitializer]
    internal static void Initialize()
    {
        refreshTimer = new Timer(
            _ => MainThread.BeginInvokeOnMainThread(ApplyToActivePage),
            null,
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(750));
    }

    static void ApplyToActivePage()
    {
        var page = FindActiveGamePage(Application.Current?.MainPage);
        page?.EnsureUnifiedScrollableLayout();
    }

    static GamePage? FindActiveGamePage(Page? page)
    {
        if (page is GamePage gamePage)
            return gamePage;

        if (page is NavigationPage navigation)
            return FindActiveGamePage(navigation.CurrentPage);

        if (page is FlyoutPage flyout)
            return FindActiveGamePage(flyout.Detail);

        if (page is TabbedPage tabbed)
            return FindActiveGamePage(tabbed.CurrentPage);

        return null;
    }
}

using System.Runtime.CompilerServices;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    readonly SemaphoreSlim rankLiveSyncGate = new(1, 1);
    DateTime lastRankingsWriteUtc = DateTime.MinValue;

    internal async Task RefreshRankProgressFromStoreAsync(bool force = false)
    {
        if (!await rankLiveSyncGate.WaitAsync(0))
            return;

        try
        {
            string rankingsPath = Path.Combine(FileSystem.AppDataDirectory, "rankings.json");
            DateTime writeUtc = File.Exists(rankingsPath)
                ? File.GetLastWriteTimeUtc(rankingsPath)
                : DateTime.MinValue;

            bool profilesMissing = team1Profile == null || team2Profile == null;
            if (!force && !profilesMissing && writeUtc <= lastRankingsWriteUtc)
                return;

            var sourceTeams = await TeamProfileService.LoadTeamsAsync();
            var rankingTeams = await RankingService.LoadTeamsAsync();

            team1Profile = ResolveRankedTeamProfile(sourceTeams, rankingTeams, team1Id);
            team2Profile = ResolveRankedTeamProfile(sourceTeams, rankingTeams, team2Id);
            lastRankingsWriteUtc = writeUtc;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ApplyTeamProgressVisuals();
                UpdateLeaderUI();
            });
        }
        catch
        {
            // Keep the active match usable if rank storage is temporarily unavailable.
        }
        finally
        {
            rankLiveSyncGate.Release();
        }
    }
}

internal static class GamePageRankLiveSyncBootstrap
{
    static readonly Timer PollTimer;

    [ModuleInitializer]
    internal static void Initialize()
    {
        AppEvents.RankingsChanged += ForceRefreshActiveGamePage;
        AppEvents.TeamsChanged += ForceRefreshActiveGamePage;
        AppEvents.MatchesChanged += ForceRefreshActiveGamePage;
        AppEvents.SeasonChanged += ForceRefreshActiveGamePage;

        PollTimer = new Timer(
            _ => QueueActivePageRefresh(force: false),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
    }

    static void ForceRefreshActiveGamePage() => QueueActivePageRefresh(force: true);

    static void QueueActivePageRefresh(bool force)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var gamePage = FindActiveGamePage(Application.Current?.MainPage);
            if (gamePage != null)
                await gamePage.RefreshRankProgressFromStoreAsync(force);
        });
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

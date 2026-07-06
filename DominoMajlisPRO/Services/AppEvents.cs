namespace DominoMajlisPRO.Services;

public static class AppEvents
{
    public static event Action? DataChanged;
    public static event Action? RankingsChanged;
    public static event Action? TeamsChanged;
    public static event Action? MatchesChanged;
    public static event Action? PlayerProfileChanged;
    public static event Action? CurrentUserChanged;
    public static event Action<string>? StoreEconomyChanged;
    public static event Action<string>? StoreProgressChanged;
    public static event Action<string>? TeamAssetsChanged;
    public static event Action<string>? TeamEffectChanged;
    public static event Action? SeasonChanged;
    public static event Action? TeamStatsChanged;
    public static event Action<string>? WalletChanged;
    public static event Action<string>? RankRewardGranted;

    static void SafeRaise(Action? action)
    {
        if (action == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            action.Invoke();
        });
    }

    static void SafeRaise(Action<string>? action, string playerId)
    {
        if (action == null)
            return;

        MainThread.BeginInvokeOnMainThread(() => action.Invoke(playerId));
    }

    public static void RaiseStoreEconomyChanged(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        SafeRaise(StoreEconomyChanged, playerId);
        SafeRaise(StoreProgressChanged, playerId);
        SafeRaise(WalletChanged, playerId);
    }

    public static void RaiseWalletChanged(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        SafeRaise(WalletChanged, playerId);
        SafeRaise(StoreEconomyChanged, playerId);
        SafeRaise(StoreProgressChanged, playerId);
    }

    public static void RaiseSeasonChanged()
    {
        SafeRaise(SeasonChanged);
        SafeRaise(RankingsChanged);
    }

    public static void RaiseTeamStatsChanged()
    {
        SafeRaise(TeamStatsChanged);
        SafeRaise(TeamsChanged);
    }

    public static void RaiseRankRewardGranted(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return;

        SafeRaise(RankRewardGranted, teamId);
        SafeRaise(RankingsChanged);
    }

    public static void RaisePlayerProfileChanged()
    {
        SafeRaise(PlayerProfileChanged);
    }

    public static void RaiseCurrentUserChanged()
    {
        SafeRaise(CurrentUserChanged);
    }

    public static void RaiseTeamAssetsChanged(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return;

        SafeRaise(TeamAssetsChanged, teamId);
    }

    public static void RaiseTeamEffectChanged(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return;

        SafeRaise(TeamEffectChanged, teamId);
        SafeRaise(TeamAssetsChanged, teamId);
        SafeRaise(TeamsChanged);
    }

    public static void RaiseDataChanged()
    {
        SafeRaise(DataChanged);
        SafeRaise(RankingsChanged);
        SafeRaise(TeamsChanged);
        SafeRaise(MatchesChanged);
        SafeRaise(PlayerProfileChanged);
    }

    public static void RaiseRankingsChanged()
    {
        SafeRaise(RankingsChanged);
        SafeRaise(PlayerProfileChanged);
    }

    public static void RaiseTeamsChanged()
    {
        SafeRaise(TeamsChanged);
        SafeRaise(PlayerProfileChanged);
    }

    public static void RaiseMatchesChanged()
    {
        SafeRaise(MatchesChanged);
        SafeRaise(PlayerProfileChanged);
    }

    public static void NotifyTeamsChanged()
    {
        RaiseTeamsChanged();
    }

    public static void NotifyRankingsChanged()
    {
        RaiseRankingsChanged();
    }
}


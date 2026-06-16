namespace DominoMajlisPRO.Services;

public static class AppEvents
{
    public static event Action? DataChanged;
    public static event Action? RankingsChanged;
    public static event Action? TeamsChanged;
    public static event Action? MatchesChanged;
    public static event Action? PlayerProfileChanged;

    static void SafeRaise(Action? action)
    {
        if (action == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            action.Invoke();
        });
    }

    public static void RaisePlayerProfileChanged()
    {
        SafeRaise(PlayerProfileChanged);
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
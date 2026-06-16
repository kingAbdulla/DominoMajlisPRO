namespace DominoMajlisPRO.Controls;

public partial class HallSideMenuView : ContentView
{
    public event Action<string>? NavigationRequested;

    public HallSideMenuView()
    {
        InitializeComponent();
        SetActive("HOME");
    }

    void OnHomeClicked(object sender, EventArgs e)
    {
        SetActive("HOME");
        NavigationRequested?.Invoke("HOME");
    }

    void OnTeamsClicked(object sender, EventArgs e)
    {
        SetActive("TEAMS");
        NavigationRequested?.Invoke("TEAMS");
    }

    void OnPlayersClicked(object sender, EventArgs e)
    {
        SetActive("PLAYERS");
        NavigationRequested?.Invoke("PLAYERS");
    }

    void OnAchievementsClicked(object sender, EventArgs e)
    {
        SetActive("ACHIEVEMENTS");
        NavigationRequested?.Invoke("ACHIEVEMENTS");
    }

    void OnHistoryClicked(object sender, EventArgs e)
    {
        SetActive("HISTORY");
        NavigationRequested?.Invoke("HISTORY");
    }

    void OnStatsClicked(object sender, EventArgs e)
    {
        SetActive("STATS");
        NavigationRequested?.Invoke("STATS");
    }

    void SetActive(string section)
    {
        ResetItem(HomeItem);
        ResetItem(TeamsItem);
        ResetItem(PlayersItem);
        ResetItem(AchievementsItem);
        ResetItem(HistoryItem);
        ResetItem(StatsItem);

        Border activeItem =
            section switch
            {
                "HOME" => HomeItem,
                "TEAMS" => TeamsItem,
                "PLAYERS" => PlayersItem,
                "ACHIEVEMENTS" => AchievementsItem,
                "HISTORY" => HistoryItem,
                "STATS" => StatsItem,
                _ => HomeItem
            };

        activeItem.BackgroundColor =
            Color.FromArgb("#251806");

        activeItem.Stroke =
            Color.FromArgb("#D4AE62");
    }

    void ResetItem(Border item)
    {
        item.BackgroundColor =
            Colors.Transparent;

        item.Stroke =
            Colors.Transparent;
    }
}
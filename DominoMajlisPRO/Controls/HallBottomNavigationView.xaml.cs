namespace DominoMajlisPRO.Controls;

public partial class HallBottomNavigationView : ContentView
{
    public event Action<string>? NavigationRequested;

    public HallBottomNavigationView()
    {
        InitializeComponent();
        Wire(SettingsItem, "SETTINGS");
        Wire(FriendsItem, "PLAYERS");
        Wire(GameItem, "GAME");
        Wire(StoreItem, "STORE");
    }

    void Wire(View item, string destination)
    {
        item.GestureRecognizers.Clear();

        var tap = new TapGestureRecognizer();
        tap.Tapped += (sender, args) => NavigationRequested?.Invoke(destination);
        item.GestureRecognizers.Add(tap);
    }
}

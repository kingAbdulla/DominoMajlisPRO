namespace DominoMajlisPRO.Controls;

public partial class HallBottomNavigationView : ContentView
{
    public event Action<string>? NavigationRequested;

    public HallBottomNavigationView()
    {
        InitializeComponent();
        HomeButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => NavigationRequested?.Invoke("HOME")) });
        PlayersButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => NavigationRequested?.Invoke("PLAYERS")) });
        GameButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => NavigationRequested?.Invoke("GAME")) });
        StoreButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => NavigationRequested?.Invoke("STORE")) });
        HallButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => NavigationRequested?.Invoke("HALL")) });
    }
}

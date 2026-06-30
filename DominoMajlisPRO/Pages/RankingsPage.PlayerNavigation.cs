namespace DominoMajlisPRO.Pages;

public partial class RankingsPage
{
    async void OnPlayersRankingClicked(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new PlayerRankingsPage());
    }
}

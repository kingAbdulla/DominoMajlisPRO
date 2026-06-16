namespace DominoMajlisPRO.Models;

public class Player
{
    public string Name { get; set; } = "";

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int MVPs { get; set; }

    public int TotalPoints { get; set; }

    public double WinRate
    {
        get
        {
            int totalGames = Wins + Losses;

            if (totalGames == 0)
                return 0;

            return (double)Wins / totalGames * 100;
        }
    }
}
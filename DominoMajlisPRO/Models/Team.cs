namespace DominoMajlisPRO.Models;

public class Team
{
    public List<Player> Players { get; set; } = new();

    public int Score { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int MalasWins { get; set; }

    public string TeamDisplayName
    {
        get
        {
            return string.Join(" + ", Players.Select(p => p.Name));
        }
    }
}
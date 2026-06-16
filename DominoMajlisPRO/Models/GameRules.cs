namespace DominoMajlisPRO.Models;

public class GameRules
{
    public int WinningScore { get; set; } = 151;

    public bool EnableMalas { get; set; } = true;

    public int MalasLimit { get; set; } = 25;

    public int NormalWinPoints { get; set; } = 1;

    public int MalasWinPoints { get; set; } = 2;
}
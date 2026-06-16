namespace DominoMajlisPRO.Models;

public class RivalryRecord
{
    public string TeamAId { get; set; } = "";

    public string TeamBId { get; set; } = "";


    public int TeamAWins { get; set; }

    public int TeamBWins { get; set; }

    public int TotalMatches { get; set; }
}
namespace DominoMajlisPRO.Models;

public class MatchModel
{
    public Team Team1 { get; set; } = new();

    public Team Team2 { get; set; } = new();

    public List<RoundModel> Rounds { get; set; } = new();

    public string WinnerTeam { get; set; } = "";

    public string LoserTeam { get; set; } = "";

    public bool IsMalas { get; set; }

    public DateTime MatchDate { get; set; } = DateTime.Now;

    public string RuleMode { get; set; } = "محلي";
}
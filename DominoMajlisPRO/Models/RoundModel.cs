public class RoundModel
{
    public int RoundNumber { get; set; }

    public string WinnerTeam { get; set; } = "";

    public string WinnerPlayers { get; set; } = "";

    public string LoserTeam { get; set; } = "";

    public string LoserPlayers { get; set; } = "";

    public int Points { get; set; }

    public int Team1RoundPoints { get; set; }

    public int Team2RoundPoints { get; set; }

    public bool IsMeles { get; set; }

    public int Team1OldScore { get; set; }

    public int Team2OldScore { get; set; }

    public int Team1NewScore { get; set; }

    public int Team2NewScore { get; set; }
    public string WinnerTeamColor { get; set; } = "";
    public string LoserTeamColor { get; set; } = "";

    public string WinnerTeamEmblem { get; set; } = "";
    public string LoserTeamEmblem { get; set; } = "";

    public DateTime RoundTime { get; set; }
    public int WinnerTeamId { get; set; }
    public string WinnerProfileId { get; set; } = "";
    public string LoserProfileId { get; set; } = "";


}
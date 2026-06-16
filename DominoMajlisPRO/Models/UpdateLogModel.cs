namespace DominoMajlisPRO.Models;

public class UpdateLogModel
{
    public string Version { get; set; } = "";
    public string ReleaseDate { get; set; } = "";
    public List<string> Updates { get; set; } = new();
}
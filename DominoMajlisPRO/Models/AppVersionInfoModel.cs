namespace DominoMajlisPRO.Models;

public class AppVersionInfoModel
{
    public string AppName { get; set; } = "";
    public string Version { get; set; } = "";
    public string Build { get; set; } = "";
    public string ReleaseType { get; set; } = "";
    public string Developer { get; set; } = "";
    public string UpdateStatus { get; set; } = "";
    public List<string> LatestUpdates { get; set; } = new();
}
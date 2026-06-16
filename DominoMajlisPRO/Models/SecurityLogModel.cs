namespace DominoMajlisPRO.Models;

public class SecurityLogModel
{
    public DateTime Date { get; set; } =
        DateTime.Now;

    public string Category { get; set; } = "";

    public string Action { get; set; } = "";

    public string Details { get; set; } = "";

    public string Severity { get; set; } = "Info";

    public bool IsPermanent { get; set; } = false;

    public string DeveloperId { get; set; } = "";

    public string DeveloperUsername { get; set; } = "";

    public string DeviceFingerprint { get; set; } = "";
}
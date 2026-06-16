namespace DominoMajlisPRO.Models;

public class PlayerTimelineItemModel
{
    public DateTime Date { get; set; }

    public string Title { get; set; } = "";

    public string Details { get; set; } = "";

    public string Icon { get; set; } = "⭐";

    public string ColorHex { get; set; } = "#D4AF37";
}
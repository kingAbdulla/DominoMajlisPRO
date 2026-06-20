namespace DominoMajlisPRO.Models;

public class PlayerTimelineItemModel
{
    public string EventId { get; set; } = "";

    public bool IsIdentityEvent { get; set; }

    public DateTime Date { get; set; }

    public string Title { get; set; } = "";

    public string Details { get; set; } = "";

    public string Icon { get; set; } = "⭐";

    public string ColorHex { get; set; } = "#D4AF37";
}

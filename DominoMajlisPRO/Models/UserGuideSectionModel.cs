namespace DominoMajlisPRO.Models;

public class UserGuideSectionModel
{
    public string Title { get; set; } = "";

    public List<UserGuideItemModel> Items { get; set; } = new();
}

public class UserGuideItemModel
{
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";
}

namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class StoreProgressCount
{
    public string Key { get; set; } = string.Empty;
    public int Owned { get; set; }
    public int Total { get; set; }
}

public sealed class PlayerStoreProgressModel
{
    public string PlayerId { get; set; } = string.Empty;
    public int TotalOwned { get; set; }
    public int TotalPublished { get; set; }
    public double OverallCollectionPercent { get; set; }
    public int TotalAvailable
    {
        get => TotalPublished;
        set => TotalPublished = value;
    }
    public double CompletionPercent
    {
        get => OverallCollectionPercent;
        set => OverallCollectionPercent = value;
    }
    public int OwnedAvatars { get; set; }
    public int OwnedBackgrounds { get; set; }
    public int OwnedFrames { get; set; }
    public int OwnedEffects { get; set; }
    public int OwnedBadges { get; set; }
    public int OwnedEmblems { get; set; }
    public int OwnedBundles { get; set; }
    public IReadOnlyList<StoreProgressCount> ByItemType { get; set; } = Array.Empty<StoreProgressCount>();
    public IReadOnlyList<StoreProgressCount> ByRarity { get; set; } = Array.Empty<StoreProgressCount>();
    public IReadOnlyList<StoreProgressCount> ByCollection { get; set; } = Array.Empty<StoreProgressCount>();
}

namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StoreResetReport
{
    public int PublishedCount { get; init; }
    public int DraftCount { get; init; }
    public int LimitedOfferCount { get; init; }
    public int CategoryCount { get; init; }
    public int OrphanReferenceCount { get; init; }
    public string BackupPath { get; init; } = string.Empty;
    public DateTime CompletedAtUtc { get; init; }
}

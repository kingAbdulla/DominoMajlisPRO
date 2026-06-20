namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum BackgroundStatus { Draft = 0, Published = 1, Hidden = 2 }
public enum BackgroundRarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3, Mythic = 4, Immortal = 5 }
public enum BackgroundCurrencyType { Coins = 0, Gems = 1, Free = 2 }
public enum BackgroundUnlockType { Free = 0, Coins = 1, Gems = 2, SeasonPass = 3, HallOfFame = 4, Rank = 5, Developer = 6, Founder = 7, Event = 8, Bundle = 9 }

public sealed class BackgroundRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public BackgroundRarity Rarity { get; set; } = BackgroundRarity.Common;
    public BackgroundCurrencyType CurrencyType { get; set; } = BackgroundCurrencyType.Gems;
    public int Price { get; set; }
    public bool IsFree { get; set; }
    public BackgroundUnlockType UnlockType { get; set; } = BackgroundUnlockType.Gems;
    public string UnlockRequirement { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public bool IsAnimated { get; set; }
    public bool IsLimited { get; set; }
    public bool IsFeatured { get; set; }
    public int FeaturedPriority { get; set; }
    public int SortOrder { get; set; }
    public string SeasonId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public BackgroundStatus Status { get; set; } = BackgroundStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

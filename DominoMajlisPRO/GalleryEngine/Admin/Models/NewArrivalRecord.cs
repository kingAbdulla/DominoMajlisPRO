namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum NewArrivalStatus
{
    Draft = 0,
    Published = 1,
    Hidden = 2
}

public enum NewArrivalCurrencyType
{
    Coins = 0,
    Gems = 1,
    Free = 2
}

public sealed class NewArrivalRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProductId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string StoreTypeId { get; set; } = string.Empty;
    public string OwnerScope { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public string AnimationType { get; set; } = string.Empty;
    public int DurationMilliseconds { get; set; }
    public string EquipTarget { get; set; } = string.Empty;
    public string PrimaryColorPresetId { get; set; } = string.Empty;
    public string SecondaryColorPresetId { get; set; } = string.Empty;
    public string CustomPrimaryColorHex { get; set; } = string.Empty;
    public string CustomSecondaryColorHex { get; set; } = string.Empty;
    public List<string> EffectLayerIds { get; set; } = new();
    public double EffectOpacity { get; set; } = 0.74;
    public double EffectScale { get; set; } = 1.18;
    public double EffectSpeed { get; set; } = 1.0;
    public double EffectIntensity { get; set; } = 1.0;
    public List<string> BundleAssetIds { get; set; } = new();
    public int DiscountPercent { get; set; }
    public int Price { get; set; }
    public NewArrivalCurrencyType CurrencyType { get; set; } = NewArrivalCurrencyType.Gems;
    public bool IsFree { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public NewArrivalStatus Status { get; set; } = NewArrivalStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

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
    public string PrimaryColorPresetId { get; set; } = string.Empty;
    public string SecondaryColorPresetId { get; set; } = string.Empty;
    public string CustomPrimaryColorHex { get; set; } = string.Empty;
    public string CustomSecondaryColorHex { get; set; } = string.Empty;
    public List<string> EffectLayerIds { get; set; } = new();
    public float EffectOpacity { get; set; } = 1;
    public float EffectScale { get; set; } = 1;
    public float EffectSpeed { get; set; } = 1;
    public float EffectIntensity { get; set; } = 1;
    public string EquipTarget { get; set; } = string.Empty;
    public string LivingVisualScope { get; set; } = string.Empty;
    public string LivingVisualKind { get; set; } = string.Empty;
    public string LivingPackagePath { get; set; } = string.Empty;
    public string PreferredBackend { get; set; } = string.Empty;
    public string FallbackPolicy { get; set; } = string.Empty;
    public string LivingVisualVersion { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
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

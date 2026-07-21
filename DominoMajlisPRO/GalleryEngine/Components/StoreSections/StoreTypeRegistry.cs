namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public sealed record StoreTypeDefinition(
    string TypeId,
    string ArabicName,
    string EnglishName,
    string Icon,
    StoreView TargetView,
    bool SupportsCollection,
    bool SupportsSeason,
    bool IsPurchasable,
    bool IsCmsSupported);

public static class StoreTypeRegistry
{
    public static readonly StoreTypeDefinition Avatar = new("Avatar", "الشخصيات", "Avatars", "👤", StoreView.Avatars, true, true, true, true);
    public static readonly StoreTypeDefinition Background = new("Background", "الخلفيات", "Backgrounds", "▣", StoreView.Backgrounds, true, true, true, true);
    public static readonly StoreTypeDefinition Frame = new("Frame", "الإطارات", "Frames", "▢", StoreView.Frames, true, true, true, false);
    public static readonly StoreTypeDefinition Badge = new("Badge", "الشارات", "Badges", "◆", StoreView.Badges, true, true, true, false);
    public static readonly StoreTypeDefinition Emblem = new("Emblem", "الشعارات", "Emblems", "✦", StoreView.Emblems, true, true, true, false);
    public static readonly StoreTypeDefinition Effect = new("Effect", "المؤثرات", "Effects", "✨", StoreView.Effects, true, true, true, false);
    public static readonly StoreTypeDefinition Bundle = new("Bundle", "الحزم", "Bundles", "🎁", StoreView.Bundles, true, true, true, false);
    public static readonly StoreTypeDefinition Title = new("Title", "الألقاب", "Titles", "♛", StoreView.Titles, true, true, true, false);
    public static readonly StoreTypeDefinition Season = new("Season", "المواسم", "Seasons", "★", StoreView.SeasonPass, false, false, false, true);
    public static readonly StoreTypeDefinition Offer = new("Offer", "العروض", "Offers", "%", StoreView.LimitedOffers, false, true, true, true);
    public static readonly StoreTypeDefinition Category = new("Category", "الفئات", "Categories", "▦", StoreView.BrowseCategories, true, true, false, true);

    private static readonly IReadOnlyList<StoreTypeDefinition> Definitions =
    [
        Avatar, Background, Frame, Badge, Emblem, Effect, Bundle, Title, Season, Offer, Category
    ];

    private static readonly IReadOnlyDictionary<string, StoreTypeDefinition> Aliases = BuildAliases();

    public static IReadOnlyList<StoreTypeDefinition> All => Definitions;

    public static IReadOnlyList<StoreTypeDefinition> DefaultCategoryTypes { get; } =
    [
        Avatar, Background, Frame, Emblem, Badge, Effect, Title, Bundle
    ];

    public static StoreTypeDefinition? Get(string? typeId) => Resolve(typeId);

    public static StoreTypeDefinition? Resolve(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var key = Normalize(candidate);
            if (key.Length > 0 && Aliases.TryGetValue(key, out var definition))
                return definition;
        }

        return null;
    }

    public static bool IsPurchasableView(StoreView view) =>
        Definitions.Any(type => type.TargetView == view && type.IsPurchasable);

    private static IReadOnlyDictionary<string, StoreTypeDefinition> BuildAliases()
    {
        var aliases = new Dictionary<string, StoreTypeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in Definitions)
        {
            Add(aliases, definition, definition.TypeId, definition.EnglishName, definition.ArabicName);
        }

        Add(aliases, Avatar, "character", "characters", "شخصية");
        Add(aliases, Background, "خلفية");
        Add(aliases, Frame, "border", "إطار", "اطار");
        Add(aliases, Badge, "شارة");
        Add(aliases, Emblem, "شعار");
        Add(aliases, Effect, "مؤثر");
        Add(aliases, Bundle, "package", "packages", "حزمة");
        Add(aliases, Title, "لقب", "القاب", "ألقاب");
        Add(aliases, Season, "موسم");
        Add(aliases, Offer, "limitedoffer", "limitedoffers", "عرض");
        Add(aliases, Category, "collection", "collections", "فئة", "تصنيف");
        return aliases;
    }

    private static void Add(
        IDictionary<string, StoreTypeDefinition> aliases,
        StoreTypeDefinition definition,
        params string[] values)
    {
        foreach (var value in values)
        {
            var key = Normalize(value);
            if (key.Length > 0)
                aliases[key] = definition;
        }
    }

    private static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty).Trim().Where(char.IsLetterOrDigit)).ToLowerInvariant();
}

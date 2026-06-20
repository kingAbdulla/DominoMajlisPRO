namespace DominoMajlisPRO.GalleryEngine.Admin.Canonical;

public static class CanonicalStoreCatalog
{
    public static Task<IReadOnlyList<CanonicalOption>> LoadCategoriesAsync() =>
        Task.FromResult(CategoriesForAdmin());

    public static Task<IReadOnlyList<CanonicalOption>> LoadCollectionsAsync() =>
        Task.FromResult(Collections());

    public static IReadOnlyList<CanonicalOption> AssetTypes() =>
    [
        new("Avatar", "الصور الشخصية"),
        new("ProfileBackground", "الخلفيات"),
        new("Frame", "الإطارات"),
        new("Effect", "المؤثرات"),
        new("Title", "الألقاب"),
        new("Emblem", "الشعارات"),
        new("EmblemBackground", "خلفيات الشعارات"),
        new("TeamColor", "ألوان الفريق"),
        new("Bundle", "الحزم"),
        new("Currency", "العملات"),
        new("ProductCard", "بطاقات المنتجات"),
        new("CategoryCard", "بطاقات التصنيفات"),
        new("Season", "المواسم")
    ];

    public static IReadOnlyList<CanonicalOption> OwnerScopes() =>
    [
        new("Player", "اللاعب"),
        new("Team", "الفريق"),
        new("Global", "عام"),
        new("Developer", "المطور")
    ];

    public static IReadOnlyList<CanonicalOption> StoreTypes() =>
    [
        new("NewArrival", "وصل حديثاً"),
        new("LimitedOffer", "عرض محدود"),
        new("Category", "تصنيف"),
        new("Avatar", "صورة شخصية"),
        new("Background", "خلفية"),
        new("Emblem", "شعار"),
        new("Effect", "مؤثر"),
        new("Bundle", "حزمة"),
        new("Currency", "عملة")
    ];

    public static IReadOnlyList<CanonicalOption> Visibility() =>
    [
        new("Visible", "ظاهر"),
        new("Hidden", "مخفي"),
        new("DeveloperOnly", "للمطور فقط"),
        new("FounderOnly", "للمؤسس فقط"),
        new("HonorOnly", "لأصحاب الشرف فقط")
    ];

    public static IReadOnlyList<CanonicalOption> PublishStates() =>
    [
        new("Draft", "مسودة"),
        new("Published", "منشور"),
        new("Hidden", "مخفي"),
        new("Archived", "مؤرشف")
    ];

    public static IReadOnlyList<CanonicalOption> EquipTargets() =>
    [
        new("PlayerAvatar", "صورة اللاعب الشخصية"),
        new("PlayerBackground", "خلفية اللاعب"),
        new("PlayerFrame", "إطار اللاعب"),
        new("PlayerEffect", "مؤثر اللاعب"),
        new("PlayerTitle", "لقب اللاعب"),
        new("TeamEmblem", "شعار الفريق"),
        new("TeamColor", "لون الفريق"),
        new("TeamEmblemBackground", "خلفية شعار الفريق")
    ];

    public static IReadOnlyList<CanonicalOption> EffectTypes() =>
    [
        new("Glow", "توهج"),
        new("Particles", "جسيمات"),
        new("Trail", "أثر متحرك"),
        new("Aura", "هالة"),
        new("Fire", "نار"),
        new("Ice", "جليد"),
        new("Lightning", "برق"),
        new("Shadow", "ظل")
    ];

    public static IReadOnlyList<CanonicalOption> AnimationTypes() =>
    [
        new("None", "بدون حركة"),
        new("Glow", "توهج"),
        new("Pulse", "نبض"),
        new("Shine", "لمعة"),
        new("Float", "طفو"),
        new("Rotate", "دوران"),
        new("Sparkle", "بريق")
    ];

    public static IReadOnlyList<CanonicalOption> Currencies() =>
    [
        new("Coins", "العملات"),
        new("Gems", "الجواهر"),
        new("Free", "مجاني")
    ];

    public static IReadOnlyList<CanonicalOption> UnlockRequirements() =>
    [
        new("None", "بدون شرط"),
        new("TrustScore", "نقاط الثقة"),
        new("Rank", "الرتبة"),
        new("SeasonLevel", "مستوى الموسم"),
        new("HallOfFame", "قاعة المشاهير"),
        new("DeveloperOnly", "للمطور فقط"),
        new("FounderOnly", "للمؤسس فقط"),
        new("EventOnly", "للحدث فقط"),
        new("BundleOnly", "للحزمة فقط")
    ];

    public static IReadOnlyList<CanonicalOption> Rarities() =>
    [
        new("Common", "شائع"),
        new("Rare", "نادر"),
        new("Epic", "ملحمي"),
        new("Legendary", "أسطوري"),
        new("Mythic", "خرافي"),
        new("Immortal", "خالد")
    ];

    public static IReadOnlyList<CanonicalOption> Seasons() =>
    [
        new("None", "بدون موسم"),
        new("Season01", "الموسم الأول"),
        new("Season02", "الموسم الثاني"),
        new("Season03", "الموسم الثالث")
    ];

    public static IReadOnlyList<CanonicalOption> Events() =>
    [
        new("None", "بدون حدث"),
        new("LaunchEvent", "حدث الإطلاق"),
        new("RamadanEvent", "حدث رمضان"),
        new("NationalEvent", "حدث وطني"),
        new("SpecialEvent", "حدث خاص")
    ];

    public static IReadOnlyList<CanonicalOption> Collections() =>
    [
        new("Default", "المجموعة الافتراضية"),
        new("Team", "مجموعة الفريق"),
        new("Gold", "المجموعة الذهبية"),
        new("Royal", "المجموعة الملكية"),
        new("Legend", "المجموعة الأسطورية"),
        new("Sports", "المجموعة الرياضية"),
        new("Arabic", "المجموعة العربية"),
        new("Modern", "المجموعة الحديثة"),
        new("Military", "المجموعة العسكرية"),
        new("Seasonal", "المجموعة الموسمية")
    ];

    public static IReadOnlyList<CanonicalOption> Frames() =>
    [
        new("None", "بدون إطار"),
        new("GoldFrame", "الإطار الذهبي"),
        new("RoyalFrame", "الإطار الملكي"),
        new("LegendFrame", "الإطار الأسطوري")
    ];

    public static IReadOnlyList<CanonicalOption> GlowEffects() =>
    [
        new("#D4AF37", "ذهبي"),
        new("#2F80ED", "أزرق"),
        new("#9B51E0", "بنفسجي"),
        new("#E34B78", "وردي"),
        new("#FFFFFF", "أبيض")
    ];

    public static IReadOnlyList<CanonicalOption> UnlockTypes() =>
    [
        new("Free", "مجاني"),
        new("Coins", "بالعملات"),
        new("Gems", "بالجواهر"),
        new("SeasonPass", "تذكرة الموسم"),
        new("HallOfFame", "قاعة المشاهير"),
        new("Rank", "الرتبة"),
        new("Developer", "المطور"),
        new("Founder", "المؤسس"),
        new("Event", "حدث"),
        new("Bundle", "حزمة")
    ];

    public static IReadOnlyList<CanonicalOption> Styles() =>
    [
        new("Normal", "عادي"),
        new("Modern", "حديث"),
        new("Royal", "ملكي"),
        new("Legendary", "أسطوري"),
        new("Arabic", "عربي"),
        new("Military", "عسكري"),
        new("Sports", "رياضي")
    ];

    public static IReadOnlyList<CanonicalOption> Tags() =>
    [
        new("None", "بدون وسم"),
        new("New", "جديد"),
        new("Featured", "مميز"),
        new("Limited", "محدود"),
        new("Premium", "فاخر"),
        new("Seasonal", "موسمي")
    ];

    public static IReadOnlyList<CanonicalOption> Versions() =>
    [
        new("v1", "الإصدار الأول"),
        new("v2", "الإصدار الثاني"),
        new("v3", "الإصدار الثالث")
    ];

    public static IReadOnlyList<CanonicalOption> Animations() =>
        AnimationTypes();

    public static IReadOnlyList<CanonicalOption> DefaultCategoriesForAdmin() =>
    [
        new("Avatar", "الصور الشخصية"),
        new("ProfileBackground", "الخلفيات"),
        new("Emblem", "الشعارات"),
        new("EmblemBackground", "خلفيات الشعارات"),
        new("TeamColor", "ألوان الفريق"),
        new("Effect", "المؤثرات"),
        new("Bundle", "الحزم"),
        new("Currency", "العملات"),
        new("Season", "المواسم"),
        new("ProductCard", "بطاقات المنتجات"),
        new("CategoryCard", "بطاقات التصنيفات")
    ];

    public static IReadOnlyList<CanonicalOption> CategoriesForAdmin() =>
        DefaultCategoriesForAdmin();

    public static string GetCategoryDisplayName(string? categoryId)
    {
        var id = categoryId?.Trim() ?? string.Empty;

        if (string.Equals(id, "Ass", StringComparison.OrdinalIgnoreCase))
            return "الصور الشخصية";

        return FindDisplayName(DefaultCategoriesForAdmin(), id, "غير محدد");
    }

    public static string GetCollectionDisplayName(string? collectionId) =>
        FindDisplayName(Collections(), collectionId, "غير محدد");

    public static string GetSeasonDisplayName(string? seasonId) =>
        FindDisplayName(Seasons(), seasonId, "بدون موسم");

    private static string FindDisplayName(
        IReadOnlyList<CanonicalOption> options,
        string? canonicalId,
        string emptyFallback)
    {
        var id = canonicalId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
            return emptyFallback;

        return options.FirstOrDefault(item =>
                   string.Equals(
                       item.CanonicalId,
                       id,
                       StringComparison.OrdinalIgnoreCase))
               ?.DisplayName
            ?? id;
    }
}

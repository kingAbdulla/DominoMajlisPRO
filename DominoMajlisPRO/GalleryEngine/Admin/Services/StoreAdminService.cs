using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StoreAdminService
{
    public static IReadOnlyList<StoreAdminSection> GetSections()
    {
        return new List<StoreAdminSection>
        {
            CreateSection("inventory-audit", "Inventory Audit", "Catalog health, malformed products, and safe repair", "✓", StoreCardTemplateType.StoreSettings, -1),
            CreateSection("current-season", "الموسم الحالي", "إدارة بطاقة الموسم والصورة الرئيسية", "♛", StoreCardTemplateType.SeasonHero, 0),
            CreateSection("new-arrivals", "وصل حديثاً", "عناصر المتجر الجديدة", "🔥", StoreCardTemplateType.ProductCard, 1),
            CreateSection("limited-offers", "عروض محدودة", "العروض المؤقتة والأسعار القديمة", "⌛", StoreCardTemplateType.ProductCard, 2),
            CreateSection("categories", "الفئات", "تصنيف المتجر وبطاقات التصفح", "▦", StoreCardTemplateType.CategoryCard, 3),
            CreateSection("avatars", "الصور الشخصية", "إدارة صور اللاعبين الرمزية", "◉", StoreCardTemplateType.Avatar, 4),
            CreateSection("backgrounds", "الخلفيات", "خلفيات البروفايل والمتجر", "▣", StoreCardTemplateType.CategoryCard, 5),
            CreateSection("emblems", "الشعارات", "شعارات الفرق والهوية", "✦", StoreCardTemplateType.ProductCard, 6),
            CreateSection("emblem-backgrounds", "خلفيات الشعارات", "خلفيات هوية شعارات الفرق", "▧", StoreCardTemplateType.ProductCard, 7),
            CreateSection("team-colors", "ألوان الفرق", "ألوان هوية الفرق", "◈", StoreCardTemplateType.ProductCard, 8),
            CreateSection("effects", "التأثيرات", "مؤثرات العرض والزينة", "✧", StoreCardTemplateType.ProductCard, 9),
            CreateSection("frames", "الإطارات", "إطارات هوية اللاعب", "▢", StoreCardTemplateType.ProductCard, 10),
            CreateSection("titles", "الألقاب", "ألقاب هوية اللاعب", "T", StoreCardTemplateType.ProductCard, 11),
            CreateSection("bundles", "الحزم", "مجموعات المنتجات والعروض", "▰", StoreCardTemplateType.ProductCard, 12),
            CreateSection("currency-pricing", "العملات والأسعار", "الجواهر والعملات وقواعد التسعير", "◆", StoreCardTemplateType.CurrencyPricing, 13),
            CreateSection("product-cards", "بطاقات المنتجات", "إدارة بطاقات المنتجات المنشورة", "▤", StoreCardTemplateType.ProductCard, 14),
            CreateSection("category-cards", "بطاقات الفئات", "إدارة بطاقات تصفح الفئات", "▦", StoreCardTemplateType.CategoryCard, 15),
            CreateSection("store-settings", "إعدادات المتجر", "إعدادات النشر والظهور العامة", "⚙", StoreCardTemplateType.StoreSettings, 16)
        };
    }

    public static bool ValidateText(StoreAdminContentItem item, StoreTextLimitRule rule, out string message)
    {
        message = rule.ValidationMessage;

        if (rule.TitleMaxLength.HasValue && item.Title.Length > rule.TitleMaxLength.Value)
            return false;

        if (rule.NameMaxLength.HasValue && item.Name.Length > rule.NameMaxLength.Value)
            return false;

        if (rule.SubtitleMaxLength.HasValue && item.Subtitle.Length > rule.SubtitleMaxLength.Value)
            return false;

        if (rule.DescriptionMaxLength.HasValue && item.Description.Length > rule.DescriptionMaxLength.Value)
            return false;

        if (rule.PriceMaxLength.HasValue && item.Price.ToString().Length > rule.PriceMaxLength.Value)
            return false;

        return true;
    }

    public static string GetAdminStorageRoot()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "gallery-store-admin");
    }

    private static StoreAdminSection CreateSection(
        string id,
        string title,
        string subtitle,
        string icon,
        StoreCardTemplateType templateType,
        int sortOrder)
    {
        return new StoreAdminSection
        {
            Id = id,
            Title = title,
            Subtitle = subtitle,
            Icon = icon,
            TemplateType = templateType,
            TextLimits = StoreTextLimitRule.ForTemplate(templateType),
            ImageRule = StoreImageRule.ForTemplate(templateType),
            SortOrder = sortOrder
        };
    }
}

using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StoreAdminService
{
    public static IReadOnlyList<StoreAdminSection> GetSections() =>
        new List<StoreAdminSection>
        {
            Section("inventory-audit", "تدقيق المخزون", "فحص الكتالوج والمنتجات غير المكتملة وإصلاحها بأمان", "✓", StoreCardTemplateType.StoreSettings, -1),
            Section("current-season", "الموسم الحالي", "إدارة بطاقة الموسم وصورته الرئيسية", "♛", StoreCardTemplateType.SeasonHero, 0),
            Section("new-arrivals", "وصل حديثًا", "عناصر المتجر الجديدة", "+", StoreCardTemplateType.ProductCard, 1),
            Section("limited-offers", "عروض محدودة", "العروض المؤقتة والأسعار السابقة", "⌛", StoreCardTemplateType.ProductCard, 2),
            Section("categories", "الفئات", "تصنيف المتجر وبطاقات التصفح", "▦", StoreCardTemplateType.CategoryCard, 3),
            Section("avatars", "الصور الشخصية", "إدارة صور اللاعبين الرمزية", "◉", StoreCardTemplateType.Avatar, 4),
            Section("backgrounds", "الخلفيات", "خلفيات ملف اللاعب والمتجر", "▣", StoreCardTemplateType.CategoryCard, 5),
            Section("emblems", "الشعارات", "شعارات الفرق والهوية", "✦", StoreCardTemplateType.ProductCard, 6),
            Section("emblem-backgrounds", "خلفيات الشعارات", "خلفيات هوية شعارات الفرق", "▧", StoreCardTemplateType.ProductCard, 7),
            Section("team-colors", "ألوان الفرق", "ألوان هوية الفرق", "◆", StoreCardTemplateType.ProductCard, 8),
            Section("effects", "التأثيرات", "المؤثرات البصرية المنشورة", "✧", StoreCardTemplateType.ProductCard, 9),
            Section("typography", "تأثيرات الأسماء والخطوط", "اسم اللاعب واسم الفريق وإطارات النص", "T", StoreCardTemplateType.ProductCard, 10),
            Section("frames", "الإطارات", "الإطارات المنشورة فقط", "□", StoreCardTemplateType.ProductCard, 11),
            Section("titles", "الألقاب", "ألقاب هوية اللاعب", "T", StoreCardTemplateType.ProductCard, 12),
            Section("bundles", "الحزم", "مجموعات المنتجات والعروض", "▰", StoreCardTemplateType.ProductCard, 13),
            Section("currency-pricing", "العملات والأسعار", "الجواهر والعملات وقواعد التسعير", "◆", StoreCardTemplateType.CurrencyPricing, 14),
            Section("product-cards", "بطاقات المنتجات", "إدارة بطاقات المنتجات المنشورة", "▤", StoreCardTemplateType.ProductCard, 15),
            Section("category-cards", "بطاقات الفئات", "إدارة بطاقات تصفح الفئات", "▦", StoreCardTemplateType.CategoryCard, 16),
            Section("store-settings", "إعدادات المتجر", "إعدادات النشر والظهور العامة", "⚙", StoreCardTemplateType.StoreSettings, 17)
        };

    public static bool ValidateText(StoreAdminContentItem item, StoreTextLimitRule rule, out string message)
    {
        message = rule.ValidationMessage;
        if (rule.TitleMaxLength.HasValue && item.Title.Length > rule.TitleMaxLength.Value) return false;
        if (rule.NameMaxLength.HasValue && item.Name.Length > rule.NameMaxLength.Value) return false;
        if (rule.SubtitleMaxLength.HasValue && item.Subtitle.Length > rule.SubtitleMaxLength.Value) return false;
        if (rule.DescriptionMaxLength.HasValue && item.Description.Length > rule.DescriptionMaxLength.Value) return false;
        if (rule.PriceMaxLength.HasValue && item.Price.ToString().Length > rule.PriceMaxLength.Value) return false;
        return true;
    }

    public static string GetAdminStorageRoot() =>
        Path.Combine(FileSystem.AppDataDirectory, "gallery-store-admin");

    private static StoreAdminSection Section(
        string id,
        string title,
        string subtitle,
        string icon,
        StoreCardTemplateType templateType,
        int sortOrder) =>
        new()
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

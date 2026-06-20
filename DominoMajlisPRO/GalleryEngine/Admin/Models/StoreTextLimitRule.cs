namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StoreTextLimitRule
{
    public int? TitleMaxLength { get; set; }
    public int? NameMaxLength { get; set; }
    public int? SubtitleMaxLength { get; set; }
    public int? DescriptionMaxLength { get; set; }
    public int? DescriptionMaxLines { get; set; }
    public int? ButtonTextMaxLength { get; set; }
    public int? PriceMaxLength { get; set; }
    public int? FullDescriptionMaxLength { get; set; }

    public string ValidationMessage { get; set; } = "النص أطول من المساحة المسموحة لهذه البطاقة";

    public static StoreTextLimitRule ForTemplate(StoreCardTemplateType templateType)
    {
        return templateType switch
        {
            StoreCardTemplateType.SeasonHero => new StoreTextLimitRule
            {
                TitleMaxLength = 32,
                SubtitleMaxLength = 44,
                DescriptionMaxLength = 120,
                DescriptionMaxLines = 2,
                ButtonTextMaxLength = 18
            },
            StoreCardTemplateType.ProductCard => new StoreTextLimitRule
            {
                NameMaxLength = 24,
                SubtitleMaxLength = 36,
                DescriptionMaxLength = 80,
                DescriptionMaxLines = 2,
                PriceMaxLength = 9,
                FullDescriptionMaxLength = 500
            },
            StoreCardTemplateType.CategoryCard => new StoreTextLimitRule
            {
                NameMaxLength = 18,
                DescriptionMaxLength = 50
            },
            StoreCardTemplateType.Avatar => new StoreTextLimitRule
            {
                NameMaxLength = 24,
                SubtitleMaxLength = 36,
                DescriptionMaxLength = 80,
                DescriptionMaxLines = 2,
                FullDescriptionMaxLength = 500
            },
            StoreCardTemplateType.FullProductDetails => new StoreTextLimitRule
            {
                FullDescriptionMaxLength = 500
            },
            _ => new StoreTextLimitRule()
        };
    }
}

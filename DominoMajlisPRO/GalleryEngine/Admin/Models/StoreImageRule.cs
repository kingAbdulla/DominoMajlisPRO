namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StoreImageRule
{
    public double AspectRatio { get; set; }
    public Aspect ImageFit { get; set; } = Aspect.AspectFill;
    public bool ClipToPreviewBounds { get; set; } = true;
    public bool UseCircleFrame { get; set; }
    public string StorageFolder { get; set; } = "store-admin";

    public static StoreImageRule ForTemplate(StoreCardTemplateType templateType)
    {
        return templateType switch
        {
            StoreCardTemplateType.SeasonHero => new StoreImageRule
            {
                AspectRatio = 16d / 9d,
                ImageFit = Aspect.AspectFill,
                StorageFolder = "season-hero"
            },
            StoreCardTemplateType.ProductCard => new StoreImageRule
            {
                AspectRatio = 1d,
                ImageFit = Aspect.AspectFit,
                StorageFolder = "products"
            },
            StoreCardTemplateType.CategoryCard => new StoreImageRule
            {
                AspectRatio = 4d / 3d,
                ImageFit = Aspect.AspectFill,
                StorageFolder = "categories"
            },
            StoreCardTemplateType.Avatar => new StoreImageRule
            {
                AspectRatio = 1d,
                ImageFit = Aspect.AspectFill,
                UseCircleFrame = true,
                StorageFolder = "avatars"
            },
            _ => new StoreImageRule
            {
                AspectRatio = 1d,
                ImageFit = Aspect.AspectFit
            }
        };
    }
}

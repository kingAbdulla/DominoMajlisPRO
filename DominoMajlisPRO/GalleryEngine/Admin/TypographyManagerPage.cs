using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class TypographyManagerPage : SpecializedStoreManagerPage
{
    public TypographyManagerPage()
        : base(new SpecializedStoreManagerDefinition(
            "أسماء و إطارات الهوية",
            "نشر تأثيرات أسماء اللاعبين والفرق وإطاراتها",
            new[]
            {
                StoreProductAssetType.PlayerNameEffect,
                StoreProductAssetType.TeamNameEffect,
                StoreProductAssetType.PlayerNameFrame,
                StoreProductAssetType.TeamNameFrame
            },
            IsEffect: true))
    {
    }
}

namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public enum StoreCmsAssetSection { CurrentSeason, NewArrivals, LimitedOffers, StoreCategories, Avatars, Backgrounds, Effects, Bundles }

public static class StoreCmsAssetPickerService
{
    private static readonly IReadOnlyDictionary<StoreCmsAssetSection, string> Folders = new Dictionary<StoreCmsAssetSection, string>
    {
        [StoreCmsAssetSection.CurrentSeason] = "season-hero", [StoreCmsAssetSection.NewArrivals] = "new-arrivals", [StoreCmsAssetSection.LimitedOffers] = "limited-offers", [StoreCmsAssetSection.StoreCategories] = "categories", [StoreCmsAssetSection.Avatars] = "avatars", [StoreCmsAssetSection.Backgrounds] = "backgrounds", [StoreCmsAssetSection.Effects] = "effects", [StoreCmsAssetSection.Bundles] = "bundles"
    };

    public static string GetSectionFolder(StoreCmsAssetSection section) => Path.Combine(FileSystem.AppDataDirectory, "store-cms-assets", Folders[section]);
    public static Task<IReadOnlyList<string>> ListAssetsAsync(StoreCmsAssetSection section)
    {
        var folder = GetSectionFolder(section); Directory.CreateDirectory(folder);
        IReadOnlyList<string> files = Directory.EnumerateFiles(folder).Where(IsImage).OrderByDescending(File.GetLastWriteTimeUtc).ToList();
        return Task.FromResult(files);
    }
    public static async Task<string?> ImportImageAsync(StoreCmsAssetSection section, string title)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title, FileTypes = FilePickerFileType.Images });
        if (result == null) return null;
        var extension = Path.GetExtension(result.FileName); var folder = GetSectionFolder(section); Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, $"{Guid.NewGuid():N}{extension}");
        await using var source = await result.OpenReadAsync(); await using var target = File.Create(destination); await source.CopyToAsync(target);
        return destination;
    }
    private static bool IsImage(string path) => new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}

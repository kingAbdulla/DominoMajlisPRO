using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StorePricingAdminService
{
    private const string FileName = "gallery_store_pricing.json";

    public static Task<List<StorePricingConfiguration>> LoadAsync() =>
        StoreCmsJsonRepository.LoadListAsync<StorePricingConfiguration>(StoragePath());

    public static async Task SaveAsync(StorePricingConfiguration configuration)
    {
        var records = await LoadAsync();
        configuration.UpdatedAt = DateTime.UtcNow;
        records.RemoveAll(item => string.Equals(item.Id, configuration.Id, StringComparison.OrdinalIgnoreCase));
        records.Add(configuration);
        await StoreCmsJsonRepository.SaveListAsync(StoragePath(), records);
    }

    private static string StoragePath() =>
        Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);
}

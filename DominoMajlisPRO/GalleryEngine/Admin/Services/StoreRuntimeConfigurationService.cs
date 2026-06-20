using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StoreRuntimeConfigurationService
{
    private const string FileName = "gallery_store_runtime_configuration.json";
    public static event Action? Changed;

    public static async Task<StoreRuntimeConfiguration> LoadAsync() =>
        (await StoreCmsJsonRepository.LoadListAsync<StoreRuntimeConfiguration>(StoragePath))
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault() ?? new StoreRuntimeConfiguration();

    public static async Task SaveAsync(StoreRuntimeConfiguration configuration)
    {
        configuration.Id = "runtime";
        configuration.PageSize = Math.Clamp(configuration.PageSize, 1, 100);
        configuration.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(StoragePath, new[] { configuration });
        Changed?.Invoke();
    }

    private static string StoragePath =>
        Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);
}

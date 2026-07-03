using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.Localization;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record StoreAssetSearchEntry(
    string AssetId,
    string AssetType,
    string Name,
    string Description,
    string Category,
    string ImagePath,
    DateTime PublishedAt);

public static class StoreAssetQueryService
{
    public static event Action? PublishedContentChanged;

    static StoreAssetQueryService()
    {
        NewArrivalsAdminService.PublishedChanged += RaisePublishedContentChanged;
        LimitedOffersAdminService.PublishedChanged += RaisePublishedContentChanged;
        StoreCategoriesAdminService.PublishedChanged += RaisePublishedContentChanged;
        AvatarsAdminService.PublishedChanged += RaisePublishedContentChanged;
        BackgroundsAdminService.PublishedChanged += RaisePublishedContentChanged;
        CurrentSeasonAdminService.PublishedChanged += _ => RaisePublishedContentChanged();
        StoreRuntimeConfigurationService.Changed += RaisePublishedContentChanged;
    }

    public static async Task<IReadOnlyList<NewArrivalRecord>> LoadNewArrivalsAsync()
    {
        var configuration = await StoreRuntimeConfigurationService.LoadAsync();
        if (!configuration.IsStoreEnabled || !configuration.ShowNewArrivals)
            return Array.Empty<NewArrivalRecord>();
        var records = (await NewArrivalsAdminService.LoadPublishedAsync()).ToList();
        return NewestDistinct(records.Where(IsValid), item => item.Id, item => item.PublishedAt ?? item.UpdatedAt);
    }

    public static async Task<IReadOnlyList<LimitedOfferRecord>> LoadActiveOffersAsync()
    {
        var configuration = await StoreRuntimeConfigurationService.LoadAsync();
        if (!configuration.IsStoreEnabled || !configuration.ShowLimitedOffers)
            return Array.Empty<LimitedOfferRecord>();
        var now = DateTime.Now;
        var records = await LimitedOffersAdminService.LoadActivePublishedAsync();
        return NewestDistinct(
            records.Where(item => IsValid(item) && item.StartsAt <= now && item.EndsAt >= now),
            item => item.Id,
            item => item.PublishedAt ?? item.UpdatedAt);
    }

    public static async Task<IReadOnlyList<StoreCategoryRecord>> LoadCategoriesAsync()
    {
        var configuration = await StoreRuntimeConfigurationService.LoadAsync();
        if (!configuration.IsStoreEnabled || !configuration.ShowBrowseCategories)
            return Array.Empty<StoreCategoryRecord>();
        var records = await StoreCategoriesAdminService.LoadPublishedAsync();
        return NewestDistinct(
            records.Where(item => item.IsVisible && IsValid(item)),
            item => item.Id,
            item => item.PublishedAt ?? item.UpdatedAt);
    }

    public static async Task<IReadOnlyList<AvatarRecord>> LoadAvatarsAsync()
    {
        if (!(await StoreRuntimeConfigurationService.LoadAsync()).IsStoreEnabled)
            return Array.Empty<AvatarRecord>();
        var records = await AvatarsAdminService.LoadPublishedAsync();
        return NewestDistinct(records.Where(IsValid), item => item.Id, item => item.PublishedAt ?? item.UpdatedAt)
            .Select(RecoverAvatar)
            .ToList();
    }

    public static async Task<IReadOnlyList<BackgroundRecord>> LoadBackgroundsAsync()
    {
        if (!(await StoreRuntimeConfigurationService.LoadAsync()).IsStoreEnabled)
            return Array.Empty<BackgroundRecord>();
        var records = await BackgroundsAdminService.LoadPublishedAsync();
        return NewestDistinct(records.Where(IsValid), item => item.Id, item => item.PublishedAt ?? item.UpdatedAt)
            .Select(RecoverBackground)
            .ToList();
    }

    public static async Task<CurrentSeasonRecord?> LoadCurrentSeasonAsync()
    {
        return (await LoadPublishedSeasonsAsync()).FirstOrDefault();
    }

    public static async Task<IReadOnlyList<CurrentSeasonRecord>> LoadPublishedSeasonsAsync()
    {
        if (!(await StoreRuntimeConfigurationService.LoadAsync()).IsStoreEnabled)
            return Array.Empty<CurrentSeasonRecord>();
        var now = DateTime.Now;
        var records = await CurrentSeasonAdminService.LoadPublishedRecordsAsync();
        return records
            .Where(item => IsValid(item) && (!item.StartsAt.HasValue || item.StartsAt <= now) && (!item.EndsAt.HasValue || item.EndsAt >= now))
            .OrderBy(item => item.SortOrder)
            .ThenByDescending(item => item.PublishedAt ?? item.UpdatedAt)
            .DistinctBy(CurrentSeasonAdminService.GetIdentity, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task<IReadOnlyList<StoreAssetSearchEntry>> SearchAsync(string? query)
    {
        var term = query?.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return Array.Empty<StoreAssetSearchEntry>();

        var arrivalsTask = LoadNewArrivalsAsync();
        var offersTask = LoadActiveOffersAsync();
        var avatarsTask = LoadAvatarsAsync();
        var backgroundsTask = LoadBackgroundsAsync();
        await Task.WhenAll(arrivalsTask, offersTask, avatarsTask, backgroundsTask);

        var results = new List<StoreAssetSearchEntry>();
        results.AddRange(arrivalsTask.Result.Select(item => Entry(item.Id, "NewArrival", Recover(item.Title), Recover(item.Description), Recover(item.Category), item.ImagePath, item.PublishedAt ?? item.UpdatedAt)));
        results.AddRange(offersTask.Result.Select(item => Entry(item.Id, StoreTypeRegistry.Offer.TypeId, Recover(item.Title), Recover(item.Description), Recover(item.Category), item.ImagePath, item.PublishedAt ?? item.UpdatedAt)));
        results.AddRange(avatarsTask.Result.Select(item => Entry(item.Id, StoreTypeRegistry.Avatar.TypeId, DisplayName(item.NameAr, item.NameEn), Recover(item.Description), Recover(item.CategoryId), PreferredImage(item.ThumbnailPath, item.ImagePath), item.PublishedAt ?? item.UpdatedAt)));
        results.AddRange(backgroundsTask.Result.Select(item => Entry(item.Id, StoreTypeRegistry.Background.TypeId, DisplayName(item.NameAr, item.NameEn), Recover(item.Description), Recover(item.CategoryId), PreferredImage(item.ThumbnailPath, item.ImagePath), item.PublishedAt ?? item.UpdatedAt)));

        return results
            .Where(item => Contains(item.Name, term) || Contains(item.Description, term) || Contains(item.Category, term))
            .OrderByDescending(item => item.PublishedAt)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool IsValid(NewArrivalRecord item) =>
        !string.IsNullOrWhiteSpace(item.Id) &&
        !string.IsNullOrWhiteSpace(item.Title) &&
        (!StoreProductAssetTypeCatalog.TryResolve(item.StoreTypeId, out var type) ||
         !StoreProductAssetTypeCatalog.RequiresImagePayload(type) ||
         HasValidImageReference(item.ImagePath));

    private static bool IsValid(LimitedOfferRecord item) =>
        !string.IsNullOrWhiteSpace(item.Id) &&
        !string.IsNullOrWhiteSpace(item.Title) &&
        HasValidImageReference(item.ImagePath);

    private static bool IsValid(StoreCategoryRecord item) =>
        !string.IsNullOrWhiteSpace(item.Id) &&
        (!string.IsNullOrWhiteSpace(item.NameAr) || !string.IsNullOrWhiteSpace(item.NameEn)) &&
        HasValidImageReference(PreferredImage(item.BannerPath, item.IconPath));

    private static bool IsValid(AvatarRecord item) =>
        !string.IsNullOrWhiteSpace(item.Id) &&
        (!string.IsNullOrWhiteSpace(item.NameAr) || !string.IsNullOrWhiteSpace(item.NameEn)) &&
        !string.IsNullOrWhiteSpace(item.CategoryId) &&
        HasValidImageReference(item.ImagePath);

    private static bool IsValid(BackgroundRecord item) =>
        !string.IsNullOrWhiteSpace(item.Id) &&
        (!string.IsNullOrWhiteSpace(item.NameAr) || !string.IsNullOrWhiteSpace(item.NameEn)) &&
        !string.IsNullOrWhiteSpace(item.CategoryId) &&
        HasValidImageReference(item.ImagePath);

    private static bool IsValid(CurrentSeasonRecord item) =>
        !string.IsNullOrWhiteSpace(CurrentSeasonAdminService.GetIdentity(item)) &&
        !string.IsNullOrWhiteSpace(item.Title) &&
        item.IsVisible &&
        HasValidImageReference(item.ImagePath);

    private static IReadOnlyList<T> NewestDistinct<T>(
        IEnumerable<T> records,
        Func<T, string> id,
        Func<T, DateTime> publishedAt) => records
        .OrderByDescending(publishedAt)
        .DistinctBy(id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static AvatarRecord RecoverAvatar(AvatarRecord item)
    {
        item.NameAr = Recover(item.NameAr);
        item.NameEn = Recover(item.NameEn);
        item.Description = Recover(item.Description);
        item.CategoryId = Recover(item.CategoryId);
        item.Collection = Recover(item.Collection);
        item.Tag = Recover(item.Tag);
        item.GenderOrStyle = Recover(item.GenderOrStyle);
        return item;
    }

    private static BackgroundRecord RecoverBackground(BackgroundRecord item)
    {
        item.NameAr = Recover(item.NameAr);
        item.NameEn = Recover(item.NameEn);
        item.Description = Recover(item.Description);
        item.CategoryId = Recover(item.CategoryId);
        item.Collection = Recover(item.Collection);
        item.Tag = Recover(item.Tag);
        return item;
    }

    private static bool HasValidImageReference(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        var value = imagePath.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return !uri.IsFile || File.Exists(uri.LocalPath);

        return !Path.IsPathRooted(value) || File.Exists(value);
    }

    private static string PreferredImage(string preferred, string fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static string DisplayName(string arabic, string english) =>
        string.IsNullOrWhiteSpace(arabic) ? Recover(english) : Recover(arabic);

    private static string Recover(string value) =>
        ArabicTextRecoveryService.RecoverDisplayText(value);

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.CurrentCultureIgnoreCase);

    private static StoreAssetSearchEntry Entry(string id, string type, string name, string description, string category, string image, DateTime publishedAt) =>
        new(id, type, name, description, category, image, publishedAt);

    private static void RaisePublishedContentChanged() => PublishedContentChanged?.Invoke();

}

using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StoreResetService
{
    private const string RuntimeConfigurationFile = "gallery_store_runtime_configuration.json";
    private static readonly HashSet<string> ProtectedLedgerFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        RuntimeConfigurationFile,
        "season_reward_claims.json",
        "season_archives.json"
    };
    private static readonly string[] ReferencePropertyNames =
    {
        "AssetId", "ProductId", "RewardAssetId", "FrameAssetId", "EffectAssetId"
    };

    public static async Task<StoreResetReport> ResetDeveloperStoreAsync()
    {
        var root = StoreAdminService.GetAdminStorageRoot();
        var completedAt = DateTime.UtcNow;
        var backupPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "gallery-store-admin-emergency-backups",
            completedAt.ToString("yyyyMMdd-HHmmssfff"));

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(backupPath);

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToList();
        var deletedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var published = 0;
        var drafts = 0;
        var offers = 0;
        var categories = 0;

        foreach (var source in files)
        {
            var relative = Path.GetRelativePath(root, source);
            var destination = Path.Combine(backupPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);

            if (ProtectedLedgerFiles.Contains(Path.GetFileName(source)))
                continue;

            var counts = await InspectAsync(source, deletedIds);
            published += counts.Published;
            drafts += counts.Drafts;
            if (relative.Contains("limited_offer", StringComparison.OrdinalIgnoreCase))
                offers += counts.Records;
            if (relative.Contains("categor", StringComparison.OrdinalIgnoreCase))
                categories += counts.Records;
        }

        var orphanReferences = await CountOrphanReferencesAsync(root, backupPath, deletedIds);

        foreach (var source in files)
        {
            if (!ProtectedLedgerFiles.Contains(Path.GetFileName(source)))
                File.Delete(source);
        }

        DeleteEmptyDirectories(root);
        NewArrivalsAdminService.NotifyPublishedChanged();
        LimitedOffersAdminService.NotifyPublishedChanged();
        CurrentSeasonAdminService.NotifyPublishedChanged();
        StoreCategoriesAdminService.NotifyPublishedChanged();
        AvatarsAdminService.NotifyPublishedChanged();
        BackgroundsAdminService.NotifyPublishedChanged();

        return new StoreResetReport
        {
            PublishedCount = published,
            DraftCount = drafts,
            LimitedOfferCount = offers,
            CategoryCount = categories,
            OrphanReferenceCount = orphanReferences,
            BackupPath = backupPath,
            CompletedAtUtc = completedAt
        };
    }

    private static async Task<(int Records, int Published, int Drafts)> InspectAsync(
        string path,
        ISet<string> deletedIds)
    {
        if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            return default;

        try
        {
            await using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(stream);
            var records = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { document.RootElement };
            var published = 0;
            var drafts = 0;

            foreach (var record in records)
            {
                CollectIds(record, deletedIds);
                if (!TryGetPropertyIgnoreCase(record, "Status", out var status))
                    continue;

                var value = status.ValueKind == JsonValueKind.String ? status.GetString() : status.ToString();
                if (string.Equals(value, "Published", StringComparison.OrdinalIgnoreCase) || value == "1")
                    published++;
                else if (string.Equals(value, "Draft", StringComparison.OrdinalIgnoreCase) || value == "0")
                    drafts++;
            }

            return (records.Count, published, drafts);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static void CollectIds(JsonElement element, ISet<string> ids)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("Id") || property.NameEquals("AssetId") || property.NameEquals("ProductId")) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.Value.GetString()))
                {
                    ids.Add(property.Value.GetString()!);
                }

                CollectIds(property.Value, ids);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                CollectIds(child, ids);
        }
    }

    private static async Task<int> CountOrphanReferencesAsync(
        string adminRoot,
        string backupRoot,
        IReadOnlySet<string> deletedIds)
    {
        if (deletedIds.Count == 0)
            return 0;

        var count = 0;
        foreach (var path in Directory.EnumerateFiles(FileSystem.AppDataDirectory, "*.json", SearchOption.AllDirectories))
        {
            if (path.StartsWith(adminRoot, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(backupRoot, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                await using var stream = File.OpenRead(path);
                using var document = await JsonDocument.ParseAsync(stream);
                count += CountReferences(document.RootElement, deletedIds);
            }
            catch (JsonException)
            {
                // Non-store JSON is intentionally ignored; reset never mutates it.
            }
        }

        return count;
    }

    private static int CountReferences(JsonElement element, IReadOnlySet<string> deletedIds)
    {
        var count = 0;
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ReferencePropertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    deletedIds.Contains(property.Value.GetString() ?? string.Empty))
                {
                    count++;
                }

                count += CountReferences(property.Value, deletedIds);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                count += CountReferences(child, deletedIds);
        }

        return count;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static void DeleteEmptyDirectories(string root)
    {
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
    }
}

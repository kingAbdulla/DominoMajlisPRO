using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Admin.Services;

namespace DominoMajlisPRO.Services;

public static class BackupService
{
    const int BackupFormatVersion = 1;
    const int SchemaVersion = 1;
    const long MaxArchiveBytes = 75L * 1024L * 1024L;
    const long MaxJsonEntryBytes = 8L * 1024L * 1024L;
    const string ManifestFileName = "backup_manifest.json";
    const string AdminRootName = "gallery-store-admin";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static readonly HashSet<string> AllowedTopLevelFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "application_users.json",
        "current_user_session.json",
        "supabase_account_links.json",
        "local_account_credentials.json",
        "developer_lock.json",
        "honor_identity.json",
        "special_honor_keys.json",
        "special_honor_identities.json",
        "security_logs.json",
        "players.json",
        "teams.json",
        "matches.json",
        "rankings.json",
        "rivalries.json",
        "rankings_position_history.json",
        "hall_of_fame_audit.json",
        "player_display_name_history.json",
        "user_privacy_profile.json",
        "rank_reward_grants.json",
        "player_wallets.json",
        "player_owned_assets.json",
        "player_owned_store_items.json",
        "team_owned_assets.json",
        "store_purchases.json",
        "recharge_wallets.json",
        "recharge_catalog.json",
        "recharge_purchase_history.json",
        "recharge_claimed_rewards.json",
        "wheel_claims.json",
        "wheel_spin_history.json"
    };

    static readonly HashSet<string> AllowedAdminFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "gallery_avatars_admin.json",
        "gallery_backgrounds_admin.json",
        "gallery_current_season_admin.json",
        "gallery_limited_offers_admin.json",
        "gallery_new_arrivals_admin.json",
        "gallery_store_categories_admin.json",
        "gallery_store_pricing.json",
        "gallery_store_runtime_configuration.json",
        "season_definitions.json",
        "season_stories.json",
        "season_reward_rules.json",
        "season_reward_claims.json",
        "season_archives.json"
    };

    public static async Task<string> CreateBackupAsync()
    {
        string backupName =
            $"DominoMajlisPRO_Backup_{DateTime.Now:yyyy_MM_dd_HH_mm}.zip";

        string backupPath =
            Path.Combine(FileSystem.CacheDirectory, backupName);

        await CreateBackupFileAsync(backupPath);

        return backupPath;
    }

    public static async Task<string> CreateEmergencyBackupAsync()
    {
        string backupName =
            $"DominoMajlisPRO_Emergency_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.zip";

        string backupPath =
            Path.Combine(FileSystem.CacheDirectory, backupName);

        await CreateBackupFileAsync(backupPath);

        return backupPath;
    }

    public static async Task<string> CreateDeveloperResetBackupAsync()
    {
        string backupName =
            $"DominoMajlisPRO_Before_Full_Reset_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.zip";

        string backupPath =
            Path.Combine(FileSystem.CacheDirectory, backupName);

        await CreateBackupFileAsync(backupPath);

        return backupPath;
    }

    static async Task CreateBackupFileAsync(string backupPath)
    {
        string temporaryPath = $"{backupPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            var candidates = await BuildBackupCandidatesAsync();
            var manifestEntries = new List<BackupManifestEntry>();

            foreach (var candidate in candidates)
            {
                await using var stream = File.OpenRead(candidate.AbsolutePath);
                await ValidateJsonAsync(stream, candidate.RelativePath);

                manifestEntries.Add(new BackupManifestEntry(
                    candidate.RelativePath,
                    await ComputeSha256Async(candidate.AbsolutePath),
                    new FileInfo(candidate.AbsolutePath).Length,
                    candidate.Dataset));
            }

            var manifest = new BackupManifest(
                BackupFormatVersion,
                AppInfo.Current.VersionString,
                SchemaVersion,
                DateTimeOffset.UtcNow,
                DeviceInfo.Current.Platform.ToString(),
                manifestEntries);

            using (ZipArchive archive =
                   ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                foreach (var candidate in candidates)
                {
                    archive.CreateEntryFromFile(
                        candidate.AbsolutePath,
                        candidate.RelativePath,
                        CompressionLevel.Optimal);
                }

                var manifestEntry = archive.CreateEntry(ManifestFileName, CompressionLevel.Optimal);
                await using var manifestStream = manifestEntry.Open();
                await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions);
            }

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(temporaryPath, backupPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static async Task RestoreBackupAsync(FileResult backupFile)
    {
        if (backupFile == null)
            throw new Exception("لم يتم اختيار ملف.");

        if (!backupFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new Exception("ملف الاستعادة يجب أن يكون بصيغة ZIP.");

        string tempFolder =
            Path.Combine(FileSystem.CacheDirectory, $"restore_temp_{Guid.NewGuid():N}");

        string? preRestoreBackupPath = null;
        var replacedTargets = new List<string>();

        try
        {
            Directory.CreateDirectory(tempFolder);

            string tempZipPath =
                Path.Combine(tempFolder, Path.GetFileName(backupFile.FileName));

            await using (var sourceStream = await backupFile.OpenReadAsync())
            await using (var targetStream = File.Create(tempZipPath))
            {
                await sourceStream.CopyToAsync(targetStream);
            }

            if (new FileInfo(tempZipPath).Length > MaxArchiveBytes)
                throw new InvalidDataException("حجم النسخة الاحتياطية أكبر من الحد المسموح.");

            var restoredJsonFiles =
                await ExtractValidatedJsonFilesAsync(tempZipPath, tempFolder);

            if (restoredJsonFiles.Count == 0)
                throw new Exception("النسخة الاحتياطية لا تحتوي على ملفات بيانات.");

            preRestoreBackupPath = await CreateEmergencyBackupAsync();

            foreach (var file in restoredJsonFiles)
            {
                string targetPath = ResolveRestoreTargetPath(file.RelativePath);
                await CopyJsonAtomicallyAsync(file.TempPath, targetPath);
                replacedTargets.Add(targetPath);
            }
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(preRestoreBackupPath))
        {
            await RollBackReplacedTargetsAsync(replacedTargets);
            throw new Exception(
                $"فشلت الاستعادة ولم يتم اعتماد النسخة الجديدة. تم إنشاء نسخة أمان قبل المحاولة: {Path.GetFileName(preRestoreBackupPath)}",
                ex);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
        }
    }

    static async Task<IReadOnlyList<BackupCandidate>> BuildBackupCandidatesAsync()
    {
        var result = new List<BackupCandidate>();
        string appData = FileSystem.AppDataDirectory;

        foreach (var file in Directory.GetFiles(appData, "*.json", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(file);
            if (AllowedTopLevelFiles.Contains(fileName))
            {
                result.Add(new BackupCandidate(file, fileName, "AppData"));
            }
        }

        string adminRoot = StoreAdminService.GetAdminStorageRoot();
        if (Directory.Exists(adminRoot))
        {
            foreach (var file in Directory.GetFiles(adminRoot, "*.json", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file);
                if (AllowedAdminFiles.Contains(fileName))
                {
                    result.Add(new BackupCandidate(
                        file,
                        $"{AdminRootName}/{fileName}",
                        "StoreAdmin"));
                }
            }
        }

        await Task.CompletedTask;
        return result;
    }

    static async Task<IReadOnlyList<RestoredBackupFile>> ExtractValidatedJsonFilesAsync(
        string zipPath,
        string tempFolder)
    {
        var files = new List<RestoredBackupFile>();
        using ZipArchive archive = ZipFile.OpenRead(zipPath);

        var manifestEntry = archive.Entries.SingleOrDefault(entry =>
            string.Equals(entry.FullName, ManifestFileName, StringComparison.Ordinal));

        if (manifestEntry == null)
            throw new InvalidDataException("النسخة الاحتياطية لا تحتوي على ملف manifest صالح.");

        BackupManifest manifest;
        await using (var manifestStream = manifestEntry.Open())
        {
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream)
                       ?? throw new InvalidDataException("ملف manifest فارغ أو غير صالح.");
        }

        ValidateManifest(manifest);

        var expectedEntries = manifest.Entries.ToDictionary(
            entry => entry.Path,
            StringComparer.OrdinalIgnoreCase);

        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;

            if (string.Equals(entry.FullName, ManifestFileName, StringComparison.Ordinal))
                continue;

            string relativePath = NormalizeArchivePath(entry.FullName);
            if (!expectedEntries.TryGetValue(relativePath, out var manifestItem))
                throw new InvalidDataException($"ملف غير مصرح به داخل النسخة الاحتياطية: {entry.FullName}");

            if (!seenEntries.Add(relativePath))
                throw new InvalidDataException($"ملف مكرر داخل النسخة الاحتياطية: {relativePath}");

            if (!IsAllowedRelativeJsonPath(relativePath))
                throw new InvalidDataException($"مسار غير مصرح به داخل النسخة الاحتياطية: {relativePath}");

            if (entry.Length <= 0 || entry.Length > MaxJsonEntryBytes)
                throw new InvalidDataException($"حجم ملف غير صالح داخل النسخة الاحتياطية: {relativePath}");

            string targetPath = SafeCombine(tempFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? tempFolder);

            await using (var source = entry.Open())
            await using (var target = File.Create(targetPath))
            {
                await source.CopyToAsync(target);
            }

            await using (var source = File.OpenRead(targetPath))
            {
                await ValidateJsonAsync(source, relativePath);
            }

            string actualHash = await ComputeSha256Async(targetPath);
            if (!string.Equals(actualHash, manifestItem.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Checksum غير مطابق للملف: {relativePath}");

            if (new FileInfo(targetPath).Length != manifestItem.Length)
                throw new InvalidDataException($"حجم الملف لا يطابق manifest: {relativePath}");

            files.Add(new RestoredBackupFile(relativePath, targetPath));
        }

        foreach (var expected in expectedEntries.Keys)
        {
            if (!seenEntries.Contains(expected))
                throw new InvalidDataException($"ملف مذكور في manifest وغير موجود داخل النسخة: {expected}");
        }

        return files;
    }

    static void ValidateManifest(BackupManifest manifest)
    {
        if (manifest.BackupFormatVersion != BackupFormatVersion)
            throw new InvalidDataException("إصدار النسخة الاحتياطية غير مدعوم.");

        if (manifest.SchemaVersion > SchemaVersion)
            throw new InvalidDataException("Schema النسخة الاحتياطية أحدث من التطبيق الحالي.");

        if (manifest.Entries.Count == 0)
            throw new InvalidDataException("ملف manifest لا يحتوي على بيانات للاستعادة.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in manifest.Entries)
        {
            string relativePath = NormalizeArchivePath(entry.Path);
            if (!string.Equals(relativePath, entry.Path, StringComparison.Ordinal))
                throw new InvalidDataException($"مسار manifest غير قياسي: {entry.Path}");

            if (!seen.Add(relativePath))
                throw new InvalidDataException($"مسار مكرر في manifest: {relativePath}");

            if (!IsAllowedRelativeJsonPath(relativePath))
                throw new InvalidDataException($"مسار غير مصرح به في manifest: {relativePath}");

            if (entry.Length <= 0 || entry.Length > MaxJsonEntryBytes)
                throw new InvalidDataException($"حجم غير صالح في manifest: {relativePath}");

            if (string.IsNullOrWhiteSpace(entry.Sha256) || entry.Sha256.Length != 64)
                throw new InvalidDataException($"Checksum غير صالح في manifest: {relativePath}");
        }
    }

    static string NormalizeArchivePath(string path)
    {
        string normalized = path.Replace('\\', '/').Trim();
        if (normalized.Contains("//", StringComparison.Ordinal) ||
            normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalized))
        {
            throw new InvalidDataException($"مسار غير آمن داخل النسخة الاحتياطية: {path}");
        }

        return normalized;
    }

    static bool IsAllowedRelativeJsonPath(string relativePath)
    {
        if (!relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!relativePath.Contains('/', StringComparison.Ordinal))
            return AllowedTopLevelFiles.Contains(relativePath);

        string[] parts = relativePath.Split('/');
        return parts.Length == 2 &&
               string.Equals(parts[0], AdminRootName, StringComparison.Ordinal) &&
               AllowedAdminFiles.Contains(parts[1]);
    }

    static string ResolveRestoreTargetPath(string relativePath)
    {
        string normalized = NormalizeArchivePath(relativePath);

        if (!normalized.Contains('/', StringComparison.Ordinal))
            return Path.Combine(FileSystem.AppDataDirectory, normalized);

        string[] parts = normalized.Split('/');
        return Path.Combine(StoreAdminService.GetAdminStorageRoot(), parts[1]);
    }

    static string SafeCombine(string root, string relativePath)
    {
        string combined = Path.GetFullPath(
            Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        string fullRoot = Path.GetFullPath(root);
        if (!combined.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"مسار غير آمن داخل النسخة الاحتياطية: {relativePath}");

        return combined;
    }

    static async Task CopyJsonAtomicallyAsync(string sourcePath, string targetPath)
    {
        await using (var source = File.OpenRead(sourcePath))
        {
            await ValidateJsonAsync(source, Path.GetFileName(sourcePath));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? FileSystem.AppDataDirectory);

        string temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        string backupPath = $"{targetPath}.bak";

        try
        {
            await using (var source = File.OpenRead(sourcePath))
            await using (var target = File.Create(temporaryPath))
            {
                await source.CopyToAsync(target);
            }

            if (File.Exists(targetPath))
                File.Copy(targetPath, backupPath, overwrite: true);

            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    static async Task RollBackReplacedTargetsAsync(IEnumerable<string> targetPaths)
    {
        foreach (string targetPath in targetPaths.Reverse())
        {
            string backupPath = $"{targetPath}.bak";
            if (!File.Exists(backupPath))
                continue;

            try
            {
                File.Copy(backupPath, targetPath, overwrite: true);
            }
            catch (IOException ex)
            {
                await SecurityLogService.AddAsync(
                    "Backup",
                    "Restore rollback failed",
                    ex.ToString(),
                    "Critical",
                    isPermanent: true);
            }
            catch (UnauthorizedAccessException ex)
            {
                await SecurityLogService.AddAsync(
                    "Backup",
                    "Restore rollback failed",
                    ex.ToString(),
                    "Critical",
                    isPermanent: true);
            }
        }
    }

    static async Task ValidateJsonAsync(Stream stream, string fileName)
    {
        try
        {
            using var _ = await JsonDocument.ParseAsync(stream);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"ملف JSON غير صالح داخل النسخة الاحتياطية: {fileName}", ex);
        }
    }

    static async Task<string> ComputeSha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash);
    }

    sealed record BackupCandidate(
        string AbsolutePath,
        string RelativePath,
        string Dataset);

    sealed record RestoredBackupFile(
        string RelativePath,
        string TempPath);

    sealed record BackupManifest(
        int BackupFormatVersion,
        string ApplicationVersion,
        int SchemaVersion,
        DateTimeOffset CreatedAt,
        string Platform,
        IReadOnlyList<BackupManifestEntry> Entries);

    sealed record BackupManifestEntry(
        string Path,
        string Sha256,
        long Length,
        string Dataset);
}

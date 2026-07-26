using System.IO.Compression;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public static class BackupService
{
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

            using (ZipArchive archive =
                   ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                string appData = FileSystem.AppDataDirectory;

                var jsonFiles =
                    Directory.GetFiles(appData, "*.json", SearchOption.TopDirectoryOnly);

                foreach (var file in jsonFiles)
                {
                    string fileName = Path.GetFileName(file);

                    var entry = archive.CreateEntry(fileName);

                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(file);

                    await fileStream.CopyToAsync(entryStream);
                }
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

        try
        {
            Directory.CreateDirectory(tempFolder);

            string tempZipPath =
                Path.Combine(tempFolder, Path.GetFileName(backupFile.FileName));

            await using (var sourceStream = await backupFile.OpenReadAsync())
            {
                await using var targetStream = File.Create(tempZipPath);
                await sourceStream.CopyToAsync(targetStream);
            }

            var restoredJsonFiles =
                await ExtractValidatedJsonFilesAsync(tempZipPath, tempFolder);

            if (restoredJsonFiles.Count == 0)
                throw new Exception("النسخة الاحتياطية لا تحتوي على ملفات بيانات.");

            preRestoreBackupPath = await CreateEmergencyBackupAsync();

            string appData = FileSystem.AppDataDirectory;
            Directory.CreateDirectory(appData);

            foreach (var file in restoredJsonFiles)
            {
                string fileName = Path.GetFileName(file);
                string targetPath = Path.Combine(appData, fileName);

                await CopyJsonAtomicallyAsync(file, targetPath);
            }
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(preRestoreBackupPath))
        {
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

    static async Task<IReadOnlyList<string>> ExtractValidatedJsonFilesAsync(
        string zipPath,
        string tempFolder)
    {
        var files = new List<string>();
        using ZipArchive archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            string fileName = Path.GetFileName(entry.FullName);
            if (string.IsNullOrWhiteSpace(fileName) ||
                !string.Equals(fileName, entry.FullName, StringComparison.Ordinal) ||
                !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string targetPath = Path.Combine(tempFolder, fileName);
            await using (var source = entry.Open())
            {
                await ValidateJsonAsync(source, fileName);
            }

            await using (var source = entry.Open())
            await using (var target = File.Create(targetPath))
            {
                await source.CopyToAsync(target);
            }

            files.Add(targetPath);
        }

        return files;
    }

    static async Task CopyJsonAtomicallyAsync(string sourcePath, string targetPath)
    {
        await using (var source = File.OpenRead(sourcePath))
        {
            await ValidateJsonAsync(source, Path.GetFileName(sourcePath));
        }

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
}

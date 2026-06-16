using System.IO.Compression;

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
        if (File.Exists(backupPath))
            File.Delete(backupPath);

        using ZipArchive archive =
            ZipFile.Open(backupPath, ZipArchiveMode.Create);

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

    public static async Task RestoreBackupAsync(FileResult backupFile)
    {
        if (backupFile == null)
            throw new Exception("لم يتم اختيار ملف.");

        if (!backupFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new Exception("ملف الاستعادة يجب أن يكون بصيغة ZIP.");

        string tempFolder =
            Path.Combine(FileSystem.CacheDirectory, "restore_temp");

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);

        Directory.CreateDirectory(tempFolder);

        string tempZipPath =
            Path.Combine(tempFolder, backupFile.FileName);

        await using (var sourceStream = await backupFile.OpenReadAsync())
        {
            await using var targetStream = File.Create(tempZipPath);
            await sourceStream.CopyToAsync(targetStream);
        }

        ZipFile.ExtractToDirectory(tempZipPath, tempFolder, true);

        var restoredJsonFiles =
            Directory.GetFiles(tempFolder, "*.json", SearchOption.TopDirectoryOnly);

        if (restoredJsonFiles.Length == 0)
            throw new Exception("النسخة الاحتياطية لا تحتوي على ملفات بيانات.");

        string appData = FileSystem.AppDataDirectory;

        foreach (var file in restoredJsonFiles)
        {
            string fileName = Path.GetFileName(file);
            string targetPath = Path.Combine(appData, fileName);

            File.Copy(file, targetPath, true);
        }

        Directory.Delete(tempFolder, true);
    }
}
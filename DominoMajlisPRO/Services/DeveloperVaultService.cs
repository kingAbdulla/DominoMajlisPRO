using System.IO.Compression;
using System.Security.Cryptography;

namespace DominoMajlisPRO.Services;

public static class DeveloperVaultService
{
    const int Iterations = 150000;

    static readonly string[] VaultFiles =
    {
        "honor_identity.json",
        "special_honor_keys.json",
        "special_honor_identities.json",
        "developer_lock.json",
        "security_logs.json"
    };

    public static async Task<string> ExportVaultAsync(string developerPassword)
    {
        if (string.IsNullOrWhiteSpace(developerPassword) ||
            developerPassword.Length < 8)
        {
            throw new Exception("كلمة مرور الخزنة هي نفس كلمة مرور المطور ويجب أن تكون 8 أحرف على الأقل.");
        }

        string tempZip =
            Path.Combine(
                FileSystem.CacheDirectory,
                $"developer_vault_temp_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        string vaultPath =
            Path.Combine(
                FileSystem.CacheDirectory,
                $"DominoMajlisPRO_DeveloperVault_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.dmpvault");

        if (File.Exists(tempZip))
            File.Delete(tempZip);

        using (ZipArchive archive =
            ZipFile.Open(
                tempZip,
                ZipArchiveMode.Create))
        {
            string appData =
                FileSystem.AppDataDirectory;

            foreach (string fileName in VaultFiles)
            {
                string sourcePath =
                    Path.Combine(
                        appData,
                        fileName);

                if (!File.Exists(sourcePath))
                    continue;

                ZipArchiveEntry entry =
                    archive.CreateEntry(fileName);

                await using Stream entryStream =
                    entry.Open();

                await using FileStream fileStream =
                    File.OpenRead(sourcePath);

                await fileStream.CopyToAsync(entryStream);
            }
        }

        byte[] plainBytes =
            await File.ReadAllBytesAsync(tempZip);

        byte[] encryptedBytes =
            EncryptBytes(
                plainBytes,
                developerPassword);

        await File.WriteAllBytesAsync(
            vaultPath,
            encryptedBytes);

        if (File.Exists(tempZip))
            File.Delete(tempZip);

        return vaultPath;
    }

    public static async Task ImportVaultAsync(
        FileResult vaultFile,
        string developerPassword)
    {
        if (vaultFile == null)
            throw new Exception("لم يتم اختيار ملف الخزنة.");

        if (string.IsNullOrWhiteSpace(developerPassword) ||
            developerPassword.Length < 8)
        {
            throw new Exception("كلمة مرور الخزنة هي نفس كلمة مرور المطور.");
        }

        if (!vaultFile.FileName.EndsWith(
                ".dmpvault",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("ملف الخزنة يجب أن يكون بصيغة .dmpvault");
        }

        string tempFolder =
            Path.Combine(
                FileSystem.CacheDirectory,
                $"developer_vault_import_{DateTime.Now:yyyyMMdd_HHmmss}");

        Directory.CreateDirectory(tempFolder);

        string encryptedPath =
            Path.Combine(
                tempFolder,
                vaultFile.FileName);

        await using (Stream sourceStream =
            await vaultFile.OpenReadAsync())
        {
            await using FileStream targetStream =
                File.Create(encryptedPath);

            await sourceStream.CopyToAsync(targetStream);
        }

        byte[] encryptedBytes =
            await File.ReadAllBytesAsync(encryptedPath);

        byte[] plainBytes =
            DecryptBytes(
                encryptedBytes,
                developerPassword);

        string tempZip =
            Path.Combine(
                tempFolder,
                "vault.zip");

        await File.WriteAllBytesAsync(
            tempZip,
            plainBytes);

        string extractFolder =
            Path.Combine(
                tempFolder,
                "extract");

        Directory.CreateDirectory(extractFolder);

        ZipFile.ExtractToDirectory(
            tempZip,
            extractFolder,
            true);

        string appData =
            FileSystem.AppDataDirectory;

        foreach (string fileName in VaultFiles)
        {
            string extractedFile =
                Path.Combine(
                    extractFolder,
                    fileName);

            if (!File.Exists(extractedFile))
                continue;

            string targetPath =
                Path.Combine(
                    appData,
                    fileName);

            File.Copy(
                extractedFile,
                targetPath,
                true);
        }

        Directory.Delete(
            tempFolder,
            true);
    }

    static byte[] EncryptBytes(
        byte[] plainBytes,
        string password)
    {
        byte[] salt =
            RandomNumberGenerator.GetBytes(16);

        byte[] iv =
            RandomNumberGenerator.GetBytes(16);

        using var derive =
            new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

        byte[] key =
            derive.GetBytes(32);

        using Aes aes =
            Aes.Create();

        aes.Key = key;
        aes.IV = iv;

        using MemoryStream output =
            new();

        output.Write(salt);
        output.Write(iv);

        using CryptoStream cryptoStream =
            new(
                output,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write);

        cryptoStream.Write(
            plainBytes,
            0,
            plainBytes.Length);

        cryptoStream.FlushFinalBlock();

        return output.ToArray();
    }

    static byte[] DecryptBytes(
        byte[] encryptedBytes,
        string password)
    {
        if (encryptedBytes.Length < 32)
            throw new Exception("ملف الخزنة غير صالح.");

        byte[] salt =
            encryptedBytes
                .Take(16)
                .ToArray();

        byte[] iv =
            encryptedBytes
                .Skip(16)
                .Take(16)
                .ToArray();

        byte[] cipher =
            encryptedBytes
                .Skip(32)
                .ToArray();

        using var derive =
            new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

        byte[] key =
            derive.GetBytes(32);

        using Aes aes =
            Aes.Create();

        aes.Key = key;
        aes.IV = iv;

        using MemoryStream input =
            new(cipher);

        using CryptoStream cryptoStream =
            new(
                input,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read);

        using MemoryStream output =
            new();

        cryptoStream.CopyTo(output);

        return output.ToArray();
    }
}
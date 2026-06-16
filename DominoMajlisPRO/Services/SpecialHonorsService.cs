using DominoMajlisPRO.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public static class SpecialHonorsService
{
    static readonly string keysFilePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "special_honor_keys.json");

    static readonly string identitiesFilePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "special_honor_identities.json");

    const string SecretSalt =
        "DOMINO_MAJLIS_PRO_SPECIAL_HONORS_V2_SECURE_KEYS";

    const int KeyExpiryDays = 365;

    public static async Task<SpecialHonorKeyModel> LoadKeysAsync()
    {
        if (!File.Exists(keysFilePath))
            return new SpecialHonorKeyModel();

        string json =
            await File.ReadAllTextAsync(keysFilePath);

        return JsonSerializer.Deserialize<SpecialHonorKeyModel>(json)
               ?? new SpecialHonorKeyModel();
    }

    public static async Task SaveKeysAsync(SpecialHonorKeyModel keys)
    {
        string json =
            JsonSerializer.Serialize(
                keys,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(keysFilePath, json);
    }

    public static async Task<List<HonorIdentityModel>> LoadIdentitiesAsync()
    {
        if (!File.Exists(identitiesFilePath))
            return new List<HonorIdentityModel>();

        string json =
            await File.ReadAllTextAsync(identitiesFilePath);

        return JsonSerializer.Deserialize<List<HonorIdentityModel>>(json)
               ?? new List<HonorIdentityModel>();
    }

    public static async Task SaveIdentitiesAsync(
        List<HonorIdentityModel> identities)
    {
        string json =
            JsonSerializer.Serialize(
                identities,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(identitiesFilePath, json);
    }

    public static async Task<string> CreateDeveloperActivationKeyAsync()
    {
        return await CreateActivationKeyAsync("Developer");
    }

    public static async Task<string> CreateFounderActivationKeyAsync()
    {
        return await CreateActivationKeyAsync("Founder");
    }

    public static async Task<string> CreateHonorActivationKeyAsync()
    {
        return await CreateActivationKeyAsync("Honor");
    }

    static async Task<string> CreateActivationKeyAsync(string keyType)
    {
        bool isDeveloper =
            await HonorIdentityService.IsDeveloperAsync();

        if (!isDeveloper)
            throw new Exception("توليد المفاتيح متاح للمطور فقط.");

        var developerAccount =
            await DeveloperLockService.LoadAsync();

        string plainKey =
            GenerateSecureActivationKey(keyType);

        string keyHash =
            HashKey(plainKey);

        var record =
            new HonorKeyRecord
            {
                KeyId = $"KEY-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
                KeyHash = keyHash,
                KeyType = keyType,
                IsUsed = false,
                CreatedAt = DateTime.Now,
                UsedAt = DateTime.MinValue,
                ExpiresAt = DateTime.Now.AddDays(KeyExpiryDays),
                CreatedByDeveloperId = developerAccount.DeveloperId,
                UsedByPlayerId = "",
                HonorOwnerId = "",
                DeviceFingerprint = "",
                SecuritySignature = GenerateKeyRecordSignature(
                    keyHash,
                    keyType,
                    developerAccount.DeveloperId)
            };

        var keys =
            await LoadKeysAsync();

        GetKeyList(keys, keyType).Add(record);

        await SaveKeysAsync(keys);

        await SecurityLogService.AddAsync(
            "HONOR_KEY",
            $"تم توليد مفتاح {keyType}",
            $"KeyId: {record.KeyId}",
            "High",
            true);

        return plainKey;
    }

    public static async Task<(bool Success, string Message, string RecoveryKey, string MasterKey)>
        ActivateDeveloperAsync(string playerId, string activationKey)
    {
        return await ActivateHonorAsync(
            playerId,
            activationKey,
            "Developer");
    }

    public static async Task<(bool Success, string Message, string RecoveryKey, string MasterKey)>
        ActivateFounderAsync(string playerId, string activationKey)
    {
        return await ActivateHonorAsync(
            playerId,
            activationKey,
            "Founder");
    }

    public static async Task<(bool Success, string Message, string RecoveryKey, string MasterKey)>
        ActivateHonorMemberAsync(string playerId, string activationKey)
    {
        return await ActivateHonorAsync(
            playerId,
            activationKey,
            "Honor");
    }

    static async Task<(bool Success, string Message, string RecoveryKey, string MasterKey)>
        ActivateHonorAsync(
            string playerId,
            string activationKey,
            string honorType)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return (false, "PlayerId غير صالح", "", "");

        if (string.IsNullOrWhiteSpace(activationKey))
            return (false, "مفتاح التفعيل فارغ", "", "");

        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x =>
                x.PlayerId == playerId);

        if (player == null)
            return (false, "اللاعب غير موجود", "", "");

        var keys =
            await LoadKeysAsync();

        var keyList =
            GetKeyList(keys, honorType);

        string inputHash =
            HashKey(activationKey.Trim());

        var keyRecord =
            keyList.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.KeyHash) &&
                x.KeyHash.Equals(
                    inputHash,
                    StringComparison.OrdinalIgnoreCase));

        if (keyRecord == null)
            return (false, "مفتاح التفعيل غير صحيح أو غير مولّد من المطور", "", "");

        if (keyRecord.KeyType != honorType)
            return (false, "نوع مفتاح التفعيل لا يطابق نوع الصلاحية", "", "");

        if (keyRecord.IsUsed)
            return (false, "مفتاح التفعيل مستخدم مسبقاً", "", "");

        if (keyRecord.ExpiresAt != DateTime.MinValue &&
            keyRecord.ExpiresAt < DateTime.Now)
        {
            return (false, "مفتاح التفعيل منتهي الصلاحية", "", "");
        }

        bool validSignature =
            keyRecord.SecuritySignature ==
            GenerateKeyRecordSignature(
                keyRecord.KeyHash,
                keyRecord.KeyType,
                keyRecord.CreatedByDeveloperId);

        if (!validSignature)
            return (false, "مفتاح التفعيل تالف أو تم تعديله", "", "");

        var identities =
            await LoadIdentitiesAsync();

        bool alreadyHasHonor =
            identities.Any(x =>
                x.PlayerId == playerId &&
                x.HonorType == honorType);

        if (alreadyHasHonor)
            return (false, "هذا اللاعب يمتلك هذا الشرف مسبقاً", "", "");

        string honorOwnerId =
            GenerateHonorOwnerId();

        string recoveryKey =
            GenerateSecureRecoveryKey(honorType);

        string masterKey =
            GenerateSecureMasterRecoveryKey(honorType);

        string signature =
            GenerateHonorSignature(
                honorOwnerId,
                playerId,
                honorType);

        HonorIdentityModel identity =
            new HonorIdentityModel
            {
                HonorOwnerId = honorOwnerId,
                PlayerId = playerId,
                HonorType = honorType,
                RecoveryKey = recoveryKey,
                MasterRecoveryKey = masterKey,
                SecuritySignature = signature,
                ActivatedAt = DateTime.Now
            };

        identities.Add(identity);

        keyRecord.IsUsed = true;
        keyRecord.UsedAt = DateTime.Now;
        keyRecord.UsedByPlayerId = playerId;
        keyRecord.HonorOwnerId = honorOwnerId;
        keyRecord.DeviceFingerprint = GetDeviceFingerprint();

        ApplyHonorToPlayer(
            player,
            honorType,
            honorOwnerId,
            signature);

        await PlayerProfileService.SavePlayersAsync(players);
        await SaveKeysAsync(keys);
        await SaveIdentitiesAsync(identities);

        await SecurityLogService.AddAsync(
            "HONOR",
            $"تم تفعيل صلاحية {honorType}",
            $"PlayerId: {playerId} | HonorOwnerId: {honorOwnerId}",
            "High",
            true);

        return
            (
                true,
                "تم تفعيل الشرف بنجاح. احفظ مفاتيح الاسترداد الآن، لن تظهر مرة أخرى.",
                recoveryKey,
                masterKey
            );
    }

    public static async Task<bool> RecoverHonorAsync(
        string playerId,
        string recoveryKey)
    {
        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x =>
                x.PlayerId == playerId);

        if (player == null)
            return false;

        var identities =
            await LoadIdentitiesAsync();

        var identity =
            identities.FirstOrDefault(x =>
                x.RecoveryKey.Equals(
                    recoveryKey,
                    StringComparison.OrdinalIgnoreCase));

        if (identity == null)
            return false;

        identity.PlayerId =
            playerId;

        identity.SecuritySignature =
            GenerateHonorSignature(
                identity.HonorOwnerId,
                playerId,
                identity.HonorType);

        ApplyHonorToPlayer(
            player,
            identity.HonorType,
            identity.HonorOwnerId,
            identity.SecuritySignature);

        await SaveIdentitiesAsync(identities);
        await PlayerProfileService.SavePlayersAsync(players);

        return true;
    }

    public static async Task<(bool Success, string NewRecoveryKey)>
        ResetRecoveryKeyWithMasterAsync(string masterRecoveryKey)
    {
        var identities =
            await LoadIdentitiesAsync();

        var identity =
            identities.FirstOrDefault(x =>
                x.MasterRecoveryKey.Equals(
                    masterRecoveryKey,
                    StringComparison.OrdinalIgnoreCase));

        if (identity == null)
            return (false, "");

        string newRecoveryKey =
            GenerateSecureRecoveryKey(
                identity.HonorType.ToUpper());

        identity.RecoveryKey =
            newRecoveryKey;

        await SaveIdentitiesAsync(identities);

        return (true, newRecoveryKey);
    }

    public static async Task ValidateAllHonorsAsync()
    {
        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var identities =
            await LoadIdentitiesAsync();

        foreach (var player in players)
        {
            bool validDeveloper =
                identities.Any(x =>
                    x.PlayerId == player.PlayerId &&
                    x.HonorType == "Developer" &&
                    x.SecuritySignature ==
                    GenerateHonorSignature(
                        x.HonorOwnerId,
                        player.PlayerId,
                        x.HonorType));

            bool validFounder =
                identities.Any(x =>
                    x.PlayerId == player.PlayerId &&
                    x.HonorType == "Founder" &&
                    x.SecuritySignature ==
                    GenerateHonorSignature(
                        x.HonorOwnerId,
                        player.PlayerId,
                        x.HonorType));

            player.IsDeveloper =
                validDeveloper;

            player.IsFounder =
                validFounder;

            if (!validDeveloper &&
                !validFounder)
            {
                player.HonorOwnerId = "";
                player.HonorSignature = "";
            }
        }

        await PlayerProfileService.SavePlayersAsync(players);
    }

    static List<HonorKeyRecord> GetKeyList(
        SpecialHonorKeyModel keys,
        string keyType)
    {
        return keyType switch
        {
            "Developer" => keys.DeveloperKeys,
            "Founder" => keys.FounderKeys,
            "Honor" => keys.HonorKeys,
            "EarlyAdopter" => keys.EarlyAdopterKeys,
            "SeasonVeteran" => keys.SeasonVeteranKeys,
            _ => keys.HonorKeys
        };
    }

    static void ApplyHonorToPlayer(
        PlayerProfileModel player,
        string honorType,
        string honorOwnerId,
        string signature)
    {
        player.HonorOwnerId =
            honorOwnerId;

        player.HonorSignature =
            signature;

        if (honorType == "Developer")
        {
            player.IsDeveloper = true;
            player.DeveloperGrantedAt = DateTime.Now;
        }

        if (honorType == "Founder")
        {
            player.IsFounder = true;
            player.FounderGrantedAt = DateTime.Now;
        }
    }

    static string GenerateSecureActivationKey(string keyType)
    {
        string prefix =
            keyType switch
            {
                "Developer" => "DEV",
                "Founder" => "FND",
                "Honor" => "HNR",
                "EarlyAdopter" => "EARLY",
                "SeasonVeteran" => "VET",
                _ => "HON"
            };

        byte[] bytes =
            RandomNumberGenerator.GetBytes(32);

        string token =
            Convert.ToHexString(bytes);

        return
            $"DMP-{prefix}-{token[..6]}-{token.Substring(6, 6)}-{token.Substring(12, 6)}-{token.Substring(18, 6)}";
    }

    static string GenerateSecureRecoveryKey(string type)
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(24);

        string token =
            Convert.ToHexString(bytes);

        return
            $"REC-{type}-{token[..6]}-{token.Substring(6, 6)}-{token.Substring(12, 6)}";
    }

    static string GenerateSecureMasterRecoveryKey(string type)
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(32);

        string token =
            Convert.ToHexString(bytes);

        return
            $"MASTER-{type}-{token[..6]}-{token.Substring(6, 6)}-{token.Substring(12, 6)}-{token.Substring(18, 6)}";
    }

    static string GenerateHonorOwnerId()
    {
        return
            $"HONOR-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
    }

    static string HashKey(string key)
    {
        using var sha =
            SHA256.Create();

        byte[] bytes =
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(
                    key.Trim().ToUpperInvariant()));

        return Convert.ToHexString(bytes);
    }

    static string GenerateKeyRecordSignature(
        string keyHash,
        string keyType,
        string developerId)
    {
        string raw =
            $"{keyHash}|{keyType}|{developerId}|{SecretSalt}";

        byte[] bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(bytes);
    }

    static string GenerateHonorSignature(
        string honorOwnerId,
        string playerId,
        string honorType)
    {
        string raw =
            $"{honorOwnerId}|{playerId}|{honorType}|{SecretSalt}";

        byte[] bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(bytes);
    }

    static string GetDeviceFingerprint()
    {
        try
        {
            return
                $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-{DeviceInfo.Platform}";
        }
        catch
        {
            return "UnknownDevice";
        }
    }
}
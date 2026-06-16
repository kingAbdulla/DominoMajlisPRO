namespace DominoMajlisPRO.Models;

public class HonorKeyRecord
{
    public string KeyId { get; set; } = "";

    public string KeyHash { get; set; } = "";

    public string KeyType { get; set; } = "";

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UsedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string CreatedByDeveloperId { get; set; } = "";

    public string UsedByPlayerId { get; set; } = "";

    public string HonorOwnerId { get; set; } = "";

    public string DeviceFingerprint { get; set; } = "";

    public string SecuritySignature { get; set; } = "";

    public string Key { get; set; } = "";
}
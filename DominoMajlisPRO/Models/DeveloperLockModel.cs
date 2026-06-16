namespace DominoMajlisPRO.Models;

public class DeveloperLockModel
{
    public bool IsEnabled { get; set; }

    public bool IsSetupCompleted { get; set; }

    public string DeveloperId { get; set; } = "";

    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string PasswordSalt { get; set; } = "";

    public string DeviceFingerprint { get; set; } = "";

    public List<string> RecoveryCodeHashes { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime LastLoginAt { get; set; }

    public DateTime LastPasswordChange { get; set; }
}
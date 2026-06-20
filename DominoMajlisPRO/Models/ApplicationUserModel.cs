namespace DominoMajlisPRO.Models;

public enum ApplicationUserRole
{
    Ghost = 0,
    Member = 1,
    Developer = 2,
    Founder = 3,
    Honor = 4
}

public class ApplicationUserModel
{
    public string ApplicationUserId { get; set; } = "";

    public string PlayerId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public ApplicationUserRole Role { get; set; } =
        ApplicationUserRole.Ghost;

    public bool IsTemporary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string LegacyIdentityId { get; set; } = "";

    public string LegacyDeveloperId { get; set; } = "";

    public string LegacyHonorOwnerId { get; set; } = "";

    public string MigrationSource { get; set; } = "";
}

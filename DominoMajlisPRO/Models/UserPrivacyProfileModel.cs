namespace DominoMajlisPRO.Models;

public class UserPrivacyProfileModel
{
    public bool HasAcceptedPrivacyProfile { get; set; } = false;

    public string AgeGroup { get; set; } = "";

    public string Gender { get; set; } = "";

    public string Governorate { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
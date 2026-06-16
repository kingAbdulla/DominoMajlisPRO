namespace DominoMajlisPRO.Models;

public class SpecialHonorKeyModel
{
    public List<HonorKeyRecord> DeveloperKeys { get; set; } = new();

    public List<HonorKeyRecord> FounderKeys { get; set; } = new();

    public List<HonorKeyRecord> HonorKeys { get; set; } = new();

    public List<HonorKeyRecord> EarlyAdopterKeys { get; set; } = new();

    public List<HonorKeyRecord> SeasonVeteranKeys { get; set; } = new();
}
namespace DominoMajlisPRO.LivingVisualPlatform.Models;

public sealed class LivingRenderRequest
{
    public string ApplicationUserId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public LivingVisualDisplayLocation DisplayLocation { get; set; } = LivingVisualDisplayLocation.Unknown;
    public string DeviceProfile { get; set; } = string.Empty;
    public bool IsPreview { get; set; }
    public bool IsDeveloperPreview { get; set; }
}

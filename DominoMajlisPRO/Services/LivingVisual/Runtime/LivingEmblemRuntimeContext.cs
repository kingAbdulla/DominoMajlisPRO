namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemRuntimeContext
{
    public string AssetId { get; init; } = string.Empty;

    public string PlayerId { get; init; } = string.Empty;

    public string? TeamId { get; init; }

    public string EmblemKind { get; init; } = string.Empty;

    public bool IsVisible { get; set; }

    public bool IsFocused { get; set; }

    public double ElapsedSeconds { get; set; }
}

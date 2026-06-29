namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMemory
{
    public string LastTouchZone { get; set; } = "None";
    public string LastReaction { get; set; } = "Settling";
    public string LastAttentionTarget { get; set; } = "Neutral";
    public double LastStimulusSeconds { get; set; }
    public bool UserInteractedRecently { get; set; }
    public double CuriosityTrend { get; set; }
    public double BoredomTrend { get; set; }

    public double TimeSinceLastStimulus(double seconds) =>
        Math.Max(0, seconds - LastStimulusSeconds);

    public void Reset()
    {
        LastTouchZone = "None";
        LastReaction = "Settling";
        LastAttentionTarget = "Neutral";
        LastStimulusSeconds = 0;
        UserInteractedRecently = false;
        CuriosityTrend = 0;
        BoredomTrend = 0;
    }
}

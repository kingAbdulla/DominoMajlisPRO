namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMindPersonality
{
    public double AwarenessBaseline { get; init; } = 0.62;
    public double CuriosityRisePerSecond { get; init; } = 0.018;
    public double BoredomRisePerSecond { get; init; } = 0.026;
    public double CalmRecoveryPerSecond { get; init; } = 0.012;
    public double FocusReturnPerSecond { get; init; } = 0.42;
    public double CuriosityDecisionThreshold { get; init; } = 0.74;
    public double BoredomDecisionThreshold { get; init; } = 0.66;
    public double MinDecisionCooldownSeconds { get; init; } = 1.15;
    public double MaxDecisionCooldownSeconds { get; init; } = 3.85;
    public double MaxFocusX { get; init; } = 1.0;
    public double MaxFocusY { get; init; } = 0.72;

    public static LivingMindPersonality TManDeveloperPreview { get; } = new();
}

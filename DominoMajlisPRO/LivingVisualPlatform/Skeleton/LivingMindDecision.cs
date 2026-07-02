namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public readonly record struct LivingMindDecision(
    string Name,
    double TargetFocusX,
    double TargetFocusY,
    double CooldownSeconds)
{
    public static LivingMindDecision None { get; } = new("Settling", 0, 0, 1.0);
}

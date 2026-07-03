namespace DominoMajlisPRO.LivingVisualPlatform.Motion;

public enum LivingMotionCommandType
{
    None,
    SetBoneRotation,
    SetMorphWeight,
    SetMaterialPulse,
    SpawnEffect,
    SetRootFloat,
    SetLod,
    Pause,
    Resume
}

public sealed class LivingMotionCommand
{
    public LivingMotionCommandType Type { get; set; } = LivingMotionCommandType.None;
    public string Target { get; set; } = string.Empty;
    public double Value { get; set; }
    public double DurationSeconds { get; set; }
    public IReadOnlyDictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static LivingMotionCommand Pause() => new() { Type = LivingMotionCommandType.Pause };
    public static LivingMotionCommand Resume() => new() { Type = LivingMotionCommandType.Resume };
}

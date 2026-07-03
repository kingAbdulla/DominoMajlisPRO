namespace DominoMajlisPRO.LivingVisualPlatform.Motion;

public sealed class LivingMotionLimit
{
    public string BoneName { get; init; } = string.Empty;

    public double MinXDegrees { get; init; }

    public double MaxXDegrees { get; init; }

    public double MinYDegrees { get; init; }

    public double MaxYDegrees { get; init; }

    public double MinZDegrees { get; init; }

    public double MaxZDegrees { get; init; }
}

public static class LivingMotionLimits
{
    private static readonly IReadOnlyDictionary<string, LivingMotionLimit> DragonMasterLimits =
        new Dictionary<string, LivingMotionLimit>(StringComparer.OrdinalIgnoreCase)
        {
            ["Root"] = new LivingMotionLimit
            {
                BoneName = "Root",
                MinXDegrees = -5,
                MaxXDegrees = 5,
                MinYDegrees = -5,
                MaxYDegrees = 5,
                MinZDegrees = -10,
                MaxZDegrees = 10
            },
            ["Bone"] = new LivingMotionLimit
            {
                BoneName = "Bone",
                MinXDegrees = -12,
                MaxXDegrees = 12,
                MinYDegrees = -20,
                MaxYDegrees = 20,
                MinZDegrees = -20,
                MaxZDegrees = 20
            },
            ["Head"] = new LivingMotionLimit
            {
                BoneName = "Head",
                MinXDegrees = -12,
                MaxXDegrees = 12,
                MinYDegrees = -20,
                MaxYDegrees = 20,
                MinZDegrees = -20,
                MaxZDegrees = 20
            },
            ["Jaw"] = new LivingMotionLimit
            {
                BoneName = "Jaw",
                MinXDegrees = -28,
                MaxXDegrees = 2,
                MinYDegrees = 0,
                MaxYDegrees = 0,
                MinZDegrees = 0,
                MaxZDegrees = 0
            }
        };

    public static LivingMotionCommand ClampDragonMasterCommand(LivingMotionCommand command)
    {
        if (command.Type != LivingMotionCommandType.SetBoneRotation)
            return command;

        if (!DragonMasterLimits.TryGetValue(command.Target, out var limit))
            return command;

        var axis = GetAxis(command);
        var clamped = axis switch
        {
            "X" => Clamp(command.Value, limit.MinXDegrees, limit.MaxXDegrees),
            "Y" => Clamp(command.Value, limit.MinYDegrees, limit.MaxYDegrees),
            "Z" => Clamp(command.Value, limit.MinZDegrees, limit.MaxZDegrees),
            _ => command.Value
        };

        if (Math.Abs(clamped - command.Value) < 0.001)
            return command;

        return new LivingMotionCommand
        {
            Type = command.Type,
            Target = command.Target,
            Value = clamped,
            DurationSeconds = command.DurationSeconds,
            Parameters = command.Parameters
        };
    }

    public static string GetAxis(LivingMotionCommand command)
    {
        if (command.Parameters.TryGetValue("axis", out var axis) && !string.IsNullOrWhiteSpace(axis))
            return axis.Trim().ToUpperInvariant();

        return "X";
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : value > max ? max : value;
}

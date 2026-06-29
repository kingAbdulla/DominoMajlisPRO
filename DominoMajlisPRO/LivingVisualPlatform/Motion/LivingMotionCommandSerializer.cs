namespace DominoMajlisPRO.LivingVisualPlatform.Motion;

public static class LivingMotionCommandSerializer
{
    public static string Serialize(LivingMotionCommand command)
    {
        var axis = LivingMotionLimits.GetAxis(command);
        return string.Join(
            '|',
            command.Type.ToString(),
            command.Target ?? string.Empty,
            command.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            command.DurationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            axis);
    }
}

using System.Globalization;

namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public readonly record struct LivingTouchStimulus(
    double X,
    double Y,
    double Intensity,
    double TimestampSeconds)
{
    public string Zone =>
        Y < 0.32 ? "Upper" :
        Y > 0.72 ? "Lower" :
        X < 0.38 ? "Left" :
        X > 0.62 ? "Right" :
        "Center";

    public static LivingTouchStimulus Create(double x, double y, double intensity, double timestampSeconds) =>
        new(Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1), Math.Clamp(intensity, 0, 1), Math.Max(0, timestampSeconds));

    public string Serialize() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{X:F4}|{Y:F4}|{Intensity:F4}|{TimestampSeconds:F4}");

    public static bool TryParse(string? serialized, out LivingTouchStimulus stimulus)
    {
        stimulus = default;
        if (string.IsNullOrWhiteSpace(serialized))
            return false;

        var parts = serialized.Split('|');
        if (parts.Length != 4)
            return false;

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var intensity) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var timestamp))
        {
            return false;
        }

        stimulus = Create(x, y, intensity, timestamp);
        return true;
    }
}

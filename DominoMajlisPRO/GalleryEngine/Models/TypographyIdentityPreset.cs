namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class TypographyIdentityPreset
{
    public string FontFamily { get; set; } = TypographyFontCatalog.DefaultFontFamily;
    public double FontSize { get; set; } = 18;
    public string MaterialPreset { get; set; } = "SatinGold";
    public string LightingPreset { get; set; } = "SoftRim";
    public string DepthPreset { get; set; } = "Low";
    public string MotionPreset { get; set; } = "None";
    public string ParticlePreset { get; set; } = "None";
    public string FrameStylePreset { get; set; } = "None";
    public double FrameThickness { get; set; } = 1.4;
    public string PrimaryColor { get; set; } = "#FFD76A";
    public string SecondaryColor { get; set; } = "#2A1B08";
    public double Opacity { get; set; } = 1;
    public double Scale { get; set; } = 1;
    public double Speed { get; set; } = 1;
    public double Intensity { get; set; } = 1;

    public static TypographyIdentityPreset CreateDefault() => new();

    public TypographyIdentityPreset Normalized()
    {
        var preset = (TypographyIdentityPreset)MemberwiseClone();
        preset.FontFamily = TypographyFontCatalog.ResolveFamily(FontFamily);
        preset.FontSize = Math.Clamp(FontSize <= 0 ? 18 : FontSize, 12, 34);
        preset.FrameThickness = Math.Clamp(FrameThickness <= 0 ? 1.4 : FrameThickness, 0.8, 4);
        preset.Opacity = Math.Clamp(Opacity <= 0 ? 1 : Opacity, 0.35, 1);
        preset.Scale = Math.Clamp(Scale <= 0 ? 1 : Scale, 0.8, 1.35);
        preset.Speed = Math.Clamp(Speed <= 0 ? 1 : Speed, 0.5, 2);
        preset.Intensity = Math.Clamp(Intensity <= 0 ? 1 : Intensity, 0.2, 1.6);
        preset.PrimaryColor = ValidColor(PrimaryColor, "#FFD76A");
        preset.SecondaryColor = ValidColor(SecondaryColor, "#2A1B08");
        preset.MaterialPreset = NormalizeToken(MaterialPreset, "SatinGold");
        preset.LightingPreset = NormalizeToken(LightingPreset, "SoftRim");
        preset.DepthPreset = NormalizeToken(DepthPreset, "Low");
        preset.MotionPreset = NormalizeToken(MotionPreset, "None");
        preset.ParticlePreset = NormalizeToken(ParticlePreset, "None");
        preset.FrameStylePreset = NormalizeToken(FrameStylePreset, "None");
        return preset;
    }

    private static string NormalizeToken(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string ValidColor(string? value, string fallback)
    {
        var token = value?.Trim();
        if (string.IsNullOrWhiteSpace(token) || token[0] != '#')
            return fallback;
        var hex = token[1..];
        return (hex.Length == 6 || hex.Length == 8) &&
               hex.All(Uri.IsHexDigit)
            ? token
            : fallback;
    }
}

public sealed record TypographyFontOption(
    string Family,
    string DisplayName,
    bool ArabicFriendly);

public static class TypographyFontCatalog
{
    public const string DefaultFontFamily = "Tajawal-Regular";

    public static IReadOnlyList<TypographyFontOption> Fonts { get; } =
    [
        new("OpenSansRegular", "Open Sans Regular", false),
        new("OpenSansSemibold", "Open Sans Semibold", false),
        new("BAUHS93", "Bauhaus 93", false),
        new("CinzelDecorative-Bold", "Cinzel Decorative Bold", false),
        new("HARLOWSI", "Harlow Solid Italic", false),
        new("Tajawal-Regular", "Tajawal Regular", true),
        new("FS_Cairo", "FS Cairo", true),
        new("timesbi", "Times Bold Italic", false),
        new("NotoNaskhArabic-VariableFont_wght", "Noto Naskh Arabic", true),
        new("DG-Nemr-V.0", "DG Nemr", true)
    ];

    public static IReadOnlyList<string> FontFamilies { get; } =
        Fonts.Select(font => font.Family).ToArray();

    public static string ResolveFamily(string? family)
    {
        var token = family?.Trim();
        return Fonts.Any(font => string.Equals(font.Family, token, StringComparison.Ordinal))
            ? token!
            : DefaultFontFamily;
    }
}

public static class TypographyPresetCatalog
{
    public static IReadOnlyList<string> Materials { get; } =
        ["SatinGold", "IvoryInk", "PearlSteel", "EmeraldGlass", "RubyLacquer"];
    public static IReadOnlyList<string> Lighting { get; } =
        ["SoftRim", "TopSheen", "InnerGlow", "LowContrast"];
    public static IReadOnlyList<string> Depth { get; } =
        ["Flat", "Low", "Medium"];
    public static IReadOnlyList<string> Motion { get; } =
        ["None", "Breath", "SoftShine"];
    public static IReadOnlyList<string> Particles { get; } =
        ["None", "Dust", "TinySparks"];
    public static IReadOnlyList<string> Frames { get; } =
        ["None", "Plate", "Ribbon", "GemInset", "SoftCapsule"];
}

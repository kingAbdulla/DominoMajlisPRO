using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record EffectColorPreset(
    EffectColorPresetId ColorPresetId,
    string DisplayName,
    string ArabicDisplayName,
    string Hex);

public sealed record EffectPresetCatalogItem(
    EffectPresetId PresetId,
    string DisplayName,
    string ArabicDisplayName,
    EffectAnimationId DefaultAnimationId,
    IReadOnlyList<EffectLayerId> DefaultLayers,
    double DefaultOpacity,
    double DefaultScale,
    double DefaultSpeed,
    double DefaultIntensity);

public static class EffectPresetCatalog
{
    public static IReadOnlyList<EffectPresetCatalogItem> Presets { get; } =
        new[]
        {
            Create(EffectPresetId.Glow, "Glow", "توهج", EffectAnimationId.Breathing, 0.74, 1.18, 1.0, 1.0, EffectLayerId.Glow, EffectLayerId.Aura),
            Create(EffectPresetId.Aura, "Aura", "هالة", EffectAnimationId.Breathing, 0.64, 1.22, 0.85, 1.1, EffectLayerId.Aura, EffectLayerId.Glow),
            Create(EffectPresetId.Ring, "Ring", "حلقة", EffectAnimationId.Rotate, 0.72, 1.18, 0.9, 1.0, EffectLayerId.Ring, EffectLayerId.Glow),
            Create(EffectPresetId.Pulse, "Pulse", "نبض", EffectAnimationId.Pulse, 0.66, 1.20, 1.1, 1.0, EffectLayerId.Pulse, EffectLayerId.Glow),
            Create(EffectPresetId.Lightning, "Lightning", "برق", EffectAnimationId.Lightning, 0.80, 1.24, 1.35, 1.15, EffectLayerId.Glow, EffectLayerId.Particle),
            Create(EffectPresetId.Fire, "Fire", "نار", EffectAnimationId.Pulse, 0.76, 1.22, 1.1, 1.1, EffectLayerId.Glow, EffectLayerId.Aura),
            Create(EffectPresetId.Ice, "Ice", "جليد", EffectAnimationId.Fade, 0.70, 1.20, 0.8, 1.0, EffectLayerId.Glow, EffectLayerId.Border),
            Create(EffectPresetId.Shadow, "Shadow", "ظل", EffectAnimationId.Fade, 0.52, 1.14, 0.8, 1.0, EffectLayerId.Shadow),
            Create(EffectPresetId.Royal, "Royal", "ملكي", EffectAnimationId.Rotate, 0.78, 1.21, 0.7, 1.15, EffectLayerId.Ring, EffectLayerId.Glow, EffectLayerId.Aura),
            Create(EffectPresetId.Diamond, "Diamond", "ماسي", EffectAnimationId.Flash, 0.76, 1.22, 1.2, 1.15, EffectLayerId.Glow, EffectLayerId.Particle)
        };

    public static IReadOnlyList<EffectColorPreset> Colors { get; } =
        new[]
        {
            new EffectColorPreset(EffectColorPresetId.Gold, "Gold", "ذهبي", "#D4AF37"),
            new EffectColorPreset(EffectColorPresetId.Silver, "Silver", "فضي", "#D8D8D8"),
            new EffectColorPreset(EffectColorPresetId.Emerald, "Emerald", "زمردي", "#00C853"),
            new EffectColorPreset(EffectColorPresetId.Sapphire, "Sapphire", "ياقوت أزرق", "#4FC3F7"),
            new EffectColorPreset(EffectColorPresetId.Ruby, "Ruby", "ياقوت أحمر", "#FF5252"),
            new EffectColorPreset(EffectColorPresetId.Purple, "Purple", "بنفسجي", "#B56CFF"),
            new EffectColorPreset(EffectColorPresetId.Fire, "Fire", "ناري", "#FF8A00"),
            new EffectColorPreset(EffectColorPresetId.Ice, "Ice", "جليدي", "#A7F3FF"),
            new EffectColorPreset(EffectColorPresetId.Shadow, "Shadow", "ظل", "#2B2B2B"),
            new EffectColorPreset(EffectColorPresetId.Rainbow, "Rainbow", "قوس قزح", "#D4AF37"),
            new EffectColorPreset(EffectColorPresetId.TeamTheme, "Team Theme", "لون الفريق", "#D4AF37"),
            new EffectColorPreset(EffectColorPresetId.PlayerTheme, "Player Theme", "لون اللاعب", "#D4AF37"),
            new EffectColorPreset(EffectColorPresetId.Custom, "Custom", "مخصص", "#D4AF37")
        };

    public static EffectPresetCatalogItem ResolvePreset(EffectPresetId presetId) =>
        Presets.FirstOrDefault(item => item.PresetId == presetId) ??
        Presets[0];

    public static Color ResolveColor(
        EffectColorPresetId presetId,
        string customHex = "",
        string themeHex = "")
    {
        if ((presetId == EffectColorPresetId.Custom ||
             presetId == EffectColorPresetId.TeamTheme ||
             presetId == EffectColorPresetId.PlayerTheme) &&
            IsValidHex(customHex))
        {
            return Color.FromArgb(customHex);
        }

        if ((presetId == EffectColorPresetId.TeamTheme ||
             presetId == EffectColorPresetId.PlayerTheme) &&
            IsValidHex(themeHex))
        {
            return Color.FromArgb(themeHex);
        }

        var color = Colors.FirstOrDefault(item => item.ColorPresetId == presetId);
        return Color.FromArgb(color?.Hex ?? "#D4AF37");
    }

    public static EffectDefinitionModel CreateDefinition(
        string effectId,
        EffectOwnerScope ownerScope,
        EffectPresetId presetId,
        EffectColorPresetId primaryColorPresetId,
        EffectColorPresetId secondaryColorPresetId)
    {
        var preset = ResolvePreset(presetId);
        return new EffectDefinitionModel(
            effectId,
            ownerScope,
            preset.PresetId,
            preset.DefaultAnimationId,
            primaryColorPresetId,
            secondaryColorPresetId,
            preset.DefaultLayers,
            preset.DefaultOpacity,
            preset.DefaultScale,
            preset.DefaultSpeed,
            preset.DefaultIntensity);
    }

    static EffectPresetCatalogItem Create(
        EffectPresetId presetId,
        string displayName,
        string arabicDisplayName,
        EffectAnimationId defaultAnimationId,
        double opacity,
        double scale,
        double speed,
        double intensity,
        params EffectLayerId[] layers) =>
        new(
            presetId,
            displayName,
            arabicDisplayName,
            defaultAnimationId,
            layers,
            opacity,
            scale,
            speed,
            intensity);

    static bool IsValidHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();
        return text.Length == 7 &&
               text[0] == '#' &&
               text.Skip(1).All(Uri.IsHexDigit);
    }
}

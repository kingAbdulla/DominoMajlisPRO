using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public enum EffectOwnerScope
{
    Player,
    Team,
    Global
}

public enum EffectPresetId
{
    Glow,
    Aura,
    Ring,
    Pulse,
    Lightning,
    Fire,
    Ice,
    Shadow,
    Royal,
    Diamond
}

public enum EffectAnimationId
{
    None,
    Pulse,
    Rotate,
    Fade,
    Flash,
    Orbit,
    Breathing,
    Lightning
}

public enum EffectLayerId
{
    Glow,
    Aura,
    Ring,
    Border,
    Pulse,
    Particle,
    Shadow
}

public enum EffectColorPresetId
{
    Gold,
    Silver,
    Emerald,
    Sapphire,
    Ruby,
    Purple,
    Fire,
    Ice,
    Shadow,
    Rainbow,
    TeamTheme,
    PlayerTheme,
    Custom
}

public sealed record EffectDefinitionModel(
    string EffectId,
    EffectOwnerScope OwnerScope,
    EffectPresetId PresetId,
    EffectAnimationId AnimationId,
    EffectColorPresetId PrimaryColorPresetId,
    EffectColorPresetId SecondaryColorPresetId,
    IReadOnlyList<EffectLayerId> Layers,
    double Opacity,
    double Scale,
    double Speed,
    double Intensity,
    string CustomPrimaryColorHex = "",
    string CustomSecondaryColorHex = "",
    string LegacyImagePath = "")
{
    public static EffectDefinitionModel DefaultPlayerGlow(string effectId) =>
        new(
            effectId,
            EffectOwnerScope.Player,
            EffectPresetId.Glow,
            EffectAnimationId.Breathing,
            EffectColorPresetId.Gold,
            EffectColorPresetId.Gold,
            new[]
            {
                EffectLayerId.Glow,
                EffectLayerId.Aura
            },
            0.74,
            1.18,
            1.0,
            1.0);
}

public sealed record EffectRenderProfile(
    Color PrimaryColor,
    Color SecondaryColor,
    double Opacity,
    double Scale,
    uint Duration,
    float ShadowRadius,
    float ShadowOpacity,
    bool UseLegacyImage,
    string LegacyImagePath);

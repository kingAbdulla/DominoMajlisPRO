namespace DominoMajlisPRO.LivingVisualPlatform.Models;

public enum LivingVisualAssetScope
{
    TeamEmblem,
    PlayerAvatar,
    PlayerFrame,
    PlayerBackground,
    Trophy,
    Stadium,
    Dice,
    Unknown
}

public enum LivingVisualAssetKind
{
    LivingLegendaryEmblem,
    LivingAvatar,
    LivingFrame,
    LivingBackground,
    LivingTrophy,
    LivingStadium,
    LivingDice,
    Unknown
}

[Flags]
public enum LivingVisualCapability
{
    None = 0,
    Bones = 1 << 0,
    MorphTargets = 1 << 1,
    Materials = 1 << 2,
    Particles = 1 << 3,
    Lighting = 1 << 4,
    Fire = 1 << 5,
    Smoke = 1 << 6,
    Ice = 1 << 7,
    Reflection = 1 << 8,
    Ripple = 1 << 9,
    Float = 1 << 10,
    Blink = 1 << 11,
    Jaw = 1 << 12,
    Wings = 1 << 13,
    EyeTracking = 1 << 14,
    DefensivePulse = 1 << 15,
    JewelPulse = 1 << 16,
    BehaviorBrain = 1 << 17,
    FallbackStatic = 1 << 18
}

public enum LivingRendererBackend
{
    None,
    StaticFallback,
    Filament,
    Unity,
    Godot,
    WebGL,
    Custom,
    Unknown
}

public enum LivingRenderEligibilityStatus
{
    StaticOnly,
    LivingAllowed,
    LightweightFallback,
    DeniedOwnership,
    DeniedLocation,
    DeniedDevice,
    DeniedAsset,
    RendererUnavailable,
    RendererFailed,
    Unknown
}

public enum LivingVisualDisplayLocation
{
    StorePreview,
    StoreActionSheet,
    Inventory,
    CreateTeamPreview,
    EditTeamPreview,
    MainPageTeamSelector,
    GamePageTeamEmblem,
    MatchDetailsTeamEmblem,
    HistoryTeamEmblem,
    RankingsTeamSection,
    HallOfFameTeamSection,
    CertificateTeamEmblem,
    Unknown
}

using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ╔══════════════════════════════════════════════════════════════════╗
// ║  Phase 2.5-C  —  Living Emblem Behavior Engine                  ║
// ║                                                                  ║
// ║  Architecture:                                                   ║
// ║    EmblemVisualDna          — fully editable per-emblem DNA      ║
// ║    EmblemBehaviorProfile    — DNA + palette per emblem type      ║
// ║    EmblemBehaviorState      — state enum per emblem              ║
// ║    EmblemTimelineStep       — one step in a behavior timeline    ║
// ║    EmblemBehaviorTimeline   — ordered timeline for one type      ║
// ║    EmblemBehaviorBrain      — state machine + timeline + random  ║
// ║    EmblemRenderFrame        — immutable snapshot sent to renderer║
// ║    IEmblemBehaviorRenderer  — renderer contract (draw only)      ║
// ║    EmblemBehaviorRendererResolver — static zero-alloc lookup     ║
// ║    Per-emblem renderers     — 6 sealed classes                   ║
// ║    LivingEmblemDrawable     — thin orchestrator                  ║
// ║    LivingTeamEmblemView     — public GraphicsView widget         ║
// ║                                                                  ║
// ║  WYSIWYG contract:                                               ║
// ║    Preview = Published = Runtime (same component, same pipeline) ║
// ╚══════════════════════════════════════════════════════════════════╝

// ══════════════════════════════════════════════════════════════════
// EmblemVisualPalette  — single source of truth for all glow colors.
// Renderers read from EmblemRenderFrame which is populated from DNA.
// No inline hex literals anywhere else in this file.
// ══════════════════════════════════════════════════════════════════
internal static class EmblemVisualPalette
{
    public static readonly Color DragonPrimary   = Color.FromArgb("#FF6B00");
    public static readonly Color DragonSecondary = Color.FromArgb("#FFD700");
    public static readonly Color DragonSmoke1    = Color.FromArgb("#802200");
    public static readonly Color DragonSmoke2    = Color.FromArgb("#552200");
    public static readonly Color DragonEyeGlow   = Color.FromArgb("#FFB830");

    public static readonly Color LionPrimary     = Color.FromArgb("#FFD700");
    public static readonly Color LionSecondary   = Color.FromArgb("#FFA500");
    public static readonly Color LionSheen       = Color.FromArgb("#FFD700");

    public static readonly Color EaglePrimary    = Color.FromArgb("#00BFFF");
    public static readonly Color EagleSecondary  = Color.FromArgb("#FFFFFF");
    public static readonly Color EagleStreak     = Color.FromArgb("#FFFFFF");
    public static readonly Color EagleGlint      = Color.FromArgb("#80DFFF");

    public static readonly Color WolfPrimary     = Color.FromArgb("#9ECFFF");
    public static readonly Color WolfSecondary   = Color.FromArgb("#E0F0FF");
    public static readonly Color WolfFrost       = Color.FromArgb("#C8E8FF");
    public static readonly Color WolfIceGlint    = Color.FromArgb("#DDEFFF");

    public static readonly Color CrownPrimary    = Color.FromArgb("#FFD700");
    public static readonly Color CrownSecondary  = Color.FromArgb("#E8C060");
    public static readonly Color CrownSparkle    = Color.FromArgb("#FFE860");

    public static readonly Color ShieldPrimary   = Color.FromArgb("#C0C8D8");
    public static readonly Color ShieldSecondary = Color.FromArgb("#E8F0FF");
    public static readonly Color ShieldArc       = Color.FromArgb("#E0E8FF");
    public static readonly Color ShieldSheen     = Color.FromArgb("#D0D8F0");
}

// ══════════════════════════════════════════════════════════════════
// EmblemPerformanceSettings  — DeviceProfile → timer interval.
// Resolved once at Attach time. Never called inside Draw().
// ══════════════════════════════════════════════════════════════════
internal static class EmblemPerformanceSettings
{
    public static TimeSpan GetTimerInterval()
    {
        return DeviceProfiler.CurrentProfile switch
        {
            DeviceProfile.VeryLite => TimeSpan.FromMilliseconds(100), // ~10 fps
            DeviceProfile.Lite     => TimeSpan.FromMilliseconds(80),  // ~12 fps
            DeviceProfile.Medium   => TimeSpan.FromMilliseconds(66),  // ~15 fps
            DeviceProfile.High     => TimeSpan.FromMilliseconds(50),  // ~20 fps
            DeviceProfile.Ultra    => TimeSpan.FromMilliseconds(40),  // ~25 fps
            _                      => TimeSpan.FromMilliseconds(66),
        };
    }
}

// ══════════════════════════════════════════════════════════════════
// EmblemVisualDna  — all visual parameters, fully editable.
// Renderer consumes ONLY these values. No hardcoded constants
// inside any renderer. Future DNA fields are added here only.
// Developer Studio publishes these values → same as runtime render.
// ══════════════════════════════════════════════════════════════════
internal sealed record EmblemVisualDna(
    // ── Palette ──────────────────────────────────────────────────
    Color PrimaryGlow,
    Color SecondaryGlow,
    Color Accent1,           // smoke1 / sheen / streak / frost / sparkle / arc
    Color Accent2,           // smoke2 / — / glint / iceGlint / — / sheen
    Color AccentEye,         // eye glow color
    // ── Glow / aura ──────────────────────────────────────────────
    float GlowAlpha,         // outer glow base opacity  0–1
    float GlowStrength,      // glow radius multiplier   0.8–1.4
    float AuraRadius,        // extra aura ring radius   0–1
    float PulseAmplitude,    // breathing depth          0–0.25
    float PulseSpeed,        // breathing cycles/sec
    float PulseCurve,        // sinusoidal curve shaping 0–1 (0=linear,1=smooth)
    // ── State machine speed ──────────────────────────────────────
    float BrainSpeed,        // global timeline scale    0.5–2.0
    // ── Smoke ─────────────────────────────────────────────────────
    float SmokeDensity,      // wisp render weight       0–1
    float SmokeSpeed,        // wisp drift speed         0–1
    // ── Eye ───────────────────────────────────────────────────────
    float EyeGlow,           // eye intensity            0–1
    float EyeBlinkInterval,  // seconds between blinks   1–6
    // ── Reflection / metal ────────────────────────────────────────
    float ReflectionStrength,// arc/sheen intensity      0–1
    // ── Heat ──────────────────────────────────────────────────────
    float HeatStrength,      // warmth overlay           0–1
    float HeatRadius,        // warmth radius scale      0–1
    // ── Particles / sparkle ───────────────────────────────────────
    float SparkCount,        // dot count multiplier     0–1
    float SparkLifetime,     // individual spark duration 0–1
    // ── Secondary ring / dignity ─────────────────────────────────
    float PulseStrength,     // secondary ring weight    0–1
    // ── Wing / head ───────────────────────────────────────────────
    float WingAmplitude,     // wing twitch scale        0–1
    float HeadRotation);     // head movement scale      0–1

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorProfile  — associates DNA + behavior ID + type.
// One profile per EmblemType. Future variants get their own profile
// without touching any renderer (data-driven, no renderer dup).
// ══════════════════════════════════════════════════════════════════
internal sealed record EmblemBehaviorProfile(
    EmblemType Type,
    string BehaviorId,
    EmblemVisualDna Dna)
{
    // ── Canonical AssetId → EmblemType (exact match, no Contains) ──
    public static EmblemType ResolveType(string? assetId)
    {
        var id = assetId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id)) return EmblemType.Shield;

        if (string.Equals(id, "team-emblem-dragon-3d", StringComparison.OrdinalIgnoreCase)) return EmblemType.Dragon;
        if (string.Equals(id, "team-emblem-lion-3d",   StringComparison.OrdinalIgnoreCase)) return EmblemType.Lion;
        if (string.Equals(id, "team-emblem-eagle-3d",  StringComparison.OrdinalIgnoreCase)) return EmblemType.Eagle;
        if (string.Equals(id, "team-emblem-wolf-3d",   StringComparison.OrdinalIgnoreCase)) return EmblemType.Wolf;
        if (string.Equals(id, "team-emblem-crown-3d",  StringComparison.OrdinalIgnoreCase)) return EmblemType.Crown;
        if (string.Equals(id, "team-emblem-shield-3d", StringComparison.OrdinalIgnoreCase)) return EmblemType.Shield;

        var norm = id.ToLowerInvariant();
        if (norm is "dragon_3d" or "dragon_3d.png" or "emblem-dragon-3d" or "dragon-3d") return EmblemType.Dragon;
        if (norm is "lion_3d"   or "lion_3d.png"   or "emblem-lion-3d"   or "lion-3d")   return EmblemType.Lion;
        if (norm is "eagle_3d"  or "eagle_3d.png"  or "emblem-eagle-3d"  or "eagle-3d")  return EmblemType.Eagle;
        if (norm is "wolf_3d"   or "wolf_3d.png"   or "emblem-wolf-3d"   or "wolf-3d")   return EmblemType.Wolf;
        if (norm is "crown_3d"  or "crown_3d.png"  or "emblem-crown-3d"  or "crown-3d")  return EmblemType.Crown;
        if (norm is "shield_3d" or "shield_3d.png" or "emblem-shield-3d" or "shield-3d") return EmblemType.Shield;

        return EmblemType.Shield;
    }

    // ── Default profiles  (Developer Studio publishes these values) ─
    public static EmblemBehaviorProfile For(string? emblemAssetId) =>
        ForType(ResolveType(emblemAssetId));

    public static EmblemBehaviorProfile ForType(EmblemType t) => t switch
    {
        EmblemType.Dragon => new(EmblemType.Dragon, "FireBreath", new EmblemVisualDna(
            PrimaryGlow: EmblemVisualPalette.DragonPrimary,
            SecondaryGlow: EmblemVisualPalette.DragonSecondary,
            Accent1: EmblemVisualPalette.DragonSmoke1,
            Accent2: EmblemVisualPalette.DragonSmoke2,
            AccentEye: EmblemVisualPalette.DragonEyeGlow,
            GlowAlpha: 0.32f, GlowStrength: 1.12f, AuraRadius: 0.18f,
            PulseAmplitude: 0.16f, PulseSpeed: 0.72f, PulseCurve: 0.80f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.90f, SmokeSpeed: 0.55f,
            EyeGlow: 1.00f, EyeBlinkInterval: 2.8f,
            ReflectionStrength: 0.0f,
            HeatStrength: 0.85f, HeatRadius: 0.60f,
            SparkCount: 0.0f, SparkLifetime: 0.0f,
            PulseStrength: 0.65f,
            WingAmplitude: 0.0f, HeadRotation: 0.20f)),

        EmblemType.Lion => new(EmblemType.Lion, "Roar", new EmblemVisualDna(
            PrimaryGlow: EmblemVisualPalette.LionPrimary,
            SecondaryGlow: EmblemVisualPalette.LionSecondary,
            Accent1: EmblemVisualPalette.LionSheen,
            Accent2: EmblemVisualPalette.LionSheen,
            AccentEye: EmblemVisualPalette.LionPrimary,
            GlowAlpha: 0.30f, GlowStrength: 1.08f, AuraRadius: 0.14f,
            PulseAmplitude: 0.13f, PulseSpeed: 0.65f, PulseCurve: 0.70f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.0f, SmokeSpeed: 0.0f,
            EyeGlow: 0.0f, EyeBlinkInterval: 3.5f,
            ReflectionStrength: 0.15f,
            HeatStrength: 0.65f, HeatRadius: 0.40f,
            SparkCount: 0.0f, SparkLifetime: 0.0f,
            PulseStrength: 1.00f,
            WingAmplitude: 0.0f, HeadRotation: 0.35f)),

        EmblemType.Eagle => new(EmblemType.Eagle, "WingPulse", new EmblemVisualDna(
            PrimaryGlow: EmblemVisualPalette.EaglePrimary,
            SecondaryGlow: EmblemVisualPalette.EagleSecondary,
            Accent1: EmblemVisualPalette.EagleStreak,
            Accent2: EmblemVisualPalette.EagleGlint,
            AccentEye: EmblemVisualPalette.EagleGlint,
            GlowAlpha: 0.28f, GlowStrength: 1.04f, AuraRadius: 0.12f,
            PulseAmplitude: 0.12f, PulseSpeed: 0.82f, PulseCurve: 0.60f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.0f, SmokeSpeed: 0.0f,
            EyeGlow: 0.85f, EyeBlinkInterval: 1.8f,
            ReflectionStrength: 0.55f,
            HeatStrength: 0.0f, HeatRadius: 0.0f,
            SparkCount: 0.0f, SparkLifetime: 0.0f,
            PulseStrength: 0.50f,
            WingAmplitude: 0.70f, HeadRotation: 0.0f)),

        EmblemType.Wolf => new(EmblemType.Wolf, "FrostBreath", new EmblemVisualDna(
            PrimaryGlow: EmblemVisualPalette.WolfPrimary,
            SecondaryGlow: EmblemVisualPalette.WolfSecondary,
            Accent1: EmblemVisualPalette.WolfFrost,
            Accent2: EmblemVisualPalette.WolfIceGlint,
            AccentEye: EmblemVisualPalette.WolfIceGlint,
            GlowAlpha: 0.26f, GlowStrength: 0.98f, AuraRadius: 0.16f,
            PulseAmplitude: 0.12f, PulseSpeed: 0.76f, PulseCurve: 0.50f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.0f, SmokeSpeed: 0.0f,
            EyeGlow: 0.0f, EyeBlinkInterval: 4.0f,
            ReflectionStrength: 0.55f,
            HeatStrength: 0.0f, HeatRadius: 0.0f,
            SparkCount: 0.0f, SparkLifetime: 0.0f,
            PulseStrength: 0.95f,
            WingAmplitude: 0.0f, HeadRotation: 0.25f)),

        EmblemType.Crown => new(EmblemType.Crown, "RoyalSparkle", new EmblemVisualDna(
            PrimaryGlow: EmblemVisualPalette.CrownPrimary,
            SecondaryGlow: EmblemVisualPalette.CrownSecondary,
            Accent1: EmblemVisualPalette.CrownSparkle,
            Accent2: EmblemVisualPalette.CrownSparkle,
            AccentEye: EmblemVisualPalette.CrownPrimary,
            GlowAlpha: 0.34f, GlowStrength: 1.18f, AuraRadius: 0.22f,
            PulseAmplitude: 0.16f, PulseSpeed: 0.62f, PulseCurve: 0.90f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.0f, SmokeSpeed: 0.0f,
            EyeGlow: 0.0f, EyeBlinkInterval: 5.0f,
            ReflectionStrength: 0.90f,
            HeatStrength: 0.0f, HeatRadius: 0.0f,
            SparkCount: 0.80f, SparkLifetime: 0.65f,
            PulseStrength: 0.60f,
            WingAmplitude: 0.0f, HeadRotation: 0.0f)),

        _ => new(EmblemType.Shield, "DefensivePulse", new EmblemVisualDna(  // Shield + unknown
            PrimaryGlow: EmblemVisualPalette.ShieldPrimary,
            SecondaryGlow: EmblemVisualPalette.ShieldSecondary,
            Accent1: EmblemVisualPalette.ShieldArc,
            Accent2: EmblemVisualPalette.ShieldSheen,
            AccentEye: EmblemVisualPalette.ShieldSecondary,
            GlowAlpha: 0.24f, GlowStrength: 0.94f, AuraRadius: 0.10f,
            PulseAmplitude: 0.10f, PulseSpeed: 0.68f, PulseCurve: 0.40f,
            BrainSpeed: 1.00f,
            SmokeDensity: 0.0f, SmokeSpeed: 0.0f,
            EyeGlow: 0.0f, EyeBlinkInterval: 6.0f,
            ReflectionStrength: 1.00f,
            HeatStrength: 0.0f, HeatRadius: 0.0f,
            SparkCount: 0.0f, SparkLifetime: 0.0f,
            PulseStrength: 0.40f,
            WingAmplitude: 0.0f, HeadRotation: 0.0f)),
    };
}

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorState  — per-emblem state machine states.
// One enum covers all emblems; unused states are simply never entered
// by a given emblem's timeline.
// ══════════════════════════════════════════════════════════════════
internal enum EmblemBehaviorState
{
    // Universal
    Idle = 0,
    Breathing,
    // Dragon
    Preparing,
    FireCharge,
    // FireBreath — future
    Cooldown,
    // Lion
    EyeMovement,
    HeadMovement,
    RoarCharge,
    // Roar — future
    Recover,
    // Eagle
    EyeGlint,
    WingTwitch,
    FeatherMotion,
    WingPulse,
    // FullFlight — future
    // Wolf
    ColdBreath,
    EarMovement,
    EyeBlink,
    FrostWind,
    // Howl — future
    // Shield
    ReflectionSweep,
    MetallicPulse,
    GuardianMode,
    // Crown
    RoyalGlow,
    GemPulse,
    SparkRain,
    RoyalAura,
}

// ══════════════════════════════════════════════════════════════════
// EmblemTimelineStep  — one step in a behavior timeline.
// Duration: seconds. NextState drives the transition.
// ══════════════════════════════════════════════════════════════════
internal readonly struct EmblemTimelineStep
{
    public readonly EmblemBehaviorState State;
    public readonly float Duration;          // seconds
    public EmblemTimelineStep(EmblemBehaviorState s, float d) { State = s; Duration = d; }
}

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorTimeline  — ordered timeline for one emblem type.
// No allocations — steps is a pre-allocated readonly array.
// The engine loops back to step 0 after the last step.
// ══════════════════════════════════════════════════════════════════
internal static class EmblemBehaviorTimeline
{
    // ── Dragon: Idle → Breathing → Preparing → FireCharge → Cooldown → loop ──
    private static readonly EmblemTimelineStep[] Dragon =
    [
        new(EmblemBehaviorState.Idle,        2.5f),
        new(EmblemBehaviorState.Breathing,   1.2f),
        new(EmblemBehaviorState.Preparing,   0.8f),
        new(EmblemBehaviorState.FireCharge,  1.0f),
        new(EmblemBehaviorState.Cooldown,    1.5f),
    ];

    // ── Lion: Idle → Breathing → EyeMovement → HeadMovement → RoarCharge → Recover → loop ──
    private static readonly EmblemTimelineStep[] Lion =
    [
        new(EmblemBehaviorState.Idle,        2.8f),
        new(EmblemBehaviorState.Breathing,   1.0f),
        new(EmblemBehaviorState.EyeMovement, 0.6f),
        new(EmblemBehaviorState.HeadMovement,0.8f),
        new(EmblemBehaviorState.RoarCharge,  0.9f),
        new(EmblemBehaviorState.Recover,     1.2f),
    ];

    // ── Eagle: Idle → EyeGlint → WingTwitch → FeatherMotion → WingPulse → loop ──
    private static readonly EmblemTimelineStep[] Eagle =
    [
        new(EmblemBehaviorState.Idle,          2.2f),
        new(EmblemBehaviorState.EyeGlint,      0.5f),
        new(EmblemBehaviorState.WingTwitch,    0.7f),
        new(EmblemBehaviorState.FeatherMotion, 0.9f),
        new(EmblemBehaviorState.WingPulse,     1.1f),
    ];

    // ── Wolf: Idle → ColdBreath → EarMovement → EyeBlink → FrostWind → loop ──
    private static readonly EmblemTimelineStep[] Wolf =
    [
        new(EmblemBehaviorState.Idle,         2.6f),
        new(EmblemBehaviorState.ColdBreath,   0.8f),
        new(EmblemBehaviorState.EarMovement,  0.6f),
        new(EmblemBehaviorState.EyeBlink,     0.3f),
        new(EmblemBehaviorState.FrostWind,    1.2f),
    ];

    // ── Crown: Idle → RoyalGlow → GemPulse → SparkRain → RoyalAura → loop ──
    private static readonly EmblemTimelineStep[] Crown =
    [
        new(EmblemBehaviorState.Idle,         2.0f),
        new(EmblemBehaviorState.RoyalGlow,    0.8f),
        new(EmblemBehaviorState.GemPulse,     0.6f),
        new(EmblemBehaviorState.SparkRain,    1.2f),
        new(EmblemBehaviorState.RoyalAura,    1.0f),
    ];

    // ── Shield: Idle → ReflectionSweep → MetallicPulse → GuardianMode → loop ──
    private static readonly EmblemTimelineStep[] Shield =
    [
        new(EmblemBehaviorState.Idle,             3.0f),
        new(EmblemBehaviorState.ReflectionSweep,  1.0f),
        new(EmblemBehaviorState.MetallicPulse,    0.8f),
        new(EmblemBehaviorState.GuardianMode,     1.2f),
    ];

    public static ReadOnlySpan<EmblemTimelineStep> For(EmblemType t) => t switch
    {
        EmblemType.Dragon => Dragon,
        EmblemType.Lion   => Lion,
        EmblemType.Eagle  => Eagle,
        EmblemType.Wolf   => Wolf,
        EmblemType.Crown  => Crown,
        _                 => Shield,
    };
}

// ══════════════════════════════════════════════════════════════════
// EmblemRenderFrame  — immutable snapshot passed to renderer.
// The Brain computes this every tick. Renderer draws it. Period.
// No decisions inside the renderer. No profile access in renderer.
// ══════════════════════════════════════════════════════════════════
internal readonly struct EmblemRenderFrame
{
    // Geometry
    public readonly float Cx;
    public readonly float Cy;
    public readonly float R;             // effective glow radius (already GlowStrength-scaled)
    // Breathing
    public readonly float Breath;        // 1 + amplitude * sin(phase*tau)
    public readonly float Sin;           // sin(phase*tau)
    public readonly float SinHalf;       // sin(phase*tau*0.5)
    // State
    public readonly EmblemBehaviorState State;
    public readonly float StateProgress; // 0→1 within current state
    // DNA (fully resolved — renderer reads these, nothing else)
    public readonly float GlowAlpha;
    public readonly float SmokeDensity;
    public readonly float SmokeSpeed;
    public readonly float EyeGlow;
    public readonly float EyeBlinkInterval;
    public readonly float ReflectionStrength;
    public readonly float HeatStrength;
    public readonly float HeatRadius;
    public readonly float SparkCount;
    public readonly float SparkLifetime;
    public readonly float PulseStrength;
    public readonly float WingAmplitude;
    public readonly float HeadRotation;
    public readonly float AuraRadius;
    // Colors (resolved from palette via DNA)
    public readonly Color PrimaryGlow;
    public readonly Color SecondaryGlow;
    public readonly Color Accent1;
    public readonly Color Accent2;
    public readonly Color AccentEye;
    // Randomization offsets (per-instance, deterministic per seed)
    public readonly float RandSmoke;     // 0–1 random smoke timing offset
    public readonly float RandBlink;     // 0–1 random blink timing offset
    public readonly float RandPulse;     // 0–1 random pulse phase offset
    public readonly float RandSpark;     // 0–1 random sparkle phase offset

    public EmblemRenderFrame(
        float cx, float cy, float r, float breath, float sin, float sinHalf,
        EmblemBehaviorState state, float stateProgress,
        EmblemVisualDna dna,
        float randSmoke, float randBlink, float randPulse, float randSpark)
    {
        Cx = cx; Cy = cy; R = r;
        Breath = breath; Sin = sin; SinHalf = sinHalf;
        State = state; StateProgress = stateProgress;
        GlowAlpha = dna.GlowAlpha;
        SmokeDensity = dna.SmokeDensity; SmokeSpeed = dna.SmokeSpeed;
        EyeGlow = dna.EyeGlow; EyeBlinkInterval = dna.EyeBlinkInterval;
        ReflectionStrength = dna.ReflectionStrength;
        HeatStrength = dna.HeatStrength; HeatRadius = dna.HeatRadius;
        SparkCount = dna.SparkCount; SparkLifetime = dna.SparkLifetime;
        PulseStrength = dna.PulseStrength;
        WingAmplitude = dna.WingAmplitude; HeadRotation = dna.HeadRotation;
        AuraRadius = dna.AuraRadius;
        PrimaryGlow = dna.PrimaryGlow; SecondaryGlow = dna.SecondaryGlow;
        Accent1 = dna.Accent1; Accent2 = dna.Accent2; AccentEye = dna.AccentEye;
        RandSmoke = randSmoke; RandBlink = randBlink;
        RandPulse = randPulse; RandSpark = randSpark;
    }
}

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorBrain  — state machine + timeline + randomization.
// One instance per LivingTeamEmblemView (per image).
// Owns the random seed → each team animates independently.
// Brain decides everything; renderer draws the resulting frame.
// ══════════════════════════════════════════════════════════════════
internal sealed class EmblemBehaviorBrain
{
    private readonly EmblemBehaviorProfile _profile;
    // Randomization — unique per instance, deterministic per seed
    private readonly float _randStartDelay;   // seconds, initial idle extra
    private readonly float _randSmoke;        // smoke timing offset
    private readonly float _randBlink;        // blink timing offset
    private readonly float _randPulse;        // pulse phase offset
    private readonly float _randSpark;        // sparkle phase offset

    // Timeline state — no allocations after construction
    private int   _stepIndex;
    private float _stepElapsed;    // seconds into current step
    private float _totalElapsed;   // seconds since brain started

    public EmblemBehaviorState CurrentState    { get; private set; } = EmblemBehaviorState.Idle;
    public float               StateProgress  { get; private set; }
    public int                 CurrentStepIndex => _stepIndex;

    public EmblemBehaviorBrain(EmblemBehaviorProfile profile, int instanceSeed)
    {
        _profile = profile;
        var rng = new Random(instanceSeed);
        _randStartDelay = (float)(rng.NextDouble() * 1.5);  // 0–1.5 sec random start offset
        _randSmoke      = (float)rng.NextDouble();
        _randBlink      = (float)rng.NextDouble();
        _randPulse      = (float)rng.NextDouble();
        _randSpark      = (float)rng.NextDouble();
    }

    // Called every timer tick. deltaSeconds = timer interval in seconds.
    public void Tick(float deltaSeconds)
    {
        _totalElapsed += deltaSeconds;

        // Respect initial random start delay (holds in Idle)
        if (_totalElapsed < _randStartDelay) return;

        var dna      = _profile.Dna;
        var timeline = EmblemBehaviorTimeline.For(_profile.Type);
        if (timeline.Length == 0) return;

        _stepElapsed += deltaSeconds * dna.BrainSpeed;

        ref readonly var step = ref timeline[_stepIndex];
        if (_stepElapsed >= step.Duration)
        {
            _stepElapsed -= step.Duration;
            _stepIndex    = (_stepIndex + 1) % timeline.Length;
        }

        ref readonly var current = ref timeline[_stepIndex];
        CurrentState  = current.State;
        StateProgress = current.Duration > 0f
            ? Math.Clamp(_stepElapsed / current.Duration, 0f, 1f)
            : 0f;
    }

    // Build an immutable RenderFrame from current state. Stack-only, no alloc.
    public EmblemRenderFrame BuildFrame(float cx, float cy, float rectSize)
    {
        var dna = _profile.Dna;
        float r   = rectSize * 0.36f * dna.GlowStrength;
        float sin     = MathF.Sin(_totalElapsed * dna.PulseSpeed * MathF.Tau);
        float sinHalf = MathF.Sin(_totalElapsed * dna.PulseSpeed * MathF.Tau * 0.5f);
        float breath  = 1f + dna.PulseAmplitude * sin;

        return new EmblemRenderFrame(
            cx, cy, r, breath, sin, sinHalf,
            CurrentState, StateProgress,
            dna,
            _randSmoke, _randBlink, _randPulse, _randSpark);
    }
}

// ══════════════════════════════════════════════════════════════════
// IEmblemBehaviorRenderer  — renderer contract.
// Receives only EmblemRenderFrame — makes ZERO decisions.
// No profile access. No animation logic. Pure draw calls.
// No allocations inside Draw.
//
// FamilyId: strongly-typed EffectBehaviorFamily enum used by the
// resolver. BehaviorId (string) kept for JSON/legacy compat.
//
// ── RENDERER INDEPENDENCE (constitutional) ───────────────────────
//
// LivingTeamEmblemView and all IEmblemBehaviorRenderer implementations
// MUST NEVER know about:
//   Dragon, Lion, Eagle, Wolf, Crown, Shield
//   EmblemType, AssetId, TeamId, PlayerId, BehaviorDefinitionId
//
// A renderer may only consume:
//   EmblemRenderFrame  — pre-resolved DNA scalars and state.
//   EffectBehaviorFamily — to identify itself (FamilyId property).
//   EmblemBehaviorProfile — passed in from Brain (already resolved).
//
// All emblem identity decisions are fully resolved BEFORE rendering:
//   AssetId → EffectDefinitionRuntimeResolver → EffectBehaviorRuntimeMapper
//   → EmblemBehaviorProfile (anonymous DNA scalars only)
//   → EmblemBehaviorBrain → EmblemRenderFrame
//   → IEmblemBehaviorRenderer.Draw (no identity knowledge)
//
// The names in EmblemVisualPalette, EmblemBehaviorProfile,
// EmblemBehaviorTimeline, and EmblemBehaviorRendererResolver are
// bootstrap/fallback data only — they exist in the view layer to
// supply a safe procedural default when no Published definition is
// loaded. They are NEVER accessed from inside Draw() or Tick().
// ══════════════════════════════════════════════════════════════════
internal interface IEmblemBehaviorRenderer
{
    string               BehaviorId   { get; }  // legacy string key
    EffectBehaviorFamily FamilyId     { get; }  // strong-type key
    string               RendererName { get; }  // class name for diagnostics only
    void Draw(ICanvas canvas, in EmblemRenderFrame frame);
}

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorRendererResolver  — zero-allocation static lookup.
// Supports both string (legacy) and EffectBehaviorFamily (strong-type).
// ══════════════════════════════════════════════════════════════════
internal static class EmblemBehaviorRendererResolver
{
    private static readonly IEmblemBehaviorRenderer[] _all =
    [
        new DragonEmblemBehaviorRenderer(),
        new LionEmblemBehaviorRenderer(),
        new EagleEmblemBehaviorRenderer(),
        new WolfEmblemBehaviorRenderer(),
        new CrownEmblemBehaviorRenderer(),
        new ShieldEmblemBehaviorRenderer(),
    ];
    private static readonly IEmblemBehaviorRenderer _default = _all[5]; // Shield

    // String-key lookup (legacy path — EmblemBehaviorProfile.BehaviorId).
    public static IEmblemBehaviorRenderer Resolve(string behaviorId)
    {
        foreach (var r in _all)
            if (r.BehaviorId == behaviorId) return r;
        return _default;
    }

    // Strong-type lookup (data-driven path — EffectBehaviorDefinitionModel.BehaviorFamily).
    // Generic falls back to the Shield renderer (safest procedural default).
    public static IEmblemBehaviorRenderer Resolve(EffectBehaviorFamily family)
    {
        foreach (var r in _all)
            if (r.FamilyId == family) return r;
        return _default;
    }
}

// ══════════════════════════════════════════════════════════════════
// Per-emblem renderer implementations
// Each renderer draws ONLY what the RenderFrame tells it to.
// No hardcoded constants. No animation decisions. No profile reads.
// Future renderers: add class + register in resolver. No dup needed.
// ══════════════════════════════════════════════════════════════════

// ── Dragon: smoke wisps (Idle/Breathing/Preparing) + eye glow + heat pulse ──
internal sealed class DragonEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "FireBreath";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.FireBreath;
    public string               RendererName => nameof(DragonEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absSin  = MathF.Abs(f.Sin);
        float smokeR  = f.R * 0.22f * f.SmokeDensity * f.Breath;
        // State-aware smoke intensity: stronger during Preparing / FireCharge
        float stateBoost = f.State is EmblemBehaviorState.Preparing or EmblemBehaviorState.FireCharge
            ? 1f + f.StateProgress * 0.5f : 1f;

        // smoke wisp 1 — drifts up-left, randomized phase
        float wispY1 = f.R * (0.80f + 0.12f * MathF.Sin((f.Sin + f.RandSmoke) * MathF.Tau));
        c.FillColor = f.Accent1.WithAlpha((0.10f + 0.06f * absSin) * stateBoost);
        c.FillCircle(f.Cx - f.R * 0.35f, f.Cy - wispY1, smokeR);

        // smoke wisp 2 — drifts up-right, independent random offset
        float wispY2 = f.R * (0.70f + 0.09f * MathF.Sin((f.Sin + f.RandSmoke * 0.7f) * MathF.Tau));
        c.FillColor = f.Accent2.WithAlpha((0.08f + 0.05f * absSin) * stateBoost);
        c.FillCircle(f.Cx + f.R * 0.20f, f.Cy - wispY2, smokeR * 0.75f);

        // eye glow — blinks on EyeMovement-equivalent (Breathing state peak)
        float eyeAlpha = (0.28f + 0.18f * absSin) * f.EyeGlow * stateBoost;
        c.FillColor = f.AccentEye.WithAlpha(eyeAlpha);
        c.FillCircle(f.Cx, f.Cy - f.R * 0.32f, f.R * 0.09f * f.Breath);

        // heat pulse during FireCharge
        if (f.State == EmblemBehaviorState.FireCharge && f.HeatStrength > 0f)
        {
            float heatAlpha = f.StateProgress * f.HeatStrength * 0.25f;
            c.FillColor = f.PrimaryGlow.WithAlpha(heatAlpha);
            c.FillCircle(f.Cx, f.Cy, f.R * (1.0f + f.HeatRadius * f.StateProgress));
        }
    }
}

// ── Lion: dignity ring (stronger in RoarCharge) + warm sheen ──────
internal sealed class LionEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "Roar";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.Roar;
    public string               RendererName => nameof(LionEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absHalf = MathF.Abs(f.SinHalf);
        // Dignity ring pulsed by PulseStrength + state
        float ringBoost = f.State == EmblemBehaviorState.RoarCharge
            ? 1f + f.StateProgress * 0.4f * f.PulseStrength : 1f;
        float dignityR = f.R * (1.55f + 0.18f * absHalf * f.PulseStrength) * f.Breath * ringBoost;

        c.StrokeColor = f.SecondaryGlow.WithAlpha(0.13f + 0.10f * (1f - absHalf));
        c.StrokeSize  = 1.2f;
        c.DrawCircle(f.Cx, f.Cy, dignityR);

        // warm sheen — HeadMovement state boosts it
        float sheenBoost = f.State == EmblemBehaviorState.HeadMovement
            ? 1f + f.StateProgress * 0.35f : 1f;
        float sheenR  = f.R * 0.28f;
        c.FillColor   = f.Accent1.WithAlpha((0.07f + 0.05f * absHalf) * f.HeatStrength * sheenBoost);
        c.FillEllipse(f.Cx - sheenR * 0.6f, f.Cy - f.R * 0.38f, sheenR * 1.2f, sheenR * 0.7f);
    }
}

// ── Eagle: shimmer streak (WingPulse state) + eye glint (EyeGlint) ──
internal sealed class EagleEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "WingPulse";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.WingPulse;
    public string               RendererName => nameof(EagleEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absSin = MathF.Abs(f.Sin);
        // Shimmer strongest during WingPulse + FeatherMotion states
        float wingBoost = f.State is EmblemBehaviorState.WingPulse or EmblemBehaviorState.FeatherMotion
            ? 1f + f.StateProgress * f.WingAmplitude * 0.5f : 1f;
        float streakAlpha = (0.15f + 0.12f * absSin) * f.ReflectionStrength * wingBoost;
        float streakW     = f.R * (1.60f + 0.20f * absSin) * f.Breath * wingBoost;
        float streakH     = f.R * 0.16f;

        c.FillColor = f.Accent1.WithAlpha(streakAlpha);
        c.FillEllipse(f.Cx - streakW * 0.5f, f.Cy - streakH * 0.5f, streakW, streakH);

        // Eye glint — amplified during EyeGlint state, uses random blink offset
        float blinkMod = f.State == EmblemBehaviorState.EyeGlint
            ? 1f + f.StateProgress * 0.6f : 1f;
        float eyeAlpha = (0.35f + 0.20f * absSin) * f.EyeGlow * blinkMod;
        c.FillColor = f.Accent2.WithAlpha(eyeAlpha);
        c.FillCircle(f.Cx + f.RandBlink * f.R * 0.05f, f.Cy - f.R * 0.30f, f.R * 0.08f * f.Breath);
    }
}

// ── Wolf: frost halo (FrostWind) + icy glint (EyeBlink) ──────────
internal sealed class WolfEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "FrostBreath";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.FrostBreath;
    public string               RendererName => nameof(WolfEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absHalf = MathF.Abs(f.SinHalf);
        // Frost ring expands during FrostWind + ColdBreath states
        float frostBoost = f.State is EmblemBehaviorState.FrostWind or EmblemBehaviorState.ColdBreath
            ? 1f + f.StateProgress * 0.3f * f.PulseStrength : 1f;
        float frostR = f.R * (1.50f + 0.14f * absHalf * f.PulseStrength) * f.Breath * frostBoost;

        c.StrokeColor = f.Accent1.WithAlpha(0.16f + 0.10f * (1f - absHalf));
        c.StrokeSize  = 1.8f;
        c.DrawCircle(f.Cx, f.Cy, frostR);

        // Icy glint — randomized position offset for visual uniqueness per instance
        float glintAlpha = (0.18f + 0.10f * absHalf) * f.ReflectionStrength;
        float glintX = f.Cx + f.R * (0.15f + f.RandBlink * 0.08f);
        float glintW = f.R * 0.30f * f.Breath;
        c.FillColor = f.Accent2.WithAlpha(glintAlpha);
        c.FillEllipse(glintX, f.Cy - f.R * 0.42f, glintW, f.R * 0.10f);
    }
}

// ── Crown: sparkle constellation (SparkRain) + royal cross-shine ──
internal sealed class CrownEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "RoyalSparkle";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.RoyalSparkle;
    public string               RendererName => nameof(CrownEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absSin = MathF.Abs(f.Sin);
        // Sparkle intensity strongest during SparkRain + RoyalAura
        float sparkBoost = f.State is EmblemBehaviorState.SparkRain or EmblemBehaviorState.RoyalAura
            ? 1f + f.StateProgress * f.SparkCount * 0.5f : 1f;
        float dotR    = f.R * 0.07f * f.Breath;
        float dist    = f.R * 1.52f;
        float twinkle = (0.20f + 0.18f * absSin) * f.ReflectionStrength * sparkBoost;

        // Four cardinal sparkle dots — RandSpark gives each instance unique phase offset
        float sparkPhase = f.RandSpark * MathF.Tau;
        c.FillColor = f.Accent1.WithAlpha(twinkle);
        c.FillCircle(f.Cx + dist * MathF.Sin(sparkPhase),        f.Cy - dist * MathF.Cos(sparkPhase),        dotR);
        c.FillCircle(f.Cx + dist * MathF.Sin(sparkPhase + 1.57f), f.Cy - dist * MathF.Cos(sparkPhase + 1.57f), dotR * 0.85f);
        c.FillCircle(f.Cx + dist * MathF.Sin(sparkPhase + 3.14f), f.Cy - dist * MathF.Cos(sparkPhase + 3.14f), dotR * 0.70f);
        c.FillCircle(f.Cx + dist * MathF.Sin(sparkPhase + 4.71f), f.Cy - dist * MathF.Cos(sparkPhase + 4.71f), dotR * 0.85f);

        // Royal cross-shine
        float shineLen   = f.R * 0.50f;
        float shineAlpha = (0.09f + 0.07f * absSin) * f.ReflectionStrength;
        c.StrokeColor = f.PrimaryGlow.WithAlpha(shineAlpha);
        c.StrokeSize  = 1.0f;
        c.DrawLine(f.Cx - shineLen, f.Cy, f.Cx + shineLen, f.Cy);
        c.DrawLine(f.Cx, f.Cy - shineLen * 0.70f, f.Cx, f.Cy + shineLen * 0.70f);
    }
}

// ── Shield: metallic arc sweep (ReflectionSweep) + top sheen ──────
internal sealed class ShieldEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string               BehaviorId   => "DefensivePulse";
    public EffectBehaviorFamily FamilyId     => EffectBehaviorFamily.ShieldReflect;
    public string               RendererName => nameof(ShieldEmblemBehaviorRenderer);

    public void Draw(ICanvas c, in EmblemRenderFrame f)
    {
        float absHalf = MathF.Abs(f.SinHalf);
        // Arc sweeps during ReflectionSweep + GuardianMode states
        float arcBoost = f.State is EmblemBehaviorState.ReflectionSweep or EmblemBehaviorState.GuardianMode
            ? 1f + f.StateProgress * 0.3f : 1f;
        float arcR     = f.R * 1.08f * f.Breath;
        float arcAlpha = (0.18f + 0.12f * absHalf) * f.ReflectionStrength * arcBoost;

        c.StrokeColor = f.Accent1.WithAlpha(arcAlpha);
        c.StrokeSize  = 2.2f;
        // Arc angle subtly shifts with RandPulse for per-instance uniqueness
        float arcStart = 120f + f.RandPulse * 15f;
        c.DrawArc(f.Cx - arcR, f.Cy - arcR, arcR * 2f, arcR * 2f, arcStart, 80, false, false);

        // Metallic pulse ring during MetallicPulse state
        if (f.State == EmblemBehaviorState.MetallicPulse)
        {
            float pulseAlpha = f.StateProgress * f.ReflectionStrength * 0.18f;
            c.StrokeColor = f.SecondaryGlow.WithAlpha(pulseAlpha);
            c.StrokeSize  = 1.0f;
            c.DrawCircle(f.Cx, f.Cy, f.R * (1.20f + f.StateProgress * 0.15f));
        }

        // Top sheen ellipse
        float sheenW = f.R * 0.90f * f.Breath;
        c.FillColor  = f.Accent2.WithAlpha((0.09f + 0.07f * absHalf) * f.ReflectionStrength);
        c.FillEllipse(f.Cx - sheenW * 0.5f, f.Cy - f.R * 0.28f, sheenW, f.R * 0.22f);
    }
}

// ══════════════════════════════════════════════════════════════════
// LivingEmblemDrawable  — thin orchestrator.
// Calls Brain.Tick() → builds RenderFrame → calls renderer.
// No animation logic here. No DNA reads. No decisions.
// Zero allocations inside Draw().
// ══════════════════════════════════════════════════════════════════
internal sealed class LivingEmblemDrawable : IDrawable
{
    public EmblemBehaviorBrain? Brain { get; set; }
    private IEmblemBehaviorRenderer _renderer = EmblemBehaviorRendererResolver.Resolve("DefensivePulse");

    // Exposes the active renderer's class name for diagnostics.
    public string RendererName => _renderer.RendererName;

    // String-key path (legacy / hardcoded-fallback)
    public void SetRenderer(string behaviorId)
        => _renderer = EmblemBehaviorRendererResolver.Resolve(behaviorId);

    // Strong-type path (data-driven / definition)
    public void SetRenderer(EffectBehaviorFamily family)
        => _renderer = EmblemBehaviorRendererResolver.Resolve(family);

    public void Draw(ICanvas canvas, RectF rect)
    {
        var brain = Brain;
        if (brain == null || rect.Width <= 1) return;

        float cx      = rect.Center.X;
        float cy      = rect.Center.Y;
        float minSize = MathF.Min(rect.Width, rect.Height);

        // Build immutable frame from brain state (no alloc — struct copy on stack)
        var frame = brain.BuildFrame(cx, cy, minSize);

        canvas.SaveState();

        // ── Layer stack (bottom → top) ────────────────────────────────
        //   [0] Shadow         — TODO future (VisualLayerType.Shadow)
        //   [1] BackgroundAura — TODO future (VisualLayerType.BackgroundAura)
        //   [2] AmbientSmoke   — drawn by per-emblem renderer below
        //   [3] BaseImage      — MAUI Image above this GraphicsView
        //   [4] HeatDistortion — TODO future (VisualLayerType.HeatDistortion)
        //   [5] Fire           — Future: FireBreath full
        //   [6] Particles      — Future: SparkRain advanced
        //   [7] Glow           — ACTIVE: universal breathing aura ↓
        //   [8] UIOverlay      — ZIndex managed by LivingEmblemBehavior

        // ── [7] Universal breathing aura ─────────────────────────────
        float outerR = frame.R * 1.30f * frame.Breath;
        float innerR = frame.R * 1.04f * frame.Breath;

        canvas.FillColor = frame.PrimaryGlow.WithAlpha(frame.GlowAlpha * 0.42f);
        canvas.FillCircle(cx, cy, outerR);

        canvas.FillColor = frame.PrimaryGlow.WithAlpha(frame.GlowAlpha * 0.72f);
        canvas.FillCircle(cx, cy, innerR);

        canvas.StrokeColor = frame.SecondaryGlow.WithAlpha(
            frame.GlowAlpha * 0.88f * (0.68f + 0.32f * MathF.Abs(frame.SinHalf)));
        canvas.StrokeSize = 1.4f;
        canvas.DrawCircle(cx, cy, innerR);

        // ── [2]+[6] Per-emblem behavior (smoke, frost, sparkle…) ─────
        _renderer.Draw(canvas, in frame);

        canvas.RestoreState();
    }
}

// ══════════════════════════════════════════════════════════════════
// LivingTeamEmblemView  — public GraphicsView widget.
//
// WYSIWYG CONTRACT — single component, all contexts:
//   Runtime pages   → LivingEmblemBehavior.Attach(image, teamId)
//                     (ownership gate applies; data-driven first, fallback second)
//   Store preview   → LivingEmblemBehavior.AttachPreview(image, assetId)
//                     or AttachPreview(image, definition) with explicit definition
//   Inventory prev  → same as Store preview
//   Developer prev  → view.SetDefinition(definition) or view.SetEmblem(assetId)
//
// All paths → SetEmblem or SetDefinition → same Brain → same renderer.
// Preview = Published = Runtime. No fake renderer. No divergence.
// ══════════════════════════════════════════════════════════════════
public sealed class LivingTeamEmblemView : GraphicsView
{
    private readonly LivingEmblemDrawable _drawable = new();
    private bool  _running;
    private long  _started;
    private int   _instanceSeed;
    // Diagnostics snapshot — updated in ActivateBrain; read by GetDiagnostics().
    private EffectBehaviorDefinitionModel? _activeDefinition;
    private EffectDefinitionSource         _activeSource         = EffectDefinitionSource.HardcodedDefault;
    private EffectOwnershipState           _ownershipState       = EffectOwnershipState.Allowed;
    private EffectBehaviorFamily           _activeRendererFamily = EffectBehaviorFamily.Generic;
    private string                         _activeRendererName   = string.Empty;
    private int                            _definitionHash;

    public LivingTeamEmblemView()
    {
        Drawable = _drawable;
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
        IsVisible = false;
        Loaded   += (_, _) => StartIfReady();
        Unloaded += (_, _) => _running = false;
    }

    // ── Data-driven path (Phase 2.5-D) ───────────────────────────
    // Called by Runtime (after definition load) and Developer Studio.
    // intensityMultiplier: 1.0 at runtime, > 1.0 in Developer Preview.
    public void SetDefinition(
        EffectBehaviorDefinitionModel definition,
        float intensityMultiplier                  = 1.0f,
        EffectDefinitionSource source              = EffectDefinitionSource.Published,
        EffectOwnershipState ownershipState        = EffectOwnershipState.Allowed)
    {
        var profile = EffectBehaviorRuntimeMapper.DefinitionToProfile(
            definition, intensityMultiplier);
        _activeDefinition     = definition;
        _activeSource         = source;
        _ownershipState       = ownershipState;
        _activeRendererFamily = definition.BehaviorFamily;
        _definitionHash       = HashCode.Combine(
            definition.BehaviorDefinitionId, definition.BehaviorVersion);
        ActivateBrain(profile, definition.BehaviorFamily);
    }

    // ── Hardcoded-fallback path (Phase 2.5-A/B/C compat) ─────────
    // Used when EffectDefinitionRuntimeResolver returns HardcodedDefault/SafeFallback.
    public void SetEmblem(
        string? emblemAssetId,
        EffectDefinitionSource source       = EffectDefinitionSource.HardcodedDefault,
        EffectOwnershipState ownershipState = EffectOwnershipState.Allowed)
    {
        var profile = EmblemBehaviorProfile.For(emblemAssetId);
        _activeDefinition     = null;
        _activeSource         = source;
        _ownershipState       = ownershipState;
        _activeRendererFamily = EffectBehaviorRuntimeMapper.ResolveBehaviorFamily(profile.BehaviorId);
        _definitionHash       = HashCode.Combine(emblemAssetId ?? string.Empty, source);
        ActivateBrain(profile, profile.BehaviorId);
    }

    // ── Preview Snapshot API (Phase 2.5-F stub) ───────────────────
    // Returns a frozen EffectStillFrameRequest for thumbnail rendering.
    // No Timer is started. No Brain ticks.
    // Actual canvas drawing is Phase 2.5-F — callers can build the request now.
    public static EffectStillFrameRequest PrepareStillFrame(
        EffectBehaviorDefinitionModel definition,
        float normalizedTime      = 0f,
        float intensityMultiplier = 1.0f,
        EffectPreviewContextType context = EffectPreviewContextType.Runtime)
        => new EffectStillFrameRequest
        {
            Definition          = definition,
            NormalizedTime      = normalizedTime,
            IntensityMultiplier = intensityMultiplier,
            ContextType         = context,
        };

    // ── Developer Diagnostics ─────────────────────────────────────
    // Returns a snapshot of current runtime state for Developer Mode.
    // Returns null when no brain is active.
    public EffectRuntimeDiagnostics? GetDiagnostics()
    {
        var brain = _drawable.Brain;
        if (brain == null) return null;
        var def      = _activeDefinition;
        var interval = EmblemPerformanceSettings.GetTimerInterval();
        return new EffectRuntimeDiagnostics
        {
            BehaviorDefinitionId = def?.BehaviorDefinitionId  ?? string.Empty,
            BehaviorVersion      = def?.BehaviorVersion       ?? 0,
            BehaviorFamily       = def?.BehaviorFamily        ?? _activeRendererFamily,
            DefinitionSource     = _activeSource,
            TargetScope          = def?.TargetScope           ?? EffectTargetScope.Team,
            TargetVisualType     = def?.TargetVisualType      ?? EffectTargetVisualType.Emblem,
            MinimumEngineVersion = def?.MinimumEngineVersion  ?? 1,
            MaximumEngineVersion = def?.MaximumEngineVersion  ?? 0,
            EngineVersion        = EffectDefinitionValidator.CurrentEngineVersion,
            IsDeprecated         = def?.Deprecated            ?? false,
            IsExperimental       = def?.Experimental          ?? false,
            BrainState           = brain.CurrentState.ToString(),
            CurrentTimelineStep  = brain.CurrentStepIndex,
            CurrentLayer         = 0,  // future: set by layer orchestrator
            CurrentFps           = interval.TotalSeconds > 0
                                   ? (float)(1.0 / interval.TotalSeconds) : 0f,
            DeviceProfile        = DeviceProfiler.CurrentProfile,
            RendererFamily       = _activeRendererFamily,
            RendererName         = _activeRendererName,
            OwnershipState       = _ownershipState,
            DefinitionCacheHit   = _activeSource == EffectDefinitionSource.Published,
            DefinitionHash       = _definitionHash,
            // Timing fields populated to 0 until Phase 2.5-F instruments the timer loop.
            DefinitionLoadTimeMs = 0f,
            CacheAgeMs           = 0f,
            RenderTimeMs         = 0f,
            BrainTickMs          = 0f,
            FrameTimeMs          = 0f,
        };
    }

    public void ClearEmblem()
    {
        _running = false;
        _drawable.Brain = null;
        IsVisible = false;
        Invalidate();
    }

    // String-key path (legacy / hardcoded-fallback)
    private void ActivateBrain(EmblemBehaviorProfile profile, string behaviorId)
    {
        _instanceSeed         = HashCode.Combine(behaviorId, Environment.TickCount64);
        _drawable.Brain       = new EmblemBehaviorBrain(profile, _instanceSeed);
        _drawable.SetRenderer(behaviorId);
        _activeRendererName   = _drawable.RendererName;
        _started    = Environment.TickCount64;
        IsVisible   = true;
        StartIfReady();
        Invalidate();
    }

    // Strong-type path (data-driven)
    private void ActivateBrain(EmblemBehaviorProfile profile, EffectBehaviorFamily family)
    {
        _instanceSeed         = HashCode.Combine(family, Environment.TickCount64);
        _drawable.Brain       = new EmblemBehaviorBrain(profile, _instanceSeed);
        _drawable.SetRenderer(family);
        _activeRendererName   = _drawable.RendererName;
        _started    = Environment.TickCount64;
        IsVisible   = true;
        StartIfReady();
        Invalidate();
    }

    private void StartIfReady()
    {
        if (_running || _drawable.Brain == null || !IsLoaded) return;
        _running = true;

        var interval = EmblemPerformanceSettings.GetTimerInterval();
        float deltaSeconds = (float)interval.TotalSeconds;

        Dispatcher.StartTimer(interval, () =>
        {
            var brain = _drawable.Brain;
            if (!_running || brain == null || !IsLoaded) return false;
            brain.Tick(deltaSeconds);
            Invalidate();
            return true;
        });
    }
}

// ══════════════════════════════════════════════════════════════════
// LivingEmblemOwnershipGate  — decides whether the living glow
// should be active for a given team.
//
// RULES (all must be satisfied for runtime activation):
//   1. The team has an EquippedTeamEffectAssetId
//      (a TeamEffect was deliberately equipped by a team member).
//   2. At least one team player owns a TeamEffect-type asset
//      eligible for the team (via TeamEligibleAssetService).
//   3. A Published definition exists for the emblem's AssetId
//      OR the hardcoded fallback is available (always true).
//
// Phase 2.5-D upgrade:
//   Gate now also tries to load the Published EffectBehaviorDefinitionModel
//   and returns it alongside the assetId so the caller can call
//   SetDefinition() instead of SetEmblem() when available.
//
// Phase 2.5-E complete:
//   Gate now uses EquippedTeamEffectAssetId as the definition lookup key.
//   EmblemType/EmblemAssetId alone never activates living behavior.
//   Gate 2 confirms at least one team player owns a TeamEffect asset.
//
// WYSIWYG CONTRACT:
//   Runtime (pages)    → Attach → gate → SetDefinition or SetEmblem
//   Store Preview      → AttachPreview → SetDefinition or SetEmblem (no gate)
//   Developer Preview  → SetDefinition(definition, intensityMult > 1) (no gate)
// ══════════════════════════════════════════════════════════════════
internal static class LivingEmblemOwnershipGate
{
    // Result carries both assetId and optional Published definition.
    public readonly struct GateResult
    {
        public readonly string?                         EmblemAssetId;
        public readonly EffectBehaviorDefinitionModel?  Definition;
        public bool IsEligible => !string.IsNullOrWhiteSpace(EmblemAssetId);
        public GateResult(string? assetId, EffectBehaviorDefinitionModel? def)
        { EmblemAssetId = assetId; Definition = def; }
    }

    public static async Task<GateResult> ResolveIfEligibleAsync(
        string teamId, TeamProfileModel team)
    {
        // Gate 1: team must have an active equipped effect
        if (string.IsNullOrWhiteSpace(team.EquippedTeamEffectAssetId))
            return default;

        // Gate 2: at least one player must own a TeamEffect asset
        var eligible = await TeamEligibleAssetService.GetEligibleAsync(
            teamId, team.Player1Id, team.IsSinglePlayer ? null : team.Player2Id);

        var hasOwnedEffect = eligible.Any(item =>
            item.IsOwned &&
            string.Equals(
                StoreAssetCatalogService.CanonicalTypeId(item.TeamAssetTypeId),
                StoreProductAssetType.TeamEffect.ToString(),
                StringComparison.OrdinalIgnoreCase));

        if (!hasOwnedEffect) return default;

        // Phase 2.5-E: lookup is against EquippedTeamEffectAssetId (the behavior/effect
        // asset the team explicitly equipped), NOT EmblemAssetId (which is just the
        // base visual emblem image). EmblemType alone never activates living behavior.
        var effectAssetId = team.EquippedTeamEffectAssetId;

        // Gate 3 (Phase 2.5-E): load published definition for the equipped effect asset.
        // If none found, fallback chain in caller uses HardcodedDefault / SafeFallback.
        var definition = await EffectBehaviorDefinitionService
            .GetByAssetIdAsync(effectAssetId);

        return new GateResult(effectAssetId, definition);
    }
}

// ──────────────────────────────────────────────────────────────────
// LivingEmblemBehavior  — attached property, no XAML required
// FIX: glow view is inserted at index (image index) so it renders
//      directly behind the emblem image regardless of ZIndex.
//      Works with Grid (MainPage, GamePage) and StackLayout alike.
// ──────────────────────────────────────────────────────────────────
public static class LivingEmblemBehavior
{
    public static readonly BindableProperty TeamIdProperty =
        BindableProperty.CreateAttached(
            "TeamId", typeof(string), typeof(LivingEmblemBehavior), string.Empty,
            propertyChanged: OnTeamIdChanged);

    public static string GetTeamId(BindableObject v) => (string)v.GetValue(TeamIdProperty);
    public static void SetTeamId(BindableObject v, string val) => v.SetValue(TeamIdProperty, val);

    public static void Attach(Image image, string? teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return;
        SetTeamId(image, teamId.Trim());
    }

    // ── WYSIWYG Preview entry-points ─────────────────────────────────
    // Ownership gate BYPASSED in all preview contexts.
    // Same LivingTeamEmblemView + same Brain + same renderer — Preview = Runtime.

    // Overload 1: assetId only → uses published definition or fallback.
    // Use for: Store preview (before acquire), Inventory preview.
    public static void AttachPreview(Image image, string? emblemAssetId)
    {
        if (string.IsNullOrWhiteSpace(emblemAssetId)) return;
        ApplyOrCreate(image, emblemAssetId.Trim(), definition: null, intensityMult: 1.0f, source: EffectDefinitionSource.StorePreview);
    }

    // Overload 2: explicit definition → Developer Studio WYSIWYG preview.
    // intensityMultiplier > 1.0 allowed for Developer Preview mode.
    // Published values (intensityMult = 1.0) = exactly what user sees at runtime.
    public static void AttachPreview(
        Image image,
        EffectBehaviorDefinitionModel definition,
        float intensityMultiplier = 1.0f)
    {
        if (definition == null) return;
        ApplyOrCreate(image, definition.AssetId, definition, intensityMultiplier, source: EffectDefinitionSource.DeveloperPreview);
    }

    private static void OnTeamIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image) return;
        EnsureHooked(image);
        _ = RefreshAsync(image);
    }

    private static void EnsureHooked(Image image)
    {
        if (image.GetValue(IsHookedProperty) is true) return;
        image.SetValue(IsHookedProperty, true);
        image.Loaded += OnLoaded;
        image.Unloaded += OnUnloaded;

        Action<string> handler = changedTeamId =>
        {
            var myId = GetTeamId(image);
            if (string.IsNullOrWhiteSpace(myId) ||
                !string.Equals(myId, changedTeamId, StringComparison.OrdinalIgnoreCase))
                return;
            MainThread.BeginInvokeOnMainThread(() => _ = RefreshAsync(image));
        };

        image.SetValue(RefreshHandlerProperty, handler);
        AppEvents.TeamAssetsChanged += handler;
    }

    private static async Task RefreshAsync(Image image)
    {
        if (!image.IsLoaded) return;

        var teamId = GetTeamId(image);
        if (string.IsNullOrWhiteSpace(teamId)) return;

        try
        {
            // ── Ownership Gate (Phase 2.5-D) ──────────────────────────
            var team = await TeamProfileService.GetTeamByIdAsync(teamId);
            if (team == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    GetHolder(image).View?.ClearEmblem());
                return;
            }

            // Returns assetId + optional published definition.
            var result = await LivingEmblemOwnershipGate
                .ResolveIfEligibleAsync(teamId, team);

            if (!result.IsEligible)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    GetHolder(image).View?.ClearEmblem());
                return;
            }

            // ── Definition resolution priority chain ──────────────
            // Ownership already passed. Now resolve which definition runs.
            // AssetId = EquippedTeamEffectAssetId (behavior asset, not emblem image).
            // Priority: Published → Hardcoded Default → Safe Fallback.
            // Draft Preview only via explicit Developer Studio call (never here).
            var resolveResult = await EffectDefinitionRuntimeResolver.ResolveAsync(
                new EffectDefinitionRuntimeResolver.ResolveRequest
                {
                    AssetId            = result.EmblemAssetId, // = EquippedTeamEffectAssetId from gate
                    TargetScope        = EffectTargetScope.Team,
                    Context            = EffectPreviewContextType.Runtime,
                    ExplicitDefinition = result.Definition,    // pre-loaded by gate (may be null)
                });

            var def    = resolveResult.Definition;
            var source = resolveResult.Source;
            await MainThread.InvokeOnMainThreadAsync(() =>
                ApplyOrCreate(image, result.EmblemAssetId, def, 1.0f, source));
        }
        catch { /* team resolution failed — skip animation */ }
    }

    // definition: if non-null, SetDefinition is used (data-driven path).
    //             if null, SetEmblem is used (hardcoded-fallback path).
    // intensityMult: 1.0 at runtime; > 1.0 in Developer Preview.
    private static void ApplyOrCreate(
        Image image,
        string? emblemAssetId,
        EffectBehaviorDefinitionModel? definition,
        float intensityMult,
        EffectDefinitionSource source = EffectDefinitionSource.Published,
        EffectOwnershipState ownership = EffectOwnershipState.Allowed)
    {
        var holder = GetHolder(image);

        if (holder.View == null)
        {
            // Walk up to find the nearest Layout ancestor (handles Border wrapper too)
            Layout? parent = null;
            Element? node = image.Parent;
            while (node != null)
            {
                if (node is Layout layout) { parent = layout; break; }
                node = node.Parent;
            }
            if (parent == null) return;

            var view = new LivingTeamEmblemView
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = image.Margin,
                InputTransparent = true,
                BackgroundColor = Colors.Transparent
            };

            SyncSize(image, view, 1.32);

            // Copy Grid attached properties if the immediate parent is a Grid
            if (image.Parent is Grid directGrid)
            {
                Grid.SetRow(view, Grid.GetRow(image));
                Grid.SetColumn(view, Grid.GetColumn(image));
                Grid.SetRowSpan(view, Grid.GetRowSpan(image));
                Grid.SetColumnSpan(view, Grid.GetColumnSpan(image));
            }
            else if (parent is Grid ancestorGrid && image.Parent is Layout immediateLayout)
            {
                // image is inside a nested layout; match outer Grid cell of that layout
                Grid.SetRow(view, Grid.GetRow(immediateLayout));
                Grid.SetColumn(view, Grid.GetColumn(immediateLayout));
                Grid.SetRowSpan(view, Grid.GetRowSpan(immediateLayout));
                Grid.SetColumnSpan(view, Grid.GetColumnSpan(immediateLayout));
            }

            // FIX: insert glow view at index of image so it renders directly behind it.
            // This avoids ZIndex fighting with TeamEffectEngine overlays added later.
            int imgIndex = parent.Children.IndexOf(image);
            int insertAt = imgIndex >= 0 ? imgIndex : parent.Children.Count;
            if (insertAt < parent.Children.Count)
                parent.Children.Insert(insertAt, view);
            else
                parent.Children.Add(view);

            // Ensure image renders on top of glow view
            image.ZIndex = Math.Max(image.ZIndex, 1);
            view.ZIndex = image.ZIndex - 1;

            holder.View = view;
        }

        SyncSize(image, holder.View, 1.32);
        // Data-driven path first; fallback to hardcoded profile.
        if (definition != null)
            holder.View.SetDefinition(definition, intensityMult, source, ownership);
        else
            holder.View.SetEmblem(emblemAssetId, source, ownership);
    }

    private static void SyncSize(Image source, LivingTeamEmblemView view, double scale)
    {
        var w = source.WidthRequest > 0 ? source.WidthRequest : source.Width;
        var h = source.HeightRequest > 0 ? source.HeightRequest : source.Height;
        if (w <= 1) w = 64;
        if (h <= 1) h = 64;
        view.WidthRequest = w * scale;
        view.HeightRequest = h * scale;
        view.MinimumWidthRequest = view.WidthRequest;
        view.MinimumHeightRequest = view.HeightRequest;
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is Image img) _ = RefreshAsync(img);
    }

    private static void OnUnloaded(object? sender, EventArgs e)
    {
        if (sender is not Image img) return;
        img.Loaded -= OnLoaded;
        img.Unloaded -= OnUnloaded;

        var handler = img.GetValue(RefreshHandlerProperty) as Action<string>;
        if (handler != null) AppEvents.TeamAssetsChanged -= handler;

        img.SetValue(RefreshHandlerProperty, null);
        img.SetValue(IsHookedProperty, false);

        var holder = GetHolder(img);
        holder.View?.ClearEmblem();
        holder.View = null;
    }

    // ---- storage helpers ----
    private static readonly BindableProperty IsHookedProperty =
        BindableProperty.CreateAttached("IsHooked", typeof(bool), typeof(LivingEmblemBehavior), false);

    private static readonly BindableProperty RefreshHandlerProperty =
        BindableProperty.CreateAttached("RefreshHandler", typeof(Action<string>), typeof(LivingEmblemBehavior), null);

    private static readonly BindableProperty HolderProperty =
        BindableProperty.CreateAttached("Holder", typeof(ViewHolder), typeof(LivingEmblemBehavior), null);

    private static ViewHolder GetHolder(Image image)
    {
        var h = image.GetValue(HolderProperty) as ViewHolder;
        if (h == null) { h = new ViewHolder(); image.SetValue(HolderProperty, h); }
        return h;
    }

    private sealed class ViewHolder { public LivingTeamEmblemView? View; }
}

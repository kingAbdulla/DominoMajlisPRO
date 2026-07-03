using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ╔══════════════════════════════════════════════════════════════════╗
// ║  Phase 2.5-E  —  Data-Driven Effect Publishing Contract         ║
// ║                                                                  ║
// ║  EffectBehaviorDefinitionModel is the single published record   ║
// ║  that bridges Developer Studio → Store → Inventory → Runtime.   ║
// ║                                                                  ║
// ║  One constitutional pipeline — no alternative path:             ║
// ║    Seed (once, idempotent)                                       ║
// ║    → Published Definition (EffectBehaviorDefinitionModel)        ║
// ║    → EffectDefinitionRuntimeResolver (highest compat version)   ║
// ║    → EffectBehaviorRuntimeMapper (DNA → EmblemBehaviorProfile)  ║
// ║    → EmblemBehaviorBrain (tick)                                  ║
// ║    → IEmblemBehaviorRenderer (draw, no identity knowledge)       ║
// ║                                                                  ║
// ║  Content Asset Guarantees (constitutional, permanent):           ║
// ║                                                                  ║
// ║  1. Official packs seeded once, then equal to any published      ║
// ║     pack. No if(OfficialPack) / if(DragonPack) in runtime.      ║
// ║     SeedAllAsync key: AssetId+BehaviorFamily — version-agnostic. ║
// ║                                                                  ║
// ║  2. BehaviorFamily is metadata only. Runtime behavior always     ║
// ║     comes from: Published Definition → Timeline → Visual DNA     ║
// ║     → LayerDefinitions. Never from hardcoded family branching.  ║
// ║                                                                  ║
// ║  3. Version upgrades never break ownership. Publishing v2 of     ║
// ║     any pack upgrades runtime behavior for all existing owners   ║
// ║     automatically. Ownership is tied to AssetId only, never to  ║
// ║     BehaviorVersion. No re-purchase. No inventory migration.     ║
// ║                                                                  ║
// ║  4. Fallback is always available. If the definition is missing,  ║
// ║     deleted, JSON-corrupted, or cache-empty, the resolver falls  ║
// ║     through: Published → HardcodedDefault → SafeFallback.        ║
// ║     Runtime never crashes. Diagnostics show the fallback source. ║
// ║                                                                  ║
// ║  5. Preview = Published = Runtime. All four contexts             ║
// ║     (Developer, Store, Inventory, Runtime) resolve through       ║
// ║     EffectDefinitionRuntimeResolver → same Brain → same          ║
// ║     Renderer. Zero separate preview renderers.                   ║
// ║                                                                  ║
// ║  BehaviorVersion contract:                                       ║
// ║    Publishing v2 does NOT revoke v1 ownership.                   ║
// ║    Ownership is always tied to AssetId, never to version.        ║
// ║    Runtime always reads the highest compatible Published version. ║
// ╚══════════════════════════════════════════════════════════════════╝

// ══════════════════════════════════════════════════════════════════
// EffectTargetScope  — who owns / activates this effect.
// ══════════════════════════════════════════════════════════════════
public enum EffectTargetScope
{
    Team,    // effect belongs to a team (e.g. Team Emblem effects)
    Player   // effect belongs to a player (e.g. Avatar Frame effects)
}

// ══════════════════════════════════════════════════════════════════
// EffectTargetVisualType  — what visual element the effect decorates.
// Model is NOT limited to emblems; same definition record is reused
// for Avatar, Frame, Background, and future visual types.
// ══════════════════════════════════════════════════════════════════
public enum EffectTargetVisualType
{
    Emblem,             // Team emblem image
    Avatar,             // Player avatar circle
    RankFrame,          // Rank/profile frame border
    ProfileBackground,  // Full profile background
    Title,              // Text title badge (future)
    Badge               // Achievement badge (future)
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorFamily  — the procedural behavior family.
// Maps to IEmblemBehaviorRenderer resolver key.
// ══════════════════════════════════════════════════════════════════
public enum EffectBehaviorFamily
{
    None,
    FireBreath,     // Dragon
    Roar,           // Lion
    WingPulse,      // Eagle / Falcon
    FrostBreath,    // Wolf
    RoyalSparkle,   // Crown
    ShieldReflect,  // Shield
    Generic         // Fallback / future emblem types
}

// ══════════════════════════════════════════════════════════════════
// EffectPreviewContextType  — identifies the rendering context.
// Used by LivingTeamEmblemView to decide whether ownership applies
// and whether developer intensity override is active.
// ══════════════════════════════════════════════════════════════════
public enum EffectPreviewContextType
{
    Runtime,       // Normal gameplay — full ownership gate enforced
    Store,         // Store product card preview — ownership bypassed
    Inventory,     // My items card preview — ownership bypassed
    Developer,     // Developer Studio WYSIWYG — ownership bypassed, intensity override allowed
    PhotoMode      // Photo Mode capture — ownership enforced, max fps
}

// ══════════════════════════════════════════════════════════════════
// EffectDefinitionSource  — describes WHERE a definition came from.
//
// CONSTITUTIONAL RULES:
//   • This enum NEVER grants or denies permissions.
//   • It is set by EffectDefinitionRuntimeResolver and stored for
//     diagnostics only. No gate, renderer, or brain reads it.
//   • Never compare using .ToString(). Always compare enum to enum.
//   • Store internally as enum. Convert to localized text only in
//     Developer Diagnostics UI (future phase).
//   • Numeric values are frozen. New values MUST be appended at the
//     end only, to avoid JSON serialization breaks.
// ══════════════════════════════════════════════════════════════════
public enum EffectDefinitionSource
{
    Published         = 0, // Live published definition from EffectDefinitionCache
    Draft             = 1, // Unpublished draft (Developer Studio internal use only)
    DeveloperPreview  = 2, // Developer Studio explicit-definition preview
    StorePreview      = 3, // Store card preview (ownership bypassed)
    InventoryPreview  = 4, // Inventory card preview (ownership bypassed)
    HardcodedDefault  = 5, // EmblemBehaviorProfile fallback (no published definition)
    SafeFallback      = 6, // Last-resort Shield generic (never null)
    // APPEND-ONLY: new values must go here, never reorder above.
}

// ══════════════════════════════════════════════════════════════════
// EffectOwnershipState  — describes WHY Runtime accepted or rejected
// activation. Returned exclusively by EffectOwnershipResolver.
//
// CONSTITUTIONAL RULES:
//   • This enum NEVER decides which definition is loaded.
//   • It is consumed only by LivingEmblemOwnershipGate to decide
//     whether to call the renderer at all.
//   • Renderer, Brain, and EffectDefinitionRuntimeResolver NEVER
//     read this value.
//   • Numeric values are frozen. New values MUST be appended at the
//     end only.
//
// OFFICIAL ARCHITECTURE CHAIN:
//   EffectOwnershipResolver → returns EffectOwnershipState
//   ↓
//   EffectDefinitionRuntimeResolver → returns EffectDefinitionSource
//   ↓
//   LivingTeamEmblemView → Brain → Renderer
//
//   Ownership and Definition Resolution are completely independent.
// ══════════════════════════════════════════════════════════════════
public enum EffectOwnershipState
{
    Allowed                     = 0, // All ownership checks passed
    DeniedNotEquipped           = 1, // Team has no EquippedTeamEffectAssetId
    DeniedNotOwned              = 2, // No player owns the required asset
    DeniedTeamMembership        = 3, // Player is not a member of the team
    DeniedNoPublishedDefinition = 4, // RequirePublishedDefinition set but none found
    PreviewBypassed             = 5, // Preview context — gate skipped intentionally
    // ── Future extensibility (Avatar / Frame / Background / Badge / Title) ──
    DeniedWrongAsset            = 6, // Equipped asset does not match required AssetId
    DeniedWrongScope            = 7, // TargetScope mismatch (e.g. Team def on Player slot)
    DeniedUnsupportedTarget     = 8, // TargetVisualType not supported for this context
    DeniedBehaviorDisabled      = 9, // Definition is Hidden/Archived by Studio
    DeniedVersionMismatch       = 10,// Definition.MinimumEngineVersion > current engine
    // APPEND-ONLY: new values must go here, never reorder above.
}

// ══════════════════════════════════════════════════════════════════
// EffectLayerTriggerType  — what event a timeline step triggers
// on its associated layer. Consumed by future LayerOrchestrator.
// ══════════════════════════════════════════════════════════════════
public enum EffectLayerTriggerType
{
    None,
    FadeIn,
    FadeOut,
    PulseOnce,
    IntensityRamp,
    IntensityDrop,
    EnableLayer,
    DisableLayer
}

// ══════════════════════════════════════════════════════════════════
// EffectEasingType  — easing curve applied to a timeline step's
// intensity multiplier transition. Consumed by future EasingEngine.
// ══════════════════════════════════════════════════════════════════
public enum EffectEasingType
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Bounce,
    Spring
}

// ══════════════════════════════════════════════════════════════════
// EmblemCanonicalAssetIds  — single source of truth for the six
// canonical TeamEffect asset IDs. Used by GetDefaultForEmblemType
// to set AssetId from EmblemType without using profile.BehaviorId.
// ══════════════════════════════════════════════════════════════════
public static class EmblemCanonicalAssetIds
{
    public const string Dragon = "team-emblem-dragon-3d";
    public const string Lion   = "team-emblem-lion-3d";
    public const string Eagle  = "team-emblem-eagle-3d";
    public const string Wolf   = "team-emblem-wolf-3d";
    public const string Crown  = "team-emblem-crown-3d";
    public const string Shield = "team-emblem-shield-3d";

    public static string FromEmblemType(EmblemType type) => type switch
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
// BehaviorDefinitionIdFactory  — low-level deterministic ID builder.
// No Guid. No random. Produces human-readable SCREAMING_SNAKE_CASE.
//
// Format:  BEHAVIOR_{SCOPE}_{VISUAL}_{FAMILY}_{ASSET_SLUG}[_V{n}]
// Examples:
//   BEHAVIOR_TEAM_EMBLEM_FIRE_BREATH_TEAM_EMBLEM_DRAGON_3D
//   BEHAVIOR_TEAM_EMBLEM_ROAR_TEAM_EMBLEM_LION_3D
//   BEHAVIOR_TEAM_EMBLEM_ROYAL_SPARKLE_TEAM_EMBLEM_CROWN_3D_V2
//
// Use EffectBehaviorIdentityService.CreateId() rather than calling
// this factory directly — it handles collision suffixes.
// ══════════════════════════════════════════════════════════════════
public static class BehaviorDefinitionIdFactory
{
    // Full canonical form including scope + visual type.
    public static string Create(
        EffectTargetScope      scope,
        EffectTargetVisualType visual,
        EffectBehaviorFamily   family,
        string                 assetId,
        int                    version = 1)
    {
        var scopeSlug   = EnumToSlug(scope.ToString());
        var visualSlug  = EnumToSlug(visual.ToString());
        var familySlug  = EnumToSlug(family.ToString());
        var assetSlug   = AssetToSlug(assetId);
        var ver         = version > 1 ? $"_V{version}" : string.Empty;
        return $"BEHAVIOR_{scopeSlug}_{visualSlug}_{familySlug}_{assetSlug}{ver}";
    }

    // Short form — scope/visual inferred as Team/Emblem (most common).
    public static string CreateForEmblem(
        EffectBehaviorFamily family,
        string               assetId,
        int                  version = 1) =>
        Create(EffectTargetScope.Team, EffectTargetVisualType.Emblem,
               family, assetId, version);

    // ── Slug helpers ──────────────────────────────────────────────
    // CamelCase → SCREAMING_SNAKE_CASE  (FireBreath → FIRE_BREATH)
    // Inserts _ before each uppercase letter that follows a lowercase one.
    internal static string EnumToSlug(string value)
    {
        if (string.IsNullOrEmpty(value)) return "UNKNOWN";
        var sb = new System.Text.StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(value[i - 1]))
                sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    // kebab-case / space → SCREAMING_SNAKE_CASE  (team-emblem-dragon-3d → TEAM_EMBLEM_DRAGON_3D)
    internal static string AssetToSlug(string? assetId) =>
        string.IsNullOrWhiteSpace(assetId)
            ? "UNKNOWN"
            : assetId.Trim()
                     .ToUpperInvariant()
                     .Replace('-', '_')
                     .Replace(' ', '_');
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorIdentityService  — the ONLY entry point for creating
// or verifying BehaviorDefinitionIds at authoring time.
//
// Rules:
//   • No Guid. No random.
//   • IDs are deterministic from scope + visual + family + assetId + version.
//   • If a collision exists in the provided set, a numeric suffix is appended
//     (_2, _3 …) — still human-readable, still no Guid.
//   • Developer never types an ID manually.
// ══════════════════════════════════════════════════════════════════
public static class EffectBehaviorIdentityService
{
    // Generate a unique ID for a new definition.
    // existingIds: IDs already in use in the current store (pass empty if first).
    public static string CreateId(
        EffectTargetScope      scope,
        EffectTargetVisualType visual,
        EffectBehaviorFamily   family,
        string                 assetId,
        int                    version,
        IEnumerable<string>?   existingIds = null)
    {
        var baseId = BehaviorDefinitionIdFactory.Create(scope, visual, family, assetId, version);
        if (existingIds == null) return baseId;

        var set = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(baseId)) return baseId;

        // Collision: append _2, _3, … until unique.
        for (int suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = $"{baseId}_{suffix}";
            if (!set.Contains(candidate)) return candidate;
        }
        // Unreachable in practice — 998 identical definitions would be a data error.
        throw new InvalidOperationException(
            $"Cannot generate unique ID for '{baseId}': too many collisions.");
    }

    // Convenience overload for the common Team/Emblem case.
    public static string CreateEmblemId(
        EffectBehaviorFamily family,
        string               assetId,
        int                  version,
        IEnumerable<string>? existingIds = null) =>
        CreateId(EffectTargetScope.Team, EffectTargetVisualType.Emblem,
                 family, assetId, version, existingIds);

    // Verify that an existing BehaviorDefinitionId is canonical (not a Guid).
    // Returns false if the value looks like a Guid or is empty.
    public static bool IsCanonical(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (id.StartsWith("BEHAVIOR_", StringComparison.Ordinal)) return true;
        return false;
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorDefinitionModel  — the published data-driven record.
// Stored as JSON in AppData. Loaded by Runtime, Studio, Preview.
// Inherits StoreCmsRecordBase for Status/CreatedAt/UpdatedAt/PublishedAt.
//
// This model is NOT emblem-specific. TargetScope + TargetVisualType
// determine what it decorates: Team Emblem, Player Avatar, Frame, etc.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectBehaviorDefinitionModel : StoreCmsRecordBase
{
    // ── Identity ──────────────────────────────────────────────────
    // Human-readable canonical ID. Generated by EffectBehaviorIdentityService.
    // Never a Guid. Never random. Deterministic from scope+visual+family+assetId+version.
    // Example: BEHAVIOR_TEAM_EMBLEM_FIRE_BREATH_TEAM_EMBLEM_DRAGON_3D
    // Use EffectBehaviorIdentityService.IsCanonical() to verify at runtime.
    public string BehaviorDefinitionId { get; set; } = string.Empty;

    // BehaviorVersion increments on each Publish.
    // Ownership is always on AssetId — version changes never revoke access.
    // Runtime always reads the latest Published definition for the AssetId.
    public int BehaviorVersion { get; set; } = 1;

    // Store asset identifier, e.g. "team-emblem-dragon-3d".
    public string AssetId { get; set; } = string.Empty;

    // Strong-typed asset type.
    public EffectTargetScope      TargetScope      { get; set; } = EffectTargetScope.Team;
    public EffectTargetVisualType TargetVisualType { get; set; } = EffectTargetVisualType.Emblem;

    // Procedural behavior family driving the renderer selection.
    public EffectBehaviorFamily BehaviorFamily { get; set; } = EffectBehaviorFamily.Generic;

    // Human-readable display name shown in Developer Studio.
    public string DisplayName { get; set; } = string.Empty;

    // Minimum engine version required to run this definition.
    // Runtime checks EffectDefinitionValidator.CurrentEngineVersion >= this value.
    // If not met, falls back silently to the hardcoded profile.
    // 1 = Phase 2.5-C/D baseline.
    public int MinimumEngineVersion { get; set; } = 1;

    // Maximum engine version this definition supports.
    // 0 = no upper bound (default — always compatible going forward).
    // RuntimeResolver skips this definition if engine > MaximumEngineVersion > 0.
    public int MaximumEngineVersion { get; set; } = 0;

    // If true, this definition is deprecated and should not be activated for
    // new users. Existing users keep it; RuntimeResolver logs a warning.
    public bool Deprecated { get; set; }

    // Experimental definitions are visible only in Developer Preview.
    // Runtime and Store/Inventory contexts skip them entirely.
    public bool Experimental { get; set; }

    // Disabled definitions are excluded from ALL contexts including Studio.
    // Equivalent to soft-delete without removing from storage.
    public bool Disabled { get; set; }

    // Convenience: IsPublished derived from StoreCmsRecordBase.Status.
    public bool IsPublished => Status == StoreCmsStatus.Published;

    // ── Visual DNA  (all editable by Developer Studio) ────────────
    public EffectVisualDnaRecord VisualDna { get; set; } = new();

    // ── Timeline steps  (editable by Developer Studio) ────────────
    public List<EffectTimelineStepRecord> TimelineSteps { get; set; } = new();

    // ── Layer definitions (ordered list, one item per layer) ───────
    public List<EffectLayerDefinitionItemRecord> LayerDefinitions { get; set; } = new();

    // ── Behavior capabilities (Studio budget warnings only) ────────
    // Runtime and Ownership never read this record.
    public EffectBehaviorCapabilitiesRecord Capabilities { get; set; } = new();

    // ── Performance profile ────────────────────────────────────────
    public EffectPerformanceProfileRecord PerformanceProfile { get; set; } = new();

    // ── Preview context ────────────────────────────────────────────
    public EffectPreviewContextRecord PreviewContext { get; set; } = new();

    // ── Ownership policy ──────────────────────────────────────────
    // Ownership is always tied to AssetId. BehaviorVersion and
    // BehaviorDefinitionId NEVER revoke or gate ownership.
    // Publishing v2/v3/v5 does NOT require re-purchase or re-equip.
    public EffectOwnershipPolicyRecord OwnershipPolicy { get; set; } = new();
}

// ══════════════════════════════════════════════════════════════════
// EffectVisualDnaRecord  — all visual DNA parameters.
//
// Extensibility: named fields cover all current parameters.
// NumericParameters / TextParameters / BooleanParameters carry
// future or emblem-specific values without breaking existing JSON.
// Partial JSON is always forward-compatible (new fields default to 0/null/false).
// ══════════════════════════════════════════════════════════════════
public sealed class EffectVisualDnaRecord
{
    // Glow / aura
    public float GlowStrength       { get; set; } = 1.00f;
    public float GlowAlpha          { get; set; } = 0.28f;
    public float AuraRadius         { get; set; } = 0.14f;
    public float PulseAmplitude     { get; set; } = 0.12f;
    public float PulseSpeed         { get; set; } = 0.70f;
    public float PulseCurve         { get; set; } = 0.60f;
    public float BrainSpeed         { get; set; } = 1.00f;
    // Smoke
    public float SmokeDensity       { get; set; } = 0.0f;
    public float SmokeSpeed         { get; set; } = 0.0f;
    // Eye
    public float EyeGlow            { get; set; } = 0.0f;
    public float EyeBlinkInterval   { get; set; } = 3.0f;
    // Reflection / heat
    public float ReflectionStrength { get; set; } = 0.0f;
    public float HeatStrength       { get; set; } = 0.0f;
    public float HeatRadius         { get; set; } = 0.0f;
    // Particles / sparkle
    public float SparkCount         { get; set; } = 0.0f;
    public float SparkLifetime      { get; set; } = 0.0f;
    // Secondary ring
    public float PulseStrength      { get; set; } = 0.40f;
    // Wing / head
    public float WingAmplitude      { get; set; } = 0.0f;
    public float HeadRotation       { get; set; } = 0.0f;
    // Colors: ARGB hex strings; null = use palette default. e.g. "#FFFF6B00"
    public string? PrimaryColor     { get; set; }
    public string? SecondaryColor   { get; set; }
    public string? AccentColor1     { get; set; }
    public string? AccentColor2     { get; set; }
    public string? EyeGlowColor     { get; set; }

    // ── Extensibility bags ─────────────────────────────────────────
    // Future or emblem-specific float values (e.g. "TailWhipAmplitude").
    // Stored alongside named fields without breaking JSON compatibility.
    public Dictionary<string, float>  NumericParameters  { get; set; } = new();
    // Future string values (e.g. "SecondaryBehaviorId").
    public Dictionary<string, string> TextParameters     { get; set; } = new();
    // Future boolean flags (e.g. "EnableEyeTracking").
    public Dictionary<string, bool>   BooleanParameters  { get; set; } = new();
}

// ══════════════════════════════════════════════════════════════════
// EffectTimelineStepRecord  — one authored timeline step.
//
// State stored as string for JSON forward-compat — new EmblemBehaviorState
// values never break old saved definitions (unmapped names fall back to Idle).
//
// TriggerType / TriggerCondition / Easing consumed by future LayerOrchestrator
// and EasingEngine. IsFutureOnly marks steps defined but not yet rendered.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectTimelineStepRecord
{
    // EmblemBehaviorState name — string for JSON forward-compat.
    public string               State               { get; set; } = "Idle";
    public float                DurationSeconds     { get; set; } = 2.0f;
    public float                IntensityMultiplier { get; set; } = 1.0f;

    // Layer event fired when this step begins.
    public EffectLayerTriggerType LayerTrigger      { get; set; } = EffectLayerTriggerType.None;
    // Target layer for the trigger (null = all layers).
    public VisualLayerType?     TriggerTargetLayer  { get; set; }

    // Easing curve for this step's intensity transition.
    public EffectEasingType     Easing              { get; set; } = EffectEasingType.Linear;

    // Random startup delay range — desynchronises simultaneous instances.
    public float RandomDelayMinSeconds { get; set; } = 0.0f;
    public float RandomDelayMaxSeconds { get; set; } = 0.0f;

    // How many times to repeat before advancing. 0 = play once.
    public int   RepeatCount           { get; set; } = 0;
    // Cooldown before this step can trigger again (for repeat > 0).
    public float CooldownSeconds       { get; set; } = 0.0f;

    // If true, this step is skipped on VeryLite/Lite devices.
    public bool  CanSkipOnLowEnd       { get; set; }

    // Step is defined in the timeline but the renderer has not yet
    // implemented it. Brain advances through it silently.
    public bool  IsFutureOnly          { get; set; }
}

// ══════════════════════════════════════════════════════════════════
// EffectLayerDefinitionItemRecord  — full configuration for one layer.
// Replaces the old boolean-flag record; list ordered by Order field.
//
// All 9 VisualLayerType values are represented. Studio can enable/
// disable individual layers and set blend mode, opacity, intensity,
// and rendering order without touching the renderer code.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectLayerDefinitionItemRecord
{
    // Strongly-typed layer identifier — maps to VisualLayerType.
    public VisualLayerType    LayerType           { get; set; }
    public bool               IsEnabled           { get; set; }
    // Rendering order within the effect stack. Lower = drawn first.
    public int                Order               { get; set; }
    // Blend mode applied when compositing this layer.
    public VisualBlendMode    BlendMode           { get; set; } = VisualBlendMode.Normal;
    // Base opacity of this layer, independent of DNA intensity.
    public float              Opacity             { get; set; } = 1.0f;
    // Multiplies DNA intensity values for this layer specifically.
    public float              IntensityMultiplier { get; set; } = 1.0f;
    // Not yet rendered by any IEmblemBehaviorRenderer implementation.
    public bool               IsFutureOnly        { get; set; }
}

// ══════════════════════════════════════════════════════════════════
// EffectLayerDefinitionFactory  — builds the default layer list for
// a new definition so the developer starts from a correct baseline.
// ══════════════════════════════════════════════════════════════════
public static class EffectLayerDefinitionFactory
{
    public static List<EffectLayerDefinitionItemRecord> CreateDefault() =>
    [
        new() { LayerType = VisualLayerType.Shadow,         IsEnabled = false, Order = 0, IsFutureOnly = true  },
        new() { LayerType = VisualLayerType.BackgroundAura, IsEnabled = false, Order = 1, IsFutureOnly = true  },
        new() { LayerType = VisualLayerType.AmbientSmoke,   IsEnabled = true,  Order = 2, BlendMode = VisualBlendMode.Additive },
        new() { LayerType = VisualLayerType.BaseImage,      IsEnabled = true,  Order = 3  },
        new() { LayerType = VisualLayerType.HeatDistortion, IsEnabled = false, Order = 4, IsFutureOnly = true  },
        new() { LayerType = VisualLayerType.Fire,           IsEnabled = false, Order = 5, IsFutureOnly = true  },
        new() { LayerType = VisualLayerType.Particles,      IsEnabled = false, Order = 6, IsFutureOnly = true  },
        new() { LayerType = VisualLayerType.Glow,           IsEnabled = true,  Order = 7, BlendMode = VisualBlendMode.Additive },
        new() { LayerType = VisualLayerType.UIOverlay,      IsEnabled = true,  Order = 8  },
    ];
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorCapabilitiesRecord  — which channels this definition
// actively uses. Informational only — Studio uses it for budget
// warnings. Runtime/Ownership never reads this.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectBehaviorCapabilitiesRecord
{
    public bool SupportsSmoke         { get; set; }
    public bool SupportsFire          { get; set; }
    public bool SupportsHeat          { get; set; }
    public bool SupportsReflection    { get; set; }
    public bool SupportsSparkles      { get; set; }
    public bool SupportsParticles     { get; set; }
    public bool SupportsEyeTracking   { get; set; }
    public bool SupportsHeadMovement  { get; set; }
    public bool SupportsWingMovement  { get; set; }
    // Audio and Haptics not yet implemented in any renderer — future only.
    public bool SupportsAudio         { get; set; }
    public bool SupportsHaptics       { get; set; }
}

// ══════════════════════════════════════════════════════════════════
// EffectPerformanceProfileRecord  — per-definition FPS + budget.
// TargetFps 0 = use DeviceProfiler default.
// Estimated* fields are authoring hints for Developer Studio only;
// Runtime never reads them to make decisions.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectPerformanceProfileRecord
{
    // 0 = use DeviceProfiler default (EmblemPerformanceSettings)
    public int  TargetFps        { get; set; }
    // 0 = no limit (future use for particle cap)
    public int  MaxParticleCount { get; set; }
    // Suppress entire effect on VeryLite/Lite devices if true.
    public bool DisableOnLowEnd  { get; set; }

    // ── Performance budget estimates (Studio authoring hints only) ──
    // Normalised 0–1. Studio shows warning when > 0.7.
    // Runtime never reads these values.
    public float EstimatedCost              { get; set; }
    public float EstimatedGpuCost          { get; set; }
    public float EstimatedCpuCost          { get; set; }
    // Minimum DeviceProfile required for full quality.
    // Studio shows downgrade warning if below this tier.
    public DeviceProfile RecommendedDeviceProfile { get; set; } = DeviceProfile.Medium;
}

// ══════════════════════════════════════════════════════════════════
// EffectPreviewContextRecord  — Developer Studio preview settings.
//
// ContextType identifies the rendering context so LivingTeamEmblemView
// can decide ownership enforcement and fps override.
//
// DeveloperIntensityMultiplier: Studio shows at > 1.0 to make subtle
// effects obvious during authoring. Runtime always uses 1.0.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectPreviewContextRecord
{
    // Explicit context type — replaces free-form PreviewLabel for logic.
    public EffectPreviewContextType ContextType { get; set; } = EffectPreviewContextType.Runtime;

    // Applied to GlowAlpha/SmokeDensity/EyeGlow in Developer context only.
    // Runtime always ignores this — receives exactly the authored DNA values.
    public float DeveloperIntensityMultiplier { get; set; } = 1.0f;

    // If true, Studio preview ignores DeviceProfiler and uses Ultra fps.
    public bool  UseMaxFpsInPreview           { get; set; }

    // Friendly display label in Studio UI — not consumed by runtime.
    public string PreviewLabel                { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════════
// EffectOwnershipPolicyRecord  — runtime ownership rules.
//
// All conditions must be true for runtime activation.
// Preview contexts (Store/Inventory/Developer) bypass ownership.
//
// Known Gap (Phase 2.5-E):
//   RequiredEquippedAssetId is empty by default — gate currently
//   accepts any equipped TeamEffect. Binding to be added in 2.5-E.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectOwnershipPolicyRecord
{
    // The specific AssetId that must be equipped.
    // Empty = accept any TeamEffect of the correct type (gap: Phase 2.5-E).
    public string RequiredEquippedAssetId   { get; set; } = string.Empty;

    // The AssetType of the required equipped item.
    // Resolved against StoreProductAssetType at runtime.
    public string RequiredAssetType         { get; set; } = "TeamEffect";

    // If non-empty, runtime checks for a Published definition with this ID.
    // Empty = fallback definition is always accepted.
    public string RequirePublishedDefinitionId { get; set; } = string.Empty;

    // If true, fallback (hardcoded) definition activates even without a
    // Published JSON definition. Disable to enforce Studio-published-only.
    public bool   AllowFallbackDefinition   { get; set; } = true;

    // Player must be a member of the team (TeamId association).
    // TeamId ownership alone does NOT activate behavior.
    public bool   RequireTeamMembership     { get; set; } = true;

    // Player must own the AssetId in their personal inventory.
    public bool   RequirePlayerOwnership    { get; set; } = true;

    // Store / Developer / Inventory preview contexts bypass all checks.
    // This field is documentation only — never toggled by Studio.
    public bool   PreviewBypassesOwnership  { get; set; } = true;
}

// ══════════════════════════════════════════════════════════════════
// EffectDefinitionValidator  — validates a definition before publish.
//
// Returns a list of error strings; empty list = valid.
// Called by EffectBehaviorDefinitionService.ValidateBeforePublish.
// ══════════════════════════════════════════════════════════════════
public static class EffectDefinitionValidator
{
    // Current engine version. Definition.MinimumEngineVersion must be <= this.
    public const int CurrentEngineVersion = 1;

    // Known-supported BehaviorFamily values (Generic is always supported as fallback).
    private static readonly HashSet<EffectBehaviorFamily> _supportedFamilies =
    [
        EffectBehaviorFamily.FireBreath,
        EffectBehaviorFamily.Roar,
        EffectBehaviorFamily.WingPulse,
        EffectBehaviorFamily.FrostBreath,
        EffectBehaviorFamily.RoyalSparkle,
        EffectBehaviorFamily.ShieldReflect,
        EffectBehaviorFamily.Generic,
    ];

    public static List<string> Validate(EffectBehaviorDefinitionModel d)
    {
        var errors = new List<string>();

        // ── Identity ───────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(d.BehaviorDefinitionId))
            errors.Add("BehaviorDefinitionId is required.");
        else if (!EffectBehaviorIdentityService.IsCanonical(d.BehaviorDefinitionId))
            errors.Add($"BehaviorDefinitionId '{d.BehaviorDefinitionId}' is not canonical (must start with BEHAVIOR_).");
        if (string.IsNullOrWhiteSpace(d.AssetId))
            errors.Add("AssetId is required.");
        if (d.BehaviorVersion < 1)
            errors.Add("BehaviorVersion must be >= 1.");
        if (d.MinimumEngineVersion > CurrentEngineVersion)
            errors.Add($"MinimumEngineVersion {d.MinimumEngineVersion} exceeds current engine {CurrentEngineVersion}.");
        if (d.MaximumEngineVersion > 0 && d.MaximumEngineVersion < d.MinimumEngineVersion)
            errors.Add($"MaximumEngineVersion {d.MaximumEngineVersion} is less than MinimumEngineVersion {d.MinimumEngineVersion}.");
        if (d.Disabled)
            errors.Add("Definition is Disabled and cannot be published.");

        // ── BehaviorFamily ───────────────────────────────────
        if (!_supportedFamilies.Contains(d.BehaviorFamily))
            errors.Add($"BehaviorFamily '{d.BehaviorFamily}' has no registered renderer.");

        // ── VisualDna ─────────────────────────────────────────
        if (d.VisualDna == null)
        {
            errors.Add("VisualDna must not be null.");
        }
        else
        {
            if (d.VisualDna.GlowAlpha    < 0f) errors.Add("VisualDna.GlowAlpha must be >= 0.");
            if (d.VisualDna.GlowAlpha    > 1f) errors.Add("VisualDna.GlowAlpha must be <= 1.");
            if (d.VisualDna.GlowStrength < 0f) errors.Add("VisualDna.GlowStrength must be >= 0.");
            if (d.VisualDna.SmokeDensity < 0f) errors.Add("VisualDna.SmokeDensity must be >= 0.");
            if (d.VisualDna.EyeGlow      < 0f) errors.Add("VisualDna.EyeGlow must be >= 0.");
            if (d.VisualDna.HeatStrength < 0f) errors.Add("VisualDna.HeatStrength must be >= 0.");
            if (d.VisualDna.PulseAmplitude < 0f) errors.Add("VisualDna.PulseAmplitude must be >= 0.");
            if (d.VisualDna.BrainSpeed   <= 0f) errors.Add("VisualDna.BrainSpeed must be > 0.");
            // Validate authored color strings
            ValidateColorField(errors, d.VisualDna.PrimaryColor,   "VisualDna.PrimaryColor");
            ValidateColorField(errors, d.VisualDna.SecondaryColor, "VisualDna.SecondaryColor");
            ValidateColorField(errors, d.VisualDna.AccentColor1,   "VisualDna.AccentColor1");
            ValidateColorField(errors, d.VisualDna.AccentColor2,   "VisualDna.AccentColor2");
            ValidateColorField(errors, d.VisualDna.EyeGlowColor,   "VisualDna.EyeGlowColor");
        }

        // ── TimelineSteps ─────────────────────────────────────
        if (d.TimelineSteps == null || d.TimelineSteps.Count == 0)
        {
            errors.Add("TimelineSteps must contain at least one step.");
        }
        else
        {
            foreach (var step in d.TimelineSteps)
            {
                if (step.DurationSeconds <= 0f)
                    errors.Add($"TimelineStep '{step.State}': DurationSeconds must be > 0.");
                // Warn about unknown state names — they fall back to Idle at runtime.
                if (!Enum.TryParse<EmblemBehaviorState>(step.State, ignoreCase: true, out _))
                    errors.Add($"TimelineStep state '{step.State}' is not a recognised EmblemBehaviorState.");
                if (step.RandomDelayMaxSeconds < step.RandomDelayMinSeconds)
                    errors.Add($"TimelineStep '{step.State}': RandomDelayMax must be >= RandomDelayMin.");
            }
        }

        // ── LayerDefinitions ─────────────────────────────────
        if (d.LayerDefinitions == null || d.LayerDefinitions.Count == 0)
        {
            errors.Add("LayerDefinitions must contain at least one layer.");
        }
        else
        {
            var orders = d.LayerDefinitions.Select(l => l.Order).ToList();
            if (orders.Count != orders.Distinct().Count())
                errors.Add("LayerDefinitions: two or more layers share the same Order.");
            foreach (var layer in d.LayerDefinitions)
            {
                if (layer.Opacity < 0f || layer.Opacity > 1f)
                    errors.Add($"Layer {layer.LayerType}: Opacity must be in [0, 1].");
                if (layer.IntensityMultiplier < 0f)
                    errors.Add($"Layer {layer.LayerType}: IntensityMultiplier must be >= 0.");
            }
        }

        // ── OwnershipPolicy ─────────────────────────────────
        if (d.OwnershipPolicy == null)
            errors.Add("OwnershipPolicy must not be null.");

        return errors;
    }

    private static void ValidateColorField(
        List<string> errors, string? hex, string fieldName)
    {
        if (string.IsNullOrEmpty(hex)) return; // null = use palette default, valid
        try { Color.FromArgb(hex); }
        catch { errors.Add($"{fieldName} value '{hex}' is not a valid ARGB hex color."); }
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectStillFrameRequest  — Preview Snapshot API contract.
//
// Store Cards, Inventory Cards, and Developer Studio pass this to
// LivingTeamEmblemView.PrepareStillFrame() for a frozen frame
// thumbnail without starting a Timer or Brain tick.
//
// Known Gap (Phase 2.5-F):
//   LivingTeamEmblemView.PrepareStillFrame is not yet implemented.
//   Callers can build this request now; output comes in Phase 2.5-F.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectStillFrameRequest
{
    // The definition to render a still from.
    public EffectBehaviorDefinitionModel Definition          { get; init; } = null!;
    // 0–1: position in the timeline to freeze (0 = start of first step).
    public float                         NormalizedTime      { get; init; } = 0f;
    // 1.0 = authored values; > 1.0 for Developer Studio preview boost.
    public float                         IntensityMultiplier { get; init; } = 1.0f;
    // Context decides ownership bypass and intensity override.
    public EffectPreviewContextType      ContextType         { get; init; } = EffectPreviewContextType.Runtime;
}

// ══════════════════════════════════════════════════════════════════
// Phase 2.5-D  —  Known Gaps (recorded for Phase 2.5-E and beyond)
//
// GAP-1: EquippedTeamEffectAssetId → BehaviorDefinition binding
//   EffectOwnershipPolicyRecord.RequiredEquippedAssetId is empty by
//   default. Gate accepts any equipped TeamEffect. Per-AssetId
//   strict enforcement scheduled for Phase 2.5-E.
//
// GAP-2: Full behavior visuals (FireBreath/Roar/WingPulse/FrostBreath)
//   Current renderers draw procedural foundation glow only. Full
//   per-state visuals (fire column, roar ring, wing spread, frost
//   cloud) are IsFutureOnly in TimelineSteps/LayerDefinitions. Phase 2.5-F.
//
// GAP-3: Developer Studio UI
//   EffectBehaviorDefinitionService Studio hooks are a complete API.
//   The visual Studio UI page is not yet built. Phase 2.5-G.
//
// GAP-4: Preview Snapshot (PrepareStillFrame)
//   EffectStillFrameRequest is defined. LivingTeamEmblemView does
//   not yet implement PrepareStillFrame. Phase 2.5-F.
//
// GAP-5: Audio / Haptics
//   EffectBehaviorCapabilitiesRecord declares SupportsAudio and
//   SupportsHaptics. No renderer implements them. Future phase.

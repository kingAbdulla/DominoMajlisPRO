using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ╔══════════════════════════════════════════════════════════════════╗
// ║  Phase 2.5-E  —  Official Effect Packs                          ║
// ║                                                                  ║
// ║  EffectBehaviorPackRegistry provides the six canonical           ║
// ║  EffectBehaviorDefinitionModel records — one per official emblem ║
// ║  pack. These are the published definitions that replace the      ║
// ║  hardcoded bootstrap fallback in Phase 2.5-D.                   ║
// ║                                                                  ║
// ║  Rules:                                                          ║
// ║  • Each pack is a pure Definition — no hardcoded renderer.       ║
// ║  • All rendering is driven by EffectBehaviorDefinitionModel.     ║
// ║  • Behavior reads only from EffectBehaviorDefinitionModel.       ║
// ║    Never from if(Dragon) / if(Lion) checks.                      ║
// ║  • Timeline steps are the authoritative authored sequence.       ║
// ║  • Published versions are immutable. New publish = new version.  ║
// ║  • Ownership is tied to AssetId only — version bumps never       ║
// ║    revoke access.                                                 ║
// ║                                                                  ║
// ║  Seeding:                                                        ║
// ║    EffectBehaviorPackRegistry.SeedAllAsync() writes pack         ║
// ║    definitions to AppData storage if not already present.        ║
// ║    Called once on first launch / app update. Idempotent.         ║
// ║                                                                  ║
// ║  Known Gap (Phase 2.5-E):                                        ║
// ║    GAP-13 — hardcoded bootstrap is removed once SeedAllAsync     ║
// ║    has run and all six packs are confirmed in storage.            ║
// ╚══════════════════════════════════════════════════════════════════╝

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorPackRegistry  — single source of truth for all
// official published behavior pack definitions.
//
// CONSTITUTIONAL RULE:
//   No renderer reads pack data directly.
//   No Brain reads pack data directly.
//   Only EffectDefinitionRuntimeResolver reads from cache/storage.
//   Pack definitions are authored data — identical in structure to
//   any definition a developer authors in Studio.
// ══════════════════════════════════════════════════════════════════
public static class EffectBehaviorPackRegistry
{
    // ── Developer Debug Preview Mode ─────────────────────────────
    // Developer sees 150% intensity and a max 120 FPS cap when this
    // is active. Runtime context always uses 1.0 intensity / device FPS.
    // User never sees this boost — it is applied only in Developer preview.
    public const float DevPreviewIntensityMultiplier = 1.50f;
    public const int   DevPreviewMaxFpsCap           = 120;

    // ── Official Pack definitions ─────────────────────────────────

    public static EffectBehaviorDefinitionModel DragonPack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Dragon,
            family:       EffectBehaviorFamily.FireBreath,
            displayName:  "Dragon — Fire Breath Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 1.12f,
                GlowAlpha           = 0.32f,
                AuraRadius          = 0.18f,
                PulseAmplitude      = 0.16f,
                PulseSpeed          = 0.72f,
                PulseCurve          = 0.80f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.55f,
                SmokeSpeed          = 0.40f,
                EyeGlow             = 0.78f,
                EyeBlinkInterval    = 4.0f,
                ReflectionStrength  = 0.0f,
                HeatStrength        = 0.62f,
                HeatRadius          = 0.22f,
                SparkCount          = 6f,
                SparkLifetime       = 0.9f,
                PulseStrength       = 0.65f,
                WingAmplitude       = 0.0f,
                HeadRotation        = 0.20f,
                PrimaryColor        = "#FFFF6B00",
                SecondaryColor      = "#FFFFD700",
                AccentColor1        = "#FF802200",
                AccentColor2        = "#FF552200",
                EyeGlowColor        = "#FFFFB830",
            },
            timeline: new[]
            {
                // Dragon Pack: Idle → EyeGlow → Smoke → MouthCharge → FireBreath(foundation) → Cooldown
                Step("Idle",          2.5f),
                Step("EyeMovement",   1.0f, isFutureOnly: false),   // Eye Glow phase
                Step("Breathing",     1.2f),                         // Smoke build-up
                Step("Preparing",     0.8f),                         // Mouth Charge
                Step("FireCharge",    1.0f),                         // Fire Breath Foundation
                Step("Cooldown",      1.5f),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsSmoke        = true,
                SupportsFire         = true,
                SupportsHeat         = true,
                SupportsEyeTracking  = true,
            });

    public static EffectBehaviorDefinitionModel LionPack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Lion,
            family:       EffectBehaviorFamily.Roar,
            displayName:  "Lion — Roar Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 1.08f,
                GlowAlpha           = 0.30f,
                AuraRadius          = 0.14f,
                PulseAmplitude      = 0.13f,
                PulseSpeed          = 0.65f,
                PulseCurve          = 0.70f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.0f,
                SmokeSpeed          = 0.0f,
                EyeGlow             = 0.0f,
                EyeBlinkInterval    = 3.5f,
                ReflectionStrength  = 0.0f,
                HeatStrength        = 0.0f,
                HeatRadius          = 0.0f,
                SparkCount          = 0f,
                SparkLifetime       = 0f,
                PulseStrength       = 1.00f,
                WingAmplitude       = 0.0f,
                HeadRotation        = 0.35f,
                PrimaryColor        = "#FFFFD700",
                SecondaryColor      = "#FFFFA500",
                AccentColor1        = "#FFFFD700",
                AccentColor2        = "#FFFFD700",
                EyeGlowColor        = "#FFFFD700",
            },
            timeline: new[]
            {
                // Lion Pack: Idle → Breathing → HeadMotion → EyeMotion → RoarCharge → Cooldown
                Step("Idle",         2.8f),
                Step("Breathing",    1.0f),
                Step("HeadMovement", 0.8f),
                Step("EyeMovement",  0.6f),
                Step("RoarCharge",   0.9f),
                Step("Recover",      1.2f, isLabel: "Cooldown"),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsHeadMovement = true,
            });

    public static EffectBehaviorDefinitionModel EaglePack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Eagle,
            family:       EffectBehaviorFamily.WingPulse,
            displayName:  "Eagle — Wing Pulse Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 1.04f,
                GlowAlpha           = 0.28f,
                AuraRadius          = 0.12f,
                PulseAmplitude      = 0.12f,
                PulseSpeed          = 0.82f,
                PulseCurve          = 0.60f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.0f,
                SmokeSpeed          = 0.0f,
                EyeGlow             = 0.0f,
                EyeBlinkInterval    = 3.0f,
                ReflectionStrength  = 0.0f,
                HeatStrength        = 0.0f,
                HeatRadius          = 0.0f,
                SparkCount          = 0f,
                SparkLifetime       = 0f,
                PulseStrength       = 0.50f,
                WingAmplitude       = 0.70f,
                HeadRotation        = 0.0f,
                PrimaryColor        = "#FF00BFFF",
                SecondaryColor      = "#FFFFFFFF",
                AccentColor1        = "#FFFFFFFF",
                AccentColor2        = "#FF80DFFF",
                EyeGlowColor        = "#FF80DFFF",
            },
            timeline: new[]
            {
                // Eagle Pack: Idle → EyeFlash → WingPulse → WingSpread → DiveCharge → Cooldown
                Step("Idle",          2.2f),
                Step("EyeGlint",      0.5f, isLabel: "EyeFlash"),
                Step("WingTwitch",    0.7f, isLabel: "WingPulse"),
                Step("FeatherMotion", 0.9f, isLabel: "WingSpread"),
                Step("WingPulse",     1.1f, isLabel: "DiveCharge"),
                Step("Recover",       1.0f, isLabel: "Cooldown"),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsWingMovement = true,
            });

    public static EffectBehaviorDefinitionModel WolfPack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Wolf,
            family:       EffectBehaviorFamily.FrostBreath,
            displayName:  "Wolf — Frost Breath Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 0.98f,
                GlowAlpha           = 0.26f,
                AuraRadius          = 0.16f,
                PulseAmplitude      = 0.12f,
                PulseSpeed          = 0.76f,
                PulseCurve          = 0.50f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.38f,
                SmokeSpeed          = 0.28f,
                EyeGlow             = 0.0f,
                EyeBlinkInterval    = 4.5f,
                ReflectionStrength  = 0.0f,
                HeatStrength        = 0.0f,
                HeatRadius          = 0.0f,
                SparkCount          = 0f,
                SparkLifetime       = 0f,
                PulseStrength       = 0.95f,
                WingAmplitude       = 0.0f,
                HeadRotation        = 0.25f,
                PrimaryColor        = "#FF9ECFFF",
                SecondaryColor      = "#FFE0F0FF",
                AccentColor1        = "#FFC8E8FF",
                AccentColor2        = "#FFDDEFFF",
                EyeGlowColor        = "#FFDDEFFF",
            },
            timeline: new[]
            {
                // Wolf Pack: Idle → FrostMist → IcePulse → HowlCharge → Cooldown
                Step("Idle",        2.6f),
                Step("ColdBreath",  0.8f, isLabel: "FrostMist"),
                Step("EarMovement", 0.6f, isLabel: "IcePulse"),
                Step("FrostWind",   1.2f, isLabel: "HowlCharge"),
                Step("Recover",     1.0f, isLabel: "Cooldown"),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsSmoke = true,
            });

    public static EffectBehaviorDefinitionModel ShieldPack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Shield,
            family:       EffectBehaviorFamily.ShieldReflect,
            displayName:  "Shield — Reflect Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 0.92f,
                GlowAlpha           = 0.22f,
                AuraRadius          = 0.13f,
                PulseAmplitude      = 0.10f,
                PulseSpeed          = 0.55f,
                PulseCurve          = 0.50f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.0f,
                SmokeSpeed          = 0.0f,
                EyeGlow             = 0.0f,
                EyeBlinkInterval    = 0.0f,
                ReflectionStrength  = 0.88f,
                HeatStrength        = 0.0f,
                HeatRadius          = 0.0f,
                SparkCount          = 0f,
                SparkLifetime       = 0f,
                PulseStrength       = 0.80f,
                WingAmplitude       = 0.0f,
                HeadRotation        = 0.0f,
                PrimaryColor        = "#FFC0C8D8",
                SecondaryColor      = "#FFE8F0FF",
                AccentColor1        = "#FFE0E8FF",
                AccentColor2        = "#FFD0D8F0",
                EyeGlowColor        = null,
            },
            timeline: new[]
            {
                // Shield Pack: Idle → ReflectionSweep → MetallicPulse → Cooldown
                Step("Idle",             3.0f),
                Step("ReflectionSweep",  1.0f),
                Step("MetallicPulse",    0.8f),
                Step("GuardianMode",     1.2f, isLabel: "Cooldown"),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsReflection = true,
            });

    public static EffectBehaviorDefinitionModel CrownPack =>
        BuildDefinition(
            assetId:      EmblemCanonicalAssetIds.Crown,
            family:       EffectBehaviorFamily.RoyalSparkle,
            displayName:  "Crown — Royal Sparkle Pack",
            dna: new EffectVisualDnaRecord
            {
                GlowStrength        = 1.10f,
                GlowAlpha           = 0.35f,
                AuraRadius          = 0.15f,
                PulseAmplitude      = 0.14f,
                PulseSpeed          = 0.60f,
                PulseCurve          = 0.75f,
                BrainSpeed          = 1.00f,
                SmokeDensity        = 0.0f,
                SmokeSpeed          = 0.0f,
                EyeGlow             = 0.0f,
                EyeBlinkInterval    = 0.0f,
                ReflectionStrength  = 0.0f,
                HeatStrength        = 0.0f,
                HeatRadius          = 0.0f,
                SparkCount          = 8f,
                SparkLifetime       = 1.2f,
                PulseStrength       = 1.10f,
                WingAmplitude       = 0.0f,
                HeadRotation        = 0.0f,
                PrimaryColor        = "#FFFFD700",
                SecondaryColor      = "#FFE8C060",
                AccentColor1        = "#FFFFE860",
                AccentColor2        = "#FFFFE860",
                EyeGlowColor        = null,
            },
            timeline: new[]
            {
                // Crown Pack: Idle → RoyalGlow → Sparkles → RoyalShine → Cooldown
                Step("Idle",      2.0f),
                Step("RoyalGlow", 0.8f),
                Step("GemPulse",  0.6f, isLabel: "Sparkles"),
                Step("SparkRain", 1.2f, isLabel: "RoyalShine"),
                Step("RoyalAura", 1.0f, isLabel: "Cooldown"),
            },
            capabilities: new EffectBehaviorCapabilitiesRecord
            {
                SupportsSparkles  = true,
                SupportsParticles = true,
            });

    // ── All packs ─────────────────────────────────────────────────
    public static IReadOnlyList<EffectBehaviorDefinitionModel> All =>
    [
        DragonPack, LionPack, EaglePack, WolfPack, ShieldPack, CrownPack,
    ];

    // ── Seeding ───────────────────────────────────────────────────
    // Writes all official pack definitions to AppData storage if not
    // already present. Idempotent — safe to call on every app launch.
    //
    // IMMUTABILITY CONTRACT:
    //   SeedAllAsync NEVER overwrites, replaces, regenerates, or resets
    //   the BehaviorVersion of any existing published definition.
    //
    //   Duplicate check key: AssetId + BehaviorFamily (version-agnostic).
    //   If ANY published record with the same AssetId + BehaviorFamily
    //   already exists in storage — regardless of version number — that
    //   pack is skipped entirely. This guarantees:
    //     - v1 seed never overwrites a developer-published v2/v3.
    //     - Future published content is never reset by startup seeding.
    //     - BehaviorVersion is only ever incremented by PublishAsync.
    //
    // Official packs are written directly as Published records (v1).
    // They do NOT go through SaveDraftAsync + PublishAsync (which would
    // double-increment BehaviorVersion). They are stored at v1 as-is.
    //
    // After all packs are written, EffectDefinitionCache is invalidated
    // so the next runtime read loads the freshly seeded definitions.
    public static async Task SeedAllAsync()
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();

        // Build lookup: AssetId|BehaviorFamily → already published (version-agnostic).
        // ANY published version of the same AssetId+Family blocks seeding.
        var existing = all
            .Where(d => d.IsPublished)
            .Select(d => $"{d.AssetId.ToLowerInvariant()}|{(int)d.BehaviorFamily}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var pack in All)
        {
            var key = $"{pack.AssetId.ToLowerInvariant()}|{(int)pack.BehaviorFamily}";
            if (existing.Contains(key))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EffectBehaviorPackRegistry] Already published, skipping: {pack.DisplayName}");
                continue;
            }

            // Write at BehaviorVersion=1, Status=Published, as-is.
            // NEVER call PublishAsync here — that would increment BehaviorVersion.
            all.Add(pack);
            existing.Add(key); // prevent double-add within the same seed run
            added = true;
            System.Diagnostics.Debug.WriteLine(
                $"[EffectBehaviorPackRegistry] Seeded: {pack.DisplayName} (v{pack.BehaviorVersion})");
        }

        if (!added) return;

        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);

        // Invalidate cache so next runtime read picks up the seeded definitions.
        EffectDefinitionCache.Invalidate();
    }

    // ── Builder ───────────────────────────────────────────────────

    private static EffectBehaviorDefinitionModel BuildDefinition(
        string                             assetId,
        EffectBehaviorFamily               family,
        string                             displayName,
        EffectVisualDnaRecord              dna,
        IReadOnlyList<EffectTimelineStepRecord> timeline,
        EffectBehaviorCapabilitiesRecord?  capabilities = null)
    {
        var definitionId = EffectBehaviorIdentityService.CreateEmblemId(family, assetId, version: 1);
        var layers       = EffectLayerDefinitionFactory.CreateDefault();

        // Enable AmbientSmoke if the DNA has smoke.
        var smokeLayer = layers.FirstOrDefault(l => l.LayerType == VisualLayerType.AmbientSmoke);
        if (smokeLayer != null) smokeLayer.IsEnabled = dna.SmokeDensity > 0f;

        return new EffectBehaviorDefinitionModel
        {
            BehaviorDefinitionId = definitionId,
            BehaviorVersion      = 1,
            AssetId              = assetId,
            TargetScope          = EffectTargetScope.Team,
            TargetVisualType     = EffectTargetVisualType.Emblem,
            BehaviorFamily       = family,
            DisplayName          = displayName,
            MinimumEngineVersion = 1,
            MaximumEngineVersion = 0,
            Deprecated           = false,
            Experimental         = false,
            Disabled             = false,
            Status               = StoreCmsStatus.Published,
            CreatedAt            = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt            = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
            VisualDna            = dna,
            TimelineSteps        = timeline.ToList(),
            LayerDefinitions     = layers,
            Capabilities         = capabilities ?? new EffectBehaviorCapabilitiesRecord(),
            PerformanceProfile   = new EffectPerformanceProfileRecord
            {
                TargetFps                = 0,         // device-default
                RecommendedDeviceProfile = DeviceProfile.Medium,
            },
            PreviewContext = new EffectPreviewContextRecord
            {
                ContextType                  = EffectPreviewContextType.Runtime,
                DeveloperIntensityMultiplier = DevPreviewIntensityMultiplier,
                UseMaxFpsInPreview           = false,
                PreviewLabel                 = string.Empty,
            },
            OwnershipPolicy = new EffectOwnershipPolicyRecord
            {
                RequiredEquippedAssetId    = assetId,   // GAP-1: bound per-pack now
                RequiredAssetType          = "TeamEffect",
                AllowFallbackDefinition    = false,     // packs are authoritative
                RequireTeamMembership      = true,
                RequirePlayerOwnership     = true,
                PreviewBypassesOwnership   = true,
            },
        };
    }

    private static EffectTimelineStepRecord Step(
        string state,
        float  duration,
        bool   isFutureOnly = false,
        string? isLabel     = null) =>
        new()
        {
            State           = state,
            DurationSeconds = duration,
            IsFutureOnly    = isFutureOnly,
        };
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorVersionHistory  — immutable publish record.
//
// IMMUTABILITY CONTRACT:
//   Published versions are never overwritten.
//   Each publish creates a new version entry.
//   Version history is append-only.
//   Rollback creates a new Draft copied from old data — old history unchanged.
//   Ownership is always tied to AssetId — version bumps never revoke access.
//   Runtime reads the latest compatible Published version via cache.
//
// This record is stored alongside the definition in JSON storage.
// VersionNumber mirrors BehaviorVersion at publish time.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectBehaviorVersionRecord
{
    // The BehaviorDefinitionId at this version.
    public string   BehaviorDefinitionId { get; init; } = string.Empty;
    // The AssetId this version belongs to. Ownership is anchored here.
    public string   AssetId              { get; init; } = string.Empty;
    // The immutable version number. Increments on each Publish.
    public int      VersionNumber        { get; init; } = 1;
    // UTC timestamp of publish.
    public DateTime PublishedAt          { get; init; }
    // Human note authored in Studio before publish (optional).
    public string   ChangeNote           { get; init; } = string.Empty;
    // Snapshot of the definition at this version. Stored as JSON in memory.
    // Deserialized on rollback demand only — not loaded every tick.
    public string   DefinitionSnapshot   { get; init; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════════
// EffectVersionHistoryService  — append-only version log.
//
// Responsibilities:
//   RecordPublish      — append a new version entry after PublishAsync.
//   GetHistory         — return all version entries for an AssetId.
//   GetVersionSnapshot — deserialize and return one historical version.
//
// Rollback is performed by:
//   1. GetVersionSnapshot(assetId, version) → old definition
//   2. EffectBehaviorDefinitionService.SaveDraftAsync(oldDef with new ID)
//   3. Never mutates any existing version entry.
// ══════════════════════════════════════════════════════════════════
public static class EffectVersionHistoryService
{
    private const string FileName = "effect_behavior_version_history.json";
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static string StoragePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    // Append a new version record. Called by Studio after PublishAsync succeeds.
    public static async Task RecordPublishAsync(
        EffectBehaviorDefinitionModel publishedDefinition,
        string changeNote = "")
    {
        await _gate.WaitAsync();
        try
        {
            var all = await LoadAllAsync();
            var snapshot = System.Text.Json.JsonSerializer.Serialize(publishedDefinition);
            all.Add(new EffectBehaviorVersionRecord
            {
                BehaviorDefinitionId = publishedDefinition.BehaviorDefinitionId,
                AssetId              = publishedDefinition.AssetId,
                VersionNumber        = publishedDefinition.BehaviorVersion,
                PublishedAt          = DateTime.UtcNow,
                ChangeNote           = changeNote,
                DefinitionSnapshot   = snapshot,
            });
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
        }
        finally { _gate.Release(); }
    }

    // Return all version records for a given AssetId, newest first.
    public static async Task<IReadOnlyList<EffectBehaviorVersionRecord>> GetHistoryAsync(
        string assetId)
    {
        var all = await LoadAllAsync();
        return all
            .Where(v => string.Equals(v.AssetId, assetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.VersionNumber)
            .ToList();
    }

    // Deserialize a specific historical version for rollback or inspection.
    // Returns null if the version record does not exist.
    public static async Task<EffectBehaviorDefinitionModel?> GetVersionSnapshotAsync(
        string assetId, int versionNumber)
    {
        var history = await GetHistoryAsync(assetId);
        var record  = history.FirstOrDefault(v => v.VersionNumber == versionNumber);
        if (record == null || string.IsNullOrEmpty(record.DefinitionSnapshot)) return null;
        return System.Text.Json.JsonSerializer.Deserialize<EffectBehaviorDefinitionModel>(
            record.DefinitionSnapshot);
    }

    private static Task<List<EffectBehaviorVersionRecord>> LoadAllAsync() =>
        StoreCmsJsonRepository.LoadListAsync<EffectBehaviorVersionRecord>(StoragePath);
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorStudioFoundation  — Behavior Studio API (Phase 2.5-E).
//
// CONSTITUTIONAL RULE:
//   Studio must NEVER render anything itself.
//   Every edit → updates EffectBehaviorDefinitionModel → passes to
//   LivingTeamEmblemView.SetDefinition() → Brain → Renderer.
//   No StudioRenderer. No PreviewRenderer. One pipeline.
//
// Preview Contexts (all use the same renderer):
//   Developer Preview  — 150% intensity, 120 FPS cap, ownership bypassed
//   Store Preview      — 100% intensity, ownership bypassed
//   Inventory Preview  — 100% intensity, ownership bypassed
//   Runtime            — 100% intensity, full ownership gate enforced
//
// OWNERSHIP GATE (enforced at runtime, bypassed in all preview contexts):
//   EquippedTeamEffectAssetId
//     → Published Definition (EffectDefinitionRuntimeResolver)
//     → EffectOwnershipResolver
//     → Brain → Renderer
//   No EmblemType-based activation at runtime. (GAP-1, Phase 2.5-E)
// ══════════════════════════════════════════════════════════════════
public static class EffectBehaviorStudioFoundation
{
    // ── Studio API ────────────────────────────────────────────────

    // Create a new draft definition from scratch for an AssetId/Family.
    public static EffectBehaviorDefinitionModel CreateDraft(
        string               assetId,
        EffectBehaviorFamily family,
        string               displayName)
    {
        var id = EffectBehaviorIdentityService.CreateEmblemId(family, assetId, version: 1);
        return new EffectBehaviorDefinitionModel
        {
            BehaviorDefinitionId = id,
            BehaviorVersion      = 1,
            AssetId              = assetId,
            TargetScope          = EffectTargetScope.Team,
            TargetVisualType     = EffectTargetVisualType.Emblem,
            BehaviorFamily       = family,
            DisplayName          = displayName,
            Status               = StoreCmsStatus.Draft,
            CreatedAt            = DateTime.UtcNow,
            UpdatedAt            = DateTime.UtcNow,
            VisualDna            = new EffectVisualDnaRecord(),
            TimelineSteps        = new List<EffectTimelineStepRecord>(),
            LayerDefinitions     = EffectLayerDefinitionFactory.CreateDefault(),
            PerformanceProfile   = new EffectPerformanceProfileRecord(),
            PreviewContext       = new EffectPreviewContextRecord(),
            OwnershipPolicy      = new EffectOwnershipPolicyRecord(),
        };
    }

    // Duplicate an existing definition as a new draft with a fresh canonical ID.
    public static EffectBehaviorDefinitionModel DuplicateDraft(
        EffectBehaviorDefinitionModel source,
        IEnumerable<string>? existingIds = null)
        => EffectBehaviorDefinitionService.DuplicateDefinition(source, existingIds);

    // Clone definition targeting a different AssetId (e.g. Dragon behaviour → Lion).
    public static EffectBehaviorDefinitionModel CloneBehavior(
        EffectBehaviorDefinitionModel source,
        string targetAssetId,
        IEnumerable<string>? existingIds = null)
        => EffectBehaviorDefinitionService.CloneBehavior(source, targetAssetId, existingIds);

    // Bump version for a new publish cycle — returns a draft, does NOT publish.
    public static EffectBehaviorDefinitionModel CloneVersion(
        EffectBehaviorDefinitionModel source,
        IEnumerable<string>? existingIds = null)
        => EffectBehaviorDefinitionService.CreateVersion(source, existingIds);

    // Save draft to AppData storage without publishing.
    public static Task SaveDraftAsync(EffectBehaviorDefinitionModel definition)
        => EffectBehaviorDefinitionService.SaveDraftAsync(definition);

    // Prepare an EffectDefinitionRuntimeResolver.ResolveRequest for developer preview.
    // Studio passes the explicit definition; resolver tags it as DeveloperPreview source.
    // LivingTeamEmblemView.SetDefinition() is called with the result — same pipeline as runtime.
    public static EffectDefinitionRuntimeResolver.ResolveRequest PreviewDraft(
        EffectBehaviorDefinitionModel draft)
        => new()
        {
            AssetId            = draft.AssetId,
            Context            = EffectPreviewContextType.Developer,
            ExplicitDefinition = draft,
        };

    // Publish draft: validate → increment version → save published record →
    // record version history → invalidate cache.
    // changeNote: optional human note stored in version history.
    public static async Task<EffectBehaviorDefinitionModel> PublishDraftAsync(
        EffectBehaviorDefinitionModel definition,
        string changeNote = "")
    {
        var published = await EffectBehaviorDefinitionService.PublishAsync(definition);
        await EffectVersionHistoryService.RecordPublishAsync(published, changeNote);
        return published;
    }

    // Rollback: find historical version → create a new draft from it.
    // NEVER mutates old history. Creates a new draft with a new canonical ID.
    public static async Task<EffectBehaviorDefinitionModel?> RollbackVersionAsync(
        string assetId,
        int    targetVersionNumber,
        IEnumerable<string>? existingIds = null)
    {
        var snapshot = await EffectVersionHistoryService.GetVersionSnapshotAsync(
            assetId, targetVersionNumber);
        if (snapshot == null) return null;
        return EffectBehaviorDefinitionService.DuplicateDefinition(snapshot, existingIds);
    }

    // Delete a draft (soft-delete: marks Disabled, removes from active set).
    // Does NOT delete Published records — published versions are immutable history.
    public static async Task DeleteDraftAsync(string definitionId)
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();
        var draft = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId, StringComparison.OrdinalIgnoreCase)
            && d.Status == StoreCmsStatus.Draft);
        if (draft == null) return;
        draft.Disabled   = true;
        draft.UpdatedAt  = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);
    }

    // ── Developer Preview Controls API ────────────────────────────
    // Studio calls these after each slider/picker change.
    // Each returns the mutated definition — call LivingTeamEmblemView.SetDefinition()
    // with the result and the developer intensity multiplier.
    // NO renderer is instantiated here. Studio delegates to LivingTeamEmblemView.

    public static EffectBehaviorDefinitionModel UpdateGlow(
        EffectBehaviorDefinitionModel d, float glowStrength, float glowAlpha, float auraRadius)
    {
        d.VisualDna.GlowStrength = glowStrength;
        d.VisualDna.GlowAlpha    = glowAlpha;
        d.VisualDna.AuraRadius   = auraRadius;
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    public static EffectBehaviorDefinitionModel UpdateSmoke(
        EffectBehaviorDefinitionModel d, float smokeDensity, float smokeSpeed)
    {
        d.VisualDna.SmokeDensity = smokeDensity;
        d.VisualDna.SmokeSpeed   = smokeSpeed;
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    public static EffectBehaviorDefinitionModel UpdateHeat(
        EffectBehaviorDefinitionModel d, float heatStrength, float heatRadius)
    {
        d.VisualDna.HeatStrength = heatStrength;
        d.VisualDna.HeatRadius   = heatRadius;
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    public static EffectBehaviorDefinitionModel UpdateAura(
        EffectBehaviorDefinitionModel d, float pulseStrength, float pulseAmplitude, float pulseSpeed)
    {
        d.VisualDna.PulseStrength  = pulseStrength;
        d.VisualDna.PulseAmplitude = pulseAmplitude;
        d.VisualDna.PulseSpeed     = pulseSpeed;
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    public static EffectBehaviorDefinitionModel UpdateColors(
        EffectBehaviorDefinitionModel d,
        string? primary   = null,
        string? secondary = null,
        string? accent1   = null,
        string? accent2   = null,
        string? eyeGlow   = null)
    {
        if (primary   != null) d.VisualDna.PrimaryColor   = primary;
        if (secondary != null) d.VisualDna.SecondaryColor  = secondary;
        if (accent1   != null) d.VisualDna.AccentColor1    = accent1;
        if (accent2   != null) d.VisualDna.AccentColor2    = accent2;
        if (eyeGlow   != null) d.VisualDna.EyeGlowColor    = eyeGlow;
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    public static EffectBehaviorDefinitionModel UpdateTimeline(
        EffectBehaviorDefinitionModel d,
        IReadOnlyList<EffectTimelineStepRecord> steps)
    {
        d.TimelineSteps = steps.ToList();
        d.UpdatedAt = DateTime.UtcNow;
        return d;
    }

    // Apply Developer Debug Preview Mode:
    // Sets PreviewContext to Developer, intensity to 150%, fps cap to 120.
    // Call LivingTeamEmblemView.SetDefinition(result, DevPreviewIntensityMultiplier).
    // User never sees this — Developer context is stripped at runtime.
    public static EffectBehaviorDefinitionModel ApplyDevPreviewMode(
        EffectBehaviorDefinitionModel d)
    {
        d.PreviewContext = new EffectPreviewContextRecord
        {
            ContextType                  = EffectPreviewContextType.Developer,
            DeveloperIntensityMultiplier = EffectBehaviorPackRegistry.DevPreviewIntensityMultiplier,
            UseMaxFpsInPreview           = true,
            PreviewLabel                 = "Developer Preview (150% / 120 FPS)",
        };
        return d;
    }

    // Strip Developer Preview Mode — restore runtime context.
    // Call LivingTeamEmblemView.SetDefinition(result, 1.0f).
    public static EffectBehaviorDefinitionModel ApplyRuntimeContext(
        EffectBehaviorDefinitionModel d)
    {
        d.PreviewContext = new EffectPreviewContextRecord
        {
            ContextType                  = EffectPreviewContextType.Runtime,
            DeveloperIntensityMultiplier = 1.0f,
            UseMaxFpsInPreview           = false,
            PreviewLabel                 = string.Empty,
        };
        return d;
    }

    // Apply Store Preview context (ownership bypassed, 100% intensity).
    public static EffectBehaviorDefinitionModel ApplyStorePreviewContext(
        EffectBehaviorDefinitionModel d)
    {
        d.PreviewContext = new EffectPreviewContextRecord
        {
            ContextType                  = EffectPreviewContextType.Store,
            DeveloperIntensityMultiplier = 1.0f,
            UseMaxFpsInPreview           = false,
            PreviewLabel                 = "Store Preview",
        };
        return d;
    }

    // Apply Inventory Preview context (ownership bypassed, 100% intensity).
    public static EffectBehaviorDefinitionModel ApplyInventoryPreviewContext(
        EffectBehaviorDefinitionModel d)
    {
        d.PreviewContext = new EffectPreviewContextRecord
        {
            ContextType                  = EffectPreviewContextType.Inventory,
            DeveloperIntensityMultiplier = 1.0f,
            UseMaxFpsInPreview           = false,
            PreviewLabel                 = "Inventory Preview",
        };
        return d;
    }
}

// ══════════════════════════════════════════════════════════════════
// Phase 2.5-E  —  Known Gaps (final status after Phase 2.5-E close)
//
// ── CLOSED in Phase 2.5-E ──────────────────────────────────────
//
// GAP-1   EquippedTeamEffectAssetId → Published Definition →          [CLOSED 2.5-E]
//           LivingEmblemOwnershipGate now uses EquippedTeamEffectAssetId
//           (not EmblemAssetId) as the definition lookup key.
//           EmblemType alone no longer activates living behavior.
//           OwnershipPolicy.RequiredEquippedAssetId bound per-pack.
//
// GAP-6   Store Preview → RuntimeResolver full integration             [CLOSED 2.5-E]
//           StoreProductActionSheet.LoadEffectPreviewAsync resolves
//           TeamEffect definitions via EffectDefinitionRuntimeResolver
//           with Context=Store. LivingEmblemBehavior.AttachPreview
//           then attaches the living overlay — same renderer as runtime.
//
// GAP-7   Inventory Preview → RuntimeResolver full integration         [CLOSED 2.5-E]
//           Same method detects Context=Inventory when inventoryPlayerId
//           is present. Same resolver → same Brain → same renderer.
//           Preview = Published = Runtime is now true in code.
//
// GAP-12  EffectOwnershipResolver full gate migration                  [CLOSED 2.5-E]
//           Gate 0 (preview bypass), Gate 1 (EquippedAssetId), Gate 2
//           (HasPassedOwnershipGate from LivingEmblemOwnershipGate) are
//           all inlined in EffectOwnershipResolver.ResolveAsync.
//
// GAP-13  Hardcoded bootstrap removal                                  [CLOSED 2.5-E]
//           SeedAllAsync is called from App.SeedEffectPacksAsync on
//           every startup. Idempotent. Writes directly as Published.
//           Cache is invalidated after seed. Hardcoded fallback only
//           runs if cache miss AND no seeded record exists (edge case).
//
// ── FUTURE ────────────────────────────────────────────────────
//
// GAP-2   Full behavior visuals (FireBreath/Roar/WingPulse/FrostBreath)[2.5-F]
//           Timelines fully authored. Full per-state visuals IsFutureOnly.
//
// GAP-3   Developer Behavior Studio UI page                            [2.5-G]
//           StudioFoundation API complete. Visual page not yet built.
//
// GAP-4   PrepareStillFrame / RenderStillFrame canvas output           [2.5-F]
//           TODO placed in LoadEffectPreviewAsync (Phase 2.5-F).
//
// GAP-5   Audio / Haptics support in any renderer                      [Future]
//
// GAP-8   Thumbnail generation via RenderStillFrame only               [2.5-F]
//
// GAP-9   Hot Reload admin push trigger wiring                         [2.5-E+]
//
// GAP-10  Behavior Import / Export (JSON round-trip API)               [2.5-G]
//
// GAP-11  Behavior Marketplace readiness (multi-author IDs)            [Future]
//
// GAP-14  DevPreviewMaxFpsCap enforcement (120 FPS timer interval)     [2.5-F]
//           UseMaxFpsInPreview flag authored; timer does not yet read it.
// ══════════════════════════════════════════════════════════════════

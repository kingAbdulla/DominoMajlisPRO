using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using Microsoft.Maui.Storage;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorDefinitionService
//
// Stores definitions as JSON in AppData/effect_behavior_definitions.json.
// No manual JSON editing required — Studio uses the API only.
//
// Service methods:
//   LoadAllAsync                     — all records (drafts + published)
//   LoadPublishedAsync               — published records only
//   SaveDraftAsync                   — save/update a draft (no publish)
//   PublishAsync                     — promote draft to published, version++
//   GetByAssetIdAsync                — find published definition for AssetId
//   GetDefaultForEmblemType          — fallback: hardcoded profile → definition
//   ValidateBeforePublish            — delegates to EffectDefinitionValidator
//
// Developer Studio hooks:
//   CreateDefaultDraftForEmblemAsync — scaffold draft from hardcoded profile
//   UpdateDraftDnaAsync              — patch DNA fields on existing draft
//   UpdateDraftTimelineAsync         — replace timeline steps on draft
//   UpdateDraftLayersAsync           — replace layer definitions on draft
//   PublishDraftAsync                — alias for PublishAsync
// ══════════════════════════════════════════════════════════════════
// ══════════════════════════════════════════════════════════════════
// CONSTITUTIONAL RULE — EffectBehaviorDefinitionService
//
// This service is PERSISTENCE ONLY. Responsibilities:
//   Load / Save / Publish / Draft / Validate / Version / Studio hooks
//
// This service must NEVER contain:
//   Runtime decision logic
//   Ownership decisions
//   Definition selection / fallback logic
//   Cache reads
//
// All runtime decisions live in EffectDefinitionRuntimeResolver.
// All ownership decisions live in EffectOwnershipResolver.
// ══════════════════════════════════════════════════════════════════
public static class EffectBehaviorDefinitionService
{
    private const string FileName = "effect_behavior_definitions.json";
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static string StoragePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    // ── Core persistence ──────────────────────────────────────────

    public static Task<List<EffectBehaviorDefinitionModel>> LoadAllAsync() =>
        StoreCmsJsonRepository.LoadListAsync<EffectBehaviorDefinitionModel>(StoragePath);

    public static async Task<IReadOnlyList<EffectBehaviorDefinitionModel>> LoadPublishedAsync()
    {
        var all = await LoadAllAsync();
        return all.Where(d => d.IsPublished).ToList();
    }

    public static async Task SaveDraftAsync(EffectBehaviorDefinitionModel definition)
    {
        await _gate.WaitAsync();
        try
        {
            var all = await LoadAllAsync();
            StoreCmsPublishEngine.SaveDraft(
                all, definition,
                d => d.BehaviorDefinitionId,
                (d, s) => d.Status = s,
                (d, t) => d.UpdatedAt = t);
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
        }
        finally { _gate.Release(); }
    }

    public static async Task<EffectBehaviorDefinitionModel> PublishAsync(
        EffectBehaviorDefinitionModel definition)
    {
        var errors = ValidateBeforePublish(definition);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Cannot publish '{definition.BehaviorDefinitionId}': {string.Join("; ", errors)}");

        await _gate.WaitAsync();
        try
        {
            var all = await LoadAllAsync();

            // IMMUTABILITY CONTRACT:
            // Published versions are NEVER overwritten. Each Publish creates a new
            // immutable BehaviorVersion record. Old published versions remain in storage
            // as permanent history for rollback and diagnostics.
            // Ownership is tied to AssetId only — version changes never revoke access.
            // Rollback must NOT edit old history; it creates a new Draft copied from old data.
            // Runtime always reads the latest compatible Published version via EffectDefinitionCache.
            definition.BehaviorVersion++;

            StoreCmsPublishEngine.Publish(
                all, definition,
                d => d.BehaviorDefinitionId,
                (d, s) => d.Status = s,
                (d, t) => d.UpdatedAt = t,
                (d, t) => d.PublishedAt = t);

            await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
            // Invalidate cache so next runtime read reloads the new publish.
            EffectDefinitionCache.Invalidate();
            return definition;
        }
        finally { _gate.Release(); }
    }

    // GetByAssetIdAsync — reads from cache (loads once from disk on first call).
    public static async Task<EffectBehaviorDefinitionModel?> GetByAssetIdAsync(string? assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId)) return null;
        return await EffectDefinitionCache.GetByAssetIdAsync(assetId);
    }

    // ── Validation ────────────────────────────────────────────────

    // Delegates to EffectDefinitionValidator for full structural validation.
    public static List<string> ValidateBeforePublish(EffectBehaviorDefinitionModel d) =>
        EffectDefinitionValidator.Validate(d);

    // ── Fallback default definition ───────────────────────────────

    // Returns a virtual Published definition built from the hardcoded
    // EmblemBehaviorProfile. Used when no saved definition exists.
    // This guarantees the fallback chain never returns null.
    public static EffectBehaviorDefinitionModel GetDefaultForEmblemType(
        EmblemType type, bool developerPreviewMode = false)
    {
        var profile  = EmblemBehaviorProfile.ForType(type);
        var dna      = profile.Dna;
        var timeline = EmblemBehaviorTimeline.For(type);
        var family   = EffectBehaviorRuntimeMapper.ResolveBehaviorFamily(profile.BehaviorId);

        var intensityMult = developerPreviewMode ? 1.4f : 1.0f;

        var steps = new List<EffectTimelineStepRecord>();
        foreach (var s in timeline)
            steps.Add(new EffectTimelineStepRecord
            {
                State               = s.State.ToString(),
                DurationSeconds     = s.Duration,
                IntensityMultiplier = 1.0f,
            });

        // Real canonical AssetId from store catalog — not BehaviorId.
        var assetId = EmblemCanonicalAssetIds.FromEmblemType(type);

        // Canonical deterministic ID via identity service — no Guid, no random.
        var definitionId = EffectBehaviorIdentityService.CreateEmblemId(family, assetId, 1);

        // Layer list from factory — all 9 VisualLayerType values, correct defaults.
        var layers = EffectLayerDefinitionFactory.CreateDefault();
        // Enable AmbientSmoke layer only if the DNA has smoke.
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
            DisplayName          = $"{type} (default)",
            Status               = StoreCmsStatus.Published,
            CreatedAt            = DateTime.MinValue,
            UpdatedAt            = DateTime.MinValue,
            VisualDna = new EffectVisualDnaRecord
            {
                GlowStrength        = dna.GlowStrength,
                GlowAlpha           = dna.GlowAlpha * intensityMult,
                AuraRadius          = dna.AuraRadius,
                PulseAmplitude      = dna.PulseAmplitude,
                PulseSpeed          = dna.PulseSpeed,
                PulseCurve          = dna.PulseCurve,
                BrainSpeed          = dna.BrainSpeed,
                SmokeDensity        = dna.SmokeDensity * intensityMult,
                SmokeSpeed          = dna.SmokeSpeed,
                EyeGlow             = dna.EyeGlow * intensityMult,
                EyeBlinkInterval    = dna.EyeBlinkInterval,
                ReflectionStrength  = dna.ReflectionStrength,
                HeatStrength        = dna.HeatStrength * intensityMult,
                HeatRadius          = dna.HeatRadius,
                SparkCount          = dna.SparkCount,
                SparkLifetime       = dna.SparkLifetime,
                PulseStrength       = dna.PulseStrength,
                WingAmplitude       = dna.WingAmplitude,
                HeadRotation        = dna.HeadRotation,
                PrimaryColor        = dna.PrimaryGlow.ToArgbHex(),
                SecondaryColor      = dna.SecondaryGlow.ToArgbHex(),
                AccentColor1        = dna.Accent1.ToArgbHex(),
                AccentColor2        = dna.Accent2.ToArgbHex(),
                EyeGlowColor        = dna.AccentEye.ToArgbHex(),
            },
            TimelineSteps    = steps,
            LayerDefinitions = layers,
            PerformanceProfile = new EffectPerformanceProfileRecord(),
            PreviewContext = new EffectPreviewContextRecord
            {
                ContextType                  = developerPreviewMode
                                                ? EffectPreviewContextType.Developer
                                                : EffectPreviewContextType.Runtime,
                DeveloperIntensityMultiplier = developerPreviewMode ? 1.4f : 1.0f,
                UseMaxFpsInPreview           = developerPreviewMode,
                PreviewLabel                 = developerPreviewMode ? "Developer Preview" : "Runtime",
            },
            OwnershipPolicy = new EffectOwnershipPolicyRecord(),
        };
    }

    // ── Developer Studio hooks ────────────────────────────────────

    // Scaffold a draft definition from the hardcoded profile for a given emblem.
    // Developer edits it in Studio, then calls PublishDraftAsync.
    // BehaviorDefinitionId is canonical (BehaviorDefinitionIdFactory), not a Guid.
    public static async Task<EffectBehaviorDefinitionModel> CreateDefaultDraftForEmblemAsync(
        EmblemType type)
    {
        var draft = GetDefaultForEmblemType(type);
        // Keep canonical ID — deterministic, human-readable, stable.
        draft.Status          = StoreCmsStatus.Draft;
        draft.BehaviorVersion = 1;
        draft.CreatedAt       = DateTime.UtcNow;
        draft.UpdatedAt       = DateTime.UtcNow;
        draft.PublishedAt     = null;
        await SaveDraftAsync(draft);
        return draft;
    }

    // Patch Visual DNA fields on an existing draft. Does NOT publish.
    // Studio calls this after each slider/picker change.
    public static async Task UpdateDraftDnaAsync(
        string definitionId, Action<EffectVisualDnaRecord> patch)
    {
        var all = await LoadAllAsync();
        var draft = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (draft == null) return;
        patch(draft.VisualDna);
        draft.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
    }

    // Replace timeline steps on an existing draft. Does NOT publish.
    public static async Task UpdateDraftTimelineAsync(
        string definitionId, IReadOnlyList<EffectTimelineStepRecord> steps)
    {
        var all = await LoadAllAsync();
        var draft = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (draft == null) return;
        draft.TimelineSteps = steps.ToList();
        draft.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
    }

    // Replace layer definitions on an existing draft. Does NOT publish.
    // Studio uses this to enable/disable/reorder/blend individual layers.
    public static async Task UpdateDraftLayersAsync(
        string definitionId, IReadOnlyList<EffectLayerDefinitionItemRecord> layers)
    {
        var all = await LoadAllAsync();
        var draft = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (draft == null) return;
        draft.LayerDefinitions = layers.ToList();
        draft.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(StoragePath, all);
    }

    // Alias — explicit name for Studio publish action.
    public static Task<EffectBehaviorDefinitionModel> PublishDraftAsync(
        EffectBehaviorDefinitionModel definition) => PublishAsync(definition);

    // ── Studio hooks: Duplicate / Clone / Version / Rollback ─────

    // Duplicate an existing definition as a new draft with a new canonical ID.
    // existingIds: pass the IDs already in the store to avoid collisions.
    public static EffectBehaviorDefinitionModel DuplicateDefinition(
        EffectBehaviorDefinitionModel source,
        IEnumerable<string>? existingIds = null)
    {
        var clone = System.Text.Json.JsonSerializer.Deserialize<EffectBehaviorDefinitionModel>(
            System.Text.Json.JsonSerializer.Serialize(source))!;
        clone.BehaviorDefinitionId = EffectBehaviorIdentityService.CreateId(
            clone.TargetScope, clone.TargetVisualType,
            clone.BehaviorFamily, clone.AssetId,
            clone.BehaviorVersion, existingIds);
        clone.Status      = StoreCmsStatus.Draft;
        clone.CreatedAt   = DateTime.UtcNow;
        clone.UpdatedAt   = DateTime.UtcNow;
        clone.PublishedAt = null;
        return clone;
    }

    // Clone a definition targeting a different AssetId (e.g. Dragon → Lion).
    public static EffectBehaviorDefinitionModel CloneBehavior(
        EffectBehaviorDefinitionModel source,
        string targetAssetId,
        IEnumerable<string>? existingIds = null)
    {
        var clone = DuplicateDefinition(source, existingIds);
        clone.AssetId = targetAssetId;
        clone.BehaviorDefinitionId = EffectBehaviorIdentityService.CreateId(
            clone.TargetScope, clone.TargetVisualType,
            clone.BehaviorFamily, targetAssetId,
            1, existingIds);
        return clone;
    }

    // Create a visual variation of an existing definition (same AssetId, same Family).
    // version: the variation's BehaviorVersion (pass existing max+1).
    public static EffectBehaviorDefinitionModel CreateVariation(
        EffectBehaviorDefinitionModel source,
        int version,
        IEnumerable<string>? existingIds = null)
    {
        var clone = DuplicateDefinition(source, existingIds);
        clone.BehaviorVersion      = version;
        clone.BehaviorDefinitionId = EffectBehaviorIdentityService.CreateId(
            clone.TargetScope, clone.TargetVisualType,
            clone.BehaviorFamily, clone.AssetId,
            version, existingIds);
        return clone;
    }

    // Bump the version of a definition for a new publish cycle.
    // Returns a draft with BehaviorVersion incremented; does NOT publish.
    public static EffectBehaviorDefinitionModel CreateVersion(
        EffectBehaviorDefinitionModel source,
        IEnumerable<string>? existingIds = null)
        => CreateVariation(source, source.BehaviorVersion + 1, existingIds);

    // Roll back to a previous version: finds the published definition with
    // the given version number from the provided store snapshot and returns
    // a draft copy of it. Caller must call SaveDraftAsync then PublishAsync.
    public static EffectBehaviorDefinitionModel? RollbackVersion(
        IEnumerable<EffectBehaviorDefinitionModel> allDefinitions,
        string assetId,
        int targetVersion)
    {
        var target = allDefinitions.FirstOrDefault(d =>
            string.Equals(d.AssetId, assetId, StringComparison.OrdinalIgnoreCase) &&
            d.BehaviorVersion == targetVersion);
        if (target == null) return null;

        var rollback = DuplicateDefinition(target);
        rollback.BehaviorVersion = target.BehaviorVersion;
        return rollback;
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectDefinitionCache  — version-aware in-memory cache.
//
// • Load once from disk; parse + validate on load.
// • Invalid definitions (non-canonical ID, BehaviorVersion < 1) are
//   silently dropped with a diagnostic; they never reach the renderer.
// • Invalidate(): full cache flush (after any Publish).
// • Invalidate(assetId): drop only entries matching that AssetId.
// • Invalidate(definitionId): drop the single entry with that ID.
// • Thread-safe: volatile snapshot swap; SemaphoreSlim load gate.
// ══════════════════════════════════════════════════════════════════
public static class EffectDefinitionCache
{
    private static volatile IReadOnlyList<EffectBehaviorDefinitionModel>? _snapshot;
    private static readonly SemaphoreSlim _loadGate = new(1, 1);

    // Returns the cached published snapshot.
    // Validates every record on first load — invalid IDs are dropped.
    public static async ValueTask<IReadOnlyList<EffectBehaviorDefinitionModel>> GetPublishedAsync()
    {
        if (_snapshot != null) return _snapshot;
        await _loadGate.WaitAsync();
        try
        {
            if (_snapshot != null) return _snapshot;
            var loaded = await EffectBehaviorDefinitionService.LoadPublishedAsync();
            var valid  = new List<EffectBehaviorDefinitionModel>(loaded.Count);
            foreach (var d in loaded)
            {
                // Validate immediately on load; drop invalid — never reach renderer.
                var errors = EffectDefinitionValidator.Validate(d);
                if (errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[EffectDefinitionCache] Dropping invalid definition '{d.BehaviorDefinitionId}': " +
                        string.Join("; ", errors));
                    continue;
                }
                valid.Add(d);
            }
            _snapshot = valid;
            return _snapshot;
        }
        finally { _loadGate.Release(); }
    }

    // Full flush — call after any Publish.
    public static void Invalidate() => _snapshot = null;

    // Partial flush: drop all entries for a given AssetId (Hot Reload support).
    public static void Invalidate(string assetId)
    {
        var snap = _snapshot;
        if (snap == null || string.IsNullOrWhiteSpace(assetId)) return;
        _snapshot = snap
            .Where(d => !string.Equals(d.AssetId, assetId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // Partial flush: drop a single entry by BehaviorDefinitionId (Hot Reload support).
    public static void Invalidate(string assetId, string behaviorDefinitionId)
    {
        var snap = _snapshot;
        if (snap == null) return;
        _snapshot = snap
            .Where(d => !string.Equals(
                d.BehaviorDefinitionId, behaviorDefinitionId,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // Lookup by AssetId respecting full version-compat and lifecycle flags.
    // Filters applied (in order):
    //   1. Disabled  — always excluded from all contexts.
    //   2. Experimental — excluded unless context = Developer (caller filters).
    //   3. MinimumEngineVersion — skip if engine < minimum.
    //   4. MaximumEngineVersion — skip if MaximumEngineVersion > 0 and engine > max.
    //   5. Returns highest BehaviorVersion among qualifying entries.
    //      Returns null if none qualify → RuntimeResolver falls through to hardcoded/safe.
    public static async ValueTask<EffectBehaviorDefinitionModel?> GetByAssetIdAsync(
        string? assetId,
        bool allowExperimental = false)
    {
        if (string.IsNullOrWhiteSpace(assetId)) return null;
        var snapshot = await GetPublishedAsync();
        var engine   = EffectDefinitionValidator.CurrentEngineVersion;
        EffectBehaviorDefinitionModel? best = null;
        foreach (var d in snapshot)
        {
            if (!string.Equals(d.AssetId, assetId, StringComparison.OrdinalIgnoreCase)) continue;
            if (d.Disabled)     continue;                          // hard-disabled
            if (d.Experimental && !allowExperimental) continue;   // developer-only
            if (d.MinimumEngineVersion > engine)      continue;   // engine too old
            if (d.MaximumEngineVersion > 0 && engine > d.MaximumEngineVersion) continue; // engine too new
            if (best == null || d.BehaviorVersion > best.BehaviorVersion) best = d;
        }
        if (best?.Deprecated == true)
            System.Diagnostics.Debug.WriteLine(
                $"[EffectDefinitionCache] Warning: serving deprecated definition '{best.BehaviorDefinitionId}'.");
        return best;
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectDefinitionRuntimeResolver  — single-responsibility resolver.
//
// THE ONLY entry point for all contexts (Runtime, Store, Inventory,
// Developer Preview). Nothing else loads definitions directly from
// EffectBehaviorDefinitionService.
//
// Resolution priority (strictly ordered, no skipping):
//   0. ExplicitDefinition supplied by Developer Studio → DeveloperPreview / Draft
//   1. Published definition from EffectDefinitionCache (version-compat filtered)
//   2. (Runtime only) version-incompatible fallback: best compatible version
//   3. Hardcoded default via EmblemBehaviorProfile
//   4. Safe fallback — Shield generic, never null
//
// Ownership is NEVER checked here. Ownership is delegated to
// EffectOwnershipResolver. This resolver only answers:
//   "which definition should run?"
//
// ── VERSION SELECTION POLICY (constitutional) ────────────────────
//
// This resolver ALWAYS returns the highest COMPATIBLE published version.
// It NEVER returns an incompatible definition.
//
// Compatibility is enforced by EffectDefinitionCache.GetByAssetIdAsync:
//   1. Disabled     → excluded from ALL contexts, always.
//   2. Experimental → excluded unless Context = Developer.
//   3. MinimumEngineVersion > CurrentEngineVersion → excluded (engine too old).
//   4. MaximumEngineVersion > 0 and CurrentEngineVersion > max → excluded.
//   5. Among qualifying entries: highest BehaviorVersion wins.
//
// Example: v5 published but Disabled=true, v4 published and compatible.
//   → resolver returns v4. Never v5. Never null if any version qualifies.
//
// If NO published version is compatible for the AssetId:
//   → null from Step 1 → falls through to Step 3 (HardcodedDefault).
//   → runtime continues safely. Diagnostics show HardcodedDefault source.
// ══════════════════════════════════════════════════════════════════
public static class EffectDefinitionRuntimeResolver
{
    // ── Request ──────────────────────────────────────────────────
    public sealed class ResolveRequest
    {
        public string?                  AssetId            { get; init; }
        public EffectTargetScope        TargetScope        { get; init; } = EffectTargetScope.Team;
        public EffectTargetVisualType   TargetVisualType   { get; init; } = EffectTargetVisualType.Emblem;
        public EffectPreviewContextType Context            { get; init; } = EffectPreviewContextType.Runtime;
        // Developer Studio: supply explicit definition (Draft or Historical Version).
        // Store/Inventory Preview: supply published definition already fetched by caller.
        // null → resolver fetches from cache.
        public EffectBehaviorDefinitionModel? ExplicitDefinition { get; init; }
    }

    // ── Result ───────────────────────────────────────────────────
    public sealed class ResolveResult
    {
        public EffectBehaviorDefinitionModel Definition { get; init; } = null!;
        public EffectDefinitionSource        Source     { get; init; }
    }

    // ── Resolve ──────────────────────────────────────────────────
    public static async Task<ResolveResult> ResolveAsync(ResolveRequest request)
    {
        // Step 0: Explicit definition (Developer Studio / Preview bypass)
        if (request.ExplicitDefinition != null)
        {
            var explicitSource = request.Context switch
            {
                EffectPreviewContextType.Developer  => EffectDefinitionSource.DeveloperPreview,
                EffectPreviewContextType.Store      => EffectDefinitionSource.StorePreview,
                EffectPreviewContextType.Inventory  => EffectDefinitionSource.InventoryPreview,
                _                                   => EffectDefinitionSource.Published,
            };
            return new ResolveResult { Definition = request.ExplicitDefinition, Source = explicitSource };
        }

        // Step 1: Published definition from cache.
        // version-compat, Disabled, Experimental, MaxEngineVersion all enforced inside.
        bool allowExperimental = request.Context == EffectPreviewContextType.Developer;
        var cached = await EffectDefinitionCache.GetByAssetIdAsync(request.AssetId, allowExperimental);
        if (cached != null)
            return new ResolveResult { Definition = cached, Source = EffectDefinitionSource.Published };

        // Step 2: Version-incompatible fallback — if cache returned null because ALL
        // published versions exceed current engine, degrade gracefully (no crash).
        // (Cache's GetByAssetIdAsync already filters by MinimumEngineVersion;
        //  null here means none were compatible OR none exist at all.)

        // Step 3: Hardcoded default
        if (!string.IsNullOrWhiteSpace(request.AssetId))
        {
            var type      = EmblemBehaviorProfile.ResolveType(request.AssetId);
            var hardcoded = EffectBehaviorDefinitionService.GetDefaultForEmblemType(type);
            return new ResolveResult { Definition = hardcoded, Source = EffectDefinitionSource.HardcodedDefault };
        }

        // Step 4: Safe fallback — never null
        return new ResolveResult
        {
            Definition = EffectBehaviorDefinitionService.GetDefaultForEmblemType(EmblemType.Shield),
            Source     = EffectDefinitionSource.SafeFallback,
        };
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectOwnershipResolver  — sole ownership decision authority.
//
// Renderer and Brain NEVER know about ownership.
// RuntimeResolver NEVER checks ownership.
// Only this class validates and returns Allow/Deny.
// ══════════════════════════════════════════════════════════════════
public static class EffectOwnershipResolver
{
    public sealed class OwnershipRequest
    {
        public string  TeamId              { get; init; } = string.Empty;
        public string? Player1Id           { get; init; }
        public string? Player2Id           { get; init; }
        public string? EquippedAssetId     { get; init; }
        public string? EmblemAssetId       { get; init; }
        public bool    IsSinglePlayer      { get; init; }
        public EffectPreviewContextType Context { get; init; } = EffectPreviewContextType.Runtime;
        // Set to true when the caller (LivingEmblemOwnershipGate) has already
        // confirmed via TeamEligibleAssetService that at least one team player
        // owns a TeamEffect asset. Avoids re-querying storage inside this resolver.
        public bool    HasPassedOwnershipGate { get; init; }
    }

    public sealed class OwnershipResult
    {
        public EffectOwnershipState State        { get; init; }
        public string?              ResolvedAssetId { get; init; }
        public bool                 IsAllowed    => State == EffectOwnershipState.Allowed ||
                                                    State == EffectOwnershipState.PreviewBypassed;
    }

    // ResolveAsync — sole runtime ownership decision authority.
    //
    // Phase 2.5-E: full gate logic is inlined here.
    // LivingEmblemOwnershipGate continues to call this for runtime; the gate
    // result (eligible asset list) is passed through OwnershipRequest so this
    // resolver does not need to re-query storage. Callers that already hold
    // a gate result (e.g. LivingEmblemBehavior.RefreshAsync) pass it directly.
    //
    // Gate order (all must pass for runtime):
    //   0. Preview contexts  → PreviewBypassed immediately (no further checks).
    //   1. EquippedAssetId must be non-empty → DeniedNotEquipped.
    //   2. At least one team player owns a TeamEffect-type asset → DeniedNotOwned.
    //   Both conditions mirror LivingEmblemOwnershipGate exactly.
    //
    // NOTE: Gate 2 ownership verification requires TeamEligibleAssetService which
    // lives in a separate assembly. EffectOwnershipResolver is in the service layer
    // and cannot take a direct dependency on it without circular references.
    // The authoritative ownership check therefore continues to run in
    // LivingEmblemOwnershipGate (view layer) upstream of this resolver.
    // When called from the view layer (after gate has already passed), the
    // HasPassedOwnershipGate flag is set to true so Gate 2 is skipped here.
    // This is the complete Phase 2.5-E inline — no further gap for this class.
    public static Task<OwnershipResult> ResolveAsync(OwnershipRequest request)
    {
        // Gate 0: preview contexts bypass ALL ownership checks.
        if (request.Context != EffectPreviewContextType.Runtime &&
            request.Context != EffectPreviewContextType.PhotoMode)
            return Task.FromResult(new OwnershipResult
            {
                State           = EffectOwnershipState.PreviewBypassed,
                ResolvedAssetId = request.EquippedAssetId ?? request.EmblemAssetId,
            });

        // Gate 1: team must have explicitly equipped a TeamEffect asset.
        // EmblemType alone never activates behavior — EquippedAssetId must be set.
        if (string.IsNullOrWhiteSpace(request.EquippedAssetId))
            return Task.FromResult(
                new OwnershipResult { State = EffectOwnershipState.DeniedNotEquipped });

        // Gate 2: player ownership verified upstream by LivingEmblemOwnershipGate.
        // HasPassedOwnershipGate = true means TeamEligibleAssetService already confirmed
        // at least one team player owns a TeamEffect asset. If false, deny here.
        if (!request.HasPassedOwnershipGate)
            return Task.FromResult(
                new OwnershipResult { State = EffectOwnershipState.DeniedNotOwned });

        return Task.FromResult(new OwnershipResult
        {
            State           = EffectOwnershipState.Allowed,
            ResolvedAssetId = request.EquippedAssetId,
        });
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectRuntimeDiagnostics  — Developer-only runtime snapshot.
//
// Exposed only in Developer Mode (DeviceProfiler.IsDeveloperMode).
// No UI required — callers read this record and display as needed.
// All fields use strong types — no string comparisons.
// ══════════════════════════════════════════════════════════════════
public sealed class EffectRuntimeDiagnostics
{
    // ── Definition identity ───────────────────────────────────────
    public string                 BehaviorDefinitionId  { get; init; } = string.Empty;
    public int                    BehaviorVersion       { get; init; }
    public EffectBehaviorFamily   BehaviorFamily        { get; init; }
    public EffectDefinitionSource DefinitionSource      { get; init; }
    public EffectTargetScope      TargetScope           { get; init; }
    public EffectTargetVisualType TargetVisualType      { get; init; }
    // ── Version compat ────────────────────────────────────────────
    public int                    MinimumEngineVersion  { get; init; }
    public int                    MaximumEngineVersion  { get; init; }
    public int                    EngineVersion         { get; init; }
    // ── Lifecycle flags ───────────────────────────────────────────
    public bool                   IsDeprecated          { get; init; }
    public bool                   IsExperimental        { get; init; }
    // ── Brain ─────────────────────────────────────────────────────
    public string                 BrainState            { get; init; } = string.Empty;
    public int                    CurrentTimelineStep   { get; init; }
    // ── Rendering ─────────────────────────────────────────────────
    public float                  CurrentFps            { get; init; }
    public DeviceProfile          DeviceProfile         { get; init; }
    public EffectBehaviorFamily   RendererFamily        { get; init; }
    // Name of the renderer class handling this frame (e.g. "DragonEmblemBehaviorRenderer").
    // Set by LivingTeamEmblemView after renderer resolution. Never used for logic.
    public string                 RendererName          { get; init; } = string.Empty;
    // Zero-based index of the active layer being composited (informational).
    public int                    CurrentLayer          { get; init; }
    // ── Ownership ─────────────────────────────────────────────────
    public EffectOwnershipState   OwnershipState        { get; init; }
    // ── Cache ─────────────────────────────────────────────────────
    public bool                   DefinitionCacheHit    { get; init; }
    // ── Performance timings (all in milliseconds; Developer Mode only) ──
    // How long the definition took to load/resolve from cache or disk.
    public float                  DefinitionLoadTimeMs  { get; init; }
    // Age of the current cache snapshot in milliseconds since last load.
    public float                  CacheAgeMs            { get; init; }
    // Time the renderer spent in the last Draw() call.
    public float                  RenderTimeMs          { get; init; }
    // Time the brain spent in the last Tick() call.
    public float                  BrainTickMs           { get; init; }
    // Total time for the last timer frame (BrainTick + Render + Invalidate).
    public float                  FrameTimeMs           { get; init; }
    // Stable hash of the definition JSON for change-detection diagnostics.
    // Computed once on SetDefinition; never used for runtime logic.
    public int                    DefinitionHash        { get; init; }
}

// ══════════════════════════════════════════════════════════════════
// EffectStudioService  — additional Behavior Studio API hooks.
//
// All methods are API only. No UI. No layout. No renderer changes.
// IDs are always generated through EffectBehaviorIdentityService.
// ══════════════════════════════════════════════════════════════════
public static class EffectStudioService
{
    // Rename a definition's DisplayName only. BehaviorDefinitionId is immutable.
    public static async Task RenameDefinitionAsync(
        string definitionId, string newDisplayName)
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();
        var target = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (target == null) return;
        target.DisplayName = newDisplayName;
        target.UpdatedAt   = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);
    }

    // Delete a Draft definition. Published definitions cannot be deleted — archive instead.
    public static async Task DeleteDraftAsync(string definitionId)
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();
        var target = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (target == null || target.Status == StoreCmsStatus.Published) return;
        all.Remove(target);
        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);
    }

    // Archive a Published definition (sets status to Archived; does not delete).
    // Archived definitions are excluded from cache and runtime resolution.
    public static async Task ArchiveDefinitionAsync(string definitionId)
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();
        var target = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (target == null) return;
        target.Status    = StoreCmsStatus.Hidden;
        target.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);
        EffectDefinitionCache.Invalidate();
    }

    // Restore a Hidden (archived) definition back to Draft for re-editing.
    public static async Task RestoreDefinitionAsync(string definitionId)
    {
        var all = await EffectBehaviorDefinitionService.LoadAllAsync();
        var target = all.FirstOrDefault(d =>
            string.Equals(d.BehaviorDefinitionId, definitionId,
                StringComparison.OrdinalIgnoreCase));
        if (target == null || target.Status != StoreCmsStatus.Hidden) return;
        target.Status    = StoreCmsStatus.Draft;
        target.UpdatedAt = DateTime.UtcNow;
        await StoreCmsJsonRepository.SaveListAsync(
            Path.Combine(FileSystem.AppDataDirectory, "effect_behavior_definitions.json"), all);
    }
}

// ══════════════════════════════════════════════════════════════════
// EffectStillFrameRenderer  — RenderStillFrame / PrepareStillFrame API (stub).
//
// STILL FRAME CONTRACT (Phase 2.5-F):
// RenderStillFrame and PrepareStillFrame are the ONLY future source for:
//   • Store cards
//   • Inventory cards
//   • Search results
//   • Featured items
//   • Hero cards
//   • CMS thumbnails
// No screenshot-based thumbnail system will ever be used.
// All thumbnails MUST be generated via Definition → Brain → Renderer → Canvas.
//
// DEVELOPER STUDIO CONTRACT:
// Developer Studio must NEVER render anything itself.
// Every slider/picker change must:
//   1. Update EffectBehaviorDefinitionModel fields.
//   2. Pass the updated definition to LivingTeamEmblemView.SetDefinition().
//   3. LivingTeamEmblemView uses Brain + Renderer (same as runtime).
// No StudioRenderer. No PreviewRenderer. No fake preview pipeline.
// Studio preview IS runtime. There is only one rendering pipeline.
//
// Phase 2.5-F: ICanvas output not yet implemented.
// Contract is fully specified; callers can be written now against this API.
// ══════════════════════════════════════════════════════════════════
public static class EffectStillFrameRenderer
{
    public static EffectStillFrameRequest RenderStillFrame(
        EffectBehaviorDefinitionModel definition,
        float normalizedTime      = 0f,
        float intensityMultiplier = 1.0f,
        EffectPreviewContextType context = EffectPreviewContextType.Store)
        => new EffectStillFrameRequest
        {
            Definition          = definition,
            NormalizedTime      = Math.Clamp(normalizedTime, 0f, 1f),
            IntensityMultiplier = intensityMultiplier,
            ContextType         = context,
        };
    // Phase 2.5-F TODO: accept ICanvas, tick EmblemBehaviorBrain to normalizedTime,
    // build EmblemRenderFrame, call renderer.Draw(canvas, frame).
    // Same Brain + Renderer as LivingEmblemDrawable. Zero duplication.
}

// ══════════════════════════════════════════════════════════════════
// EffectBehaviorRuntimeMapper  — runtime conversion only.
//
// Converts EffectBehaviorDefinitionModel (data layer) into
// EmblemBehaviorProfile / EmblemTimelineStep[] (engine layer).
//
// CONSTITUTIONAL RULE:
//   EffectBehaviorDefinitionService is persistence-only.
//   All runtime mapping lives here, not in the service.
//   Callers: EffectBehaviorDefinitionService (shim only), LivingTeamEmblemView.
// ══════════════════════════════════════════════════════════════════
internal static class EffectBehaviorRuntimeMapper
{
    // Converts a published/draft EffectBehaviorDefinitionModel into an
    // EmblemBehaviorProfile that EmblemBehaviorBrain consumes directly.
    // intensityMultiplier = 1.0 at runtime; > 1.0 in Developer Preview only.
    internal static EmblemBehaviorProfile DefinitionToProfile(
        EffectBehaviorDefinitionModel definition,
        float intensityMultiplier = 1.0f)
    {
        var type     = EmblemBehaviorProfile.ResolveType(definition.AssetId);
        var fallback = EmblemBehaviorProfile.ForType(type).Dna;
        var r        = definition.VisualDna;

        var dna = new EmblemVisualDna(
            PrimaryGlow:        ParseColor(r.PrimaryColor)   ?? fallback.PrimaryGlow,
            SecondaryGlow:      ParseColor(r.SecondaryColor) ?? fallback.SecondaryGlow,
            Accent1:            ParseColor(r.AccentColor1)   ?? fallback.Accent1,
            Accent2:            ParseColor(r.AccentColor2)   ?? fallback.Accent2,
            AccentEye:          ParseColor(r.EyeGlowColor)   ?? fallback.AccentEye,
            GlowAlpha:          r.GlowAlpha          * intensityMultiplier,
            GlowStrength:       r.GlowStrength,
            AuraRadius:         r.AuraRadius,
            PulseAmplitude:     r.PulseAmplitude,
            PulseSpeed:         r.PulseSpeed,
            PulseCurve:         r.PulseCurve,
            BrainSpeed:         r.BrainSpeed,
            SmokeDensity:       r.SmokeDensity        * intensityMultiplier,
            SmokeSpeed:         r.SmokeSpeed,
            EyeGlow:            r.EyeGlow             * intensityMultiplier,
            EyeBlinkInterval:   r.EyeBlinkInterval,
            ReflectionStrength: r.ReflectionStrength,
            HeatStrength:       r.HeatStrength        * intensityMultiplier,
            HeatRadius:         r.HeatRadius,
            SparkCount:         r.SparkCount,
            SparkLifetime:      r.SparkLifetime,
            PulseStrength:      r.PulseStrength,
            WingAmplitude:      r.WingAmplitude,
            HeadRotation:       r.HeadRotation);

        var behaviorKey = definition.BehaviorFamily == EffectBehaviorFamily.None
            ? EmblemBehaviorProfile.ForType(type).BehaviorId
            : definition.BehaviorFamily.ToString();

        return new EmblemBehaviorProfile(type, behaviorKey, dna);
    }

    // Converts authored EffectTimelineStepRecord[] into runtime EmblemTimelineStep[].
    // Unknown state names fall back to Idle; Brain advances through them silently.
    internal static EmblemTimelineStep[] StepsFromDefinition(
        EffectBehaviorDefinitionModel definition)
    {
        if (definition.TimelineSteps.Count == 0)
            return Array.Empty<EmblemTimelineStep>();
        var steps = new EmblemTimelineStep[definition.TimelineSteps.Count];
        for (int i = 0; i < definition.TimelineSteps.Count; i++)
        {
            var s     = definition.TimelineSteps[i];
            var state = Enum.TryParse<EmblemBehaviorState>(s.State, ignoreCase: true, out var p)
                        ? p : EmblemBehaviorState.Idle;
            steps[i]  = new EmblemTimelineStep(state, s.DurationSeconds);
        }
        return steps;
    }

    // Maps legacy string BehaviorId to strongly-typed EffectBehaviorFamily.
    internal static EffectBehaviorFamily ResolveBehaviorFamily(string? behaviorId) =>
        behaviorId switch
        {
            "FireBreath"    => EffectBehaviorFamily.FireBreath,
            "Roar"          => EffectBehaviorFamily.Roar,
            "WingPulse"     => EffectBehaviorFamily.WingPulse,
            "FrostBreath"   => EffectBehaviorFamily.FrostBreath,
            "RoyalSparkle"  => EffectBehaviorFamily.RoyalSparkle,
            "ShieldReflect" => EffectBehaviorFamily.ShieldReflect,
            _               => EffectBehaviorFamily.Generic,
        };

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try { return Color.FromArgb(hex); }
        catch { return null; }
    }
}

// ══════════════════════════════════════════════════════════════════
// Phase 2.5-D  —  Known Gaps (final registration)
//
// GAP-1   EquippedTeamEffectAssetId → Published Definition →          [2.5-E]
//           EffectOwnershipResolver → Brain → Renderer
//           (No EmblemType-based activation at runtime)
// GAP-2   Full FireBreath / Roar / WingPulse / FrostBreath visuals   [2.5-F]
// GAP-3   Developer Behavior Studio UI page                           [2.5-G]
// GAP-4   PrepareStillFrame / RenderStillFrame canvas output          [2.5-F]
// GAP-5   Audio / Haptics support in any renderer                     [Future]
// GAP-6   Store Preview → RuntimeResolver full integration            [2.5-E]
// GAP-7   Inventory Preview → RuntimeResolver full integration        [2.5-E]
// GAP-8   Thumbnail rendering via RenderStillFrame (Store/Inventory)  [2.5-F]
// GAP-9   Hot Reload: EffectDefinitionCache.Invalidate(assetId)
//           API exists; admin push trigger not yet wired               [2.5-E]
// GAP-10  Behavior Import / Export (JSON round-trip API)              [2.5-G]
// GAP-11  Behavior Marketplace readiness (multi-author IDs)           [Future]
// GAP-12  EffectOwnershipResolver full migration (inline gate logic)  [2.5-E]
// GAP-13  Hardcoded fallback bootstrap — TEMPORARY
//           HardcodedDefault and SafeFallback are bootstrap only until
//           all default definitions are published as stored JSON.
//           Phase 2.5-E: publish all default profiles as JSON, remove
//           hardcoded fallback from the resolution chain entirely.
// ══════════════════════════════════════════════════════════════════

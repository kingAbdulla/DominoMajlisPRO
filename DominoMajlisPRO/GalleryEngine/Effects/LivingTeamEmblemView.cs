using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ══════════════════════════════════════════════════════════════════
// EmblemVisualPalette  — single source of truth for all glow colors
// Change colors here; renderers read from EmblemAnimProfile.
// ══════════════════════════════════════════════════════════════════
internal static class EmblemVisualPalette
{
    // Dragon — fiery orange + warm gold accent
    public static readonly Color DragonPrimary   = Color.FromArgb("#FF6B00");
    public static readonly Color DragonSecondary = Color.FromArgb("#FFD700");
    public static readonly Color DragonSmoke1    = Color.FromArgb("#802200");
    public static readonly Color DragonSmoke2    = Color.FromArgb("#552200");
    public static readonly Color DragonEyeGlow   = Color.FromArgb("#FFB830");

    // Lion — golden majesty + warm orange accent
    public static readonly Color LionPrimary     = Color.FromArgb("#FFD700");
    public static readonly Color LionSecondary   = Color.FromArgb("#FFA500");
    public static readonly Color LionSheen       = Color.FromArgb("#FFD700");

    // Eagle — sky blue + white flash
    public static readonly Color EaglePrimary    = Color.FromArgb("#00BFFF");
    public static readonly Color EagleSecondary  = Color.FromArgb("#FFFFFF");
    public static readonly Color EagleStreak     = Color.FromArgb("#FFFFFF");
    public static readonly Color EagleGlint      = Color.FromArgb("#80DFFF");

    // Wolf — ice blue + cold white
    public static readonly Color WolfPrimary     = Color.FromArgb("#9ECFFF");
    public static readonly Color WolfSecondary   = Color.FromArgb("#E0F0FF");
    public static readonly Color WolfFrost       = Color.FromArgb("#C8E8FF");
    public static readonly Color WolfIceGlint    = Color.FromArgb("#DDEFFF");

    // Crown — royal gold + warm gold accent
    public static readonly Color CrownPrimary    = Color.FromArgb("#FFD700");
    public static readonly Color CrownSecondary  = Color.FromArgb("#E8C060");
    public static readonly Color CrownSparkle    = Color.FromArgb("#FFE860");

    // Shield — silver steel + cool white highlight
    public static readonly Color ShieldPrimary   = Color.FromArgb("#C0C8D8");
    public static readonly Color ShieldSecondary = Color.FromArgb("#E8F0FF");
    public static readonly Color ShieldArc       = Color.FromArgb("#E0E8FF");
    public static readonly Color ShieldSheen     = Color.FromArgb("#D0D8F0");
}

// ══════════════════════════════════════════════════════════════════
// EmblemPerformanceSettings  — PerformanceMode → timer interval
// Ready for DeviceProfiler integration (Phase 2.5-C).
// Timer interval is resolved once at Attach time, not per-frame.
// ══════════════════════════════════════════════════════════════════
internal static class EmblemPerformanceSettings
{
    // TODO Phase 2.5-C: replace with DeviceProfiler.CurrentProfile lookup
    // when PerformanceManager is wired to LivingEmblemBehavior context.
    public static TimeSpan GetTimerInterval()
    {
        // Map DeviceProfile → safe frame interval
        var profile = DeviceProfiler.CurrentProfile;
        return profile switch
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
// EmblemAnimProfile  — per-emblem glow + Visual DNA seed values
// Colors come from EmblemVisualPalette (no inline hex literals).
// Visual DNA fields are read by renderers — no hardcoding there.
// Future fields (HeatStrength, SparkDensity, etc.) can be added
// here without touching any renderer.
// ══════════════════════════════════════════════════════════════════
internal sealed record EmblemAnimProfile(
    Color PrimaryGlow,
    Color SecondaryGlow,
    float GlowAlpha,         // 0–1 (calm 80 %)
    float PulseAmplitude,    // 0–1 subtle breathing
    float Speed,             // breathing cycles / second
    string BehaviorId,       // matches IEmblemBehaviorRenderer.BehaviorId
    // ── Visual DNA seed fields (Phase 2.5-B foundation) ────────────
    float GlowStrength,      // multiplier on glow radius (1.0 = default)
    float HeatStrength,      // warmth overlay intensity — used by Dragon/Lion
    float ReflectionStrength,// metallic surface reflection — used by Shield/Crown
    float SmokeDensity,      // smoke wisp density — used by Dragon
    float EyeGlow,           // eye highlight intensity — used by Dragon/Eagle
    float PulseStrength)     // secondary-ring pulse weight — used by Lion/Wolf
{
    // ── Canonical AssetId → EmblemType (no string sniffing) ─────────
    // Source of truth: TeamAssetPayloadCatalog canonical IDs.
    private static EmblemType ResolveType(string? assetId)
    {
        var id = assetId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id)) return EmblemType.Shield;

        // Exact canonical match first
        if (string.Equals(id, "team-emblem-dragon-3d", StringComparison.OrdinalIgnoreCase)) return EmblemType.Dragon;
        if (string.Equals(id, "team-emblem-lion-3d",   StringComparison.OrdinalIgnoreCase)) return EmblemType.Lion;
        if (string.Equals(id, "team-emblem-eagle-3d",  StringComparison.OrdinalIgnoreCase)) return EmblemType.Eagle;
        if (string.Equals(id, "team-emblem-wolf-3d",   StringComparison.OrdinalIgnoreCase)) return EmblemType.Wolf;
        if (string.Equals(id, "team-emblem-crown-3d",  StringComparison.OrdinalIgnoreCase)) return EmblemType.Crown;
        if (string.Equals(id, "team-emblem-shield-3d", StringComparison.OrdinalIgnoreCase)) return EmblemType.Shield;

        // Alias / filename fallback (dragon_3d.png, lion-3d, etc.)
        // Uses catalog normalization aliases — still exact key, not substring.
        var norm = id.ToLowerInvariant();
        if (norm is "dragon_3d" or "dragon_3d.png" or "emblem-dragon-3d" or "dragon-3d") return EmblemType.Dragon;
        if (norm is "lion_3d"   or "lion_3d.png"   or "emblem-lion-3d"   or "lion-3d")   return EmblemType.Lion;
        if (norm is "eagle_3d"  or "eagle_3d.png"  or "emblem-eagle-3d"  or "eagle-3d")  return EmblemType.Eagle;
        if (norm is "wolf_3d"   or "wolf_3d.png"   or "emblem-wolf-3d"   or "wolf-3d")   return EmblemType.Wolf;
        if (norm is "crown_3d"  or "crown_3d.png"  or "emblem-crown-3d"  or "crown-3d")  return EmblemType.Crown;
        if (norm is "shield_3d" or "shield_3d.png" or "emblem-shield-3d" or "shield-3d") return EmblemType.Shield;

        return EmblemType.Shield; // safe default
    }

    public static EmblemAnimProfile For(string? emblemAssetId)
    {
        return ResolveType(emblemAssetId) switch
        {
            EmblemType.Dragon => new(
                EmblemVisualPalette.DragonPrimary, EmblemVisualPalette.DragonSecondary,
                0.24f, 0.14f, 0.72f, "FireBreath",
                GlowStrength: 1.10f, HeatStrength: 0.70f, ReflectionStrength: 0.0f,
                SmokeDensity: 0.65f, EyeGlow: 0.80f, PulseStrength: 0.55f),

            EmblemType.Lion => new(
                EmblemVisualPalette.LionPrimary, EmblemVisualPalette.LionSecondary,
                0.22f, 0.11f, 0.65f, "Roar",
                GlowStrength: 1.05f, HeatStrength: 0.45f, ReflectionStrength: 0.10f,
                SmokeDensity: 0.0f,  EyeGlow: 0.0f,  PulseStrength: 0.80f),

            EmblemType.Eagle => new(
                EmblemVisualPalette.EaglePrimary, EmblemVisualPalette.EagleSecondary,
                0.20f, 0.10f, 0.82f, "WingPulse",
                GlowStrength: 1.00f, HeatStrength: 0.0f,  ReflectionStrength: 0.20f,
                SmokeDensity: 0.0f,  EyeGlow: 0.60f, PulseStrength: 0.40f),

            EmblemType.Wolf => new(
                EmblemVisualPalette.WolfPrimary, EmblemVisualPalette.WolfSecondary,
                0.19f, 0.10f, 0.76f, "FrostBreath",
                GlowStrength: 0.95f, HeatStrength: 0.0f,  ReflectionStrength: 0.30f,
                SmokeDensity: 0.0f,  EyeGlow: 0.0f,  PulseStrength: 0.75f),

            EmblemType.Crown => new(
                EmblemVisualPalette.CrownPrimary, EmblemVisualPalette.CrownSecondary,
                0.26f, 0.14f, 0.62f, "RoyalSparkle",
                GlowStrength: 1.15f, HeatStrength: 0.0f,  ReflectionStrength: 0.60f,
                SmokeDensity: 0.0f,  EyeGlow: 0.0f,  PulseStrength: 0.50f),

            _ => new( // Shield + unknown
                EmblemVisualPalette.ShieldPrimary, EmblemVisualPalette.ShieldSecondary,
                0.18f, 0.08f, 0.68f, "DefensivePulse",
                GlowStrength: 0.90f, HeatStrength: 0.0f,  ReflectionStrength: 0.80f,
                SmokeDensity: 0.0f,  EyeGlow: 0.0f,  PulseStrength: 0.30f),
        };
    }
}

// ══════════════════════════════════════════════════════════════════
// IEmblemBehaviorRenderer  — per-emblem draw contract
// Each renderer handles only its own emblem personality.
// No allocations inside Draw — all math is stack-only floats.
// ══════════════════════════════════════════════════════════════════
internal interface IEmblemBehaviorRenderer
{
    string BehaviorId { get; }

    // phase    = elapsed * profile.Speed (pre-computed by caller)
    // sin/sinH = pre-computed sin values to avoid redundant trig in caller
    void Draw(ICanvas canvas, float cx, float cy, float r,
              EmblemAnimProfile profile, float breath, float sin, float sinHalf);
}

// ══════════════════════════════════════════════════════════════════
// EmblemBehaviorRendererResolver  — zero-allocation static lookup
// Array search over 6 entries is cheaper than Dictionary<> alloc.
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

    public static IEmblemBehaviorRenderer Resolve(string behaviorId)
    {
        foreach (var r in _all)
            if (r.BehaviorId == behaviorId) return r;
        return _default;
    }
}

// ══════════════════════════════════════════════════════════════════
// Per-emblem renderer implementations
// Foundation only — heavy FutureBehaviors (FireBreath, Roar, etc.)
// are NOT implemented here; they are registered as BehaviorId only.
// ══════════════════════════════════════════════════════════════════

// ── Dragon: smoke wisps + warm eye-glow hint ─────────────────────
// Future: FireBreath full (NOT implemented)
internal sealed class DragonEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "FireBreath";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float smokeR = r * 0.22f * p.SmokeDensity * breath;
        float absSin = MathF.Abs(sin);

        // wisp 1 — up-left drift
        c.FillColor = EmblemVisualPalette.DragonSmoke1.WithAlpha(0.10f + 0.06f * absSin);
        c.FillCircle(cx - r * 0.35f, cy - r * (0.80f + 0.12f * sin), smokeR);

        // wisp 2 — up-right
        c.FillColor = EmblemVisualPalette.DragonSmoke2.WithAlpha(0.08f + 0.05f * absSin);
        c.FillCircle(cx + r * 0.20f, cy - r * (0.70f + 0.09f * sin), smokeR * 0.75f);

        // eye-glow hint — intensity driven by EyeGlow DNA field
        c.FillColor = EmblemVisualPalette.DragonEyeGlow.WithAlpha((0.28f + 0.18f * absSin) * p.EyeGlow);
        c.FillCircle(cx, cy - r * 0.32f, r * 0.09f * breath);
    }
}

// ── Lion: dignity pulse ring + warm top sheen ────────────────────
// Future: Roar (NOT implemented)
internal sealed class LionEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "Roar";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float absHalf = MathF.Abs(sinHalf);
        // PulseStrength DNA field drives dignity ring weight
        float dignityR = r * (1.55f + 0.18f * absHalf * p.PulseStrength) * breath;

        c.StrokeColor = p.SecondaryGlow.WithAlpha(0.13f + 0.10f * (1f - absHalf));
        c.StrokeSize = 1.2f;
        c.DrawCircle(cx, cy, dignityR);

        float sheenR = r * 0.28f;
        c.FillColor = EmblemVisualPalette.LionSheen.WithAlpha((0.07f + 0.05f * absHalf) * p.HeatStrength);
        c.FillEllipse(cx - sheenR * 0.6f, cy - r * 0.38f, sheenR * 1.2f, sheenR * 0.7f);
    }
}

// ── Eagle: shimmer streak + eye glint ────────────────────────────
// Future: WingPulse full (NOT implemented)
internal sealed class EagleEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "WingPulse";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float absSin = MathF.Abs(sin);
        // ReflectionStrength drives shimmer intensity
        float streakAlpha = (0.15f + 0.12f * absSin) * p.ReflectionStrength;
        float streakW = r * (1.60f + 0.20f * absSin) * breath;
        float streakH = r * 0.16f;

        c.FillColor = EmblemVisualPalette.EagleStreak.WithAlpha(streakAlpha);
        c.FillEllipse(cx - streakW * 0.5f, cy - streakH * 0.5f, streakW, streakH);

        // EyeGlow DNA field drives glint intensity
        c.FillColor = EmblemVisualPalette.EagleGlint.WithAlpha((0.35f + 0.20f * absSin) * p.EyeGlow);
        c.FillCircle(cx, cy - r * 0.30f, r * 0.08f * breath);
    }
}

// ── Wolf: frost halo + icy glint ─────────────────────────────────
// Future: FrostBreath full (NOT implemented)
internal sealed class WolfEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "FrostBreath";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float absHalf = MathF.Abs(sinHalf);
        // PulseStrength DNA field scales frost ring radius
        float frostR = r * (1.50f + 0.14f * absHalf * p.PulseStrength) * breath;

        c.StrokeColor = EmblemVisualPalette.WolfFrost.WithAlpha(0.16f + 0.10f * (1f - absHalf));
        c.StrokeSize = 1.8f;
        c.DrawCircle(cx, cy, frostR);

        // ReflectionStrength DNA field drives icy glint
        float glintW = r * 0.30f * breath;
        c.FillColor = EmblemVisualPalette.WolfIceGlint.WithAlpha((0.18f + 0.10f * absHalf) * p.ReflectionStrength);
        c.FillEllipse(cx + r * 0.15f, cy - r * 0.42f, glintW, r * 0.10f);
    }
}

// ── Crown: sparkle dots + royal cross-shine ──────────────────────
// Future: RoyalSparkle advanced (NOT implemented)
internal sealed class CrownEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "RoyalSparkle";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float absSin = MathF.Abs(sin);
        float dotR = r * 0.07f * breath;
        float dist = r * 1.52f;
        // ReflectionStrength DNA field scales sparkle brightness
        float twinkle = (0.20f + 0.18f * absSin) * p.ReflectionStrength;

        c.FillColor = EmblemVisualPalette.CrownSparkle.WithAlpha(twinkle);
        c.FillCircle(cx, cy - dist, dotR);
        c.FillCircle(cx + dist, cy, dotR * 0.85f);
        c.FillCircle(cx, cy + dist * 0.80f, dotR * 0.70f);
        c.FillCircle(cx - dist, cy, dotR * 0.85f);

        float shineLen = r * 0.50f;
        float shineAlpha = (0.09f + 0.07f * absSin) * p.ReflectionStrength;
        c.StrokeColor = p.PrimaryGlow.WithAlpha(shineAlpha);
        c.StrokeSize = 1.0f;
        c.DrawLine(cx - shineLen, cy, cx + shineLen, cy);
        c.DrawLine(cx, cy - shineLen * 0.70f, cx, cy + shineLen * 0.70f);
    }
}

// ── Shield: metallic arc + top sheen ─────────────────────────────
// Future: DefensivePulse advanced (NOT implemented)
internal sealed class ShieldEmblemBehaviorRenderer : IEmblemBehaviorRenderer
{
    public string BehaviorId => "DefensivePulse";

    public void Draw(ICanvas c, float cx, float cy, float r,
                     EmblemAnimProfile p, float breath, float sin, float sinHalf)
    {
        float absHalf = MathF.Abs(sinHalf);
        float arcR = r * 1.08f * breath;
        // ReflectionStrength DNA field drives metallic arc brightness
        float arcAlpha = (0.18f + 0.12f * absHalf) * p.ReflectionStrength;

        c.StrokeColor = EmblemVisualPalette.ShieldArc.WithAlpha(arcAlpha);
        c.StrokeSize = 2.2f;
        c.DrawArc(cx - arcR, cy - arcR, arcR * 2f, arcR * 2f, 120, 80, false, false);

        float sheenW = r * 0.90f * breath;
        c.FillColor = EmblemVisualPalette.ShieldSheen.WithAlpha((0.09f + 0.07f * absHalf) * p.ReflectionStrength);
        c.FillEllipse(cx - sheenW * 0.5f, cy - r * 0.28f, sheenW, r * 0.22f);
    }
}

// ══════════════════════════════════════════════════════════════════
// LivingEmblemDrawable  — thin orchestrator
// Responsibilities: universal glow layer + delegate to renderer.
// No allocations inside Draw loop.
// ══════════════════════════════════════════════════════════════════
internal sealed class LivingEmblemDrawable : IDrawable
{
    public EmblemAnimProfile? Profile { get; set; }
    public float Phase { get; set; }

    private IEmblemBehaviorRenderer _renderer = EmblemBehaviorRendererResolver.Resolve("DefensivePulse");

    public void SetRenderer(string behaviorId)
        => _renderer = EmblemBehaviorRendererResolver.Resolve(behaviorId);

    public void Draw(ICanvas canvas, RectF rect)
    {
        var p = Profile;
        if (p == null || rect.Width <= 1) return;

        canvas.SaveState();

        float cx = rect.Center.X;
        float cy = rect.Center.Y;
        // GlowStrength DNA field scales glow radius
        float r = MathF.Min(rect.Width, rect.Height) * 0.36f * p.GlowStrength;
        float sin = MathF.Sin(Phase * MathF.Tau);
        float sinHalf = MathF.Sin(Phase * MathF.Tau * 0.5f);
        float breath = 1f + p.PulseAmplitude * sin;
        float outerR = r * 1.30f * breath;
        float innerR = r * 1.04f * breath;

        // ── Layer stack (rendered bottom → top) ──────────────────────
        // Current layers active in this engine:
        //   [0] Shadow          — TODO Phase 2.5-C (VisualLayerType.Shadow)
        //   [1] BackgroundAura  — TODO Phase 2.5-C (VisualLayerType.BackgroundAura)
        //   [2] AmbientSmoke    — partial: Dragon smoke wisps below
        //   [3] BaseImage       — rendered by MAUI Image above this GraphicsView
        //   [4] HeatDistortion  — TODO Phase 2.5-C (VisualLayerType.HeatDistortion)
        //   [5] Fire            — Future: FireBreath (VisualLayerType.Fire)
        //   [6] Particles       — Future: RoyalSparkle advanced (VisualLayerType.Particles)
        //   [7] Glow            — ACTIVE below ↓
        //   [8] UIOverlay       — handled by ZIndex above this view

        // ── [7] Glow: universal breathing aura ───────────────────────
        canvas.FillColor = p.PrimaryGlow.WithAlpha(p.GlowAlpha * 0.42f);
        canvas.FillCircle(cx, cy, outerR);

        canvas.FillColor = p.PrimaryGlow.WithAlpha(p.GlowAlpha * 0.72f);
        canvas.FillCircle(cx, cy, innerR);

        canvas.StrokeColor = p.SecondaryGlow.WithAlpha(
            p.GlowAlpha * 0.88f * (0.68f + 0.32f * MathF.Abs(sinHalf)));
        canvas.StrokeSize = 1.4f;
        canvas.DrawCircle(cx, cy, innerR);

        // ── [2]+[6] Per-emblem personality (smoke, sparkle, frost…) ──
        _renderer.Draw(canvas, cx, cy, r, p, breath, sin, sinHalf);

        canvas.RestoreState();
    }
}

// ──────────────────────────────────────────────────────────────────
// LivingTeamEmblemView  — self-animating GraphicsView overlay
// Wraps around any existing Image emblem. No XAML required.
// ──────────────────────────────────────────────────────────────────
public sealed class LivingTeamEmblemView : GraphicsView
{
    private readonly LivingEmblemDrawable _drawable = new();
    private bool _running;
    private long _started;

    public LivingTeamEmblemView()
    {
        Drawable = _drawable;
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
        IsVisible = false;
        Loaded += (_, _) => StartIfReady();
        Unloaded += (_, _) => _running = false;
    }

    public void SetEmblem(string? emblemAssetId)
    {
        var profile = EmblemAnimProfile.For(emblemAssetId);

        if (_drawable.Profile?.BehaviorId == profile.BehaviorId &&
            string.Equals(_drawable.Profile?.PrimaryGlow.ToHex(), profile.PrimaryGlow.ToHex()))
            return;

        _drawable.Profile = profile;
        _drawable.SetRenderer(profile.BehaviorId);
        _drawable.Phase = 0f;
        _started = Environment.TickCount64;
        IsVisible = true;
        StartIfReady();
        Invalidate();
    }

    public void ClearEmblem()
    {
        _running = false;
        _drawable.Profile = null;
        IsVisible = false;
        Invalidate();
    }

    private void StartIfReady()
    {
        if (_running || _drawable.Profile == null || !IsLoaded)
            return;

        _running = true;

        // Frame rate is resolved from DeviceProfiler at start time (EmblemPerformanceSettings).
        // VeryLite=10fps, Lite=12fps, Medium=15fps, High=20fps, Ultra=25fps
        Dispatcher.StartTimer(EmblemPerformanceSettings.GetTimerInterval(), () =>
        {
            if (!_running || _drawable.Profile == null || !IsLoaded)
                return false;

            var elapsed = (Environment.TickCount64 - _started) / 1000f;
            _drawable.Phase = elapsed * _drawable.Profile.Speed;
            Invalidate();
            return true;
        });
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
            var identity = await TeamIdentityResolver.ResolveAsync(teamId);
            var emblemAssetId = identity.EmblemAssetId;
            await MainThread.InvokeOnMainThreadAsync(() => ApplyOrCreate(image, emblemAssetId));
        }
        catch { /* identity resolution failed — skip animation */ }
    }

    private static void ApplyOrCreate(Image image, string? emblemAssetId)
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
        holder.View.SetEmblem(emblemAssetId);
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

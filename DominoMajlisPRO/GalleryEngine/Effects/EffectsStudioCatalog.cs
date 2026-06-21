using DominoMajlisPRO.GalleryEngine.Admin.Canonical;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class EffectsStudioCatalog
{
    public static IReadOnlyList<CanonicalOption> Presets() =>
        EffectPresetCatalog.Presets
            .Select(item => new CanonicalOption(
                item.PresetId.ToString(),
                $"{item.ArabicDisplayName} / {item.DisplayName}"))
            .ToList();

    public static IReadOnlyList<CanonicalOption> Animations() =>
        Enum.GetValues<EffectAnimationId>()
            .Select(item => new CanonicalOption(
                item.ToString(),
                AnimationDisplayName(item)))
            .ToList();

    public static IReadOnlyList<CanonicalOption> ColorPresets() =>
        EffectPresetCatalog.Colors
            .Select(item => new CanonicalOption(
                item.ColorPresetId.ToString(),
                $"{item.ArabicDisplayName} / {item.DisplayName}"))
            .ToList();

    public static IReadOnlyList<CanonicalOption> Layers() =>
        Enum.GetValues<EffectLayerId>()
            .Select(item => new CanonicalOption(
                item.ToString(),
                LayerDisplayName(item)))
            .ToList();

    public static IReadOnlyList<CanonicalOption> OwnerScopes() =>
        Enum.GetValues<EffectOwnerScope>()
            .Select(item => new CanonicalOption(
                item.ToString(),
                OwnerScopeDisplayName(item)))
            .ToList();

    public static IReadOnlyList<CanonicalOption> EquipTargets() =>
    [
        new("PlayerEffect", "مؤثر اللاعب"),
        new("TeamEffect", "مؤثر الفريق"),
        new("GlobalEffect", "مؤثر عام")
    ];

    static string AnimationDisplayName(EffectAnimationId animationId) =>
        animationId switch
        {
            EffectAnimationId.None => "بدون حركة / None",
            EffectAnimationId.Pulse => "نبض / Pulse",
            EffectAnimationId.Rotate => "دوران / Rotate",
            EffectAnimationId.Fade => "تلاشي / Fade",
            EffectAnimationId.Flash => "وميض / Flash",
            EffectAnimationId.Orbit => "مدار / Orbit",
            EffectAnimationId.Breathing => "تنفس / Breathing",
            EffectAnimationId.Lightning => "برق / Lightning",
            _ => animationId.ToString()
        };

    static string LayerDisplayName(EffectLayerId layerId) =>
        layerId switch
        {
            EffectLayerId.Glow => "توهج / Glow",
            EffectLayerId.Aura => "هالة / Aura",
            EffectLayerId.Ring => "حلقة / Ring",
            EffectLayerId.Border => "حد / Border",
            EffectLayerId.Pulse => "نبض / Pulse",
            EffectLayerId.Particle => "جسيمات / Particles",
            EffectLayerId.Shadow => "ظل / Shadow",
            _ => layerId.ToString()
        };

    static string OwnerScopeDisplayName(EffectOwnerScope ownerScope) =>
        ownerScope switch
        {
            EffectOwnerScope.Player => "اللاعب / Player",
            EffectOwnerScope.Team => "الفريق / Team",
            EffectOwnerScope.Global => "عام / Global",
            _ => ownerScope.ToString()
        };
}

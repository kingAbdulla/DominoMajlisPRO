using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Performance;

public enum LivingVisualLod
{
    StaticOnly,
    VeryLite,
    Lite,
    Medium,
    High,
    Ultra
}

public sealed class LivingVisualPerformanceDecision
{
    public LivingVisualLod Lod { get; set; } = LivingVisualLod.StaticOnly;
    public bool ShouldUseStaticFallback { get; set; } = true;
    public bool ShouldPause { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class LivingVisualPerformanceService
{
    public LivingVisualPerformanceDecision Decide(
        string? deviceProfile,
        bool isOffscreen,
        LivingRendererBackend backend,
        bool backendAvailable)
    {
        if (isOffscreen)
        {
            return new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.StaticOnly,
                ShouldUseStaticFallback = true,
                ShouldPause = true,
                Reason = "Host is offscreen."
            };
        }

        if (!backendAvailable || backend is LivingRendererBackend.None or LivingRendererBackend.StaticFallback)
        {
            return new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.StaticOnly,
                ShouldUseStaticFallback = true,
                Reason = "Living backend is unavailable or static-only."
            };
        }

        var normalized = (deviceProfile ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "weak" or "low" or "verylite" => new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.VeryLite,
                ShouldUseStaticFallback = false,
                Reason = "Weak device profile selected very-lite rendering."
            },
            "lite" => new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.Lite,
                ShouldUseStaticFallback = false,
                Reason = "Lite device profile selected lite rendering."
            },
            "high" => new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.High,
                ShouldUseStaticFallback = false,
                Reason = "High device profile selected high rendering."
            },
            "ultra" => new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.Ultra,
                ShouldUseStaticFallback = false,
                Reason = "Ultra device profile selected ultra rendering."
            },
            _ => new LivingVisualPerformanceDecision
            {
                Lod = LivingVisualLod.Medium,
                ShouldUseStaticFallback = false,
                Reason = "Unknown device profile selected medium rendering."
            }
        };
    }
}

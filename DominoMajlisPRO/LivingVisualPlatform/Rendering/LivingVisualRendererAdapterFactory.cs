using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class LivingVisualRendererAdapterFactory : ILivingVisualRendererAdapterFactory
{
    public bool IsBackendAvailable(LivingRendererBackend backend)
    {
        return backend is LivingRendererBackend.StaticFallback or LivingRendererBackend.None;
    }

    public ILivingVisualRendererAdapter CreateAdapter(LivingRendererBackend backend)
    {
        return backend switch
        {
            LivingRendererBackend.StaticFallback or LivingRendererBackend.None => new StaticFallbackLivingRendererAdapter(),
            _ => throw new NotSupportedException($"Renderer backend '{backend}' is not available in this foundation stage.")
        };
    }
}

using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class LivingVisualRendererAdapterFactory : ILivingVisualRendererAdapterFactory
{
    public bool IsBackendAvailable(LivingRendererBackend backend)
    {
        return backend is LivingRendererBackend.StaticFallback or LivingRendererBackend.None
#if ANDROID
            or LivingRendererBackend.Filament
#endif
            ;
    }

    public ILivingVisualRendererAdapter CreateAdapter(LivingRendererBackend backend)
    {
        return backend switch
        {
            LivingRendererBackend.StaticFallback or LivingRendererBackend.None => new StaticFallbackLivingRendererAdapter(),
#if ANDROID
            LivingRendererBackend.Filament => new FilamentLivingVisualRendererAdapter(),
#endif
            _ => throw new NotSupportedException($"Renderer backend '{backend}' is not available in this foundation stage.")
        };
    }
}

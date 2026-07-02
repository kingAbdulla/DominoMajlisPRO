using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public interface ILivingVisualRendererAdapterFactory
{
    bool IsBackendAvailable(LivingRendererBackend backend);
    ILivingVisualRendererAdapter CreateAdapter(LivingRendererBackend backend);
}

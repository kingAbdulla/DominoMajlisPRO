#if ANDROID
using DominoMajlisPRO.LivingVisualPlatform.Rendering;
using Microsoft.Maui.Handlers;

namespace DominoMajlisPRO.Platforms.Android;

public sealed class FilamentLivingVisualViewHandler :
    ViewHandler<FilamentLivingVisualView, FilamentRenderSurfaceView>
{
    public static readonly IPropertyMapper<FilamentLivingVisualView, FilamentLivingVisualViewHandler> Mapper =
        new PropertyMapper<FilamentLivingVisualView, FilamentLivingVisualViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(FilamentLivingVisualView.AssetPath)] = MapAssetPath,
            [nameof(FilamentLivingVisualView.IsPaused)] = MapIsPaused
        };

    public FilamentLivingVisualViewHandler()
        : base(Mapper)
    {
    }

    protected override FilamentRenderSurfaceView CreatePlatformView() =>
        new(Context);

    protected override void ConnectHandler(FilamentRenderSurfaceView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SetAssetPath(VirtualView.AssetPath);
        platformView.SetPaused(VirtualView.IsPaused);
    }

    protected override void DisconnectHandler(FilamentRenderSurfaceView platformView)
    {
        platformView.DisposeRenderer();
        base.DisconnectHandler(platformView);
    }

    private static void MapAssetPath(
        FilamentLivingVisualViewHandler handler,
        FilamentLivingVisualView view)
    {
        handler.PlatformView.SetAssetPath(view.AssetPath);
    }

    private static void MapIsPaused(
        FilamentLivingVisualViewHandler handler,
        FilamentLivingVisualView view)
    {
        handler.PlatformView.SetPaused(view.IsPaused);
    }
}
#endif

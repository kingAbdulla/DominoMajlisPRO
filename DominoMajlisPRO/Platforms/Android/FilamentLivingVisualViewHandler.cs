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
            [nameof(FilamentLivingVisualView.IsPaused)] = MapIsPaused,
            [nameof(FilamentLivingVisualView.LastMotionCommandVersion)] = MapLastMotionCommand,
            [nameof(FilamentLivingVisualView.LastTouchStimulusVersion)] = MapLastTouchStimulus
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
        platformView.SetMotionCommand(VirtualView.LastMotionCommand);
        platformView.SetTouchStimulus(VirtualView.LastTouchStimulus);
    }

    protected override void DisconnectHandler(FilamentRenderSurfaceView platformView)
    {
        platformView.DisposeRenderer();
        base.DisconnectHandler(platformView);
    }

    private static void MapAssetPath(FilamentLivingVisualViewHandler handler, FilamentLivingVisualView view)
    {
        handler.PlatformView.SetAssetPath(view.AssetPath);
    }

    private static void MapIsPaused(FilamentLivingVisualViewHandler handler, FilamentLivingVisualView view)
    {
        handler.PlatformView.SetPaused(view.IsPaused);
    }

    private static void MapLastMotionCommand(FilamentLivingVisualViewHandler handler, FilamentLivingVisualView view)
    {
        handler.PlatformView.SetMotionCommand(view.LastMotionCommand);
    }

    private static void MapLastTouchStimulus(FilamentLivingVisualViewHandler handler, FilamentLivingVisualView view)
    {
        handler.PlatformView.SetTouchStimulus(view.LastTouchStimulus);
    }
}
#endif

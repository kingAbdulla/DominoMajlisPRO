using Microsoft.Extensions.Logging;
#if ANDROID
using DominoMajlisPRO.LivingVisualPlatform.Rendering;
using DominoMajlisPRO.Platforms.Android;
#endif
namespace DominoMajlisPRO
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<FilamentLivingVisualView, FilamentLivingVisualViewHandler>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("BAUHS93.TTF","BAUHS93");
                    fonts.AddFont("CinzelDecorative-Bold.ttf", "CinzelDecorative-Bold");
                    fonts.AddFont("HARLOWSI.TTF", "HARLOWSI");
                    fonts.AddFont("Tajawal-Regular.ttf", "Tajawal-Regular");
                    fonts.AddFont("FS_Cairo.ttf", "FS_Cairo");
                    fonts.AddFont("timesbi.ttf", "timesbi");
                    fonts.AddFont("NotoNaskhArabic-VariableFont_wght.ttf", "NotoNaskhArabic-VariableFont_wght");
                    fonts.AddFont("DG-Nemr-V.0.ttf", "DG-Nemr-V.0");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

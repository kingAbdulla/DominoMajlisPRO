using Microsoft.Extensions.Logging;
namespace DominoMajlisPRO
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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

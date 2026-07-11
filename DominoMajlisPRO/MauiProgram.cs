using DominoMajlisPRO.Cloud;
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
                    fonts.AddFont("BAUHS93.TTF", "BAUHS93");
                    fonts.AddFont("CinzelDecorative-Bold.ttf", "CinzelDecorative-Bold");
                    fonts.AddFont("HARLOWSI.TTF", "HARLOWSI");
                    fonts.AddFont("Tajawal-Regular.ttf", "Tajawal-Regular");
                    fonts.AddFont("FS_Cairo.ttf", "FS_Cairo");
                    fonts.AddFont("timesbi.ttf", "timesbi");
                    fonts.AddFont("NotoNaskhArabic-VariableFont_wght.ttf", "NotoNaskhArabic-VariableFont_wght");
                    fonts.AddFont("DG-Nemr-V.0.ttf", "DG-Nemr-V.0");
                });

            var cloudOptions = new CloudApiOptions();
            builder.Services.AddSingleton(cloudOptions);
            builder.Services.AddSingleton<CloudSessionStore>();
            builder.Services.AddHttpClient<CloudApiClient>(client =>
            {
                client.BaseAddress = new Uri(cloudOptions.BaseUrl, UriKind.Absolute);
                client.Timeout = cloudOptions.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DominoMajlisPRO/1.0");
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

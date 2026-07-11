using DominoMajlisPRO.Cloud;
using Microsoft.Extensions.DependencyInjection;
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
            builder.Services.AddSingleton<CloudSyncStateStore>();
            builder.Services.AddSingleton(_ =>
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(cloudOptions.BaseUrl, UriKind.Absolute),
                    Timeout = cloudOptions.Timeout
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DominoMajlisPRO/1.0");
                return client;
            });
            builder.Services.AddSingleton<CloudApiClient>();
            builder.Services.AddSingleton<CloudSyncCoordinator>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            var client = app.Services.GetRequiredService<CloudApiClient>();
            var stateStore = app.Services.GetRequiredService<CloudSyncStateStore>();
            var coordinator = app.Services.GetRequiredService<CloudSyncCoordinator>();
            CloudSyncRuntime.Configure(client, stateStore, coordinator);
            coordinator.StartBackgroundSync();
            return app;
        }
    }
}

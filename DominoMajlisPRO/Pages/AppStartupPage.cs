using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public sealed class AppStartupPage : ContentPage
{
    bool routed;

    public AppStartupPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.Black;
        FlowDirection = FlowDirection.RightToLeft;

        Content = new Grid
        {
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#020202"), 0),
                    new(Color.FromArgb("#151006"), 0.60f),
                    new(Color.FromArgb("#050505"), 1)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "DOMINO MAJLIS PRO",
                            TextColor = Color.FromArgb("#D4AF37"),
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            Color = Color.FromArgb("#D4AF37")
                        }
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (routed)
            return;

        routed = true;
        await RouteAsync();
    }

    static async Task RouteAsync()
    {
        bool hasActiveSession = await StartupSessionRouterService.HasActiveRegisteredSessionAsync();

        Application.Current!.MainPage = hasActiveSession
            ? new NavigationPage(new MainPage())
            : new NavigationPage(new PremiumAuthPage());
    }
}

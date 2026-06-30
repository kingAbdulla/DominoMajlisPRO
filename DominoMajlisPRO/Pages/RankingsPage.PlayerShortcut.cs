using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class RankingsPage
{
    private bool _playerRankingsShortcutAttached;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AttachPlayerRankingsShortcut();
    }

    private void AttachPlayerRankingsShortcut()
    {
        if (_playerRankingsShortcutAttached || LeaderboardContainer == null)
            return;

        _playerRankingsShortcutAttached = true;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        grid.Add(new Image
        {
            Source = "rankings_gold.png",
            WidthRequest = 28,
            HeightRequest = 28,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        grid.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label
                {
                    Text = "Player Rankings",
                    TextColor = Color.FromArgb("#FFD700"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1
                },
                new Label
                {
                    Text = "Open player rank page",
                    TextColor = Color.FromArgb("#B8A77D"),
                    FontSize = 11,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1
                }
            }
        }, 1, 0);

        grid.Add(new Label
        {
            Text = ">",
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center
        }, 2, 0);

        var shortcut = new Border
        {
            BackgroundColor = Color.FromArgb("#17110A"),
            Stroke = Color.FromArgb("#D4AF37"),
            StrokeThickness = 1.4,
            Padding = new Thickness(14, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = grid
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Navigation.PushAsync(new PlayerRankingsPage());
        shortcut.GestureRecognizers.Add(tap);

        LeaderboardContainer.Children.Insert(0, shortcut);
    }
}

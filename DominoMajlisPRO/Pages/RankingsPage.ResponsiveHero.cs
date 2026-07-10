using System.Runtime.CompilerServices;
using DominoMajlisPRO.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class RankingsPage
{
    const string ResponsiveHeroAutomationId = "RankingsResponsiveHeroV2";

    internal void EnsureResponsiveChampionHero()
    {
        if (PageContent.Children.Count < 2)
            return;

        if (PageContent.Children[1] is View current &&
            string.Equals(current.AutomationId, ResponsiveHeroAutomationId, StringComparison.Ordinal))
        {
            return;
        }

        var replacement = BuildResponsiveChampionHero();
        PageContent.Children.RemoveAt(1);
        PageContent.Children.Insert(1, replacement);
    }

    View BuildResponsiveChampionHero()
    {
        var slide = CurrentSlide();
        var champion = snapshot.Champion;
        var reward = snapshot.ChampionNextReward;
        bool isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        var root = Card("#E60A0D10", "#B37A25", 0, 16);
        root.AutomationId = ResponsiveHeroAutomationId;
        root.Padding = 0;
        root.HeightRequest = isPhone ? 362 : 390;
        root.MinimumHeightRequest = isPhone ? 362 : 390;

        var layers = new Grid();
        heroSlideImage = new Image
        {
            Source = ResolveSource(slide.ImagePath, "season_reward_gold.png"),
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        layers.Children.Add(heroSlideImage);
        layers.Children.Add(new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#E8050608"), 0f),
                    new GradientStop(Color.FromArgb("#B8050608"), 0.55f),
                    new GradientStop(Color.FromArgb("#E0050608"), 1f)
                }
            }
        });

        var content = new Grid
        {
            Padding = new Thickness(isPhone ? 10 : 16),
            RowSpacing = isPhone ? 5 : 8,
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(isPhone ? 30 : 34) },
                new RowDefinition { Height = new GridLength(isPhone ? 38 : 44) },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var heading = ResponsiveLabel("بطل الموسم الحالي", isPhone ? 14 : 18, "#FFFFFF", false, TextAlignment.Center);
        heading.HorizontalOptions = LayoutOptions.Center;
        content.Children.Add(heading);

        var seasonBadge = BuildResponsiveSeasonBadge(isPhone);
        Grid.SetRow(seasonBadge, 0);
        Grid.SetRowSpan(seasonBadge, 2);
        seasonBadge.HorizontalOptions = LayoutOptions.End;
        seasonBadge.VerticalOptions = LayoutOptions.Start;
        seasonBadge.ZIndex = 10;
        content.Children.Add(seasonBadge);

        if (champion != null)
        {
            var teamName = ResponsiveLabel(
                string.IsNullOrWhiteSpace(champion.Team.TeamName) ? "فريق بدون اسم" : champion.Team.TeamName.Trim(),
                isPhone ? 19 : 24,
                "#FFFFFF",
                true,
                TextAlignment.Center);
            teamName.Margin = new Thickness(isPhone ? 74 : 92, 0, isPhone ? 74 : 92, 0);
            Grid.SetRow(teamName, 1);
            content.Children.Add(teamName);

            var identity = new Grid
            {
                FlowDirection = FlowDirection.LeftToRight,
                ColumnSpacing = isPhone ? 7 : 16,
                Padding = new Thickness(isPhone ? 2 : 12, 0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(isPhone ? 116 : 156) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            var player1 = BuildResponsivePlayerIdentity(
                champion.Team.Player1Id,
                champion.Team.Player1,
                isPhone ? 38 : 48,
                isPhone);
            Grid.SetColumn(player1, 0);
            identity.Children.Add(player1);

            var emblemHost = new Grid
            {
                WidthRequest = isPhone ? 112 : 150,
                HeightRequest = isPhone ? 142 : 180,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            emblemHost.Children.Add(TeamEmblem(champion, isPhone ? 106 : 142, false));
            emblemHost.Children.Add(new Border
            {
                WidthRequest = isPhone ? 28 : 34,
                HeightRequest = isPhone ? 28 : 34,
                BackgroundColor = Color.FromArgb("#E00A0B0D"),
                Stroke = Color.FromArgb("#F2C46D"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = isPhone ? 14 : 17 },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Content = ResponsiveLabel("1", isPhone ? 11 : 13, "#FFD98A", true, TextAlignment.Center)
            });
            Grid.SetColumn(emblemHost, 1);
            identity.Children.Add(emblemHost);

            var player2 = BuildResponsivePlayerIdentity(
                champion.Team.Player2Id,
                champion.Team.Player2,
                isPhone ? 38 : 48,
                isPhone);
            Grid.SetColumn(player2, 2);
            identity.Children.Add(player2);

            Grid.SetRow(identity, 2);
            content.Children.Add(identity);

            var rank = CenteredRankTitle(champion.Rank, isPhone ? 42 : 54);
            rank.HorizontalOptions = LayoutOptions.Center;
            rank.VerticalOptions = LayoutOptions.Center;
            Grid.SetRow(rank, 3);
            content.Children.Add(rank);

            var footer = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = isPhone ? 8 : 14,
                VerticalOptions = LayoutOptions.Center
            };

            var progress = BuildHeroWideProgress(champion);
            progress.HorizontalOptions = LayoutOptions.Fill;
            progress.MinimumWidthRequest = 0;
            footer.Children.Add(progress);

            var rewards = BuildResponsiveRewards(reward, isPhone);
            Grid.SetColumn(rewards, 1);
            footer.Children.Add(rewards);

            Grid.SetRow(footer, 4);
            content.Children.Add(footer);
        }
        else
        {
            var empty = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    ResponsiveLabel("موسم التصنيفات", 14, "#F2C46D", true, TextAlignment.Center),
                    ResponsiveLabel(slide.Title, isPhone ? 22 : 28, "#FFFFFF", true, TextAlignment.Center),
                    ResponsiveLabel(slide.Subtitle, 12, "#DEC894", false, TextAlignment.Center)
                }
            };
            Grid.SetRow(empty, 2);
            Grid.SetRowSpan(empty, 3);
            content.Children.Add(empty);
        }

        layers.Children.Add(content);
        root.Content = layers;
        return root;
    }

    Border BuildResponsiveSeasonBadge(bool isPhone)
    {
        return new Border
        {
            WidthRequest = isPhone ? 72 : 88,
            HeightRequest = isPhone ? 72 : 84,
            Margin = new Thickness(0),
            BackgroundColor = Color.FromArgb("#E3070809"),
            Stroke = Color.FromArgb("#8B621F"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = isPhone ? 12 : 14 },
            Padding = new Thickness(4, 3),
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    ResponsiveLabel($"الموسم {snapshot.SeasonNumber}", isPhone ? 9 : 11, "#F2C46D", true, TextAlignment.Center),
                    ResponsiveLabel("ينتهي بعد", isPhone ? 7 : 9, "#DEC894", false, TextAlignment.Center),
                    ResponsiveLabel($"{snapshot.SeasonDaysLeft}", isPhone ? 20 : 24, "#FFFFFF", true, TextAlignment.Center),
                    ResponsiveLabel("يوم", isPhone ? 8 : 10, "#F2C46D", true, TextAlignment.Center)
                }
            }
        };
    }

    View BuildResponsivePlayerIdentity(string? playerId, string? playerName, double avatarSize, bool isPhone)
    {
        string name = string.IsNullOrWhiteSpace(playerName) ? "-" : playerName.Trim();
        var nameLabel = ResponsiveLabel(name, isPhone ? 9 : 12, "#FFFFFF", false, TextAlignment.Center);
        nameLabel.WidthRequest = isPhone ? 74 : 110;

        return new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = isPhone ? 78 : 116,
            Children =
            {
                PlayerAvatar(playerId, name, avatarSize),
                nameLabel
            }
        };
    }

    View BuildResponsiveRewards(RankingRankReward? reward, bool isPhone)
    {
        var values = new HorizontalStackLayout
        {
            FlowDirection = FlowDirection.LeftToRight,
            Spacing = isPhone ? 3 : 6,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = "gems.png", WidthRequest = isPhone ? 16 : 20, HeightRequest = isPhone ? 16 : 20 },
                ResponsiveLabel((reward?.GemsReward ?? 0).ToString("N0"), isPhone ? 9 : 11, "#FFFFFF", true),
                new Image { Source = "coins.png", WidthRequest = isPhone ? 16 : 20, HeightRequest = isPhone ? 16 : 20 },
                ResponsiveLabel((reward?.CoinsReward ?? 0).ToString("N0"), isPhone ? 9 : 11, "#FFFFFF", true)
            }
        };

        var button = TextButton("عرض الجوائز", "gift_gems.png", async (_, _) => await ShowSeasonPrizesAsync());
        button.HeightRequest = isPhone ? 34 : 40;
        button.MinimumWidthRequest = isPhone ? 104 : 132;

        return new VerticalStackLayout
        {
            Spacing = 3,
            WidthRequest = isPhone ? 112 : 142,
            HorizontalOptions = LayoutOptions.End,
            Children = { values, button }
        };
    }

    static Label ResponsiveLabel(
        string? text,
        double size,
        string color,
        bool bold = false,
        TextAlignment alignment = TextAlignment.Start)
    {
        return new Label
        {
            Text = text ?? string.Empty,
            FontSize = size,
            TextColor = Color.FromArgb(color),
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = alignment,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            HorizontalOptions = LayoutOptions.Fill
        };
    }
}

internal static class RankingsResponsiveHeroBootstrap
{
    static readonly Timer RefreshTimer;

    [ModuleInitializer]
    internal static void Initialize()
    {
        RefreshTimer = new Timer(
            _ => MainThread.BeginInvokeOnMainThread(ApplyToActivePage),
            null,
            TimeSpan.FromMilliseconds(400),
            TimeSpan.FromMilliseconds(700));
    }

    static void ApplyToActivePage()
    {
        var page = FindActiveRankingsPage(Application.Current?.MainPage);
        page?.EnsureResponsiveChampionHero();
    }

    static RankingsPage? FindActiveRankingsPage(Page? page)
    {
        if (page is RankingsPage rankings)
            return rankings;

        if (page is NavigationPage navigation)
            return FindActiveRankingsPage(navigation.CurrentPage);

        if (page is FlyoutPage flyout)
            return FindActiveRankingsPage(flyout.Detail);

        if (page is TabbedPage tabbed)
            return FindActiveRankingsPage(tabbed.CurrentPage);

        return null;
    }
}

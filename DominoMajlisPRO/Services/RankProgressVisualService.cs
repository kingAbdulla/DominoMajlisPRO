using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.Services;

/// <summary>
/// Single shared helper for the rank-progress visual used across the app
/// (GamePage constitution): on the right the current rank icon + roman tier,
/// inside the bar only the percentage, on the left the next rank icon + roman
/// tier. There is intentionally no "remaining XP" / "next rank" text and no
/// long rank name. All rank numbers come from <see cref="PlayerRankService"/>
/// so there is never a second rank logic.
/// </summary>
public static class RankProgressVisualService
{
    public sealed class RankProgressVisual
    {
        public string CurrentIcon { get; init; } = "rank_unranked.png";
        public string CurrentTierRoman { get; init; } = "";
        public Color CurrentColor { get; init; } = Colors.Gray;

        public string NextIcon { get; init; } = "rank_unranked.png";
        public string NextTierRoman { get; init; } = "";
        public Color NextColor { get; init; } = Colors.Gray;

        public double Progress { get; init; }
        public int Percent { get; init; }
        public bool IsMaxRank { get; init; }
    }

    public static RankProgressVisual Resolve(int xp)
    {
        var current = PlayerRankService.Calculate(xp);
        var next = PlayerRankService.Calculate(current.NextRankXP);

        bool isMax =
            string.Equals(next.RankBase, current.RankBase, StringComparison.Ordinal) &&
            next.Tier == current.Tier;

        return new RankProgressVisual
        {
            CurrentIcon = current.RankIcon,
            CurrentTierRoman = ToRoman(current.Tier),
            CurrentColor = ParseColor(current.RankColor),
            NextIcon = next.RankIcon,
            NextTierRoman = ToRoman(next.Tier),
            NextColor = ParseColor(next.RankColor),
            Progress = Math.Clamp(current.Progress, 0, 1),
            Percent = (int)Math.Round(Math.Clamp(current.Progress, 0, 1) * 100),
            IsMaxRank = isMax
        };
    }

    /// <summary>
    /// Builds the full progress row: [current icon + tier] [bar + %] [next icon + tier].
    /// Lightweight (static shapes, no animation, no glow).
    /// </summary>
    public static View Build(
        int xp,
        double barHeight = 16,
        double iconSize = 26)
    {
        var visual = Resolve(xp);

        var row = new Grid
        {
            ColumnSpacing = 8,
            VerticalOptions = LayoutOptions.Center,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Right side (RTL start): current rank.
        var currentSide = BuildRankBadge(
            visual.CurrentIcon,
            visual.CurrentTierRoman,
            visual.CurrentColor,
            iconSize);
        Grid.SetColumn(currentSide, 0);
        row.Children.Add(currentSide);

        var bar = BuildBar(visual, barHeight);
        Grid.SetColumn(bar, 1);
        row.Children.Add(bar);

        // Left side: next rank. At max rank we keep the slot but dim it.
        var nextSide = BuildRankBadge(
            visual.NextIcon,
            visual.NextTierRoman,
            visual.NextColor,
            iconSize);
        nextSide.Opacity = visual.IsMaxRank ? 0.35 : 1;
        Grid.SetColumn(nextSide, 2);
        row.Children.Add(nextSide);

        return row;
    }

    static View BuildRankBadge(
        string icon,
        string tierRoman,
        Color color,
        double iconSize)
    {
        var stack = new HorizontalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center
        };

        stack.Children.Add(new Image
        {
            Source = icon,
            WidthRequest = iconSize,
            HeightRequest = iconSize,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center
        });

        if (!string.IsNullOrEmpty(tierRoman))
        {
            stack.Children.Add(new Label
            {
                Text = tierRoman,
                TextColor = color,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            });
        }

        return stack;
    }

    static View BuildBar(RankProgressVisual visual, double barHeight)
    {
        var container = new Grid
        {
            HeightRequest = barHeight,
            VerticalOptions = LayoutOptions.Center
        };

        // Track.
        container.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#26324D"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = (float)(barHeight / 2) }
        });

        // Fill via proportional columns (no measuring needed).
        double progress = Math.Clamp(visual.Progress, 0, 1);
        double filled = Math.Max(progress, 0.0001);
        double rest = Math.Max(1 - progress, 0.0001);

        var fillGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(filled, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(rest, GridUnitType.Star) }
            }
        };

        var fill = new Border
        {
            BackgroundColor = visual.CurrentColor,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = (float)(barHeight / 2) },
            IsVisible = progress > 0
        };
        Grid.SetColumn(fill, 0);
        fillGrid.Children.Add(fill);
        container.Children.Add(fillGrid);

        // Percentage inside the bar (the only text on the bar).
        container.Children.Add(new Label
        {
            Text = $"{visual.Percent}%",
            TextColor = Colors.White,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        });

        return container;
    }

    static string ToRoman(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => ""
    };

    static Color ParseColor(string hex)
    {
        try
        {
            return string.IsNullOrWhiteSpace(hex)
                ? Colors.Gray
                : Color.FromArgb(hex);
        }
        catch
        {
            return Colors.Gray;
        }
    }
}

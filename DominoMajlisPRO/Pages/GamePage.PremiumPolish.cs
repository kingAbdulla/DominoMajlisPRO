using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    bool premiumPolishApplied;
    bool suppressTeamBackgroundCorrection;
    bool premiumMatchRewardsGranted;

    async void OnGamePagePremiumLoaded(object? sender, EventArgs e)
    {
        ApplyPremiumGamePagePolish();
        await RefreshPremiumRankBarsAsync();
    }

    void ApplyPremiumGamePagePolish()
    {
        if (premiumPolishApplied)
            return;

        premiumPolishApplied = true;
        HideMatchHonorBadges();
        ApplyStableSelectionSizing();

        Team1SpecialHonorIcon.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsVisible) || args.PropertyName == nameof(Opacity))
                HideMatchHonorBadges();
        };
        Team2SpecialHonorIcon.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsVisible) || args.PropertyName == nameof(Opacity))
                HideMatchHonorBadges();
        };

        Team1Card.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "BackgroundColor")
                CorrectTeamCardBackgrounds();
            if (args.PropertyName == nameof(Scale))
                ApplyStableSelectionSizing();
        };
        Team2Card.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "BackgroundColor")
                CorrectTeamCardBackgrounds();
            if (args.PropertyName == nameof(Scale))
                ApplyStableSelectionSizing();
        };
        Team1Emblem.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Scale))
                ApplyStableSelectionSizing();
        };
        Team2Emblem.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Scale))
                ApplyStableSelectionSizing();
        };

        RoundsContainer.ChildAdded += (_, args) =>
        {
            if (args.Element is View view)
                ApplyPremiumRoundRowStyle(view);
        };

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(450), () =>
        {
            ApplyStableSelectionSizing();
            CorrectTeamCardBackgrounds();
            _ = TryGrantPremiumMatchRewardsAsync();
            return !HandlerDisconnected();
        });

        CorrectTeamCardBackgrounds();
        ApplyPremiumRoundsContainerStyle();
    }

    bool HandlerDisconnected() => Handler == null;

    void HideMatchHonorBadges()
    {
        Team1SpecialHonorIcon.IsVisible = false;
        Team2SpecialHonorIcon.IsVisible = false;
        Team1SpecialHonorIcon.Opacity = 0;
        Team2SpecialHonorIcon.Opacity = 0;
        Team1SpecialHonorIcon.WidthRequest = 0;
        Team2SpecialHonorIcon.WidthRequest = 0;
        Team1SpecialHonorIcon.HeightRequest = 0;
        Team2SpecialHonorIcon.HeightRequest = 0;
    }

    void ApplyStableSelectionSizing()
    {
        Team1Card.Scale = 1.0;
        Team2Card.Scale = 1.0;
        Team1Emblem.Scale = 1.0;
        Team2Emblem.Scale = 1.0;
        Team1Emblem.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 68 : 76;
        Team1Emblem.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 68 : 76;
        Team2Emblem.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 68 : 76;
        Team2Emblem.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 68 : 76;
        Team1Players.FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 9 : 11;
        Team2Players.FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 9 : 11;
    }

    void CorrectTeamCardBackgrounds()
    {
        if (suppressTeamBackgroundCorrection)
            return;

        suppressTeamBackgroundCorrection = true;
        try
        {
            Team1Card.BackgroundColor = ResolvePremiumTeamBackground(team1Identity, team1Profile, selectedTeam == 1);
            Team2Card.BackgroundColor = ResolvePremiumTeamBackground(team2Identity, team2Profile, selectedTeam == 2);
            Team1Score.TextColor = selectedTeam == 1 ? Color.FromArgb("#D4AF37") : Color.FromArgb("#FFFFFF");
            Team2Score.TextColor = selectedTeam == 2 ? Color.FromArgb("#D4AF37") : Color.FromArgb("#FFFFFF");
            Team1Name.TextColor = selectedTeam == 1 ? Color.FromArgb("#D4AF37") : Color.FromArgb("#FFFFFF");
            Team2Name.TextColor = selectedTeam == 2 ? Color.FromArgb("#D4AF37") : Color.FromArgb("#FFFFFF");
        }
        finally
        {
            suppressTeamBackgroundCorrection = false;
        }
    }

    static Color ResolvePremiumTeamBackground(TeamIdentityModel? identity, TeamProfileModel? profile, bool selected)
    {
        var hex = identity?.TeamColorHex;
        if (string.IsNullOrWhiteSpace(hex))
            hex = profile?.ColorHex;

        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = Color.FromArgb(hex);
                var alpha = selected ? 0.18f : 0.09f;
                return new Color(color.Red, color.Green, color.Blue, alpha);
            }
            catch { }
        }

        return selected ? Color.FromArgb("#15110A") : Colors.Transparent;
    }

    async Task RefreshPremiumRankBarsAsync()
    {
        try
        {
            var teams = await TeamProfileService.LoadTeamsAsync();
            team1Profile = teams.FirstOrDefault(team => string.Equals(team.TeamId, team1Id, StringComparison.OrdinalIgnoreCase)) ?? team1Profile;
            team2Profile = teams.FirstOrDefault(team => string.Equals(team.TeamId, team2Id, StringComparison.OrdinalIgnoreCase)) ?? team2Profile;
            ApplyRankBar(team1Profile, Team1CurrentRankLabel, Team1NextRankLabel, Team1RankProgressBar, Team1RankPercentLabel);
            ApplyRankBar(team2Profile, Team2CurrentRankLabel, Team2NextRankLabel, Team2RankProgressBar, Team2RankPercentLabel);
            CorrectTeamCardBackgrounds();
        }
        catch
        {
            ApplyRankBar(null, Team1CurrentRankLabel, Team1NextRankLabel, Team1RankProgressBar, Team1RankPercentLabel);
            ApplyRankBar(null, Team2CurrentRankLabel, Team2NextRankLabel, Team2RankProgressBar, Team2RankPercentLabel);
        }
    }

    static void ApplyRankBar(TeamProfileModel? team, Label currentLabel, Label nextLabel, ProgressBar bar, Label percentLabel)
    {
        var xp = team?.XP ?? 0;
        if (xp <= 0 && team != null)
            xp = team.SeasonXP;

        var rank = ResolveRank(xp);
        currentLabel.Text = $"{rank.CurrentIcon} {rank.CurrentLevel}";
        nextLabel.Text = $"{rank.NextIcon} {rank.NextLevel}";
        bar.Progress = rank.Progress;
        percentLabel.Text = $"{rank.Progress * 100:0}%";
    }

    async Task TryGrantPremiumMatchRewardsAsync()
    {
        if (premiumMatchRewardsGranted || !matchSaved || !gameFinished || !currentMatch.IsFinished)
            return;

        var winnerTeamId = currentMatch.WinnerTeamId;
        if (string.IsNullOrWhiteSpace(winnerTeamId))
            return;

        var winnerProfile = string.Equals(winnerTeamId, currentMatch.Team1Id, StringComparison.OrdinalIgnoreCase)
            ? team1Profile
            : team2Profile;
        if (winnerProfile == null)
            return;

        premiumMatchRewardsGranted = true;
        var teamCoins = currentMatch.HasMeles ? 100 : 50;
        var playerCoins = teamCoins / 2;
        var creditedPlayers = new[] { winnerProfile.Player1Id, winnerProfile.Player2Id }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var playerId in creditedPlayers)
        {
            await PlayerWalletService.CreditAsync(playerId, playerCoins, 0);
            AppEvents.RaiseStoreEconomyChanged(playerId);
        }

        AppEvents.RaiseDataChanged();
    }

    void ApplyPremiumRoundsContainerStyle()
    {
        foreach (var child in RoundsContainer.Children.OfType<View>())
            ApplyPremiumRoundRowStyle(child);
    }

    void ApplyPremiumRoundRowStyle(View view)
    {
        if (view is Frame frame)
        {
            frame.HasShadow = false;
            frame.BackgroundColor = Color.FromArgb("#101010");
            frame.BorderColor = Color.FromArgb("#8A5B27");
            frame.CornerRadius = 18;
            frame.Padding = DeviceInfo.Idiom == DeviceIdiom.Phone ? 10 : 12;
            frame.Margin = new Thickness(0, 3);
        }
        else if (view is Border border)
        {
            border.BackgroundColor = Color.FromArgb("#101010");
            border.Stroke = Color.FromArgb("#8A5B27");
            border.StrokeThickness = 1;
            border.StrokeShape = new RoundRectangle { CornerRadius = 18 };
            border.Padding = DeviceInfo.Idiom == DeviceIdiom.Phone ? 10 : 12;
            border.Margin = new Thickness(0, 3);
        }

        ApplyPremiumRoundTextStyle(view);
    }

    void ApplyPremiumRoundTextStyle(IView root)
    {
        switch (root)
        {
            case Label label:
                label.TextColor = ResolvePremiumRoundLabelColor(label.Text, label.FontAttributes);
                label.FontSize = Math.Min(label.FontSize, DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : 18);
                label.LineBreakMode = LineBreakMode.TailTruncation;
                break;
            case Border border when border.Content is IView content:
                ApplyPremiumRoundTextStyle(content);
                break;
            case ContentView contentView when contentView.Content is IView content:
                ApplyPremiumRoundTextStyle(content);
                break;
            case Frame frame when frame.Content is IView content:
                ApplyPremiumRoundTextStyle(content);
                break;
            case Layout layout:
                foreach (var child in layout.Children)
                    ApplyPremiumRoundTextStyle(child);
                break;
        }
    }

    static Color ResolvePremiumRoundLabelColor(string? text, FontAttributes attributes)
    {
        if (!string.IsNullOrWhiteSpace(text) && text.Contains('#'))
            return Color.FromArgb("#8C8C8C");
        if (!string.IsNullOrWhiteSpace(text) && (text.Contains('+') || text.Contains("نقطة")))
            return Color.FromArgb("#D4AF37");
        if (attributes.HasFlag(FontAttributes.Bold))
            return Color.FromArgb("#FFFFFF");
        return Color.FromArgb("#CFCFCF");
    }

    static TeamRankVisual ResolveRank(int xp)
    {
        var tiers = new[]
        {
            new RankTier("◇", "U", 0),
            new RankTier("🥉", "V", 100),
            new RankTier("🥉", "IV", 200),
            new RankTier("🥉", "III", 300),
            new RankTier("⚪", "V", 450),
            new RankTier("⚪", "IV", 600),
            new RankTier("⚪", "III", 800),
            new RankTier("🟡", "V", 1050),
            new RankTier("🟡", "IV", 1350),
            new RankTier("🟡", "III", 1700),
            new RankTier("💎", "V", 2150),
            new RankTier("💎", "IV", 2700),
            new RankTier("👑", "I", 3400),
            new RankTier("🏆", "I", 4300)
        };

        var currentIndex = 0;
        for (var i = 0; i < tiers.Length; i++)
        {
            if (xp >= tiers[i].Xp)
                currentIndex = i;
        }

        var current = tiers[currentIndex];
        var next = tiers[Math.Min(currentIndex + 1, tiers.Length - 1)];
        var span = Math.Max(1, next.Xp - current.Xp);
        var progress = currentIndex == tiers.Length - 1 ? 1.0 : Math.Clamp((xp - current.Xp) / (double)span, 0, 1);
        return new TeamRankVisual(current.Icon, current.Level, next.Icon, next.Level, progress);
    }

    readonly record struct RankTier(string Icon, string Level, int Xp);
    readonly record struct TeamRankVisual(string CurrentIcon, string CurrentLevel, string NextIcon, string NextLevel, double Progress);
}

using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    bool premiumPolishApplied;
    bool suppressTeamBackgroundCorrection;

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
        Team1SpecialHonorIcon.IsVisible = false;
        Team2SpecialHonorIcon.IsVisible = false;
        Team1SpecialHonorIcon.Opacity = 0;
        Team2SpecialHonorIcon.Opacity = 0;

        Team1Card.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(BackgroundColor))
                CorrectTeamCardBackgrounds();
        };
        Team2Card.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(BackgroundColor))
                CorrectTeamCardBackgrounds();
        };

        CorrectTeamCardBackgrounds();
    }

    void CorrectTeamCardBackgrounds()
    {
        if (suppressTeamBackgroundCorrection)
            return;

        suppressTeamBackgroundCorrection = true;
        try
        {
            Team1Card.BackgroundColor = ResolvePremiumTeamBackground(team1Identity, team1Profile);
            Team2Card.BackgroundColor = ResolvePremiumTeamBackground(team2Identity, team2Profile);
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

    static Color ResolvePremiumTeamBackground(TeamIdentityModel? identity, TeamProfileModel? profile)
    {
        var hex = identity?.TeamColorHex;
        if (string.IsNullOrWhiteSpace(hex))
            hex = profile?.ColorHex;

        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = Color.FromArgb(hex);
                return new Color(color.Red, color.Green, color.Blue, 0.13f);
            }
            catch { }
        }

        return Colors.Transparent;
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
        var rank = ResolveRank(team?.XP ?? team?.SeasonXP ?? 0);
        currentLabel.Text = $"{rank.CurrentIcon} {rank.CurrentLevel}";
        nextLabel.Text = $"{rank.NextIcon} {rank.NextLevel}";
        bar.Progress = rank.Progress;
        percentLabel.Text = $"{rank.Progress * 100:0}%";
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

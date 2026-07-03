using DominoMajlisPRO.Models;

namespace DominoMajlisPRO;

public partial class MainPage
{
    bool isStartingGame;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
            return;

        ApplyProductionEmptyStateIfNeeded();
    }

    void ApplyProductionEmptyStateIfNeeded()
    {
        if (selectedTeam1 == null)
            ResetPreviewTeam1Card();

        if (selectedTeam2 == null)
            ResetPreviewTeam2Card();

        RefreshProductionMatchPreviewState();
    }

    void ResetPreviewTeam1Card()
    {
        PreviewTeam1NameLabel.Text = "اختر الفريق الأول";
        PreviewTeam1PlayersLabel.Text = "اضغط لاختيار فريق من المجلس";
        PreviewTeam1Logo.Source = ResolveStoredImage("shield_3d.png");
    }

    void ResetPreviewTeam2Card()
    {
        PreviewTeam2NameLabel.Text = "اختر الفريق الثاني";
        PreviewTeam2PlayersLabel.Text = "اضغط لاختيار فريق من المجلس";
        PreviewTeam2Logo.Source = ResolveStoredImage("shield_3d.png");
    }

    void RefreshProductionMatchPreviewState()
    {
        PreviewRulesLabel.Text = selectedRules;

        if (selectedTeam1 == null && selectedTeam2 == null)
        {
            PreviewTeamsLabel.Text = "اختر فريقين لبدء المواجهة";
            return;
        }

        if (selectedTeam1 == null)
        {
            PreviewTeamsLabel.Text = "اختر الفريق الأول";
            return;
        }

        if (selectedTeam2 == null)
        {
            PreviewTeamsLabel.Text = "اختر الفريق الثاني";
            return;
        }

        PreviewTeamsLabel.Text =
            $"{selectedTeam1.TeamName} VS {selectedTeam2.TeamName}";
    }

    bool IsProductionInvalidTeam(TeamProfileModel? team)
    {
        if (team == null)
            return true;

        if (string.IsNullOrWhiteSpace(team.TeamId))
            return true;

        if (string.IsNullOrWhiteSpace(team.TeamName))
            return true;

        if (string.IsNullOrWhiteSpace(team.Player1))
            return true;

        return false;
    }

    bool ContainsBlockedProductionTestText(TeamProfileModel team)
    {
        string combined =
            $"{team.TeamName} {team.Player1} {team.Player2}";

        string[] blockedTerms =
        {
            "test",
            "tester",
            "pussy"
        };

        return blockedTerms.Any(term =>
            combined.Contains(
                term,
                StringComparison.OrdinalIgnoreCase));
    }

    async Task<bool> ConfirmProductionMatchReadinessAsync()
    {
        if (isStartingGame)
            return false;

        if (IsProductionInvalidTeam(selectedTeam1) ||
            IsProductionInvalidTeam(selectedTeam2))
        {
            ApplyProductionEmptyStateIfNeeded();
            await DisplayAlert(
                "تنبيه",
                "يجب اختيار فريقين مكتملين قبل بدء المباراة.",
                "حسناً");
            return false;
        }

        if (ContainsBlockedProductionTestText(selectedTeam1!) ||
            ContainsBlockedProductionTestText(selectedTeam2!))
        {
            await DisplayAlert(
                "بيانات اختبارية",
                "توجد أسماء اختبارية أو غير مناسبة في بيانات المباراة. يرجى تعديل الفريق قبل الإطلاق أو قبل بدء مباراة إنتاجية.",
                "حسناً");
            return false;
        }

        isStartingGame = true;
        return true;
    }

    void ReleaseProductionMatchStartGate()
    {
        isStartingGame = false;
    }
}

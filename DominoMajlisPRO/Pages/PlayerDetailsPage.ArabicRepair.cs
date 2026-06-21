using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class PlayerDetailsPage
{
    bool _arabicRepairTimerStarted;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null || _arabicRepairTimerStarted)
            return;

        _arabicRepairTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(
            TimeSpan.FromMilliseconds(350),
            () =>
            {
                runs++;
                RepairStaticArabicUiText();
                return Handler != null && runs < 10;
            });
    }

    void RepairStaticArabicUiText()
    {
        if (currentPlayer == null)
            return;

        var rank = PlayerRankService.Calculate(currentPlayer.PlayerXP);

        if (IsBrokenArabicText(RankProgressLabel.Text))
        {
            RankProgressLabel.Text =
                $"المتبقي للرتبة التالية: {rank.RemainingXP} XP";
        }

        IdentityLastUpdateLabel.Text =
            $"آخر تحديث للهوية: {currentPlayer.LastUpdatedAt:yyyy/MM/dd HH:mm}";

        if (IsBrokenArabicText(CurrentTeamsLabel.Text))
            CurrentTeamsLabel.Text = "لا يوجد";

        LastRankHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.RankHistory));

        LastXPHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.XPHistory));

        LastAchievementHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.AchievementHistory));
    }

    static string CleanHistoryValue(string? value)
    {
        if (IsBrokenArabicText(value) || string.IsNullOrWhiteSpace(value))
            return "لا يوجد";

        return value.Trim();
    }

    static bool IsBrokenArabicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains('�') ||
               value.Contains("?�", StringComparison.Ordinal) ||
               value.Contains("â", StringComparison.Ordinal) ||
               value.Contains("Ã", StringComparison.Ordinal) ||
               value.Contains("ط", StringComparison.Ordinal) ||
               value.Contains("ظ", StringComparison.Ordinal);
    }
}

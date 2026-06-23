namespace DominoMajlisPRO.Pages;

public partial class CreateTeamPage
{
    private bool _previewSyncHooked;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (_previewSyncHooked || Handler == null)
            return;

        _previewSyncHooked = true;

        TeamNameEntry.TextChanged += OnPreviewIdentityTextChanged;
        Player1Entry.TextChanged += OnPreviewIdentityTextChanged;
        Player2Entry.TextChanged += OnPreviewIdentityTextChanged;

        Dispatcher.Dispatch(UpdatePreviewIdentityLabelsSafely);
    }

    private void OnPreviewIdentityTextChanged(object? sender, TextChangedEventArgs e) =>
        UpdatePreviewIdentityLabelsSafely();

    private void UpdatePreviewIdentityLabelsSafely()
    {
        try
        {
            PreviewTeamName.Text = string.IsNullOrWhiteSpace(TeamNameEntry.Text)
                ? "اسم الفريق"
                : TeamNameEntry.Text.Trim();

            var player1 = string.IsNullOrWhiteSpace(Player1Entry.Text)
                ? "اللاعب الأول"
                : Player1Entry.Text.Trim();

            var player2 = string.IsNullOrWhiteSpace(Player2Entry.Text)
                ? "اللاعب الثاني"
                : Player2Entry.Text.Trim();

            // The preview grid is rendered visually right-to-left: column 2 is the right slot
            // and column 0 is the left slot. Keep Player1 on the right and Player2 on the left.
            PreviewPlayer1.Text = isTeamMode ? player2 : player1;
            PreviewPlayer2.Text = isTeamMode ? player1 : string.Empty;
            PreviewPlayer2.IsVisible = isTeamMode;

            PreviewMode.Text = isTeamMode ? "فريق" : "فردي";
            SaveButtonText.Text = IsEditMode ? "تحديث الفريق" : "إنشاء الفريق";
        }
        catch
        {
            // Preview text sync is visual-only and must never block CreateTeamPage.
        }
    }
}

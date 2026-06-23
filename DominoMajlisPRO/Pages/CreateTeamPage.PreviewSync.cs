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

            PreviewPlayer1.Text = string.IsNullOrWhiteSpace(Player1Entry.Text)
                ? "اللاعب الأول"
                : Player1Entry.Text.Trim();

            PreviewPlayer2.Text = isTeamMode
                ? string.IsNullOrWhiteSpace(Player2Entry.Text)
                    ? "اللاعب الثاني"
                    : Player2Entry.Text.Trim()
                : string.Empty;

            PreviewPlayer2.IsVisible = isTeamMode;
        }
        catch
        {
            // Preview text sync is visual-only and must never block CreateTeamPage.
        }
    }
}

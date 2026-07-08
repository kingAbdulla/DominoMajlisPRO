using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Services;
namespace DominoMajlisPRO.Pages;

public partial class CertificatePage : ContentPage
{
    SavedMatch? match;

    public CertificatePage(
        SavedMatch savedMatch)
    {
        InitializeComponent();

        match = savedMatch;

        _=LoadCertificate();
  
    }

    // Back 
    async void OnBackClicked(
    object sender,
    TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Load Certificate
    async Task LoadCertificate()
    {

        if (match == null)
            return;
        var team1 =
            await TeamProfileService
                .GetTeamByIdAsync(match.Team1Id);

        var team2 =
            await TeamProfileService
                .GetTeamByIdAsync(match.Team2Id);
        if (team1 != null)
        {
            CertificateTeam1Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team1.Emblem,
                    "shield_3d.png");

            match.Team1Emblem =
                team1.Emblem;

            match.Team1ColorHex =
                team1.ColorHex;
        }
        if (team2 != null)
        {
            CertificateTeam2Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team2.Emblem,
                    "shield_3d.png");

            match.Team2Emblem =
                team2.Emblem;

            match.Team2ColorHex =
                team2.ColorHex;
        }
        CertificateMatchIdLabel.Text =
            $"Match ID : {match.MatchId.ToString()[..8].ToUpper()}";
        CertificateDateLabel.Text =
            $"Match Date : {match.MatchDate:yyyy-MM-dd}";
        CertificateStartTimeLabel.Text = $"Start : {match.MatchDate:HH:mm}";


        CertificateEndTimeLabel.Text =
            $"End : {match.MatchEndDate:HH:mm}";

        var duration =
            match.MatchEndDate -
            match.MatchDate;

        CertificateDurationLabel.Text =
            $"Duration : {duration.Hours:D2}h {duration.Minutes:D2}m";
        CertificateTeam1Name.Text =
            match.Team1Name;

        CertificateTeam2Name.Text =
            match.Team2Name;

        CertificateTeam1Id.Text =
            match.Team1Id;

        CertificateTeam2Id.Text =
            match.Team2Id;

        CertificateTeam1Players.Text =
            match.Team1Players;

        CertificateTeam2Players.Text =
            match.Team2Players;

        CertificateScoreLabel.Text =
            $"{match.Team1Score} - {match.Team2Score}";

        CertificateWinnerLabel.Text =
            string.IsNullOrWhiteSpace(match.WinnerTeamName)
            ? $"Winner : {match.WinnerTeam}"
            : $"Winner : {match.WinnerTeamName}";
        CertificateRoundsLabel.Text =
            $"Rounds\n{match.RoundsHistory.Count}";

        CertificateRulesLabel.Text =
            match.IsLocalRules
            ? "Local"
            : "International";

        CertificateMelesLabel.Text =
            match.HasMeles
            ? "Meles"
            : "No Meles";

        CertificateHighestRoundLabel.Text =
            $"Highest\n{(match.RoundsHistory.Any() ? match.RoundsHistory.Max(x => x.Points) : 0)}";

        var certificateId = CertificateQrService.BuildCertificateId(match);

        CertificateHashLabel.Text =
            $"CERTIFICATE ID : {certificateId}";

        try
        {
            CertificateQrImage.Source =
                CertificateQrService.GenerateQrImageSource(match);
        }
        catch
        {
            CertificateQrImage.Source = "qr_gold.png";
        }
    }
    // PDF export. The heavy render/capture/PDF work happens on the hidden
    // CertificatePrintPage (covered by a loading overlay + counter). Here we
    // just guard against repeated taps and reflect a loading state on the
    // button, restoring it when we return from the export page.

    bool _isExporting;

    async void OnExportPdfClicked(
        object sender,
        TappedEventArgs e)
    {
        if (_isExporting || match == null)
            return;

        _isExporting = true;
        PdfButton.Text = "جاري إنشاء PDF...";

        try
        {
            await Navigation.PushAsync(new CertificatePrintPage(match), false);
        }
        catch
        {
            RestoreExportButton();

            await DisplayAlert(
                "تعذر التصدير",
                "تعذر إنشاء ملف PDF حالياً. حاول مرة أخرى.",
                "حسناً");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RestoreExportButton();
    }

    void RestoreExportButton()
    {
        _isExporting = false;

        if (PdfButton != null)
            PdfButton.Text = "Export PDF";
    }
}


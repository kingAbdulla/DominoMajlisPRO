using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Graphics;
namespace DominoMajlisPRO.Pages;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;

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
                team1.Emblem;

            match.Team1Emblem =
                team1.Emblem;

            match.Team1ColorHex =
                team1.ColorHex;
        }
        if (team2 != null)
        {
            CertificateTeam2Emblem.Source =
                team2.Emblem;

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
            $"Highest\n{match.RoundsHistory.Max(x => x.Points)}";

        CertificateHashLabel.Text =
            $"CERTIFICATE HASH : {match.MatchId.ToString().ToUpper()}";
    }
    // save as png
  
    // PDF

    async void OnExportPdfClicked(
   object sender,
   TappedEventArgs e)
    {
        await Navigation.PushAsync(
 new CertificatePrintPage(match));
       

        
     
    }
   
}
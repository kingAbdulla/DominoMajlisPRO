using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Microsoft.Maui.ApplicationModel.DataTransfer;
namespace DominoMajlisPRO.Pages;

public partial class CertificatePrintPage : ContentPage
{
    SavedMatch match;
    bool _isExporting;
    public CertificatePrintPage(
        SavedMatch savedMatch)
    {
        InitializeComponent();

        match = savedMatch;

        LoadData();

        Loaded += async (_, __) =>
        {
            if (_isExporting)
                return;

            _isExporting = true;

            await Task.Delay(1500);

            await ExportPdfAsync();
        };
    }

    async void LoadData()
    {
        PrintWinner.Text =
            $"WINNER : {match.WinnerTeamName}";

        PrintTeam1Name.Text =
            match.Team1Name;

        PrintTeam2Name.Text =
            match.Team2Name;

        PrintScore.Text =
            $"{match.Team1Score} - {match.Team2Score}";

        PrintDate.Text =
            match.MatchDate
                .ToString("yyyy-MM-dd");
        PrintMatchId.Text =
    match.MatchId
    .ToString()[..8]
    .ToUpper();

        PrintTeam1Players.Text =
            match.Team1Players;

        PrintTeam2Players.Text =
            match.Team2Players;

        PrintHash.Text =
     $"HASH : {match.MatchId.ToString()[..8].ToUpper()}";

        var duration =
            match.MatchEndDate -
            match.MatchDate;

        PrintDuration.Text =
            $"{duration.Hours:D2}h {duration.Minutes:D2}m";
        PrintRules.Text =
    match.IsLocalRules
        ? "Local"
        : "International";

        PrintRounds.Text =
            match.RoundsHistory.Count.ToString();

        PrintHighest.Text =
            match.RoundsHistory.Any()
                ? match.RoundsHistory.Max(x => x.Points).ToString()
                : "0";

        PrintMeles.Text =
            match.HasMeles
                ? "YES"
                : "NO";

        var team1 =
            await TeamProfileService
                .GetTeamAsync(match.Team1Id);

        var team2 =
            await TeamProfileService
                .GetTeamAsync(match.Team2Id);

        if (team1 != null)
        {
            PrintTeam1Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team1.Emblem,
                    "shield_3d.png");
        }

        if (team2 != null)
        {
            PrintTeam2Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team2.Emblem,
                    "shield_3d.png");
        }
       
    }
    // Export Pdf
    async Task ExportPdfAsync()
    {
        try
        {
            await Task.Delay(500);

            var screenshot =
                await PrintContainer.CaptureAsync();

            if (screenshot == null)
                return;

            var pngPath =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    $"certificate_{Guid.NewGuid()}.png");

            using (var imageStream =
                await screenshot.OpenReadAsync())
            {
                using var fileStream =
                    File.Create(pngPath);

                await imageStream.CopyToAsync(fileStream);
            }

            var pdfPath =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    $"DominoMajlisPRO_Certificate_{Guid.NewGuid()}.pdf");

            var document =
                new PdfDocument();

            var page =
                document.AddPage();

            page.Size =
                PdfSharpCore.PageSize.A4;

            using var gfx =
                XGraphics.FromPdfPage(page);

            gfx.DrawRectangle(
                XBrushes.Black,
                0,
                0,
                page.Width,
                page.Height);

            using var fs =
                new FileStream(
                    pngPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            var image =
                XImage.FromStream(() => fs);

            double margin = 10;

            double pageWidth =
                page.Width - (margin * 2);

            double pageHeight =
                page.Height - (margin * 2);

            double imageRatio =
                image.PixelWidth /
                (double)image.PixelHeight;

            double pageRatio =
                pageWidth /
                pageHeight;

            double drawWidth;
            double drawHeight;

            if (imageRatio > pageRatio)
            {
                drawWidth = pageWidth;
                drawHeight = drawWidth / imageRatio;
            }
            else
            {
                drawHeight = pageHeight;
                drawWidth = drawHeight * imageRatio;
            }

            double x =
                (page.Width - drawWidth) / 2;

            double y =
                (page.Height - drawHeight) / 2;

            gfx.DrawImage(
                image,
                x,
                y,
                drawWidth,
                drawHeight);

            document.Save(pdfPath);

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Domino Majlis PRO Certificate",
                    File = new ShareFile(pdfPath)
                });
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PopAsync();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "PDF Error",
                ex.Message,
                "OK");
        }
    }

   
}





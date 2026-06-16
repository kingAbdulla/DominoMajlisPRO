using System.Text;

namespace DominoMajlisPRO.Services;

public static class SupportReportService
{
    public static async Task<string> CreateSupportReportAsync()
    {
        var version =
            AppVersionService.GetVersionInfo();

        var status =
            await DataStatusService.GetStatusAsync();

        var diagnostic =
            await DiagnosticService.RunDiagnosticsAsync();

        StringBuilder report =
            new();

        report.AppendLine("Domino Majlis PRO - Support Report");
        report.AppendLine("----------------------------------");
        report.AppendLine($"Date: {DateTime.Now:yyyy/MM/dd HH:mm}");
        report.AppendLine();

        report.AppendLine("Version Info");
        report.AppendLine($"App: {version.AppName}");
        report.AppendLine($"Version: {version.Version}");
        report.AppendLine($"Build: {version.Build}");
        report.AppendLine($"Release Type: {version.ReleaseType}");
        report.AppendLine();

        report.AppendLine("Data Status");
        report.AppendLine($"Teams: {status.TeamsCount}");
        report.AppendLine($"Players: {status.PlayersCount}");
        report.AppendLine($"Matches: {status.MatchesCount}");
        report.AppendLine($"Rankings: {status.RankingsCount}");
        report.AppendLine($"Hall Of Fame: {status.HallOfFameCount}");
        report.AppendLine($"Data Size: {status.DataSizeText}");
        report.AppendLine();

        report.AppendLine("Diagnostics");
        report.AppendLine(
            diagnostic.HasProblems
            ? "Status: Has Notes"
            : "Status: Clean");

        foreach (string message in diagnostic.Messages)
        {
            report.AppendLine(message);
        }

        string fileName =
            $"DominoMajlisPRO_SupportReport_{DateTime.Now:yyyy_MM_dd_HH_mm}.txt";

        string filePath =
            Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

        await File.WriteAllTextAsync(
            filePath,
            report.ToString());

        return filePath;
    }
}
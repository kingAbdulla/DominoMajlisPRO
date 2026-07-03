using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO;

public partial class MainPage
{
    bool _settingsSymbolsTimerStarted;
    bool _mainTeamEffectsTimerStarted;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        _ = RefreshMainPreviewTeamEffectsAsync();
        ApplyProductionReadinessOnHandlerReady();
        ApplyDeveloperAccessGuard();

        if (Handler == null || _settingsSymbolsTimerStarted)
            return;

        _settingsSymbolsTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
        {
            runs++;
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
            _ = RefreshMainPreviewTeamEffectsAsync();
            ApplyProductionReadinessOnHandlerReady();
            ApplyDeveloperAccessGuard();
            NormalizeSettingsSymbols();
            RepairVisibleMainPageText(this);
            return Handler != null && runs < 16;
        });

        if (!_mainTeamEffectsTimerStarted)
        {
            _mainTeamEffectsTimerStarted = true;
            int effectRuns = 0;

            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                effectRuns++;
                _ = RefreshMainPreviewTeamEffectsAsync();
                return Handler != null && effectRuns < 8;
            });
        }
    }

    async Task RefreshMainPreviewTeamEffectsAsync()
    {
        try
        {
            if (selectedTeam1 != null && !string.IsNullOrWhiteSpace(selectedTeam1.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(
                    PreviewTeam1Logo,
                    selectedTeam1.TeamId,
                    1.18,
                    lightweight: true);

            if (selectedTeam2 != null && !string.IsNullOrWhiteSpace(selectedTeam2.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(
                    PreviewTeam2Logo,
                    selectedTeam2.TeamId,
                    1.18,
                    lightweight: true);
        }
        catch
        {
            // Main preview effects are visual-only and must never block the home page.
        }
    }

    void NormalizeSettingsSymbols()
    {
        SetSymbol(DataArrow, isDataExpanded);
        SetSymbol(SystemArrow, isSystemExpanded);
        SetSymbol(HonorsArrow, isHonorsExpanded);
        SetSymbol(SupportArrow, isSupportExpanded);
        SetSymbol(AboutArrow, isAboutExpanded);
        SetSymbol(SecurityArrow, isSecurityExpanded);
    }

    static void RepairVisibleMainPageText(Element element)
    {
        if (element is Label label)
        {
            label.Text = RepairKnownBrokenMainText(label.Text);
        }
        else if (element is Button button)
        {
            button.Text = RepairKnownBrokenMainText(button.Text);
        }
        else if (element is Entry entry)
        {
            entry.Placeholder = RepairKnownBrokenMainText(entry.Placeholder);
        }
        else if (element is Editor editor)
        {
            editor.Placeholder = RepairKnownBrokenMainText(editor.Placeholder);
        }

        foreach (var child in element.LogicalChildren)
            RepairVisibleMainPageText(child);
    }

    static string RepairKnownBrokenMainText(string? value)
    {
        var text = NormalizeBrokenUiText(value);

        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (!LooksBrokenMainText(text))
            return text;

        return "غير متاح";
    }

    static bool LooksBrokenMainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("?", StringComparison.Ordinal) ||
               value.Contains("â", StringComparison.Ordinal) ||
               value.Contains("Ã", StringComparison.Ordinal) ||
               value.Contains("أƒ", StringComparison.Ordinal);
    }

    static void SetSymbol(Label? label, bool isExpanded)
    {
        if (label == null)
            return;

        label.Text = isExpanded ? "-" : "+";
    }
}

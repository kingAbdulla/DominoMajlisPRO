namespace DominoMajlisPRO;

public partial class MainPage
{
    bool _settingsSymbolsTimerStarted;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null || _settingsSymbolsTimerStarted)
            return;

        _settingsSymbolsTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
        {
            runs++;
            NormalizeSettingsSymbols();
            RepairVisibleMainPageText(this);
            return Handler != null && runs < 16;
        });
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

        foreach (var child in element.LogicalChildren)
            RepairVisibleMainPageText(child);
    }

    static string RepairKnownBrokenMainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        var text = value.Trim();

        text = text.Replace("âœ“", "✓", StringComparison.Ordinal);
        text = text.Replace("â–¼", "+", StringComparison.Ordinal);
        text = text.Replace("â–²", "-", StringComparison.Ordinal);

        if (LooksBrokenMainText(text))
            return "غير متاح";

        return text;
    }

    static bool LooksBrokenMainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains('�') ||
               value.Contains("?�", StringComparison.Ordinal) ||
               value.Contains("â", StringComparison.Ordinal) ||
               value.Contains("Ã", StringComparison.Ordinal);
    }

    static void SetSymbol(Label? label, bool isExpanded)
    {
        if (label == null)
            return;

        label.Text = isExpanded ? "-" : "+";
    }
}

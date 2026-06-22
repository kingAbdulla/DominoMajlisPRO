namespace DominoMajlisPRO;

public partial class MainPage
{
    bool _settingsSymbolsTimerStarted;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();

        if (Handler == null || _settingsSymbolsTimerStarted)
            return;

        _settingsSymbolsTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
        {
            runs++;
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
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

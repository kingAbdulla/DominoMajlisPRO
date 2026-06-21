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
            return Handler != null && runs < 12;
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

    static void SetSymbol(Label? label, bool isExpanded)
    {
        if (label == null)
            return;

        label.Text = isExpanded ? "-" : "+";
    }
}

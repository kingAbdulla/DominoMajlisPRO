namespace DominoMajlisPRO.Pages;

public partial class PlayerProfilesPage
{
    bool _playerProfilesArabicRepairStarted;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null || _playerProfilesArabicRepairStarted)
            return;

        _playerProfilesArabicRepairStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
        {
            runs++;
            RepairVisiblePlayerProfilesText(this);
            return Handler != null && runs < 16;
        });
    }

    static void RepairVisiblePlayerProfilesText(Element element)
    {
        if (element is Label label)
        {
            label.Text = NormalizePlayerProfilesText(label.Text);
        }
        else if (element is Button button)
        {
            button.Text = NormalizePlayerProfilesText(button.Text);
        }
        else if (element is Entry entry)
        {
            entry.Placeholder = NormalizePlayerProfilesText(entry.Placeholder);
        }
        else if (element is Editor editor)
        {
            editor.Placeholder = NormalizePlayerProfilesText(editor.Placeholder);
        }

        foreach (var child in element.LogicalChildren)
            RepairVisiblePlayerProfilesText(child);
    }

    static string NormalizePlayerProfilesText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        var text = value.Trim()
            .Replace("\u00E2\u0153\u201C", "OK", StringComparison.Ordinal)
            .Replace("\u00E2\u2013\u00BC", "+", StringComparison.Ordinal)
            .Replace("\u00E2\u2013\u00B2", "-", StringComparison.Ordinal)
            .Replace("\u00E2\u0152\u0192", "-", StringComparison.Ordinal)
            .Replace("\u00E2\u0152\u201E", "+", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u00A2", "*", StringComparison.Ordinal);

        if (LooksBrokenPlayerProfilesText(text))
            return "غير متاح";

        return text;
    }

    static bool LooksBrokenPlayerProfilesText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains('\uFFFD') ||
               value.Contains("?\uFFFD", StringComparison.Ordinal) ||
               value.Contains("\u00E2", StringComparison.Ordinal) ||
               value.Contains("\u00C3", StringComparison.Ordinal);
    }
}

using System.Collections.ObjectModel;
namespace DominoMajlisPRO.GalleryEngine.Admin.Canonical;
public static class CanonicalPickerExtensions
{
    private static readonly Dictionary<Picker, List<CanonicalOption>> PickerOptions = new();
    public static void SetOptions(
        this Picker picker,
        IEnumerable<CanonicalOption> options)
    {
        var list = options
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.CanonicalId) &&
                !string.IsNullOrWhiteSpace(item.DisplayName))
            .ToList();
        PickerOptions[picker] = list;
        picker.Items.Clear();
        foreach (var item in list)
            picker.Items.Add(item.DisplayName);
    }
    public static string SelectedCanonicalId(this Picker picker)
    {
        if (!PickerOptions.TryGetValue(picker, out var options))
            return string.Empty;
        if (picker.SelectedIndex < 0 ||
            picker.SelectedIndex >= options.Count)
            return string.Empty;
        return options[picker.SelectedIndex].CanonicalId;
    }
    public static string SelectedDisplayName(this Picker picker)
    {
        if (!PickerOptions.TryGetValue(picker, out var options))
            return string.Empty;
        if (picker.SelectedIndex < 0 ||
            picker.SelectedIndex >= options.Count)
            return string.Empty;
        return options[picker.SelectedIndex].DisplayName;
    }
    public static void SelectCanonicalId(
        this Picker picker,
        string? canonicalId)
    {
        if (!PickerOptions.TryGetValue(picker, out var options))
            return;
        var normalizedId = canonicalId?.Trim() ?? string.Empty;
        if (string.Equals(normalizedId, "Ass", StringComparison.OrdinalIgnoreCase))
            normalizedId = "Avatar";
        if (string.Equals(normalizedId, "Team", StringComparison.OrdinalIgnoreCase) &&
            !options.Any(item => string.Equals(item.CanonicalId, "Team", StringComparison.OrdinalIgnoreCase)) &&
            options.Any(item => string.Equals(item.CanonicalId, "Default", StringComparison.OrdinalIgnoreCase)))
            normalizedId = "Default";
        var index = options.FindIndex(item =>
            string.Equals(
                item.CanonicalId,
                normalizedId,
                StringComparison.OrdinalIgnoreCase));
        picker.SelectedIndex = index;
    }
}

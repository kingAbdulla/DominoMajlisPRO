using System.Globalization;
using System.Text;

namespace DominoMajlisPRO.Services;

public static class AutomationIdService
{
    public static void Attach(Page page)
    {
        Apply(page);
        page.Loaded -= OnPageLoaded;
        page.Loaded += OnPageLoaded;
    }

    static void OnPageLoaded(object? sender, EventArgs e)
    {
        if (sender is Page page)
            Apply(page);
    }

    static void Apply(Element root)
    {
        var pageName = root.GetType().Name;
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        ApplyRecursive(root, pageName, counters);
    }

    static void ApplyRecursive(
        IVisualTreeElement node,
        string pageName,
        IDictionary<string, int> counters)
    {
        if (node is Element element)
            EnsureAutomationId(element, pageName, counters);

        foreach (var child in node.GetVisualChildren())
            ApplyRecursive(child, pageName, counters);
    }

    static void EnsureAutomationId(
        Element element,
        string pageName,
        IDictionary<string, int> counters)
    {
        if (element is not VisualElement visual ||
            !string.IsNullOrWhiteSpace(visual.AutomationId) ||
            !IsImportantControl(element))
        {
            return;
        }

        var typeName = element.GetType().Name;
        var key = BuildKey(element);
        var counterKey = $"{typeName}:{key}";
        counters.TryGetValue(counterKey, out var count);
        counters[counterKey] = ++count;

        visual.AutomationId = count == 1
            ? $"{pageName}_{typeName}_{key}"
            : $"{pageName}_{typeName}_{key}_{count.ToString(CultureInfo.InvariantCulture)}";
    }

    static bool IsImportantControl(Element element) =>
        element is Button ||
        element is ImageButton ||
        element is Picker ||
        element is Switch ||
        element is CheckBox ||
        element is RadioButton ||
        element is Slider ||
        element is Entry ||
        element is Editor ||
        element is SearchBar ||
        element is CollectionView;

    static string BuildKey(Element element)
    {
        var raw = element switch
        {
            Button button => button.Text,
            Picker picker => picker.Title,
            Entry entry => entry.Placeholder,
            Editor editor => editor.Placeholder,
            SearchBar searchBar => searchBar.Placeholder,
            RadioButton radioButton => radioButton.Content?.ToString(),
            _ => null
        };

        return Slug(string.IsNullOrWhiteSpace(raw) ? "control" : raw);
    }

    static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        var slug = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "control" : slug;
    }
}

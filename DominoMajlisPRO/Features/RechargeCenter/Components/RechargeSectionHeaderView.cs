namespace DominoMajlisPRO.Features.RechargeCenter.Components;

public sealed class RechargeSectionHeaderView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(RechargeSectionHeaderView),
        string.Empty,
        propertyChanged: static (bindable, _, value) =>
            ((RechargeSectionHeaderView)bindable)._label.Text = value?.ToString() ?? string.Empty);

    private readonly Label _label;

    public RechargeSectionHeaderView()
    {
        _label = new Label
        {
            FontFamily = "Tajawal-Regular",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#E6B94F")
        };
        Content = _label;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}

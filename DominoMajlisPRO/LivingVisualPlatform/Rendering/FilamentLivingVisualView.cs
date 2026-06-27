namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class FilamentLivingVisualView : View
{
    public static readonly BindableProperty AssetPathProperty =
        BindableProperty.Create(nameof(AssetPath), typeof(string), typeof(FilamentLivingVisualView), string.Empty);

    public static readonly BindableProperty IsPausedProperty =
        BindableProperty.Create(nameof(IsPaused), typeof(bool), typeof(FilamentLivingVisualView), true);

    public string AssetPath
    {
        get => (string)GetValue(AssetPathProperty);
        set => SetValue(AssetPathProperty, value);
    }

    public bool IsPaused
    {
        get => (bool)GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }
}

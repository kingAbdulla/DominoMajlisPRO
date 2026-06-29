namespace DominoMajlisPRO.LivingVisualPlatform.Rendering;

public sealed class FilamentLivingVisualView : View
{
    public static readonly BindableProperty AssetPathProperty =
        BindableProperty.Create(nameof(AssetPath), typeof(string), typeof(FilamentLivingVisualView), string.Empty);

    public static readonly BindableProperty IsPausedProperty =
        BindableProperty.Create(nameof(IsPaused), typeof(bool), typeof(FilamentLivingVisualView), true);

    public static readonly BindableProperty LastMotionCommandProperty =
        BindableProperty.Create(nameof(LastMotionCommand), typeof(string), typeof(FilamentLivingVisualView), string.Empty);

    public static readonly BindableProperty LastMotionCommandVersionProperty =
        BindableProperty.Create(nameof(LastMotionCommandVersion), typeof(int), typeof(FilamentLivingVisualView), 0);

    public static readonly BindableProperty LastTouchStimulusProperty =
        BindableProperty.Create(nameof(LastTouchStimulus), typeof(string), typeof(FilamentLivingVisualView), string.Empty);

    public static readonly BindableProperty LastTouchStimulusVersionProperty =
        BindableProperty.Create(nameof(LastTouchStimulusVersion), typeof(int), typeof(FilamentLivingVisualView), 0);

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

    public string LastMotionCommand
    {
        get => (string)GetValue(LastMotionCommandProperty);
        set => SetValue(LastMotionCommandProperty, value);
    }

    public int LastMotionCommandVersion
    {
        get => (int)GetValue(LastMotionCommandVersionProperty);
        set => SetValue(LastMotionCommandVersionProperty, value);
    }

    public string LastTouchStimulus
    {
        get => (string)GetValue(LastTouchStimulusProperty);
        set => SetValue(LastTouchStimulusProperty, value);
    }

    public int LastTouchStimulusVersion
    {
        get => (int)GetValue(LastTouchStimulusVersionProperty);
        set => SetValue(LastTouchStimulusVersionProperty, value);
    }
}

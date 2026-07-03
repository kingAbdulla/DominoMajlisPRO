namespace DominoMajlisPRO;

public partial class MainPage
{
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        ApplyHeaderAvatarAfterNavigation();
    }

    void ApplyHeaderAvatarAfterNavigation()
    {
        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        });

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(600), () =>
        {
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        });
    }
}

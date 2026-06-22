namespace DominoMajlisPRO;

public partial class MainPage
{
    bool _headerAvatarParentSyncAttached;

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (_headerAvatarParentSyncAttached)
            return;

        _headerAvatarParentSyncAttached = true;
        SizeChanged += OnMainHeaderAvatarPageSizeChanged;
        ApplyMainHeaderAvatarShapeNowAndLater();
    }

    void OnMainHeaderAvatarPageSizeChanged(object? sender, EventArgs e)
    {
        ApplyMainHeaderAvatarShapeNowAndLater();
    }

    void ApplyMainHeaderAvatarShapeNowAndLater()
    {
        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        });

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
        {
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        });
    }
}

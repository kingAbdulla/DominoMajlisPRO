namespace DominoMajlisPRO;

public partial class MainPage
{
    double _lastHeaderAvatarLayoutWidth;
    double _lastHeaderAvatarLayoutHeight;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        if (Math.Abs(_lastHeaderAvatarLayoutWidth - width) < 0.5 &&
            Math.Abs(_lastHeaderAvatarLayoutHeight - height) < 0.5)
            return;

        _lastHeaderAvatarLayoutWidth = width;
        _lastHeaderAvatarLayoutHeight = height;

        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
    }
}

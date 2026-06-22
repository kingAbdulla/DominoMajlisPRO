using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ApplyMainHeaderAvatarShape();
    }

    void ApplyMainHeaderAvatarShape()
    {
        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? 58
            : 72;

        ConfigureHeaderAvatarImage(HeaderPlayerAvatar, avatarSize, 1);
        ConfigureHeaderAvatarImage(HeaderAvatarEffectOverlay, avatarSize, 5);
        ConfigureHeaderAvatarImage(HeaderAvatarFrameOverlay, avatarSize, 10);

        HeaderAvatarEffectOverlay.Scale = 1.18;
        HeaderAvatarFrameOverlay.Scale = 1.12;
    }

    static void ConfigureHeaderAvatarImage(Image image, double size, int zIndex)
    {
        double radius = size / 2.0;

        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Clip = new EllipseGeometry
        {
            Center = new Point(radius, radius),
            RadiusX = radius,
            RadiusY = radius
        };
    }
}

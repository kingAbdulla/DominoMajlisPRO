using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    void ApplyMainHeaderAvatarShape()
    {
        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? 58
            : 72;

        ConfigureHeaderAvatarImage(HeaderPlayerAvatar, avatarSize, 1);
        ConfigureHeaderAvatarImage(HeaderAvatarFrameOverlay, avatarSize, 2);
        ConfigureHeaderAvatarImage(HeaderAvatarEffectOverlay, avatarSize, 3);

        HeaderPlayerAvatar.Aspect = Aspect.AspectFill;
        HeaderAvatarFrameOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.Aspect = Aspect.AspectFit;
    }

    static void ConfigureHeaderAvatarImage(Image image, double size, int zIndex)
    {
        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Opacity = 1.0;
        image.Clip = null;
        image.Shadow = null;
        image.BackgroundColor = Colors.Transparent;
    }
}

using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    static MainPage()
    {
        Application.Current?.Dispatcher.StartTimer(
            TimeSpan.FromMilliseconds(300),
            () =>
            {
                var page = Application.Current?.Windows
                    .FirstOrDefault()?
                    .Page;

                if (page is MainPage mainPage)
                    mainPage.ApplyMainHeaderAvatarShape();

                return true;
            });
    }

    void ApplyMainHeaderAvatarShape()
    {
        const double phoneAvatarSize = 58;
        const double tabletAvatarSize = 72;

        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? phoneAvatarSize
            : tabletAvatarSize;

        ConfigureCircularAvatarImage(HeaderPlayerAvatar, avatarSize, 2);
        ConfigureEffectOverlay(HeaderAvatarEffectOverlay, avatarSize, 1);
        ConfigureFrameOverlay(HeaderAvatarFrameOverlay, avatarSize, 3);
    }

    static void ConfigureCircularAvatarImage(Image image, double size, int zIndex)
    {
        double radius = size / 2.0;

        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Clip = new EllipseGeometry
        {
            Center = new Point(radius, radius),
            RadiusX = radius,
            RadiusY = radius
        };
    }

    static void ConfigureEffectOverlay(Image image, double avatarSize, int zIndex)
    {
        double size = avatarSize * 1.32;

        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Clip = null;
    }

    static void ConfigureFrameOverlay(Image image, double avatarSize, int zIndex)
    {
        double size = avatarSize * 1.10;

        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Clip = null;
    }
}

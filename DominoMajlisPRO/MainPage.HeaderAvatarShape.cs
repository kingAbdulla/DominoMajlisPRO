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
        const double phoneAvatarSize = 50;
        const double tabletAvatarSize = 62;

        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? phoneAvatarSize
            : tabletAvatarSize;

        ConfigureEffectOverlay(HeaderAvatarEffectOverlay, avatarSize, 1);
        ConfigureCircularAvatarImage(HeaderPlayerAvatar, avatarSize, 2);
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
        image.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#F2C14E")),
            Radius = 10,
            Opacity = 0.32f
        };
    }

    static void ConfigureEffectOverlay(Image image, double avatarSize, int zIndex)
    {
        double size = avatarSize * 1.08;
        double radius = size / 2.0;

        image.WidthRequest = size;
        image.HeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Center;
        image.VerticalOptions = LayoutOptions.Center;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Opacity = 0.58;
        image.Clip = new EllipseGeometry
        {
            Center = new Point(radius, radius),
            RadiusX = radius,
            RadiusY = radius
        };
        image.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#F2C14E")),
            Radius = 14,
            Opacity = 0.42f
        };
    }

    static void ConfigureFrameOverlay(Image image, double avatarSize, int zIndex)
    {
        double size = avatarSize * 1.02;
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
}

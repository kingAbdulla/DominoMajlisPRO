using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    const string HeaderAvatarOuterHostId = "MainHeaderAvatarOuterHost";
    const string HeaderAvatarInnerHostId = "MainHeaderAvatarInnerHost";

    void ApplyMainHeaderAvatarShape()
    {
        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? 58
            : 72;

        Border border = EnsureMainHeaderAvatarBorder();

        if (border.Parent is Grid outerHost)
        {
            outerHost.WidthRequest = avatarSize;
            outerHost.HeightRequest = avatarSize;
            outerHost.MinimumWidthRequest = avatarSize;
            outerHost.MinimumHeightRequest = avatarSize;
            outerHost.HorizontalOptions = LayoutOptions.Center;
            outerHost.VerticalOptions = LayoutOptions.Center;
            outerHost.Clip = null;
        }

        border.WidthRequest = avatarSize;
        border.HeightRequest = avatarSize;
        border.MinimumWidthRequest = avatarSize;
        border.MinimumHeightRequest = avatarSize;
        border.HorizontalOptions = LayoutOptions.Center;
        border.VerticalOptions = LayoutOptions.Center;
        border.BackgroundColor = Color.FromArgb("#151515");
        border.Stroke = Color.FromArgb("#D4AF37");
        border.StrokeThickness = 2.4;
        border.StrokeShape = new RoundRectangle { CornerRadius = 999 };
        border.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#D4AF37")),
            Radius = 18,
            Opacity = 0.45f
        };

        if (border.Content is Grid innerHost)
        {
            innerHost.WidthRequest = avatarSize;
            innerHost.HeightRequest = avatarSize;
            innerHost.MinimumWidthRequest = avatarSize;
            innerHost.MinimumHeightRequest = avatarSize;
            innerHost.HorizontalOptions = LayoutOptions.Center;
            innerHost.VerticalOptions = LayoutOptions.Center;
            innerHost.Clip = null;
        }

        ConfigureHeaderAvatarImage(HeaderPlayerAvatar, avatarSize, 0);
        ConfigureHeaderAvatarImage(HeaderAvatarFrameOverlay, avatarSize, 1);
        ConfigureHeaderAvatarImage(HeaderAvatarEffectOverlay, avatarSize, 2);

        HeaderPlayerAvatar.Aspect = Aspect.AspectFill;
        HeaderAvatarFrameOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.Aspect = Aspect.AspectFit;
    }

    Border EnsureMainHeaderAvatarBorder()
    {
        if (HeaderPlayerAvatar.Parent is Grid innerHost &&
            string.Equals(innerHost.StyleId, HeaderAvatarInnerHostId, StringComparison.Ordinal) &&
            innerHost.Parent is Border existingBorder)
        {
            return existingBorder;
        }

        if (HeaderPlayerAvatar.Parent is not Grid originalHost)
            throw new InvalidOperationException("Header avatar host was not found.");

        var existingChildren = originalHost.Children.ToList();
        originalHost.Children.Clear();

        Grid newInnerHost = new()
        {
            StyleId = HeaderAvatarInnerHostId,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        foreach (var child in existingChildren)
            newInnerHost.Children.Add(child);

        Border newBorder = new()
        {
            StyleId = HeaderAvatarOuterHostId,
            Padding = 0,
            Content = newInnerHost
        };

        originalHost.Children.Add(newBorder);
        return newBorder;
    }

    static void ConfigureHeaderAvatarImage(Image image, double size, int zIndex)
    {
        image.WidthRequest = size;
        image.HeightRequest = size;
        image.MinimumWidthRequest = size;
        image.MinimumHeightRequest = size;
        image.HorizontalOptions = LayoutOptions.Fill;
        image.VerticalOptions = LayoutOptions.Fill;
        image.InputTransparent = true;
        image.ZIndex = zIndex;
        image.Scale = 1.0;
        image.Opacity = 1.0;
        image.Clip = null;
        image.Shadow = null;
        image.BackgroundColor = Colors.Transparent;
    }
}

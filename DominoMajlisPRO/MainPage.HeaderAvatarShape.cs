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
        var avatarClip = CreateAvatarClip(avatarSize);

        if (border.Parent is Grid outerHost)
        {
            outerHost.WidthRequest = avatarSize;
            outerHost.HeightRequest = avatarSize;
            outerHost.MinimumWidthRequest = avatarSize;
            outerHost.MinimumHeightRequest = avatarSize;
            outerHost.HorizontalOptions = LayoutOptions.Center;
            outerHost.VerticalOptions = LayoutOptions.Center;
            outerHost.Clip = null;
            outerHost.BackgroundColor = Colors.Transparent;
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
        border.Clip = avatarClip;

        if (border.Content is Grid innerHost)
        {
            innerHost.WidthRequest = avatarSize;
            innerHost.HeightRequest = avatarSize;
            innerHost.MinimumWidthRequest = avatarSize;
            innerHost.MinimumHeightRequest = avatarSize;
            innerHost.HorizontalOptions = LayoutOptions.Center;
            innerHost.VerticalOptions = LayoutOptions.Center;
            innerHost.BackgroundColor = Colors.Transparent;
            innerHost.Clip = CreateAvatarClip(avatarSize);
            NormalizeProceduralEffectChildren(innerHost, avatarSize);
        }

        ConfigureHeaderAvatarImage(HeaderPlayerAvatar, avatarSize, 0, true);
        ConfigureHeaderAvatarImage(HeaderAvatarFrameOverlay, avatarSize, 1, false);
        ConfigureHeaderAvatarImage(HeaderAvatarEffectOverlay, avatarSize, 2, false);
        ConfigureProfileStatusBadge(avatarSize);

        HeaderPlayerAvatar.Aspect = Aspect.AspectFill;
        HeaderAvatarFrameOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.StyleId = "MainHeaderAvatarEffectOverlay";
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
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Transparent
        };

        foreach (var child in existingChildren)
        {
            if (ReferenceEquals(child, ProfileStatusBadge))
                continue;

            newInnerHost.Children.Add(child);
        }

        Border newBorder = new()
        {
            StyleId = HeaderAvatarOuterHostId,
            Padding = 0,
            Content = newInnerHost
        };

        originalHost.Children.Add(newBorder);
        originalHost.Children.Add(ProfileStatusBadge);
        return newBorder;
    }

    static void ConfigureHeaderAvatarImage(
        Image image,
        double size,
        int zIndex,
        bool clip)
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
        image.Clip = clip ? CreateAvatarClip(size) : null;
        image.Shadow = null;
        image.BackgroundColor = Colors.Transparent;
    }

    void ConfigureProfileStatusBadge(double avatarSize)
    {
        double badgeSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 10 : 12;
        ProfileStatusBadge.WidthRequest = badgeSize;
        ProfileStatusBadge.HeightRequest = badgeSize;
        ProfileStatusBadge.HorizontalOptions = LayoutOptions.End;
        ProfileStatusBadge.VerticalOptions = LayoutOptions.End;
        ProfileStatusBadge.ZIndex = 20;
        ProfileStatusBadge.Margin = new Thickness(0, 0, Math.Max(0, (avatarSize - 40) / 2.0), Math.Max(0, (avatarSize - 40) / 2.0));
    }

    static EllipseGeometry CreateAvatarClip(double size)
    {
        double radius = size / 2.0;
        return new EllipseGeometry
        {
            Center = new Point(radius, radius),
            RadiusX = radius,
            RadiusY = radius
        };
    }

    static void NormalizeProceduralEffectChildren(Grid host, double size)
    {
        foreach (var child in host.Children)
        {
            if (child is GraphicsView graphicsView)
            {
                graphicsView.WidthRequest = size;
                graphicsView.HeightRequest = size;
                graphicsView.MinimumWidthRequest = size;
                graphicsView.MinimumHeightRequest = size;
                graphicsView.HorizontalOptions = LayoutOptions.Fill;
                graphicsView.VerticalOptions = LayoutOptions.Fill;
                graphicsView.Scale = 1.0;
                graphicsView.Opacity = Math.Min(graphicsView.Opacity <= 0 ? 1.0 : graphicsView.Opacity, 0.95);
                graphicsView.Clip = CreateAvatarClip(size);
                graphicsView.BackgroundColor = Colors.Transparent;
                graphicsView.InputTransparent = true;
                graphicsView.ZIndex = 3;
            }
            else if (child is Image image && !ReferenceEquals(image, HeaderAvatarEffectOverlay))
            {
                image.BackgroundColor = Colors.Transparent;
                image.Shadow = null;
            }
        }
    }
}

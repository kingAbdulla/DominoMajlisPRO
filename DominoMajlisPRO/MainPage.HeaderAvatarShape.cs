using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    const string HeaderAvatarOuterHostId = "MainHeaderAvatarOuterHost";
    const string HeaderAvatarInnerHostId = "MainHeaderAvatarInnerHost";
    const double MainHeaderAvatarEffectScale = 1.03;
    const double MainHeaderAvatarPhoneSize = 54;
    const double MainHeaderAvatarTabletSize = 68;

    void ApplyMainHeaderAvatarShape()
    {
        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? MainHeaderAvatarPhoneSize
            : MainHeaderAvatarTabletSize;

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
            outerHost.BackgroundColor = Colors.Transparent;
            outerHost.Shadow = null;
        }

        border.WidthRequest = avatarSize;
        border.HeightRequest = avatarSize;
        border.MinimumWidthRequest = avatarSize;
        border.MinimumHeightRequest = avatarSize;
        border.HorizontalOptions = LayoutOptions.Center;
        border.VerticalOptions = LayoutOptions.Center;
        border.BackgroundColor = Color.FromArgb("#151515");
        border.Stroke = Color.FromArgb("#D4AF37");
        border.StrokeThickness = 1.2;
        border.StrokeShape = new RoundRectangle { CornerRadius = 999 };
        border.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#D4AF37")),
            Radius = 6,
            Opacity = 0.20f
        };
        border.Clip = CreateCircleClip(avatarSize);

        if (border.Content is Grid innerHost)
        {
            innerHost.WidthRequest = avatarSize;
            innerHost.HeightRequest = avatarSize;
            innerHost.MinimumWidthRequest = avatarSize;
            innerHost.MinimumHeightRequest = avatarSize;
            innerHost.HorizontalOptions = LayoutOptions.Center;
            innerHost.VerticalOptions = LayoutOptions.Center;
            innerHost.BackgroundColor = Colors.Transparent;
            innerHost.Clip = CreateCircleClip(avatarSize);
            innerHost.Shadow = null;
            NormalizeProceduralEffectChildren(innerHost, avatarSize);
        }

        ConfigureHeaderAvatarImage(HeaderPlayerAvatar, avatarSize, 0, true);
        ConfigureHeaderAvatarImage(HeaderAvatarFrameOverlay, avatarSize, 1, true);
        ConfigureHeaderAvatarImage(HeaderAvatarEffectOverlay, avatarSize, 2, true);
        HeaderPlayerAvatar.Scale = 0.88;
        HeaderAvatarFrameOverlay.Scale = 0.94;
        HeaderAvatarEffectOverlay.Scale = 0.96;

        ConfigureProfileStatusBadge(avatarSize);
        ApplyMainHeaderTextPolish();
        ApplyMainHeaderSecondaryPolish();

        HeaderPlayerAvatar.Aspect = Aspect.AspectFill;
        HeaderAvatarFrameOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.StyleId = "MainHeaderAvatarEffectOverlay";
    }

    void ApplyMainHeaderAvatarIdentityVisuals(PlayerVisualIdentity identity)
    {
        ApplyMainHeaderAvatarShape();

        var frameSource =
            ToHeaderImageSource(identity.Frame?.PreviewImage);
        HeaderAvatarFrameOverlay.Source = frameSource;
        HeaderAvatarFrameOverlay.IsVisible = frameSource != null;

        PlayerEffectEngine.Apply(
            HeaderAvatarEffectOverlay,
            identity.Effect,
            MainHeaderAvatarEffectScale);

        ApplyMainHeaderAvatarShape();

        Border border = EnsureMainHeaderAvatarBorder();
        border.Stroke = identity.Frame == null
            ? Color.FromArgb("#D4AF37")
            : Colors.Transparent;
        border.Shadow = identity.Effect == null
            ? new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#D4AF37")),
                Radius = 6,
                Opacity = 0.20f
            }
            : new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#F2C14E")),
                Radius = 7,
                Opacity = 0.22f
            };
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
        image.Clip = clip ? CreateCircleClip(size) : null;
        image.Shadow = null;
        image.BackgroundColor = Colors.Transparent;
    }

    void ConfigureProfileStatusBadge(double avatarSize)
    {
        double badgeSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 7 : 9;
        ProfileStatusBadge.WidthRequest = badgeSize;
        ProfileStatusBadge.HeightRequest = badgeSize;
        ProfileStatusBadge.HorizontalOptions = LayoutOptions.End;
        ProfileStatusBadge.VerticalOptions = LayoutOptions.End;
        ProfileStatusBadge.ZIndex = 20;
        ProfileStatusBadge.Margin = new Thickness(0, 0, 4, 4);
    }

    void ApplyMainHeaderTextPolish()
    {
        HeaderPlayerNameLabel.FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 14 : 17;
        HeaderPlayerNameLabel.TextColor = Colors.White;
        HeaderPlayerNameLabel.VerticalOptions = LayoutOptions.End;
        HeaderPlayerNameLabel.Margin = new Thickness(2, 0, 0, 0);

        MemberLevelLabel.FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 11 : 13;
        MemberLevelLabel.TextColor = Color.FromArgb("#D8B85A");
        MemberLevelLabel.VerticalOptions = LayoutOptions.Start;
        MemberLevelLabel.Margin = new Thickness(2, -1, 0, 0);
    }

    void ApplyMainHeaderSecondaryPolish()
    {
        MainPlayerHeaderSurface.Padding = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? new Thickness(1, 0)
            : new Thickness(6, 2);

        MainSeasonProgressBar.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 10 : 12;
        TryPolishSettingsButton();
    }

    void TryPolishSettingsButton()
    {
        foreach (var child in MainPlayerHeaderSurface.Children)
        {
            if (child is VerticalStackLayout settingsStack)
            {
                settingsStack.Spacing = 3;

                foreach (var item in settingsStack.Children)
                {
                    if (item is Grid settingsGrid)
                    {
                        settingsGrid.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 43 : 50;
                        settingsGrid.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 43 : 50;

                        foreach (var gridChild in settingsGrid.Children)
                        {
                            if (gridChild is Image image)
                            {
                                image.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 35 : 40;
                                image.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 35 : 40;
                                image.Aspect = Aspect.AspectFit;
                            }
                        }
                    }
                    else if (item is Label label)
                    {
                        label.FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 10 : 12;
                        label.TextColor = Color.FromArgb("#C8B56A");
                    }
                }
            }
        }
    }

    static Geometry CreateCircleClip(double size)
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
                graphicsView.Clip = CreateCircleClip(size);
                graphicsView.BackgroundColor = Colors.Transparent;
                graphicsView.InputTransparent = true;
                graphicsView.ZIndex = 3;
                graphicsView.Scale = 0.96;
                graphicsView.Opacity = 0.60;
            }
            else if (child is Image image)
            {
                image.BackgroundColor = Colors.Transparent;
                image.Shadow = null;
                image.Clip = CreateCircleClip(size);
                image.Scale = 0.96;
            }
        }
    }
}

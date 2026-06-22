using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO;

public partial class MainPage
{
    const double HeaderAvatarParityScale = 1.18;
    const double HeaderAvatarParityPhoneSize = 52;
    const double HeaderAvatarParityTabletSize = 62;

    bool headerAvatarParityEventsAttached;

    static MainPage()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current == null)
                return;

            Application.Current.PageAppearing -= OnAnyPageAppearingForHeaderAvatarParity;
            Application.Current.PageAppearing += OnAnyPageAppearingForHeaderAvatarParity;
            Application.Current.PageDisappearing -= OnAnyPageDisappearingForHeaderAvatarParity;
            Application.Current.PageDisappearing += OnAnyPageDisappearingForHeaderAvatarParity;
        });
    }

    static void OnAnyPageAppearingForHeaderAvatarParity(
        object? sender,
        Page page)
    {
        if (page is not MainPage mainPage)
            return;

        mainPage.AttachHeaderAvatarParityEvents();
        _ = mainPage.ApplyHeaderAvatarParityAfterRefreshAsync();
    }

    static void OnAnyPageDisappearingForHeaderAvatarParity(
        object? sender,
        Page page)
    {
        if (page is MainPage mainPage)
            mainPage.DetachHeaderAvatarParityEvents();
    }

    void AttachHeaderAvatarParityEvents()
    {
        if (headerAvatarParityEventsAttached)
            return;

        AppEvents.CurrentUserChanged += OnHeaderAvatarParityCurrentUserChanged;
        AppEvents.PlayerProfileChanged += OnHeaderAvatarParityProfileChanged;
        AppEvents.StoreEconomyChanged += OnHeaderAvatarParityStoreChanged;
        AppEvents.StoreProgressChanged += OnHeaderAvatarParityStoreChanged;
        headerAvatarParityEventsAttached = true;
    }

    void DetachHeaderAvatarParityEvents()
    {
        if (!headerAvatarParityEventsAttached)
            return;

        AppEvents.CurrentUserChanged -= OnHeaderAvatarParityCurrentUserChanged;
        AppEvents.PlayerProfileChanged -= OnHeaderAvatarParityProfileChanged;
        AppEvents.StoreEconomyChanged -= OnHeaderAvatarParityStoreChanged;
        AppEvents.StoreProgressChanged -= OnHeaderAvatarParityStoreChanged;
        headerAvatarParityEventsAttached = false;
    }

    async void OnHeaderAvatarParityCurrentUserChanged()
    {
        await ApplyHeaderAvatarParityAfterRefreshAsync();
    }

    async void OnHeaderAvatarParityProfileChanged()
    {
        await ApplyHeaderAvatarParityAfterRefreshAsync();
    }

    async void OnHeaderAvatarParityStoreChanged(string playerId)
    {
        var currentUser = await ApplicationUserService.GetCurrentUserAsync();
        if (!string.Equals(
                playerId,
                currentUser.PlayerId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await ApplyHeaderAvatarParityAfterRefreshAsync();
    }

    async Task ApplyHeaderAvatarParityAfterRefreshAsync()
    {
        await Task.Delay(180);
        await ApplyHeaderAvatarParityAsync();
        await Task.Delay(260);
        await ApplyHeaderAvatarParityAsync();
    }

    async Task ApplyHeaderAvatarParityAsync()
    {
        try
        {
            var currentUser =
                await ApplicationUserService.GetCurrentUserAsync();

            if (string.IsNullOrWhiteSpace(currentUser.PlayerId))
                return;

            var visualIdentity =
                await PlayerVisualIdentityResolver.ResolveAsync(
                    currentUser.PlayerId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                NormalizeHeaderAvatarSurface();

                ApplyHeaderOverlay(
                    HeaderAvatarFrameOverlay,
                    visualIdentity.Frame?.PreviewImage);

                PlayerEffectEngine.Apply(
                    HeaderAvatarEffectOverlay,
                    visualIdentity.Effect,
                    HeaderAvatarParityScale);

                var avatarHost = HeaderPlayerAvatar.Parent as Grid;
                if (avatarHost != null)
                {
                    avatarHost.Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(
                            visualIdentity.Effect == null
                                ? Color.FromArgb("#D4AF37")
                                : Color.FromArgb("#F2C14E")),
                        Radius = visualIdentity.Effect == null ? 18 : 24,
                        Opacity = visualIdentity.Effect == null ? 0.45f : 0.65f
                    };
                }
            });
        }
        catch
        {
            // Header avatar parity must never block MainPage loading.
        }
    }

    void NormalizeHeaderAvatarSurface()
    {
        var avatarHost = HeaderPlayerAvatar.Parent as Grid;
        if (avatarHost == null)
            return;

        double avatarSize = DeviceInfo.Idiom == DeviceIdiom.Phone
            ? HeaderAvatarParityPhoneSize
            : HeaderAvatarParityTabletSize;

        avatarHost.WidthRequest = avatarSize;
        avatarHost.HeightRequest = avatarSize;
        avatarHost.VerticalOptions = LayoutOptions.Center;
        avatarHost.HorizontalOptions = LayoutOptions.Center;
        avatarHost.Clip = new EllipseGeometry
        {
            Center = new Point(avatarSize / 2, avatarSize / 2),
            RadiusX = avatarSize / 2,
            RadiusY = avatarSize / 2
        };

        HeaderPlayerAvatar.Aspect = Aspect.AspectFill;
        HeaderPlayerAvatar.WidthRequest = avatarSize;
        HeaderPlayerAvatar.HeightRequest = avatarSize;

        HeaderAvatarFrameOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarFrameOverlay.WidthRequest = avatarSize;
        HeaderAvatarFrameOverlay.HeightRequest = avatarSize;
        HeaderAvatarFrameOverlay.InputTransparent = true;

        HeaderAvatarEffectOverlay.Aspect = Aspect.AspectFit;
        HeaderAvatarEffectOverlay.WidthRequest = avatarSize;
        HeaderAvatarEffectOverlay.HeightRequest = avatarSize;
        HeaderAvatarEffectOverlay.InputTransparent = true;

        ProfileStatusBadge.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 11 : 13;
        ProfileStatusBadge.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 11 : 13;
    }
}

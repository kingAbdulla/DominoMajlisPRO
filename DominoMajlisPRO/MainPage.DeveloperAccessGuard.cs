using System.Collections.Generic;
using System.Linq;
using DominoMajlisPRO.Pages;

namespace DominoMajlisPRO;

public partial class MainPage
{
    const int DeveloperLogoHoldMilliseconds = 5000;

    bool developerAccessGuardInstalled;
    bool developerAccessOpening;
    Image? developerAccessLogoImage;
    CancellationTokenSource? developerAccessHoldToken;

    void ApplyDeveloperAccessGuard()
    {
        if (developerAccessGuardInstalled)
            return;

        var logoImage = FindImageBySource(Content, "domino_gold_icon.png");
        if (logoImage == null)
            return;

        developerAccessLogoImage = logoImage;
        logoImage.GestureRecognizers.Clear();

        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += async (sender, args) =>
            await BeginDeveloperAccessHoldAsync(sender);
        pointer.PointerReleased += (_, _) => CancelDeveloperAccessHold();
        pointer.PointerExited += (_, _) => CancelDeveloperAccessHold();

        logoImage.GestureRecognizers.Add(pointer);
        developerAccessGuardInstalled = true;
    }

    async Task BeginDeveloperAccessHoldAsync(object? sender)
    {
        CancelDeveloperAccessHold();

        if (!ReferenceEquals(sender, developerAccessLogoImage))
            return;

        if (developerAccessOpening)
            return;

        logoPressed = true;
        developerAccessHoldToken = new CancellationTokenSource();
        var token = developerAccessHoldToken.Token;

        try
        {
            await Task.Delay(DeveloperLogoHoldMilliseconds, token);

            if (token.IsCancellationRequested || !logoPressed)
                return;

            if (developerAccessLogoImage == null ||
                !ReferenceEquals(sender, developerAccessLogoImage))
                return;

            if (Navigation?.NavigationStack?.LastOrDefault() != this)
                return;

            developerAccessOpening = true;
            logoPressed = false;
            developerAccessHoldToken = null;

            await Navigation.PushAsync(new DeveloperLoginPage());
        }
        catch (TaskCanceledException)
        {
            // Expected when the hold is released, exits the logo, or the page changes.
        }
        finally
        {
            logoPressed = false;
            developerAccessOpening = false;
        }
    }

    void CancelDeveloperAccessHold()
    {
        logoPressed = false;

        if (developerAccessHoldToken == null)
            return;

        try
        {
            developerAccessHoldToken.Cancel();
            developerAccessHoldToken.Dispose();
        }
        catch
        {
            // Gesture cancellation must never affect MainPage.
        }
        finally
        {
            developerAccessHoldToken = null;
        }
    }

    void CancelDeveloperAccessOnMainPageExit()
    {
        CancelDeveloperAccessHold();
        developerAccessOpening = false;
    }

    static Image? FindImageBySource(View? root, string sourceName)
    {
        if (root == null)
            return null;

        if (root is Image image &&
            image.Source?.ToString()?.Contains(
                sourceName,
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return image;
        }

        foreach (var child in EnumerateDeveloperAccessChildViews(root))
        {
            var result = FindImageBySource(child, sourceName);
            if (result != null)
                return result;
        }

        return null;
    }

    static IEnumerable<View> EnumerateDeveloperAccessChildViews(View root)
    {
        switch (root)
        {
            case Layout layout:
                foreach (var child in layout.Children.OfType<View>())
                    yield return child;
                break;

            case Border border when border.Content is View borderContent:
                yield return borderContent;
                break;

            case ScrollView scrollView when scrollView.Content is View scrollContent:
                yield return scrollContent;
                break;

            case ContentView contentView when contentView.Content is View content:
                yield return content;
                break;
        }
    }
}

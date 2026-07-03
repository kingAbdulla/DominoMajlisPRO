using System.Collections.Generic;
using System.Linq;

namespace DominoMajlisPRO;

public partial class MainPage
{
    bool startMatchGuardApplied;

    void ApplyStartMatchProductionGuard()
    {
        if (startMatchGuardApplied)
            return;

        var startImage = FindStartMatchImage(Content);
        if (startImage?.Parent is not Border startBorder)
            return;

        startBorder.GestureRecognizers.Clear();
        startBorder.GestureRecognizers.Add(
            new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    if (!await ConfirmProductionMatchReadinessAsync())
                        return;

                    OnStartGame(startBorder, EventArgs.Empty);
                })
            });

        startMatchGuardApplied = true;
    }

    static Image? FindStartMatchImage(View? root)
    {
        if (root == null)
            return null;

        if (root is Image image &&
            image.Source?.ToString()?.Contains(
                "startmatch_gold.png",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return image;
        }

        foreach (var child in EnumerateChildViews(root))
        {
            var result = FindStartMatchImage(child);
            if (result != null)
                return result;
        }

        return null;
    }

    static IEnumerable<View> EnumerateChildViews(View root)
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

            case ContentView contentView when contentView.Content is View viewContent:
                yield return viewContent;
                break;
        }
    }
}

namespace DominoMajlisPRO;

public partial class MainPage
{
    bool startMatchGuardApplied;

    void ApplyStartMatchProductionGuard()
    {
        if (startMatchGuardApplied)
            return;

        var startImage = FindStartMatchImage(this);
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

    static Image? FindStartMatchImage(Element root)
    {
        if (root is Image image &&
            image.Source?.ToString()?.Contains(
                "startmatch_gold.png",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return image;
        }

        foreach (var child in root.LogicalChildren)
        {
            var result = FindStartMatchImage(child);
            if (result != null)
                return result;
        }

        return null;
    }
}

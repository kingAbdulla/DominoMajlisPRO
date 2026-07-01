using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class RankingsPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.TeamsChanged -= OnRankingsTypographyChanged;
        AppEvents.TeamAssetsChanged -= OnRankingsTeamTypographyChanged;
        AppEvents.TeamsChanged += OnRankingsTypographyChanged;
        AppEvents.TeamAssetsChanged += OnRankingsTeamTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnRankingsTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnRankingsTeamTypographyChanged(string teamId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class PlayerRankingsPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.PlayerProfileChanged -= OnPlayerRankingsTypographyChanged;
        AppEvents.StoreEconomyChanged -= OnPlayerRankingsStoreTypographyChanged;
        AppEvents.PlayerProfileChanged += OnPlayerRankingsTypographyChanged;
        AppEvents.StoreEconomyChanged += OnPlayerRankingsStoreTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnPlayerRankingsTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnPlayerRankingsStoreTypographyChanged(string playerId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class PlayerProfilesPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.PlayerProfileChanged -= OnProfilesTypographyChanged;
        AppEvents.StoreEconomyChanged -= OnProfilesStoreTypographyChanged;
        AppEvents.PlayerProfileChanged += OnProfilesTypographyChanged;
        AppEvents.StoreEconomyChanged += OnProfilesStoreTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnProfilesTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnProfilesStoreTypographyChanged(string playerId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class PlayerDetailsPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.PlayerProfileChanged -= OnPlayerDetailsTypographyChanged;
        AppEvents.StoreEconomyChanged -= OnPlayerDetailsStoreTypographyChanged;
        AppEvents.PlayerProfileChanged += OnPlayerDetailsTypographyChanged;
        AppEvents.StoreEconomyChanged += OnPlayerDetailsStoreTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnPlayerDetailsTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnPlayerDetailsStoreTypographyChanged(string playerId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class HistoryPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.TeamsChanged -= OnHistoryTypographyChanged;
        AppEvents.TeamAssetsChanged -= OnHistoryTeamTypographyChanged;
        AppEvents.TeamsChanged += OnHistoryTypographyChanged;
        AppEvents.TeamAssetsChanged += OnHistoryTeamTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnHistoryTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnHistoryTeamTypographyChanged(string teamId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class MatchDetailsPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.TeamsChanged -= OnMatchDetailsTypographyChanged;
        AppEvents.TeamAssetsChanged -= OnMatchDetailsTeamTypographyChanged;
        AppEvents.TeamsChanged += OnMatchDetailsTypographyChanged;
        AppEvents.TeamAssetsChanged += OnMatchDetailsTeamTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnMatchDetailsTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnMatchDetailsTeamTypographyChanged(string teamId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

public partial class HallOfFamePage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AppEvents.TeamsChanged -= OnHallTypographyChanged;
        AppEvents.TeamAssetsChanged -= OnHallTeamTypographyChanged;
        AppEvents.TeamsChanged += OnHallTypographyChanged;
        AppEvents.TeamAssetsChanged += OnHallTeamTypographyChanged;
        _ = NameTypographyPageScanner.ApplyDelayedAsync(this, 180);
    }

    async void OnHallTypographyChanged() => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
    async void OnHallTeamTypographyChanged(string teamId) => await NameTypographyPageScanner.ApplyDelayedAsync(this, 120);
}

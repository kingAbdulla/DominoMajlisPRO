using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class RuntimePlayerNameLabel : Label
{
    private string? _resolvedPlayerId;
    private bool _applying;

    public RuntimePlayerNameLabel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        AppEvents.PlayerProfileChanged -= OnIdentityChanged;
        AppEvents.PlayerProfileChanged += OnIdentityChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreEconomyChanged += OnStoreChanged;
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        AppEvents.PlayerProfileChanged -= OnIdentityChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
    }

    private async void OnIdentityChanged() => await RefreshAsync();

    private async void OnStoreChanged(string playerId)
    {
        if (string.IsNullOrWhiteSpace(_resolvedPlayerId) ||
            string.Equals(_resolvedPlayerId, playerId, StringComparison.OrdinalIgnoreCase))
        {
            await RefreshAsync();
        }
    }

    protected override async void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (!_applying && propertyName == TextProperty.PropertyName && IsLoaded)
            await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_applying)
            return;

        try
        {
            _applying = true;
            var user = await ApplicationUserService.GetCurrentUserAsync();
            _resolvedPlayerId = user.PlayerId;
            if (string.IsNullOrWhiteSpace(_resolvedPlayerId))
                return;

            var identity = await NameTypographyResolver.ResolvePlayerAsync(_resolvedPlayerId);
            IdentityPlateBinder.ApplyToLabel(this, Text ?? string.Empty, identity?.ResolvePreset());
        }
        catch
        {
            // Name typography must never break the host page.
        }
        finally
        {
            _applying = false;
        }
    }
}

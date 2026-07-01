using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class NameTypographyRuntime
{
    public static async Task ApplyPlayerAsync(Label label, string? playerId)
    {
        if (label == null || string.IsNullOrWhiteSpace(playerId))
            return;

        var identity = await NameTypographyResolver.ResolvePlayerAsync(playerId);
        ApplyToLabel(label, identity?.ResolvePreset());
    }

    public static async Task ApplyTeamAsync(Label label, string? teamId)
    {
        if (label == null || string.IsNullOrWhiteSpace(teamId))
            return;

        var identity = await NameTypographyResolver.ResolveTeamAsync(teamId);
        ApplyToLabel(label, identity?.ResolvePreset());
    }

    public static void ApplyToLabel(Label label, TypographyIdentityPreset? preset)
    {
        if (preset == null)
            return;

        var normalized = preset.Normalized();
        label.FontFamily = normalized.FontFamily;
        label.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 10, 34);
        label.TextColor = Color.FromArgb(normalized.PrimaryColor);
        label.Opacity = normalized.Opacity;

        // MAUI Label cannot host a frame without changing layout. Frame rendering is applied in cards/preview overlays;
        // label runtime safely applies font/material color/effect identity in-place.
    }
}

public sealed class RuntimePlayerNameLabel : Label
{
    public static readonly BindableProperty PlayerIdProperty =
        BindableProperty.Create(nameof(PlayerId), typeof(string), typeof(RuntimePlayerNameLabel), string.Empty, propertyChanged: OnIdentityChanged);

    public string PlayerId
    {
        get => (string)GetValue(PlayerIdProperty);
        set => SetValue(PlayerIdProperty, value);
    }

    public RuntimePlayerNameLabel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        AppEvents.PlayerProfileChanged -= OnRefresh;
        AppEvents.PlayerProfileChanged += OnRefresh;
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e) => AppEvents.PlayerProfileChanged -= OnRefresh;
    private async void OnRefresh() => await RefreshAsync();
    private static async void OnIdentityChanged(BindableObject bindable, object oldValue, object newValue) => await ((RuntimePlayerNameLabel)bindable).RefreshAsync();
    private async Task RefreshAsync() => await NameTypographyRuntime.ApplyPlayerAsync(this, PlayerId);
}

public sealed class RuntimeTeamNameLabel : Label
{
    public static readonly BindableProperty TeamIdProperty =
        BindableProperty.Create(nameof(TeamId), typeof(string), typeof(RuntimeTeamNameLabel), string.Empty, propertyChanged: OnIdentityChanged);

    public string TeamId
    {
        get => (string)GetValue(TeamIdProperty);
        set => SetValue(TeamIdProperty, value);
    }

    public RuntimeTeamNameLabel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        AppEvents.TeamsChanged -= OnRefresh;
        AppEvents.TeamsChanged += OnRefresh;
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e) => AppEvents.TeamsChanged -= OnRefresh;
    private async void OnRefresh() => await RefreshAsync();
    private static async void OnIdentityChanged(BindableObject bindable, object oldValue, object newValue) => await ((RuntimeTeamNameLabel)bindable).RefreshAsync();
    private async Task RefreshAsync() => await NameTypographyRuntime.ApplyTeamAsync(this, TeamId);
}

using System.Runtime.CompilerServices;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class NameTypographyRuntime
{
    private sealed class LabelRuntimeState
    {
        public int Version;
    }

    private static readonly ConditionalWeakTable<Label, LabelRuntimeState> RuntimeStates = new();

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

        var state = RuntimeStates.GetOrCreateValue(label);
        state.Version++;
        label.CancelAnimations();

        var normalized = preset.Normalized();
        var textColor = Color.FromArgb(normalized.TextColor);
        var shadowColor = Color.FromArgb(normalized.ShadowColor);
        label.FontFamily = normalized.FontFamily;
        label.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 10, 34);
        label.TextColor = textColor;
        label.Opacity = normalized.Opacity;
        label.Scale = 1;
        label.TranslationX = 0;
        label.TranslationY = 0;
        label.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(shadowColor.WithAlpha((float)Math.Clamp(0.24 + normalized.Intensity * 0.18, 0.24, 0.62))),
            Offset = new Point(0, 1),
            Radius = (float)Math.Clamp(7 + normalized.Intensity * 8, 7, 22),
            Opacity = (float)Math.Clamp(0.26 + normalized.Intensity * 0.16, 0.26, 0.58)
        };

        StartMotion(label, normalized, state.Version);
    }

    private static void StartMotion(Label label, TypographyIdentityPreset preset, int version)
    {
        var state = RuntimeStates.GetOrCreateValue(label);
        var motion = preset.MotionPreset?.Trim() ?? "None";
        var particles = preset.ParticlePreset?.Trim() ?? "None";
        var lighting = preset.LightingPreset?.Trim() ?? "None";
        var shouldAnimate = !string.Equals(motion, "None", StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(particles, "None", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lighting, "InnerGlow", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lighting, "SoftShine", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lighting, "TopSheen", StringComparison.OrdinalIgnoreCase);
        if (!shouldAnimate)
            return;

        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            while (state.Version == version && label.Handler != null)
            {
                var duration = (uint)Math.Clamp(900 / Math.Max(0.45, preset.Speed), 320, 1600);
                if (string.Equals(motion, "Breath", StringComparison.OrdinalIgnoreCase))
                {
                    await label.ScaleToAsync(1.0 + Math.Min(0.07, preset.Intensity * 0.045), duration, Easing.SinInOut);
                    await label.ScaleToAsync(1.0, duration, Easing.SinInOut);
                }
                else if (string.Equals(motion, "Pulse", StringComparison.OrdinalIgnoreCase))
                {
                    await label.ScaleToAsync(1.0 + Math.Min(0.09, preset.Intensity * 0.055), duration / 2, Easing.CubicOut);
                    await label.ScaleToAsync(1.0, duration / 2, Easing.CubicIn);
                }
                else if (string.Equals(particles, "Dust", StringComparison.OrdinalIgnoreCase))
                {
                    await label.TranslateToAsync(1.2, -0.8, duration / 2, Easing.SinInOut);
                    await label.TranslateToAsync(-1.0, 0.7, duration / 2, Easing.SinInOut);
                    await label.TranslateToAsync(0, 0, duration / 2, Easing.SinInOut);
                }
                else
                {
                    await label.FadeToAsync(Math.Clamp(preset.Opacity * 0.76, 0.35, 1), duration, Easing.SinInOut);
                    await label.FadeToAsync(preset.Opacity, duration, Easing.SinInOut);
                }
            }
        });
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

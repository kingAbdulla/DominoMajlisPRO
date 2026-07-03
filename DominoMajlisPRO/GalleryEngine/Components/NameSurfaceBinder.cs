using System.Runtime.CompilerServices;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class NameSurfaceBinder
{
    private sealed class BindingState
    {
        public RuntimeNameSurfaceView? Surface;
    }

    private static readonly ConditionalWeakTable<Label, BindingState> Bindings = new();

    public static void BindPlayer(Label label, string? playerId, string? displayText = null)
    {
        if (label == null)
            return;

        if (string.IsNullOrWhiteSpace(playerId))
        {
            RestoreLabel(label, displayText);
            return;
        }

        var surface = EnsureSurface(
            label,
            () => new RuntimePlayerNameSurfaceView());
        if (surface is RuntimePlayerNameSurfaceView playerSurface)
        {
            playerSurface.PlayerId = playerId.Trim();
            playerSurface.DisplayText = ResolveDisplayText(displayText);
        }
    }

    public static void BindTeam(Label label, string? teamId, string? displayText = null)
    {
        if (label == null)
            return;

        if (string.IsNullOrWhiteSpace(teamId))
        {
            RestoreLabel(label, displayText);
            return;
        }

        var surface = EnsureSurface(
            label,
            () => new RuntimeTeamNameSurfaceView());
        if (surface is RuntimeTeamNameSurfaceView teamSurface)
        {
            teamSurface.TeamId = teamId.Trim();
            teamSurface.DisplayText = ResolveDisplayText(displayText);
        }
    }

    public static RuntimePlayerNameSurfaceView PlayerSurface(
        string? playerId,
        string displayText,
        double? heightRequest = null)
    {
        var surface = new RuntimePlayerNameSurfaceView
        {
            PlayerId = playerId?.Trim() ?? string.Empty,
            DisplayText = displayText,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };
        ApplyInlineSizing(surface, displayText, heightRequest);
        return surface;
    }

    public static RuntimeTeamNameSurfaceView TeamSurface(
        string? teamId,
        string displayText,
        double? heightRequest = null)
    {
        var surface = new RuntimeTeamNameSurfaceView
        {
            TeamId = teamId?.Trim() ?? string.Empty,
            DisplayText = displayText,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };
        ApplyInlineSizing(surface, displayText, heightRequest);
        return surface;
    }

    private static RuntimeNameSurfaceView EnsureSurface(
        Label label,
        Func<RuntimeNameSurfaceView> create)
    {
        var state = Bindings.GetOrCreateValue(label);
        if (state.Surface != null)
            return state.Surface;

        var surface = create();
        MirrorLayout(label, surface);

        if (label.Parent is Grid grid)
        {
            Grid.SetRow(surface, Grid.GetRow(label));
            Grid.SetColumn(surface, Grid.GetColumn(label));
            Grid.SetRowSpan(surface, Grid.GetRowSpan(label));
            Grid.SetColumnSpan(surface, Grid.GetColumnSpan(label));
            surface.ZIndex = label.ZIndex + 1;
            grid.Children.Add(surface);
            label.IsVisible = false;
        }
        else if (label.Parent is Layout layout)
        {
            var index = layout.Children.IndexOf(label);
            if (index < 0)
                layout.Children.Add(surface);
            else
                layout.Children.Insert(index + 1, surface);
            label.IsVisible = false;
        }
        else
        {
            state.Surface = surface;
            return surface;
        }

        state.Surface = surface;
        return surface;
    }

    private static void MirrorLayout(Label label, RuntimeNameSurfaceView surface)
    {
        var text = ResolveDisplayText(label.Text);
        surface.Margin = label.Margin;
        surface.MinimumWidthRequest = label.MinimumWidthRequest;
        surface.MinimumHeightRequest = 0;
        surface.MaximumHeightRequest = InlineHeight(label.FontSize);
        surface.WidthRequest = -1;
        surface.HeightRequest = label.HeightRequest > 0
            ? Math.Min(label.HeightRequest, InlineHeight(label.FontSize))
            : InlineHeight(label.FontSize);
        surface.MaximumWidthRequest = label.MaximumWidthRequest > 0
            ? Math.Min(label.MaximumWidthRequest, InlineWidth(text, label.FontSize))
            : InlineWidth(text, label.FontSize);

        surface.HorizontalOptions = label.HorizontalTextAlignment == TextAlignment.Center
            ? LayoutOptions.Center
            : label.HorizontalOptions.Alignment == LayoutAlignment.Fill
                ? LayoutOptions.Start
                : label.HorizontalOptions;
        surface.VerticalOptions = label.VerticalOptions;
        surface.FlowDirection = label.FlowDirection;
        surface.InputTransparent = true;
    }

    private static void ApplyInlineSizing(RuntimeNameSurfaceView surface, string? displayText, double? heightRequest)
    {
        var fontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 11d : 13d;
        surface.HeightRequest = heightRequest.HasValue
            ? Math.Min(heightRequest.Value, InlineHeight(fontSize))
            : InlineHeight(fontSize);
        surface.MaximumWidthRequest = InlineWidth(displayText, fontSize);
        surface.WidthRequest = -1;
    }

    private static double InlineHeight(double fontSize) => Math.Clamp(fontSize + 8, 22, 28);

    private static double InlineWidth(string? text, double fontSize)
    {
        var length = Math.Clamp((text ?? string.Empty).Trim().Length, 1, 14);
        var estimated = 18 + (length * Math.Clamp(fontSize * 0.58, 6, 9));
        var max = DeviceInfo.Idiom == DeviceIdiom.Phone ? 112 : 160;
        return Math.Clamp(estimated, 34, max);
    }

    private static void RestoreLabel(Label label, string? displayText)
    {
        if (!string.IsNullOrWhiteSpace(displayText))
            label.Text = displayText;

        if (!Bindings.TryGetValue(label, out var state) || state.Surface == null)
        {
            label.IsVisible = true;
            return;
        }

        state.Surface.IsVisible = false;
        label.IsVisible = true;
    }

    private static string ResolveDisplayText(string? displayText) =>
        displayText?.Trim() ?? string.Empty;
}

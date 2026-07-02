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
            playerSurface.DisplayText = ResolveDisplayText(label, displayText);
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
            teamSurface.DisplayText = ResolveDisplayText(label, displayText);
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
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        if (heightRequest.HasValue)
            surface.HeightRequest = heightRequest.Value;
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
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        if (heightRequest.HasValue)
            surface.HeightRequest = heightRequest.Value;
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
        surface.Margin = label.Margin;
        surface.MinimumWidthRequest = label.MinimumWidthRequest;
        surface.MaximumWidthRequest = label.MaximumWidthRequest;
        surface.MinimumHeightRequest = label.MinimumHeightRequest;
        surface.MaximumHeightRequest = label.MaximumHeightRequest;
        surface.WidthRequest = label.WidthRequest;
        surface.HeightRequest = label.HeightRequest > 0
            ? label.HeightRequest
            : Math.Max(26, label.FontSize + 12);
        surface.HorizontalOptions = label.HorizontalOptions;
        surface.VerticalOptions = label.VerticalOptions;
        surface.FlowDirection = label.FlowDirection;
        surface.InputTransparent = label.InputTransparent;
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

    private static string ResolveDisplayText(Label label, string? displayText) =>
        string.IsNullOrWhiteSpace(displayText)
            ? (label.Text ?? string.Empty)
            : displayText.Trim();
}

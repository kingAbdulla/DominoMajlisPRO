using System.ComponentModel;
using System.Runtime.CompilerServices;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Handlers;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class NameTypographyGlobalLabelMapper
{
    private sealed class HookState
    {
        public int Version;
        public bool Hooked;
    }

    private static readonly ConditionalWeakTable<Label, HookState> Hooks = new();

    [ModuleInitializer]
    public static void Initialize()
    {
        LabelHandler.Mapper.AppendToMapping("DominoNameTypographyRuntime", (handler, view) =>
        {
            if (view is Label label)
                Hook(label);
        });
    }

    private static void Hook(Label label)
    {
        var state = Hooks.GetOrCreateValue(label);
        if (state.Hooked)
            return;

        state.Hooked = true;
        label.Loaded += async (_, _) => await RefreshAsync(label, state);
        label.Unloaded += (_, _) => state.Version++;
        label.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == Label.TextProperty.PropertyName)
                await RefreshAsync(label, state);
        };
    }

    private static async Task RefreshAsync(Label label, HookState state)
    {
        var version = ++state.Version;
        await Task.Delay(80);
        if (version != state.Version || label.Handler == null)
            return;

        var visibleText = Normalize(label.Text);
        if (string.IsNullOrWhiteSpace(visibleText) || visibleText.Length > 48)
            return;

        try
        {
            var players = await PlayerProfileService.LoadPlayersAsync();
            var player = players.FirstOrDefault(item => SameVisibleName(visibleText, item.PlayerName));
            if (player != null)
            {
                await NameTypographyRuntime.ApplyPlayerAsync(label, player.PlayerId);
                return;
            }

            var teams = await TeamProfileService.LoadTeamsAsync();
            var team = teams.FirstOrDefault(item => SameVisibleName(visibleText, item.TeamName));
            if (team != null)
                await NameTypographyRuntime.ApplyTeamAsync(label, team.TeamId);
        }
        catch
        {
            // Visual typography must never crash runtime pages.
        }
    }

    private static bool SameVisibleName(string visibleText, string? sourceName)
    {
        var source = Normalize(sourceName);
        if (string.IsNullOrWhiteSpace(source))
            return false;

        if (string.Equals(visibleText, source, StringComparison.OrdinalIgnoreCase))
            return true;

        if (source.Length > 14)
        {
            var truncated = source[..14] + "...";
            return string.Equals(visibleText, truncated, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal);
    }
}

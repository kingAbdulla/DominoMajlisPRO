using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerManagementService
{
    public static async Task<bool> CanManagePlayersAsync()
    {
        var identity =
            await HonorIdentityService.LoadAsync();

        return identity.IsActivated &&
               (identity.Role == HonorRoleType.Developer ||
                identity.Role == HonorRoleType.Founder);
    }

    public static async Task DeletePlayerAsync(string playerId)
    {
        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x => x.PlayerId == playerId);

        if (player == null)
            return;

        players.Remove(player);

        await PlayerProfileService.SavePlayersAsync(players);

        await RemovePlayerIdFromTeamsAsync(playerId);

        AppEvents.RaiseDataChanged();
    }

    public static async Task DeleteAllPlayersAsync()
    {
        await PlayerProfileService.SavePlayersAsync(
            new List<PlayerProfileModel>());

        await ClearAllPlayerIdsFromTeamsAsync();

        AppEvents.RaiseDataChanged();
    }

    static async Task RemovePlayerIdFromTeamsAsync(string playerId)
    {
        var teams =
            await TeamProfileService.LoadTeamsAsync();

        bool changed = false;

        foreach (var team in teams)
        {
            if (team.Player1Id == playerId)
            {
                team.Player1Id = "";
                changed = true;
            }

            if (team.Player2Id == playerId)
            {
                team.Player2Id = "";
                changed = true;
            }
        }

        if (changed)
        {
            await TeamProfileService.SaveTeamsAsync(teams);
        }
    }

    static async Task ClearAllPlayerIdsFromTeamsAsync()
    {
        var teams =
            await TeamProfileService.LoadTeamsAsync();

        foreach (var team in teams)
        {
            team.Player1Id = "";
            team.Player2Id = "";
        }

        await TeamProfileService.SaveTeamsAsync(teams);
    }
}
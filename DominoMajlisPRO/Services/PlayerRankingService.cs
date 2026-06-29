using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

/// <summary>
/// Central service that aggregates player ranking data for the
/// PlayerRankingsPage. It is the single source of leaderboard data: it pulls
/// player profiles (kept in sync with team results), assigns positions, and
/// returns a ready-to-render <see cref="PlayerRankingSnapshot"/>. The page does
/// not compute any ranking logic itself, and rank tiers always come from the
/// shared <see cref="PlayerRankService"/>.
/// </summary>
public static class PlayerRankingService
{
    public static async Task<PlayerRankingSnapshot> GetSnapshotAsync(
        PlayerRankingFilter filter = PlayerRankingFilter.All)
    {
        // Make sure player profiles reflect the latest match / team results
        // before we read them, so the leaderboard never shows stale XP.
        try
        {
            await PlayerTeamSyncService.SyncPlayersFromTeamsAsync();
        }
        catch
        {
            // Sync is best-effort; fall back to whatever is persisted.
        }

        var players =
            await PlayerProfileService.LoadPlayersAsync();

        string? currentPlayerId =
            await ApplicationUserService.GetCurrentUserPlayerIdAsync();

        var ranked = players
            .Where(p => !string.IsNullOrWhiteSpace(p.PlayerId))
            .ToList();

        var ordered = ApplyFilter(ranked, filter, currentPlayerId);

        var entries = new List<PlayerRankingEntry>(ordered.Count);
        int position = 1;
        foreach (var player in ordered)
        {
            entries.Add(MapEntry(player, position, currentPlayerId));
            position++;
        }

        int coins = 0;
        int gems = 0;
        if (!string.IsNullOrWhiteSpace(currentPlayerId))
        {
            try
            {
                var wallet =
                    await PlayerWalletService.GetOrCreateAsync(currentPlayerId);
                coins = wallet.Coins;
                gems = wallet.Gems;
            }
            catch
            {
                // Wallet is optional for the header chips.
            }
        }

        return new PlayerRankingSnapshot
        {
            Entries = entries,
            Filter = filter,
            CurrentPlayerId = currentPlayerId,
            Coins = coins,
            Gems = gems
        };
    }

    static List<PlayerProfileModel> ApplyFilter(
        List<PlayerProfileModel> players,
        PlayerRankingFilter filter,
        string? currentPlayerId)
    {
        IEnumerable<PlayerProfileModel> query = players;

        switch (filter)
        {
            case PlayerRankingFilter.Friends:
                // Friends are not wired yet; the page shows a premium empty
                // state. Returning nothing here keeps the contract honest.
                query = players.Where(p => false);
                break;

            case PlayerRankingFilter.Season:
                query = players
                    .OrderByDescending(p => p.SeasonXP)
                    .ThenByDescending(p => p.PlayerXP);
                break;

            case PlayerRankingFilter.TopWins:
                query = players
                    .OrderByDescending(p => p.Wins)
                    .ThenByDescending(p => p.PlayerXP);
                break;

            case PlayerRankingFilter.TopTrust:
                query = players
                    .OrderByDescending(p => p.TrustScore)
                    .ThenByDescending(p => p.PlayerXP);
                break;

            case PlayerRankingFilter.MostActive:
                query = players
                    .OrderByDescending(p => p.TotalMatches)
                    .ThenByDescending(p => p.LastActiveAt);
                break;

            case PlayerRankingFilter.TopXP:
            case PlayerRankingFilter.All:
            default:
                query = players
                    .OrderByDescending(p => p.PlayerXP)
                    .ThenByDescending(p => p.Wins);
                break;
        }

        return query.ToList();
    }

    static PlayerRankingEntry MapEntry(
        PlayerProfileModel player,
        int position,
        string? currentPlayerId)
    {
        PlayerEngine.Normalize(player);

        return new PlayerRankingEntry
        {
            Position = position,
            PlayerId = player.PlayerId,
            DisplayName = string.IsNullOrWhiteSpace(player.PlayerName)
                ? player.PlayerId
                : player.PlayerName,
            Player = player,
            XP = player.PlayerXP,
            Level = player.PlayerLevel,
            Legacy = player.LegacyScore,
            Wins = player.Wins,
            Losses = player.Losses,
            Matches = player.TotalMatches,
            WinRate = player.WinRate,
            TrustScore = player.TrustScore,
            Status = player.ProfileStatus,
            IsDeveloper = player.IsDeveloper ||
                player.ProfileStatus == PlayerProfileStatus.Developer,
            IsFounder = player.IsFounder ||
                player.ProfileStatus == PlayerProfileStatus.Founder,
            IsHonor = player.ProfileStatus == PlayerProfileStatus.Honor,
            IsCurrentUser = !string.IsNullOrWhiteSpace(currentPlayerId) &&
                string.Equals(
                    player.PlayerId,
                    currentPlayerId,
                    StringComparison.OrdinalIgnoreCase),
            IsFriend = false,
            IsOnline = false,
            LastSeen = player.LastActiveAt
        };
    }
}

using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerEngine
{
    public const int CurrentProfileVersion = 1;

    // =========================
    // NORMALIZE / MIGRATE
    // =========================

    public static void Normalize(PlayerProfileModel player)
    {
        if (player == null)
            return;

        player.PlayerName =
            player.PlayerName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(player.AvatarImage))
            player.AvatarImage = "player_card.png";

        if (player.TrustScore <= 0)
            player.TrustScore = 100;

        if (player.PlayerLevel <= 0)
            player.PlayerLevel = 1;

        UpdateProfileCompletion(player);
        UpdateStatus(player);
        UpdateRank(player);
        UpdateLegacyScore(player);
    }

    // =========================
    // PROFILE COMPLETION
    // =========================

    public static void UpdateProfileCompletion(
        PlayerProfileModel player)
    {
        bool hasName =
            !string.IsNullOrWhiteSpace(player.PlayerName);

        bool hasCustomImage =
            !string.IsNullOrWhiteSpace(player.ProfileImagePath) &&
            File.Exists(player.ProfileImagePath);

        bool hasBuiltInAvatar =
            !string.IsNullOrWhiteSpace(player.AvatarImage) &&
            player.AvatarImage != "player_card.png";

        player.IsProfileCompleted =
            hasName &&
            (hasCustomImage || hasBuiltInAvatar);
    }

    // =========================
    // STATUS
    // =========================

    public static void UpdateStatus(
        PlayerProfileModel player)
    {
        if (player.IsDeveloper)
        {
            player.ProfileStatus =
                PlayerProfileStatus.Developer;
            return;
        }

        if (player.IsFounder)
        {
            player.ProfileStatus =
                PlayerProfileStatus.Founder;
            return;
        }

        if (!string.IsNullOrWhiteSpace(player.HonorOwnerId))
        {
            player.ProfileStatus =
                PlayerProfileStatus.Honor;
            return;
        }

        if (!player.IsProfileCompleted)
        {
            player.ProfileStatus =
                PlayerProfileStatus.Ghost;
            return;
        }

        player.ProfileStatus =
            PlayerProfileStatus.Normal;
    }

    public static string GetStatusDisplayName(
        PlayerProfileStatus status)
    {
        return status switch
        {
            PlayerProfileStatus.Developer => "Developer",
            PlayerProfileStatus.Founder => "Founder",
            PlayerProfileStatus.Honor => "Honor",
            PlayerProfileStatus.Ghost => "Ghost",
            _ => "Normal Player"
        };
    }

    public static string GetStatusIcon(
        PlayerProfileStatus status)
    {
        return status switch
        {
            PlayerProfileStatus.Developer => "developer_founder_combo.png",
            PlayerProfileStatus.Founder => "founder_gold.png",
            PlayerProfileStatus.Honor => "halloffame_gold.png",
            PlayerProfileStatus.Ghost => "player_card.png",
            _ => "player_card.png"
        };
    }

    public static string GetStatusColor(
        PlayerProfileStatus status)
    {
        return status switch
        {
            PlayerProfileStatus.Developer => "#FF4FD8",
            PlayerProfileStatus.Founder => "#FFD700",
            PlayerProfileStatus.Honor => "#D4AF37",
            PlayerProfileStatus.Ghost => "#777777",
            _ => "#C8B58A"
        };
    }

    public static int GetStatusOrder(
        PlayerProfileStatus status)
    {
        return status switch
        {
            PlayerProfileStatus.Developer => 1,
            PlayerProfileStatus.Founder => 2,
            PlayerProfileStatus.Honor => 3,
            PlayerProfileStatus.Normal => 4,
            PlayerProfileStatus.Ghost => 5,
            _ => 99
        };
    }

    // =========================
    // XP / RANK
    // =========================

    public static void AddMatchXP(
        PlayerProfileModel player,
        bool wonMatch)
    {
        int xp =
            wonMatch ? 25 : 8;

        player.PlayerXP += xp;
        player.SeasonXP += xp;
        player.LifetimeXP += xp;

        player.LastActiveAt =
            DateTime.Now;

        UpdateRank(player);
        UpdateLegacyScore(player);
    }

    public static void UpdateRank(
        PlayerProfileModel player)
    {
        PlayerRankService.ApplyToPlayer(player);
    }

    public static PlayerRankResult GetRankResult(
        PlayerProfileModel player)
    {
        return PlayerRankService.Calculate(
            player.PlayerXP);
    }

    // =========================
    // STATS
    // =========================

    public static void ApplyMatchResult(
        PlayerProfileModel player,
        bool wonMatch)
    {
        player.TotalMatches++;

        if (wonMatch)
        {
            player.Wins++;
            player.CurrentWinStreak++;

            player.BestWinStreak =
                Math.Max(
                    player.BestWinStreak,
                    player.CurrentWinStreak);
        }
        else
        {
            player.Losses++;
            player.CurrentWinStreak = 0;
        }

        player.WinRate =
            player.TotalMatches == 0
                ? 0
                : Math.Round(
                    (double)player.Wins /
                    player.TotalMatches * 100,
                    2);

        AddMatchXP(player, wonMatch);

        Normalize(player);
    }

    // =========================
    // LEGACY
    // =========================

    public static void UpdateLegacyScore(
        PlayerProfileModel player)
    {
        player.LegacyScore =
            player.PlayerXP +
            player.Wins * 30 +
            player.BestWinStreak * 20 +
            player.HallOfFameCount * 150 +
            player.RankTitles * 100 +
            player.ChampionCount * 200 +
            player.HallOfLegendsPoints * 3 +
            player.TrustScore;
    }

    // =========================
    // HALL OF FAME / LEGENDS
    // =========================

    public static bool IsEligibleForHallOfLegends(
        PlayerProfileModel player)
    {
        Normalize(player);

        if (player.ProfileStatus == PlayerProfileStatus.Ghost)
            return false;

        if (player.TrustScore < 95)
            return false;

        if (player.TotalMatches < 30)
            return false;

        if (player.WinRate < 60)
            return false;

        if (player.LegacyScore < 1500)
            return false;

        return true;
    }

    // =========================
    // SORTING
    // =========================

    public static List<PlayerProfileModel> SortForDisplay(
        List<PlayerProfileModel> players)
    {
        foreach (var player in players)
            Normalize(player);

        return players
            .OrderBy(x => GetStatusOrder(x.ProfileStatus))
            .ThenByDescending(x => x.PlayerXP)
            .ThenByDescending(x => x.LegacyScore)
            .ThenByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Wins)
            .ToList();
    }

    // =========================
    // IMAGE
    // =========================

    public static ImageSource GetImageSource(
        PlayerProfileModel player)
    {
        var imagePath =
            player.UseCustomAvatar &&
            !string.IsNullOrWhiteSpace(player.AvatarPath)
                ? player.AvatarPath
                : !string.IsNullOrWhiteSpace(player.ProfileImagePath)
                    ? player.ProfileImagePath
                    : !string.IsNullOrWhiteSpace(player.AvatarImage)
                        ? player.AvatarImage
                        : !string.IsNullOrWhiteSpace(player.BuiltInAvatar)
                            ? player.BuiltInAvatar
                            : "player_card.png";

        return global::DominoMajlisPRO.GalleryEngine.Services
            .InventoryDisplayResolver.ResolveImageSource(
                imagePath,
                "player_card.png");
    }
}

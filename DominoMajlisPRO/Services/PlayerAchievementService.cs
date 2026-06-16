using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerAchievementService
{
    public static List<PlayerAchievementModel> GetAchievements(
        PlayerProfileModel player)
    {
        PlayerEngine.Normalize(player);

        var achievements =
            new List<PlayerAchievementModel>
            {
                new()
                {
                    Title = "أول 10 مباريات",
                    Description = "العب 10 مباريات",
                    Icon = "joystick_gold.png",
                    IsUnlocked = player.TotalMatches >= 10,
                    ProgressText = $"{Math.Min(player.TotalMatches, 10)} / 10"
                },

                new()
                {
                    Title = "أول 10 انتصارات",
                    Description = "حقق 10 انتصارات",
                    Icon = "wins_gold.png",
                    IsUnlocked = player.Wins >= 10,
                    ProgressText = $"{Math.Min(player.Wins, 10)} / 10"
                },

                new()
                {
                    Title = "لاعب موثوق",
                    Description = "حافظ على Win Rate 60% أو أكثر",
                    Icon = "trust_gold.png",
                    IsUnlocked = player.WinRate >= 60 && player.TotalMatches >= 10,
                    ProgressText = $"{player.WinRate:0}%"
                },

                new()
                {
                    Title = "محارب المجلس",
                    Description = "العب 30 مباراة",
                    Icon = "activity_gold.png",
                    IsUnlocked = player.TotalMatches >= 30,
                    ProgressText = $"{Math.Min(player.TotalMatches, 30)} / 30"
                },

                new()
                {
                    Title = "أسطورة ناشئة",
                    Description = "اجمع 1000 Legacy",
                    Icon = "halloffame_gold.png",
                    IsUnlocked = player.LegacyScore >= 1000,
                    ProgressText = $"{Math.Min(player.LegacyScore, 1000)} / 1000"
                },

                new()
                {
                    Title = "Hall Of Fame",
                    Description = "ادخل قاعة الأساطير",
                    Icon = "hall_of_fame_gold.png",
                    IsUnlocked = player.HallOfFameCount > 0,
                    ProgressText = player.HallOfFameCount.ToString()
                }
            };

        foreach (var achievement in achievements)
        {
            if (!achievement.IsUnlocked)
                continue;

            if (player.AchievementHistory.Contains(achievement.Title))
                continue;

            PlayerIdentityHistoryService.AddAchievementHistory(
                player,
                achievement.Title);

            PlayerTimelineService.AddEvent(
                player,
                "إنجاز جديد",
                $"تم فتح إنجاز: {achievement.Title}",
                "🏆",
                "#00C853");
        }

        return achievements;
    }
}
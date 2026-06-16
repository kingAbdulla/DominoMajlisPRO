using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class AvatarService
{
    public static List<string> GetCategories()
    {
        return new()
        {
            "All",
            "Normal",
            "Men",
            "Women",
            "Royal",
            "Legend",
            "Military",
            "Domino"
        };
    }

    public static List<AvatarItemModel> GetAll()
    {
        List<AvatarItemModel> avatars = new();

        AddRange(avatars, "Normal", "normal_avatar", 1, 4);
        AddRange(avatars, "Men", "man_avatar", 1, 10);
        AddRange(avatars, "Women", "woman_avatar", 1, 10);
        AddRange(avatars, "Royal", "royal_avatar", 1, 10);
        AddRange(avatars, "Legend", "legend_avatar", 1, 7);
        AddRange(avatars, "Military", "military_avatar", 1, 5);
        AddRange(avatars, "Domino", "domino_avatar", 1, 5);

        return avatars;
    }

    public static List<AvatarItemModel> GetByCategory(
        string category)
    {
        var avatars =
            GetAll();

        if (string.IsNullOrWhiteSpace(category) ||
            category == "All")
        {
            return avatars;
        }

        return avatars
            .Where(x => x.Category == category)
            .ToList();
    }

    public static AvatarItemModel? GetById(
        string avatarId)
    {
        return GetAll()
            .FirstOrDefault(x =>
                x.Id == avatarId);
    }

    public static string GetImageById(
        string avatarId)
    {
        var avatar =
            GetById(avatarId);

        return avatar == null
            ? "player_card.png"
            : avatar.Image;
    }

    static void AddRange(
        List<AvatarItemModel> avatars,
        string category,
        string prefix,
        int start,
        int end)
    {
        for (int i = start; i <= end; i++)
        {
            string id =
                $"{prefix}_{i}";

            string fileName =
                $"{prefix}_{i}.png";

            avatars.Add(
                new AvatarItemModel
                {
                    Id = id,
                    Category = category,
                    DisplayName = $"{category} #{i}",
                    Image = fileName,
                    IsUnlocked = true
                });
        }
    }
}
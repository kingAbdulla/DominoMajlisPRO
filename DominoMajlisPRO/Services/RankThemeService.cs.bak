using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.Services;

public class RankTheme
{
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color GlowColor { get; set; }
    public Color ProgressColor { get; set; }
    public Color TrustRingColor { get; set; }
    public Color TextColor { get; set; }
}

public static class RankThemeService
{
    public static RankTheme GetTheme(string? rank)
    {
        rank ??= "Unranked";

        string tier = GetTier(rank);

        return GetBaseTheme(rank, tier);
    }
    static RankTheme GetBaseTheme(
     string? rank,
     string tier)
    {
        rank ??= "Unranked";
        // =====================
        // BRONZE
        // =====================

        if (rank.StartsWith("Bronze"))
        {
            return tier switch
            {
                "I" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#1E1612"),

                    BorderColor =
                        Color.FromArgb("#8B5A2B"),

                    GlowColor =
                        Color.FromArgb("#8B5A2B"),

                    ProgressColor =
                        Color.FromArgb("#A97142"),

                    TrustRingColor =
                        Color.FromArgb("#A97142"),

                    TextColor =
                        Colors.White
                },

                "II" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#241A14"),

                    BorderColor =
                        Color.FromArgb("#B87333"),

                    GlowColor =
                        Color.FromArgb("#B87333"),

                    ProgressColor =
                        Color.FromArgb("#C67C3A"),

                    TrustRingColor =
                        Color.FromArgb("#C67C3A"),

                    TextColor =
                        Colors.White
                },

                _ => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#2A1C14"),

                    BorderColor =
                        Color.FromArgb("#CD7F32"),

                    GlowColor =
                        Color.FromArgb("#CD7F32"),

                    ProgressColor =
                        Color.FromArgb("#D88A3F"),

                    TrustRingColor =
                        Color.FromArgb("#D88A3F"),

                    TextColor =
                        Colors.White
                }
            };
        }

        // =====================
        // SILVER
        // =====================

        if (rank.StartsWith("Silver"))
        {
            return tier switch
            {
                "I" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#171A1E"),

                    BorderColor =
                        Color.FromArgb("#8A8A8A"),

                    GlowColor =
                        Color.FromArgb("#8A8A8A"),

                    ProgressColor =
                        Color.FromArgb("#A0A0A0"),

                    TrustRingColor =
                        Color.FromArgb("#A0A0A0"),

                    TextColor =
                        Colors.White
                },

                "II" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#1B1E22"),

                    BorderColor =
                        Color.FromArgb("#B0B0B0"),

                    GlowColor =
                        Color.FromArgb("#B0B0B0"),

                    ProgressColor =
                        Color.FromArgb("#C0C0C0"),

                    TrustRingColor =
                        Color.FromArgb("#C0C0C0"),

                    TextColor =
                        Colors.White
                },

                _ => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#1F2328"),

                    BorderColor =
                        Color.FromArgb("#D3D3D3"),

                    GlowColor =
                        Color.FromArgb("#D3D3D3"),

                    ProgressColor =
                        Color.FromArgb("#E0E0E0"),

                    TrustRingColor =
                        Color.FromArgb("#E0E0E0"),

                    TextColor =
                        Colors.White
                }
            };
        }

        // =====================
        // GOLD
        // =====================

        if (rank.StartsWith("Gold"))
        {
            return tier switch
            {
                "I" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#231F10"),

                    BorderColor =
                        Color.FromArgb("#C7A63A"),

                    GlowColor =
                        Color.FromArgb("#C7A63A"),

                    ProgressColor =
                        Color.FromArgb("#D6B44A"),

                    TrustRingColor =
                        Color.FromArgb("#D6B44A"),

                    TextColor =
                        Colors.White
                },

                "II" => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#29220F"),

                    BorderColor =
                        Color.FromArgb("#FFD700"),

                    GlowColor =
                        Color.FromArgb("#FFD700"),

                    ProgressColor =
                        Color.FromArgb("#FFD700"),

                    TrustRingColor =
                        Color.FromArgb("#FFD700"),

                    TextColor =
                        Colors.White
                },

                _ => new RankTheme
                {
                    BackgroundColor =
                        Color.FromArgb("#32270A"),

                    BorderColor =
                        Color.FromArgb("#FFE45C"),

                    GlowColor =
                        Color.FromArgb("#FFE45C"),

                    ProgressColor =
                        Color.FromArgb("#FFE45C"),

                    TrustRingColor =
                        Color.FromArgb("#FFE45C"),

                    TextColor =
                        Colors.White
                }
            };
        }

        // =====================
        // PLATINUM
        // =====================

        if (rank.StartsWith("Platinum"))
        {
            return new RankTheme
            {
                BackgroundColor =
                    Color.FromArgb("#17102A"),

                BorderColor =
                    Color.FromArgb("#A259FF"),

                GlowColor =
                    Color.FromArgb("#A259FF"),

                ProgressColor =
                    Color.FromArgb("#A259FF"),

                TrustRingColor =
                    Color.FromArgb("#A259FF"),

                TextColor =
                    Colors.White
            };
        }

        // =====================
        // DIAMOND
        // =====================

        if (rank.StartsWith("Diamond"))
        {
            return new RankTheme
            {
                BackgroundColor =
                    Color.FromArgb("#091A35"),

                BorderColor =
                    Color.FromArgb("#3A86FF"),

                GlowColor =
                    Color.FromArgb("#3A86FF"),

                ProgressColor =
                    Color.FromArgb("#3A86FF"),

                TrustRingColor =
                    Color.FromArgb("#3A86FF"),

                TextColor =
                    Colors.White
            };
        }

        // =====================
        // MASTER / LEGEND
        // =====================

        return new RankTheme
        {
            BackgroundColor =
                Color.FromArgb("#101526"),

            BorderColor =
                Color.FromArgb("#FFD700"),

            GlowColor =
                Color.FromArgb("#FFD700"),

            ProgressColor =
                Color.FromArgb("#FFD700"),

            TrustRingColor =
                Color.FromArgb("#FFD700"),

            TextColor =
                Colors.White
        };
    }

    static string GetTier(string? rank)
    {
        if (string.IsNullOrWhiteSpace(rank))
            return "";

        if (rank.EndsWith("III"))
            return "III";

        if (rank.EndsWith("II"))
            return "II";

        if (rank.EndsWith("I"))
            return "I";

        return "";
    }
}
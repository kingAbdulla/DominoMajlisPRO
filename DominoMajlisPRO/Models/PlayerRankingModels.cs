namespace DominoMajlisPRO.Models;

/// <summary>
/// Sorting / scope options for the player rankings leaderboard.
/// </summary>
public enum PlayerRankingFilter
{
    All = 0,
    Friends = 1,
    Season = 2,
    TopXP = 3,
    TopWins = 4,
    TopTrust = 5,
    MostActive = 6
}

/// <summary>
/// One player row in the rankings leaderboard. This is a read-only view model
/// produced by <see cref="DominoMajlisPRO.Services.PlayerRankingService"/>.
/// The page never recomputes ranking logic from this object — it only renders.
/// Identity is always bound to <see cref="PlayerId"/>, never to the name.
/// </summary>
public sealed class PlayerRankingEntry
{
    public int Position { get; set; }

    public string PlayerId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    /// <summary>
    /// The underlying profile, kept for canonical avatar/identity resolution
    /// (same path used by PlayerDetailsPage / PlayerProfilesPage). Runtime only.
    /// </summary>
    public PlayerProfileModel Player { get; set; } = new();

    public int XP { get; set; }

    public int Level { get; set; } = 1;

    public int Legacy { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Matches { get; set; }

    public double WinRate { get; set; }

    public int? TrustScore { get; set; }

    public PlayerProfileStatus Status { get; set; } = PlayerProfileStatus.Normal;

    public bool IsDeveloper { get; set; }

    public bool IsFounder { get; set; }

    public bool IsHonor { get; set; }

    public bool IsCurrentUser { get; set; }

    // Forward-looking fields. Defaulted now so the leaderboard can grow into
    // friends / online presence without breaking the model later.
    public bool IsFriend { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? LastSeen { get; set; }
}

/// <summary>
/// A single immutable snapshot of the player leaderboard for one filter.
/// The page reloads a fresh snapshot on every OnAppearing and every AppEvent;
/// it never mutates cached state in place.
/// </summary>
public sealed class PlayerRankingSnapshot
{
    public IReadOnlyList<PlayerRankingEntry> Entries { get; init; } =
        new List<PlayerRankingEntry>();

    public PlayerRankingFilter Filter { get; init; } = PlayerRankingFilter.All;

    public string? CurrentPlayerId { get; init; }

    public int Coins { get; init; }

    public int Gems { get; init; }

    public DateTime GeneratedAt { get; init; } = DateTime.Now;

    public bool HasData => Entries.Count > 0;

    public PlayerRankingEntry? Top =>
        Entries.Count > 0 ? Entries[0] : null;
}

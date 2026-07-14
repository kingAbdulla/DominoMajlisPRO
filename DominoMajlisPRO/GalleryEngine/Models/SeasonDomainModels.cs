namespace DominoMajlisPRO.GalleryEngine.Models;

public enum SeasonLifecycleStatus { Draft, Published, Hidden, Completed, Archived }
public enum SeasonConditionType
{
    MatchesCompleted, Wins, ConsecutiveWins, SeasonXpEarned, ReachRank,
    TrustScoreAtLeast, WinRateAtLeast, WinsAgainstDistinctTeams, PerfectMatchWin,
    ComebackWin, ActiveDays, ConsecutiveActiveDays, WeeklyMatches, WeeklyWins,
    TopPlacementAtSeasonEnd, ChampionAtSeasonEnd, MvpAtSeasonEnd, CustomCompositeRule
}
public enum SeasonTimeWindowType { Season, Lifetime, Daily, Weekly, Custom }
public enum SeasonTargetScope { Player, Team }
public enum SeasonRewardType
{
    Coins, Gems, SeasonXP, Avatar, ProfileBackground, PlayerFrame, PlayerEffect,
    PlayerNameEffect, PlayerNameFrame, Title, Badge, Emblem, TeamLivingEmblem,
    TeamColor, EmblemBackground, TeamEffect, TeamNameEffect, TeamNameFrame, Bundle
}
public enum SeasonClaimMode { AutoGrant, ManualClaim }
public enum SeasonRepeatPolicy { OncePerSeason, OnceEver, Repeatable, Daily, Weekly }
public enum SeasonGrantStatus { Pending, Succeeded, Failed }
public enum CompositeLogicalOperator { And, Or }

public sealed class SeasonDefinition
{
    public string SeasonId { get; set; } = Guid.NewGuid().ToString("N");
    public int SeasonNumber { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SubtitleAr { get; set; } = string.Empty;
    public string SubtitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public SeasonLifecycleStatus Status { get; set; }
    public string ThemeId { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#D6A642";
    public string? HeroImagePath { get; set; }
    public string? TrailerOrMediaReference { get; set; }
    public string? StoryId { get; set; }
    public string? RewardCollectionId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SeasonStory
{
    public string StoryId { get; set; } = Guid.NewGuid().ToString("N");
    public string SeasonId { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SynopsisAr { get; set; } = string.Empty;
    public string SynopsisEn { get; set; } = string.Empty;
    public string PrologueAr { get; set; } = string.Empty;
    public string PrologueEn { get; set; } = string.Empty;
    public string EpilogueAr { get; set; } = string.Empty;
    public string EpilogueEn { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public bool IsPublished { get; set; }
    public List<SeasonStoryChapter> Chapters { get; set; } = new();
    public List<SeasonStoryCharacter> Characters { get; set; } = new();
    public List<SeasonStoryFaction> Factions { get; set; } = new();
    public List<SeasonStoryTimelineEntry> TimelineEntries { get; set; } = new();
}

public sealed class SeasonStoryChapter
{
    public string ChapterId { get; set; } = Guid.NewGuid().ToString("N");
    public string StoryId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public string? UnlockRuleId { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public bool IsInitiallyVisible { get; set; }
    public List<string> RewardRuleIds { get; set; } = new();
    public string? MediaPath { get; set; }
}

public sealed class SeasonStoryFaction
{
    public string FactionId { get; set; } = Guid.NewGuid().ToString("N");
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string? EmblemAssetId { get; set; }
}

public sealed class SeasonStoryCharacter
{
    public string CharacterId { get; set; } = Guid.NewGuid().ToString("N");
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string RoleAr { get; set; } = string.Empty;
    public string RoleEn { get; set; } = string.Empty;
    public string BiographyAr { get; set; } = string.Empty;
    public string BiographyEn { get; set; } = string.Empty;
    public string? AvatarAssetId { get; set; }
    public string? FactionId { get; set; }
}

public sealed class SeasonStoryTimelineEntry
{
    public string TimelineEntryId { get; set; } = Guid.NewGuid().ToString("N");
    public int SortOrder { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public DateTime? OccursAtUtc { get; set; }
}

public sealed class SeasonCompositeCondition
{
    public CompositeLogicalOperator Operator { get; set; } = CompositeLogicalOperator.And;
    public int? MinimumMatches { get; set; }
    public List<SeasonCompositeConditionItem> Items { get; set; } = new();
}

public sealed class SeasonCompositeConditionItem
{
    public SeasonConditionType ConditionType { get; set; }
    public double Threshold { get; set; }
    public SeasonCompositeCondition? Group { get; set; }
}

public sealed class SeasonRewardRule
{
    public string RewardRuleId { get; set; } = Guid.NewGuid().ToString("N");
    public string SeasonId { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public SeasonConditionType ConditionType { get; set; }
    public double PrimaryThreshold { get; set; }
    public double? SecondaryThreshold { get; set; }
    public int? MinimumMatches { get; set; }
    public SeasonTimeWindowType TimeWindowType { get; set; } = SeasonTimeWindowType.Season;
    public SeasonTargetScope TargetScope { get; set; } = SeasonTargetScope.Player;
    public SeasonRewardType RewardType { get; set; }
    public string? RewardAssetId { get; set; }
    public string? RewardStoreTypeId { get; set; }
    public int? RewardAmount { get; set; }
    public string? RewardBundleId { get; set; }
    public SeasonClaimMode ClaimMode { get; set; }
    public SeasonRepeatPolicy RepeatPolicy { get; set; } = SeasonRepeatPolicy.OncePerSeason;
    public int MaxClaims { get; set; } = 1;
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool IsHiddenUntilUnlocked { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public string? StoryChapterId { get; set; }
    public string? IconAssetId { get; set; }
    public SeasonCompositeCondition? CompositeCondition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SeasonRewardClaim
{
    public string ClaimId { get; set; } = Guid.NewGuid().ToString("N");
    public string RewardRuleId { get; set; } = string.Empty;
    public string SeasonId { get; set; } = string.Empty;
    public string ApplicationUserId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public int ClaimSequence { get; set; }
    public DateTime ClaimedAtUtc { get; set; }
    public SeasonGrantStatus GrantStatus { get; set; }
    public string RewardSnapshot { get; set; } = string.Empty;
    public string ConditionSnapshot { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}

public sealed class SeasonRewardProgress
{
    public string RewardRuleId { get; set; } = string.Empty;
    public string SeasonId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public double CurrentValue { get; set; }
    public double RequiredValue { get; set; }
    public double? SecondaryCurrentValue { get; set; }
    public double? SecondaryRequiredValue { get; set; }
    public double ProgressRatio { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimable { get; set; }
    public bool IsClaimed { get; set; }
    public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SeasonArchiveSnapshot
{
    public string ArchiveId { get; set; } = Guid.NewGuid().ToString("N");
    public string SeasonId { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public DateTime ClosedAtUtc { get; set; }
    public SeasonDefinition? DefinitionSnapshot { get; set; }
    public SeasonStory? StorySnapshot { get; set; }
    public List<SeasonRewardRule> RewardRuleSnapshots { get; set; } = new();
    public List<SeasonStandingSnapshot> TeamStandings { get; set; } = new();
}

public sealed class SeasonStandingSnapshot
{
    public int Placement { get; set; }
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int SeasonXp { get; set; }
    public int Wins { get; set; }
    public int TotalMatches { get; set; }
}

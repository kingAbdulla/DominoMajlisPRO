using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class SeasonExperienceService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string Root => StoreAdminService.GetAdminStorageRoot();
    private static string DefinitionsPath => Path.Combine(Root, "season_definitions.json");
    private static string StoriesPath => Path.Combine(Root, "season_stories.json");
    private static string RulesPath => Path.Combine(Root, "season_reward_rules.json");
    private static string ClaimsPath => Path.Combine(Root, "season_reward_claims.json");
    private static string ArchivesPath => Path.Combine(Root, "season_archives.json");
    private static int _initialized;
    private static int _automationQueued;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;
        AppEvents.MatchesChanged += QueueAutomation;
        AppEvents.RankingsChanged += QueueAutomation;
        AppEvents.TeamsChanged += QueueAutomation;
        AppEvents.PlayerProfileChanged += QueueAutomation;
        AppEvents.CurrentUserChanged += QueueAutomation;
        AppEvents.SeasonChanged += QueueAutomation;
        _ = RunAutomationAsync();
    }

    public static void RequestProgressRefresh() => QueueAutomation();

    private static void QueueAutomation()
    {
        if (Interlocked.Exchange(ref _automationQueued, 1) != 0) return;
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(180); await RunAutomationAsync(); }
            finally { Interlocked.Exchange(ref _automationQueued, 0); }
        });
    }

    public static Task<List<SeasonDefinition>> LoadDefinitionsAsync() =>
        StoreCmsJsonRepository.LoadListAsync<SeasonDefinition>(DefinitionsPath);

    public static Task<List<SeasonStory>> LoadStoriesAsync() =>
        StoreCmsJsonRepository.LoadListAsync<SeasonStory>(StoriesPath);

    public static Task<List<SeasonRewardRule>> LoadRulesAsync() =>
        StoreCmsJsonRepository.LoadListAsync<SeasonRewardRule>(RulesPath);

    public static Task<List<SeasonRewardClaim>> LoadClaimsAsync() =>
        StoreCmsJsonRepository.LoadListAsync<SeasonRewardClaim>(ClaimsPath);

    public static Task<List<SeasonArchiveSnapshot>> LoadArchivesAsync() =>
        StoreCmsJsonRepository.LoadListAsync<SeasonArchiveSnapshot>(ArchivesPath);

    public static async Task<SeasonDefinition?> LoadActiveSeasonAsync(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return (await LoadDefinitionsAsync())
            .Where(item => item.IsPublished && item.Status == SeasonLifecycleStatus.Published)
            .Where(item => item.StartDateUtc <= now && item.EndDateUtc >= now)
            .OrderByDescending(item => item.StartDateUtc)
            .FirstOrDefault();
    }

    public static async Task<SeasonStory?> LoadPublishedStoryAsync(string seasonId)
    {
        return (await LoadStoriesAsync()).FirstOrDefault(item =>
            Same(item.SeasonId, seasonId) && item.IsPublished);
    }

    public static async Task<IReadOnlyList<SeasonRewardRule>> LoadPublishedRulesAsync(string seasonId)
    {
        return (await LoadRulesAsync())
            .Where(item => Same(item.SeasonId, seasonId) && item.IsPublished && item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.TitleAr)
            .ToList();
    }

    public static async Task<SeasonDefinition> SaveSeasonAsync(SeasonDefinition season, bool publish)
    {
        ValidateSeason(season, publish);
        await Gate.WaitAsync();
        try
        {
            var definitions = await LoadDefinitionsAsync();
            if (publish)
            {
                foreach (var current in definitions.Where(item =>
                             item.IsPublished && item.Status == SeasonLifecycleStatus.Published &&
                             !Same(item.SeasonId, season.SeasonId)))
                {
                    current.IsPublished = false;
                    current.Status = SeasonLifecycleStatus.Completed;
                    current.UpdatedAt = DateTime.UtcNow;
                }
            }

            season.SeasonId = RequiredId(season.SeasonId);
            season.IsPublished = publish;
            season.Status = publish ? SeasonLifecycleStatus.Published : SeasonLifecycleStatus.Draft;
            season.UpdatedAt = DateTime.UtcNow;
            Replace(definitions, season, item => item.SeasonId);
            await StoreCmsJsonRepository.SaveListAsync(DefinitionsPath, definitions);
        }
        finally
        {
            Gate.Release();
        }

        AppEvents.RaiseSeasonChanged();
        return season;
    }

    public static async Task<SeasonDefinition> SyncPublishedStoreSeasonAsync(CurrentSeasonRecord record)
    {
        var definitions = await LoadDefinitionsAsync();
        var existing = definitions.FirstOrDefault(item => Same(item.SeasonId, record.SeasonId));
        var number = existing?.SeasonNumber ?? definitions.Select(item => item.SeasonNumber).DefaultIfEmpty(0).Max() + 1;
        var start = (record.StartsAt ?? record.PublishedAt ?? DateTime.UtcNow).ToUniversalTime();
        var end = (record.EndsAt ?? start.AddDays(SeasonManager.SeasonDurationDays)).ToUniversalTime();
        return await SaveSeasonAsync(new SeasonDefinition
        {
            SeasonId = string.IsNullOrWhiteSpace(record.SeasonId) ? record.Id : record.SeasonId,
            SeasonNumber = Math.Max(1, number),
            InternalCode = existing?.InternalCode ?? $"season-{Math.Max(1, number)}",
            TitleAr = record.Title,
            TitleEn = existing?.TitleEn ?? string.Empty,
            SubtitleAr = record.Subtitle,
            SubtitleEn = existing?.SubtitleEn ?? string.Empty,
            DescriptionAr = record.Description,
            DescriptionEn = existing?.DescriptionEn ?? string.Empty,
            StartDateUtc = start,
            EndDateUtc = end,
            ThemeId = existing?.ThemeId ?? string.Empty,
            AccentColor = existing?.AccentColor ?? "#D6A642",
            HeroImagePath = record.ImagePath,
            StoryId = existing?.StoryId,
            RewardCollectionId = existing?.RewardCollectionId,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow
        }, true);
    }

    public static async Task SetRulePublishedAsync(string rewardRuleId, bool published)
    {
        await Gate.WaitAsync();
        try
        {
            var rules = await LoadRulesAsync();
            var rule = rules.FirstOrDefault(item => Same(item.RewardRuleId, rewardRuleId));
            if (rule == null) return;
            rule.IsPublished = published;
            rule.IsActive = published;
            rule.UpdatedAt = DateTime.UtcNow;
            await StoreCmsJsonRepository.SaveListAsync(RulesPath, rules);
        }
        finally { Gate.Release(); }
        AppEvents.RaiseSeasonChanged();
    }

    public static async Task DeleteRuleDraftAsync(string rewardRuleId)
    {
        await Gate.WaitAsync();
        try
        {
            var rules = await LoadRulesAsync();
            rules.RemoveAll(item => Same(item.RewardRuleId, rewardRuleId) && !item.IsPublished);
            await StoreCmsJsonRepository.SaveListAsync(RulesPath, rules);
        }
        finally { Gate.Release(); }
        AppEvents.RaiseSeasonChanged();
    }

    public static async Task ArchiveSeasonAsync(string seasonId)
    {
        await Gate.WaitAsync();
        try
        {
            var definitions = await LoadDefinitionsAsync();
            var season = definitions.FirstOrDefault(item => Same(item.SeasonId, seasonId));
            if (season == null) return;
            var archives = await LoadArchivesAsync();
            if (!archives.Any(item => Same(item.SeasonId, seasonId)))
            {
                archives.Add(await CreateArchiveSnapshotAsync(season, DateTime.UtcNow));
                await StoreCmsJsonRepository.SaveListAsync(ArchivesPath, archives);
            }
            season.IsPublished = false;
            season.Status = SeasonLifecycleStatus.Archived;
            season.UpdatedAt = DateTime.UtcNow;
            await StoreCmsJsonRepository.SaveListAsync(DefinitionsPath, definitions);
        }
        finally { Gate.Release(); }
        AppEvents.RaiseSeasonChanged();
    }

    public static async Task<SeasonDefinition> DuplicateSeasonAsync(string seasonId)
    {
        var source = (await LoadDefinitionsAsync()).FirstOrDefault(item => Same(item.SeasonId, seasonId))
            ?? throw new InvalidOperationException("تعذر العثور على الموسم.");
        var copy = JsonSerializer.Deserialize<SeasonDefinition>(JsonSerializer.Serialize(source))!;
        copy.SeasonId = Guid.NewGuid().ToString("N");
        copy.SeasonNumber = (await LoadDefinitionsAsync()).Select(item => item.SeasonNumber).DefaultIfEmpty(0).Max() + 1;
        copy.InternalCode = $"{source.InternalCode}-copy-{copy.SeasonNumber}";
        copy.TitleAr = $"{source.TitleAr} - نسخة";
        copy.IsPublished = false;
        copy.Status = SeasonLifecycleStatus.Draft;
        copy.CreatedAt = copy.UpdatedAt = DateTime.UtcNow;
        return await SaveSeasonAsync(copy, false);
    }

    public static async Task UnpublishSeasonAsync(string seasonId)
    {
        await Gate.WaitAsync();
        try
        {
            var definitions = await LoadDefinitionsAsync();
            var season = definitions.FirstOrDefault(item => Same(item.SeasonId, seasonId));
            if (season == null) return;
            season.IsPublished = false;
            season.Status = SeasonLifecycleStatus.Hidden;
            season.UpdatedAt = DateTime.UtcNow;
            await StoreCmsJsonRepository.SaveListAsync(DefinitionsPath, definitions);
        }
        finally { Gate.Release(); }
        await CurrentSeasonAdminService.HidePublishedAsync(seasonId);
        AppEvents.RaiseSeasonChanged();
    }

    public static async Task<SeasonStory> SaveStoryAsync(SeasonStory story, bool publish)
    {
        if (string.IsNullOrWhiteSpace(story.SeasonId))
            throw new InvalidOperationException("يجب ربط القصة بموسم صالح.");
        if (publish && string.IsNullOrWhiteSpace(story.TitleAr))
            throw new InvalidOperationException("عنوان قصة الموسم مطلوب قبل النشر.");

        await Gate.WaitAsync();
        try
        {
            var stories = await LoadStoriesAsync();
            story.StoryId = RequiredId(story.StoryId);
            story.IsPublished = publish;
            foreach (var chapter in story.Chapters)
            {
                chapter.ChapterId = RequiredId(chapter.ChapterId);
                chapter.StoryId = story.StoryId;
            }
            Replace(stories, story, item => item.StoryId);
            await StoreCmsJsonRepository.SaveListAsync(StoriesPath, stories);
        }
        finally { Gate.Release(); }
        AppEvents.RaiseSeasonChanged();
        return story;
    }

    public static async Task<SeasonRewardRule> SaveRuleAsync(SeasonRewardRule rule, bool publish)
    {
        await ValidateRuleAsync(rule, publish);
        await Gate.WaitAsync();
        try
        {
            var rules = await LoadRulesAsync();
            rule.RewardRuleId = RequiredId(rule.RewardRuleId);
            rule.IsPublished = publish;
            rule.UpdatedAt = DateTime.UtcNow;
            Replace(rules, rule, item => item.RewardRuleId);
            await StoreCmsJsonRepository.SaveListAsync(RulesPath, rules);
        }
        finally { Gate.Release(); }
        AppEvents.RaiseSeasonChanged();
        return rule;
    }

    public static async Task<int> ValidateSeasonConfigurationAsync(string seasonId)
    {
        var definitions = await LoadDefinitionsAsync();
        var season = definitions.FirstOrDefault(item => Same(item.SeasonId, seasonId))
            ?? throw new InvalidOperationException("تعذر العثور على الموسم المطلوب.");
        ValidateSeason(season, season.IsPublished);

        var duplicateSeasonNumbers = definitions
            .Where(item => item.SeasonNumber == season.SeasonNumber)
            .Select(item => item.SeasonId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (duplicateSeasonNumbers > 1)
            throw new InvalidOperationException("رقم الموسم مكرر ويجب أن يكون فريدا.");

        var rules = (await LoadRulesAsync())
            .Where(item => Same(item.SeasonId, seasonId))
            .ToList();
        if (rules.GroupBy(item => item.RewardRuleId, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new InvalidOperationException("يوجد معرف جائزة مكرر داخل الموسم.");

        var story = (await LoadStoriesAsync()).FirstOrDefault(item => Same(item.SeasonId, seasonId));
        if (story?.Chapters.GroupBy(item => item.ChapterId, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1) == true)
            throw new InvalidOperationException("يوجد معرف فصل مكرر داخل قصة الموسم.");

        foreach (var rule in rules)
            await ValidateRuleAsync(rule, rule.IsPublished);
        return rules.Count;
    }

    public static async Task<SeasonRewardRule> DuplicateRuleAsync(string rewardRuleId)
    {
        var source = (await LoadRulesAsync()).FirstOrDefault(item => Same(item.RewardRuleId, rewardRuleId))
            ?? throw new InvalidOperationException("تعذر العثور على قاعدة الجائزة.");
        var copy = JsonSerializer.Deserialize<SeasonRewardRule>(JsonSerializer.Serialize(source))!;
        copy.RewardRuleId = Guid.NewGuid().ToString("N");
        copy.TitleAr = $"{source.TitleAr} - نسخة";
        copy.IsPublished = false;
        copy.CreatedAt = copy.UpdatedAt = DateTime.UtcNow;
        return await SaveRuleAsync(copy, false);
    }

    public static async Task<SeasonRewardProgress> EvaluateProgressAsync(
        SeasonRewardRule rule,
        string playerId,
        string? teamId = null,
        bool allowExpiredWindow = false)
    {
        var season = (await LoadDefinitionsAsync()).FirstOrDefault(item => Same(item.SeasonId, rule.SeasonId))
            ?? throw new InvalidOperationException("الموسم المرتبط بالجائزة غير موجود.");
        var player = await PlayerProfileService.GetPlayerByIdAsync(playerId)
            ?? throw new InvalidOperationException("تعذر العثور على ملف اللاعب.");
        var team = !string.IsNullOrWhiteSpace(teamId)
            ? await TeamProfileService.GetTeamByIdAsync(teamId)
            : await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
        if (rule.TargetScope == SeasonTargetScope.Team && team == null)
            throw new InvalidOperationException("هذه الجائزة تتطلب فريقاً مرتبطاً باللاعب.");

        var matches = (await GameService.LoadMatchesAsync())
            .Where(item => item.IsFinished && item.MatchDate.ToUniversalTime() >= season.StartDateUtc &&
                           item.MatchDate.ToUniversalTime() <= season.EndDateUtc)
            .ToList();
        var allTeams = await TeamProfileService.LoadTeamsAsync();
        var placement = team == null ? 0 : allTeams
            .OrderByDescending(item => item.SeasonXP)
            .ThenByDescending(item => item.XP)
            .ThenBy(item => item.TeamId, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new { item.TeamId, Placement = index + 1 })
            .FirstOrDefault(item => Same(item.TeamId, team.TeamId))?.Placement ?? 0;
        var metric = EvaluateMetric(rule, player, team, matches, playerId, placement);
        var claims = await LoadClaimsAsync();
        var relevantClaims = claims.Where(item =>
            Same(item.RewardRuleId, rule.RewardRuleId) &&
            (rule.TargetScope == SeasonTargetScope.Player
                ? Same(item.PlayerId, playerId)
                : Same(item.TeamId, team?.TeamId)) &&
            item.GrantStatus == SeasonGrantStatus.Succeeded).ToList();
        var completed = (rule.ConditionType == SeasonConditionType.CustomCompositeRule
                            ? metric.CompositeSatisfied
                            : metric.Primary >= rule.PrimaryThreshold) &&
                        (!rule.SecondaryThreshold.HasValue || metric.Secondary >= rule.SecondaryThreshold.Value) &&
                        (!rule.MinimumMatches.HasValue || metric.Matches >= rule.MinimumMatches.Value);
        var claimLimitReached = rule.RepeatPolicy is SeasonRepeatPolicy.OnceEver or SeasonRepeatPolicy.OncePerSeason
            ? relevantClaims.Count > 0
            : rule.MaxClaims > 0 && relevantClaims.Count >= rule.MaxClaims;
        var now = DateTime.UtcNow;
        var inWindow = allowExpiredWindow ||
                       ((!rule.StartDateUtc.HasValue || rule.StartDateUtc <= now) &&
                        (!rule.EndDateUtc.HasValue || rule.EndDateUtc >= now));

        return new SeasonRewardProgress
        {
            RewardRuleId = rule.RewardRuleId,
            SeasonId = rule.SeasonId,
            PlayerId = playerId,
            TeamId = team?.TeamId,
            CurrentValue = metric.Primary,
            RequiredValue = rule.PrimaryThreshold,
            SecondaryCurrentValue = rule.SecondaryThreshold.HasValue ? metric.Secondary : null,
            SecondaryRequiredValue = rule.SecondaryThreshold,
            ProgressRatio = rule.PrimaryThreshold <= 0 ? (completed ? 1 : 0) : Math.Clamp(metric.Primary / rule.PrimaryThreshold, 0, 1),
            IsCompleted = completed,
            IsClaimed = claimLimitReached,
            IsClaimable = completed && inWindow && !claimLimitReached && rule.IsPublished &&
                          (rule.IsActive || allowExpiredWindow),
            LastEvaluatedAtUtc = now
        };
    }

    public static async Task<SeasonRewardClaim> ClaimAsync(
        string rewardRuleId,
        string? requestedTeamId = null)
    {
        var rule = (await LoadRulesAsync()).FirstOrDefault(item => Same(item.RewardRuleId, rewardRuleId));
        var season = rule == null
            ? null
            : (await LoadDefinitionsAsync()).FirstOrDefault(item => Same(item.SeasonId, rule.SeasonId));
        var isCompletedManualClaim = rule?.ClaimMode == SeasonClaimMode.ManualClaim &&
                                     season?.Status is SeasonLifecycleStatus.Completed or SeasonLifecycleStatus.Archived &&
                                     rule.ConditionType is SeasonConditionType.TopPlacementAtSeasonEnd or
                                         SeasonConditionType.ChampionAtSeasonEnd or SeasonConditionType.MvpAtSeasonEnd;
        return await ClaimInternalAsync(
            rewardRuleId,
            requestedTeamId,
            allowExpiredSeason: isCompletedManualClaim);
    }

    private static async Task<SeasonRewardClaim> ClaimInternalAsync(
        string rewardRuleId,
        string? requestedTeamId,
        bool allowExpiredSeason,
        ApplicationUserModel? claimUser = null)
    {
        await Gate.WaitAsync();
        try
        {
            var user = claimUser ?? await ApplicationUserService.EnsureCurrentSessionAsync();
            var playerId = user.PlayerId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(playerId))
                throw new InvalidOperationException("يجب ربط الحساب بملف لاعب قبل استلام الجائزة.");

            var rule = (await LoadRulesAsync()).FirstOrDefault(item => Same(item.RewardRuleId, rewardRuleId))
                ?? throw new InvalidOperationException("قاعدة الجائزة غير موجودة.");
            var season = allowExpiredSeason
                ? (await LoadDefinitionsAsync()).FirstOrDefault(item => item.IsPublished && Same(item.SeasonId, rule.SeasonId))
                : await LoadActiveSeasonAsync();
            if (season == null)
                throw new InvalidOperationException("لا يوجد موسم منشور ونشط حالياً.");
            if (!Same(rule.SeasonId, season.SeasonId))
                throw new InvalidOperationException("هذه الجائزة لا تتبع الموسم النشط.");

            var progress = await EvaluateProgressAsync(
                rule,
                playerId,
                requestedTeamId,
                allowExpiredWindow: allowExpiredSeason);
            if (!progress.IsClaimable)
                throw new InvalidOperationException(progress.IsClaimed
                    ? "تم استلام هذه الجائزة مسبقاً."
                    : "لم يتحقق شرط الجائزة بعد.");

            var claims = await LoadClaimsAsync();
            var bucket = RepeatBucket(rule.RepeatPolicy, DateTime.UtcNow);
            var canonicalOwnerId = rule.TargetScope == SeasonTargetScope.Team
                ? progress.TeamId ?? string.Empty
                : playerId;
            var key = string.Join("|", rule.SeasonId, rule.RewardRuleId,
                rule.TargetScope, canonicalOwnerId, bucket).ToLowerInvariant();
            var existing = claims.FirstOrDefault(item => Same(item.IdempotencyKey, key));
            if (existing != null)
                return existing;

            var claim = new SeasonRewardClaim
            {
                RewardRuleId = rule.RewardRuleId,
                SeasonId = rule.SeasonId,
                ApplicationUserId = user.ApplicationUserId ?? string.Empty,
                PlayerId = playerId,
                TeamId = progress.TeamId,
                ClaimSequence = claims.Count(item => Same(item.RewardRuleId, rule.RewardRuleId) && Same(item.PlayerId, playerId)) + 1,
                ClaimedAtUtc = DateTime.UtcNow,
                GrantStatus = SeasonGrantStatus.Pending,
                RewardSnapshot = JsonSerializer.Serialize(new { rule.RewardType, rule.RewardAssetId, rule.RewardStoreTypeId, rule.RewardAmount }),
                ConditionSnapshot = JsonSerializer.Serialize(progress),
                IdempotencyKey = key
            };
            claims.Add(claim);
            await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);

            try
            {
                await GrantAsync(rule, playerId, progress.TeamId);
                claim.GrantStatus = SeasonGrantStatus.Succeeded;
            }
            catch (Exception ex)
            {
                claim.GrantStatus = SeasonGrantStatus.Failed;
                claim.FailureReason = ex.Message;
                await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);
                throw;
            }

            await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);
            AppEvents.RaiseSeasonRewardClaimChanged(playerId);
            return claim;
        }
        finally { Gate.Release(); }
    }

    public static string DescribeCondition(SeasonRewardRule rule) => rule.ConditionType switch
    {
        SeasonConditionType.MatchesCompleted => $"إكمال {rule.PrimaryThreshold:0} مباراة",
        SeasonConditionType.Wins => $"تحقيق {rule.PrimaryThreshold:0} انتصار",
        SeasonConditionType.ConsecutiveWins => $"تحقيق {rule.PrimaryThreshold:0} انتصارات متتالية",
        SeasonConditionType.SeasonXpEarned => $"جمع {rule.PrimaryThreshold:0} XP موسمي",
        SeasonConditionType.ReachRank => $"الوصول إلى المرتبة {rule.PrimaryThreshold:0} أو أعلى",
        SeasonConditionType.TrustScoreAtLeast => $"الوصول إلى ثقة {rule.PrimaryThreshold:0}%",
        SeasonConditionType.WinRateAtLeast => $"الوصول إلى نسبة فوز {rule.PrimaryThreshold:0}%",
        SeasonConditionType.WinsAgainstDistinctTeams => $"الفوز على {rule.PrimaryThreshold:0} فرق مختلفة",
        SeasonConditionType.ActiveDays => $"النشاط خلال {rule.PrimaryThreshold:0} أيام",
        SeasonConditionType.ConsecutiveActiveDays => $"النشاط {rule.PrimaryThreshold:0} أيام متتالية",
        SeasonConditionType.WeeklyMatches => $"إكمال {rule.PrimaryThreshold:0} مباراة أسبوعية",
        SeasonConditionType.WeeklyWins => $"تحقيق {rule.PrimaryThreshold:0} انتصار أسبوعي",
        SeasonConditionType.PerfectMatchWin => $"تحقيق {rule.PrimaryThreshold:0} فوز مثالي",
        SeasonConditionType.ComebackWin => $"تحقيق {rule.PrimaryThreshold:0} فوز بعد عودة",
        SeasonConditionType.TopPlacementAtSeasonEnd => $"إنهاء الموسم ضمن أفضل {rule.PrimaryThreshold:0}",
        SeasonConditionType.ChampionAtSeasonEnd => "تحقيق بطولة الموسم",
        SeasonConditionType.MvpAtSeasonEnd => "تحقيق أفضل لاعب في الموسم",
        SeasonConditionType.CustomCompositeRule => DescribeComposite(rule.CompositeCondition, rule.DescriptionAr),
        _ => rule.DescriptionAr
    };

    private static string DescribeComposite(SeasonCompositeCondition? condition, string fallback)
    {
        if (condition?.Items.Count is not > 0)
            return fallback;
        var separator = condition.Operator == CompositeLogicalOperator.And ? " و " : " أو ";
        return string.Join(separator, condition.Items.Select(item => item.Group == null
            ? DescribeCondition(new SeasonRewardRule
            {
                ConditionType = item.ConditionType,
                PrimaryThreshold = item.Threshold,
                DescriptionAr = fallback
            })
            : $"({DescribeComposite(item.Group, fallback)})"));
    }

    public static async Task RunAutomationAsync()
    {
        await CloseExpiredSeasonsAsync();
        var season = await LoadActiveSeasonAsync();
        if (season == null) return;
        var rules = await LoadPublishedRulesAsync(season.SeasonId);
        foreach (var rule in rules.Where(item => item.ClaimMode == SeasonClaimMode.AutoGrant))
        {
            try
            {
                var playerId = await ApplicationUserService.GetCurrentUserPlayerIdAsync();
                if (string.IsNullOrWhiteSpace(playerId)) return;
                var progress = await EvaluateProgressAsync(rule, playerId);
                if (progress.IsClaimable)
                    await ClaimInternalAsync(rule.RewardRuleId, progress.TeamId, allowExpiredSeason: false);
            }
            catch { /* Diagnostics remain in failed claims; automation retries on the next authoritative event. */ }
        }
    }

    public static async Task CloseExpiredSeasonsAsync(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var expired = (await LoadDefinitionsAsync())
            .Where(item => item.IsPublished && item.Status == SeasonLifecycleStatus.Published && item.EndDateUtc < now)
            .ToList();
        foreach (var season in expired)
        {
            var rules = await LoadPublishedRulesAsync(season.SeasonId);
            var users = (await ApplicationUserService.GetAllUsersAsync())
                .Where(item => !string.IsNullOrWhiteSpace(item.ApplicationUserId) &&
                               !string.IsNullOrWhiteSpace(item.PlayerId))
                .GroupBy(item => item.PlayerId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            var teams = (await TeamProfileService.LoadTeamsAsync())
                .OrderByDescending(item => item.SeasonXP)
                .ThenByDescending(item => item.XP)
                .ThenBy(item => item.TeamId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var rule in rules.Where(item => item.ClaimMode == SeasonClaimMode.AutoGrant ||
                         item.ConditionType is SeasonConditionType.TopPlacementAtSeasonEnd or
                             SeasonConditionType.ChampionAtSeasonEnd or SeasonConditionType.MvpAtSeasonEnd))
            {
                foreach (var user in users)
                {
                    var teamIds = rule.TargetScope == SeasonTargetScope.Team
                        ? teams.Where(team => Same(team.Player1Id, user.PlayerId) || Same(team.Player2Id, user.PlayerId))
                            .Select(team => (string?)team.TeamId)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList()
                        : new List<string?> { null };
                    foreach (var teamId in teamIds)
                    {
                        try
                        {
                            var progress = await EvaluateProgressAsync(
                                rule,
                                user.PlayerId,
                                teamId,
                                allowExpiredWindow: true);
                            if (progress.IsClaimable && rule.ClaimMode == SeasonClaimMode.AutoGrant)
                                await ClaimInternalAsync(
                                    rule.RewardRuleId,
                                    progress.TeamId,
                                    allowExpiredSeason: true,
                                    claimUser: user);
                        }
                        catch
                        {
                            // One invalid identity must not block deterministic closure for other owners.
                        }
                    }
                }
            }

            var archives = await LoadArchivesAsync();
            if (!archives.Any(item => Same(item.SeasonId, season.SeasonId)))
            {
                archives.Add(await CreateArchiveSnapshotAsync(season, now, teams));
                await StoreCmsJsonRepository.SaveListAsync(ArchivesPath, archives);
            }

            await Gate.WaitAsync();
            try
            {
                var definitions = await LoadDefinitionsAsync();
                var target = definitions.FirstOrDefault(item => Same(item.SeasonId, season.SeasonId));
                if (target != null)
                {
                    target.IsPublished = false;
                    target.Status = SeasonLifecycleStatus.Completed;
                    target.UpdatedAt = now;
                    await StoreCmsJsonRepository.SaveListAsync(DefinitionsPath, definitions);
                }
                var allRules = await LoadRulesAsync();
                foreach (var rule in allRules.Where(item => Same(item.SeasonId, season.SeasonId)))
                    rule.IsActive = false;
                await StoreCmsJsonRepository.SaveListAsync(RulesPath, allRules);
            }
            finally { Gate.Release(); }
            AppEvents.RaiseSeasonChanged();
        }
    }

    private static void ValidateSeason(SeasonDefinition season, bool publish)
    {
        if (season.EndDateUtc <= season.StartDateUtc)
            throw new InvalidOperationException("تاريخ نهاية الموسم يجب أن يلي تاريخ البداية.");
        if (publish && (string.IsNullOrWhiteSpace(season.TitleAr) || season.SeasonNumber <= 0))
            throw new InvalidOperationException("رقم الموسم وعنوانه العربي مطلوبان قبل النشر.");
    }

    private static async Task ValidateRuleAsync(SeasonRewardRule rule, bool publish)
    {
        if (!Enum.IsDefined(rule.ConditionType) || !Enum.IsDefined(rule.TargetScope) ||
            !Enum.IsDefined(rule.RewardType) || !Enum.IsDefined(rule.ClaimMode) ||
            !Enum.IsDefined(rule.RepeatPolicy) || !Enum.IsDefined(rule.TimeWindowType))
        {
            throw new InvalidOperationException("تحتوي قاعدة الموسم على قيمة تعداد غير صالحة.");
        }
        if (string.IsNullOrWhiteSpace(rule.SeasonId))
            throw new InvalidOperationException("يجب اختيار موسم صالح.");
        if (rule.PrimaryThreshold <= 0 || rule.RewardAmount < 0)
            throw new InvalidOperationException("قيمة الشرط يجب أن تكون أكبر من صفر، ولا يمكن أن تكون الجائزة سالبة.");
        if (rule.MinimumMatches is < 0)
            throw new InvalidOperationException("الحد الأدنى للمباريات لا يمكن أن يكون سالبا.");
        if (rule.ConditionType == SeasonConditionType.CustomCompositeRule)
            ValidateComposite(rule.CompositeCondition);
        if (!publish) return;
        if (string.IsNullOrWhiteSpace(rule.TitleAr))
            throw new InvalidOperationException("عنوان الجائزة مطلوب قبل النشر.");
        var season = (await LoadDefinitionsAsync()).FirstOrDefault(item => Same(item.SeasonId, rule.SeasonId))
            ?? throw new InvalidOperationException("الموسم المرتبط بالقاعدة غير موجود.");
        if (rule.StartDateUtc.HasValue && rule.StartDateUtc.Value < season.StartDateUtc ||
            rule.EndDateUtc.HasValue && rule.EndDateUtc.Value > season.EndDateUtc ||
            rule.StartDateUtc.HasValue && rule.EndDateUtc.HasValue && rule.EndDateUtc <= rule.StartDateUtc)
        {
            throw new InvalidOperationException("فترة المطالبة يجب أن تكون صحيحة وضمن فترة الموسم.");
        }
        if (RequiresAsset(rule.RewardType))
        {
            var asset = await StoreAssetCatalogService.ResolvePublishedRewardAssetAsync(rule.RewardAssetId, rule.RewardStoreTypeId);
            if (asset == null)
                throw new InvalidOperationException("اختر أصلاً منشوراً من كتالوج المتجر، ولا تستخدم معرفاً يدوياً.");
            var expectedAssetType = RewardAssetType(rule.RewardType);
            if (expectedAssetType != null && asset.AssetType != expectedAssetType)
                throw new InvalidOperationException("نوع أصل الجائزة لا يطابق نوع الجائزة المحدد.");
            var expected = rule.TargetScope == SeasonTargetScope.Team
                ? StoreProductOwnerScope.Team
                : StoreProductOwnerScope.Player;
            if (asset.OwnerScope != expected)
                throw new InvalidOperationException("نطاق مالك الجائزة لا يطابق نطاق الشرط.");
        }
        else if ((rule.RewardAmount ?? 0) <= 0)
        {
            throw new InvalidOperationException("قيمة الجائزة مطلوبة.");
        }
    }

    private static void ValidateComposite(SeasonCompositeCondition? condition)
    {
        if (condition == null || condition.Items.Count < 2)
            throw new InvalidOperationException("الشرط المركب يحتاج شرطين صالحين على الأقل.");
        if (!Enum.IsDefined(condition.Operator) || condition.MinimumMatches is < 0)
            throw new InvalidOperationException("إعدادات مجموعة الشروط المركبة غير صالحة.");
        foreach (var item in condition.Items)
        {
            if (!Enum.IsDefined(item.ConditionType) || item.Threshold <= 0)
                throw new InvalidOperationException("كل شرط داخل المجموعة يحتاج نوعا وقيمة صحيحة.");
            if (item.Group != null)
                ValidateComposite(item.Group);
        }
    }

    private static async Task GrantAsync(SeasonRewardRule rule, string playerId, string? teamId)
    {
        var amount = rule.RewardAmount ?? 0;
        switch (rule.RewardType)
        {
            case SeasonRewardType.Coins:
                await PlayerWalletService.CreditAsync(playerId, coins: amount);
                AppEvents.RaiseWalletChanged(playerId);
                return;
            case SeasonRewardType.Gems:
                await PlayerWalletService.CreditAsync(playerId, gems: amount);
                AppEvents.RaiseWalletChanged(playerId);
                return;
            case SeasonRewardType.SeasonXP:
            {
                var player = await PlayerProfileService.GetPlayerByIdAsync(playerId)
                    ?? throw new InvalidOperationException("تعذر العثور على اللاعب.");
                player.SeasonXP = checked(player.SeasonXP + amount);
                await PlayerProfileService.UpdatePlayerProfileAsync(player);
                AppEvents.RaiseSeasonProgressChanged(playerId);
                return;
            }
            default:
            {
                var asset = await StoreAssetCatalogService.ResolvePublishedRewardAssetAsync(rule.RewardAssetId, rule.RewardStoreTypeId)
                    ?? throw new InvalidOperationException("أصل الجائزة لم يعد منشوراً أو صالحاً.");
                if (asset.OwnerScope == StoreProductOwnerScope.Team)
                {
                    if (string.IsNullOrWhiteSpace(teamId))
                        throw new InvalidOperationException("جائزة الفريق تتطلب TeamId صالحاً.");
                    await TeamAssetInventoryService.AddOwnedAssetAsync(
                        teamId, asset.AssetId, asset.AssetType.ToString(), "SeasonReward", rule.SeasonId);
                    AppEvents.RaiseTeamIdentityChanged(teamId);
                }
                else
                {
                    await PlayerInventoryService.AddOwnedItemAsync(
                        playerId, asset.AssetId, asset.AssetType.ToString(), "SeasonReward", seasonId: rule.SeasonId);
                    AppEvents.RaiseInventoryChanged(playerId);
                    AppEvents.RaisePlayerIdentityChanged(playerId);
                }
                return;
            }
        }
    }

    private static (double Primary, double Secondary, int Matches, bool CompositeSatisfied) EvaluateMetric(
        SeasonRewardRule rule,
        PlayerProfileModel player,
        TeamProfileModel? team,
        IReadOnlyList<SavedMatch> seasonMatches,
        string playerId,
        int placement)
    {
        var relevant = seasonMatches.Where(match => IsPlayerInMatch(match, playerId)).ToList();
        var wins = relevant.Where(match => IsPlayerWinner(match, playerId)).ToList();
        double ValueFor(SeasonConditionType condition) => condition switch
        {
            SeasonConditionType.MatchesCompleted => relevant.Count,
            SeasonConditionType.Wins => wins.Count,
            SeasonConditionType.ConsecutiveWins => rule.TargetScope == SeasonTargetScope.Team ? team?.ConsecutiveWins ?? 0 : player.CurrentWinStreak,
            SeasonConditionType.SeasonXpEarned => rule.TargetScope == SeasonTargetScope.Team ? team?.SeasonXP ?? 0 : player.SeasonXP,
            SeasonConditionType.TrustScoreAtLeast => rule.TargetScope == SeasonTargetScope.Team ? team?.TrustScore ?? 0 : player.TrustScore,
            SeasonConditionType.WinRateAtLeast => rule.TargetScope == SeasonTargetScope.Team ? team?.WinRate ?? 0 : player.WinRate,
            SeasonConditionType.ActiveDays => relevant.Select(item => item.MatchDate.Date).Distinct().Count(),
            SeasonConditionType.ConsecutiveActiveDays => ConsecutiveActiveDays(relevant.Select(item => item.MatchDate.Date)),
            SeasonConditionType.WeeklyMatches => relevant.Count(item => item.MatchDate >= DateTime.Now.AddDays(-7)),
            SeasonConditionType.WeeklyWins => wins.Count(item => item.MatchDate >= DateTime.Now.AddDays(-7)),
            SeasonConditionType.WinsAgainstDistinctTeams => wins.Select(item => OpponentTeamId(item, playerId)).Where(item => item.Length > 0).Distinct().Count(),
            SeasonConditionType.PerfectMatchWin => wins.Count(item => Math.Min(item.Team1Score, item.Team2Score) == 0),
            SeasonConditionType.ComebackWin => wins.Count(item => item.RoundsHistory.Count > 1 && item.RoundsHistory.Any(round => round.Team1NewScore != round.Team2NewScore)),
            SeasonConditionType.ReachRank => placement > 0 && placement <= Math.Max(1, rule.PrimaryThreshold) ? rule.PrimaryThreshold : 0,
            SeasonConditionType.TopPlacementAtSeasonEnd => placement > 0 && placement <= Math.Max(1, rule.PrimaryThreshold) ? rule.PrimaryThreshold : 0,
            SeasonConditionType.ChampionAtSeasonEnd => placement == 1 || team?.HasChampionBadge == true ? 1 : 0,
            SeasonConditionType.MvpAtSeasonEnd => player.MVPCount,
            _ => 0
        };
        bool EvaluateGroup(SeasonCompositeCondition group)
        {
            if (group.MinimumMatches.HasValue && relevant.Count < group.MinimumMatches.Value) return false;
            var values = group.Items.Select(item => item.Group != null
                ? EvaluateGroup(item.Group)
                : ValueFor(item.ConditionType) >= item.Threshold).ToList();
            return values.Count > 0 && (group.Operator == CompositeLogicalOperator.And ? values.All(item => item) : values.Any(item => item));
        }
        var composite = rule.CompositeCondition != null && EvaluateGroup(rule.CompositeCondition);
        var primary = rule.ConditionType == SeasonConditionType.CustomCompositeRule && rule.CompositeCondition != null
            ? rule.CompositeCondition.Items.Count(item => item.Group != null ? EvaluateGroup(item.Group) : ValueFor(item.ConditionType) >= item.Threshold)
            : ValueFor(rule.ConditionType);
        return (primary, relevant.Count, relevant.Count, composite);
    }

    private static bool IsPlayerInMatch(SavedMatch match, string playerId) =>
        Same(match.Team1Player1Id, playerId) || Same(match.Team1Player2Id, playerId) ||
        Same(match.Team2Player1Id, playerId) || Same(match.Team2Player2Id, playerId);

    private static bool IsPlayerWinner(SavedMatch match, string playerId)
    {
        var playerTeamId = Same(match.Team1Player1Id, playerId) || Same(match.Team1Player2Id, playerId)
            ? match.Team1Id : match.Team2Id;
        return Same(match.WinnerTeamId, playerTeamId) || Same(match.WinnerTeamName, playerTeamId);
    }

    private static string OpponentTeamId(SavedMatch match, string playerId) =>
        Same(match.Team1Player1Id, playerId) || Same(match.Team1Player2Id, playerId)
            ? match.Team2Id : match.Team1Id;

    private static int ConsecutiveActiveDays(IEnumerable<DateTime> dates)
    {
        var set = dates.Distinct().OrderByDescending(item => item).ToHashSet();
        if (set.Count == 0) return 0;
        var cursor = set.Max();
        var count = 0;
        while (set.Contains(cursor)) { count++; cursor = cursor.AddDays(-1); }
        return count;
    }

    private static bool RequiresAsset(SeasonRewardType type) => type is not
        (SeasonRewardType.Coins or SeasonRewardType.Gems or SeasonRewardType.SeasonXP);

    private static StoreProductAssetType? RewardAssetType(SeasonRewardType type) => type switch
    {
        SeasonRewardType.Avatar => StoreProductAssetType.Avatar,
        SeasonRewardType.ProfileBackground => StoreProductAssetType.ProfileBackground,
        SeasonRewardType.PlayerFrame => StoreProductAssetType.Frame,
        SeasonRewardType.PlayerEffect => StoreProductAssetType.Effect,
        SeasonRewardType.PlayerNameEffect => StoreProductAssetType.PlayerNameEffect,
        SeasonRewardType.PlayerNameFrame => StoreProductAssetType.PlayerNameFrame,
        SeasonRewardType.Title => StoreProductAssetType.Title,
        SeasonRewardType.Badge => StoreProductAssetType.Badge,
        SeasonRewardType.Emblem or SeasonRewardType.TeamLivingEmblem => StoreProductAssetType.Emblem,
        SeasonRewardType.TeamColor => StoreProductAssetType.TeamColor,
        SeasonRewardType.EmblemBackground => StoreProductAssetType.EmblemBackground,
        SeasonRewardType.TeamEffect => StoreProductAssetType.TeamEffect,
        SeasonRewardType.TeamNameEffect => StoreProductAssetType.TeamNameEffect,
        SeasonRewardType.TeamNameFrame => StoreProductAssetType.TeamNameFrame,
        SeasonRewardType.Bundle => StoreProductAssetType.Bundle,
        _ => null
    };

    private static async Task<SeasonArchiveSnapshot> CreateArchiveSnapshotAsync(
        SeasonDefinition season,
        DateTime closedAtUtc,
        IReadOnlyList<TeamProfileModel>? orderedTeams = null)
    {
        var teams = orderedTeams ?? (await TeamProfileService.LoadTeamsAsync())
            .OrderByDescending(item => item.SeasonXP)
            .ThenByDescending(item => item.XP)
            .ThenBy(item => item.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var story = (await LoadStoriesAsync())
            .Where(item => Same(item.SeasonId, season.SeasonId))
            .OrderByDescending(item => item.IsPublished)
            .FirstOrDefault();
        var rules = (await LoadRulesAsync())
            .Where(item => Same(item.SeasonId, season.SeasonId))
            .OrderBy(item => item.SortOrder)
            .ToList();
        return new SeasonArchiveSnapshot
        {
            SeasonId = season.SeasonId,
            SeasonNumber = season.SeasonNumber,
            TitleAr = season.TitleAr,
            ClosedAtUtc = closedAtUtc,
            DefinitionSnapshot = JsonSerializer.Deserialize<SeasonDefinition>(JsonSerializer.Serialize(season)),
            StorySnapshot = story == null
                ? null
                : JsonSerializer.Deserialize<SeasonStory>(JsonSerializer.Serialize(story)),
            RewardRuleSnapshots = JsonSerializer.Deserialize<List<SeasonRewardRule>>(JsonSerializer.Serialize(rules)) ?? new(),
            TeamStandings = teams.Select((team, index) => new SeasonStandingSnapshot
            {
                Placement = index + 1,
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                SeasonXp = team.SeasonXP,
                Wins = team.Wins,
                TotalMatches = team.TotalMatches
            }).ToList()
        };
    }

    private static string RepeatBucket(SeasonRepeatPolicy policy, DateTime now) => policy switch
    {
        SeasonRepeatPolicy.Daily => now.ToString("yyyyMMdd"),
        SeasonRepeatPolicy.Weekly => $"{now:yyyy}-{System.Globalization.ISOWeek.GetWeekOfYear(now):00}",
        SeasonRepeatPolicy.OnceEver => "ever",
        _ => "season"
    };

    private static string RequiredId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim();

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void Replace<T>(List<T> list, T item, Func<T, string> id)
    {
        var index = list.FindIndex(current => Same(id(current), id(item)));
        if (index >= 0) list[index] = item; else list.Add(item);
    }
}

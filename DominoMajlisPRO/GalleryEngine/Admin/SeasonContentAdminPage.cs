using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class SeasonContentAdminPage : ContentPage
{
    private readonly VerticalStackLayout _root = new() { Spacing = 14, Padding = new Thickness(14, 18, 14, 90) };
    private readonly Picker _seasonPicker = Picker("اختر الموسم");
    private readonly DatePicker _startDate = new() { Format = "yyyy-MM-dd" };
    private readonly DatePicker _endDate = new() { Format = "yyyy-MM-dd" };
    private readonly Entry _storyTitle = Entry("عنوان القصة بالعربية");
    private readonly Editor _storySynopsis = Editor("ملخص القصة");
    private readonly Editor _storyPrologue = Editor("مقدمة القصة");
    private readonly Editor _storyEpilogue = Editor("خاتمة القصة");
    private readonly Entry _chapterTitle = Entry("عنوان الفصل");
    private readonly Editor _chapterBody = Editor("نص الفصل");
    private readonly Entry _characterName = Entry("اسم الشخصية");
    private readonly Entry _characterRole = Entry("دور الشخصية");
    private readonly Entry _factionName = Entry("اسم الفصيل");
    private readonly Editor _factionDescription = Editor("وصف الفصيل");
    private readonly Entry _timelineTitle = Entry("عنوان الحدث");
    private readonly Editor _timelineBody = Editor("تفاصيل الحدث");
    private readonly Label _storySummary = Label(string.Empty, 12, "#A9A294");
    private readonly VerticalStackLayout _chapterList = new() { Spacing = 8 };
    private readonly Entry _ruleTitle = Entry("عنوان التحدي أو الجائزة");
    private readonly Editor _ruleDescription = Editor("الوصف الظاهر للمستخدم");
    private readonly Picker _conditionPicker = Picker("نوع الشرط");
    private readonly Entry _threshold = Entry("القيمة المطلوبة", Keyboard.Numeric);
    private readonly Picker _targetPicker = Picker("نطاق الشرط");
    private readonly Picker _rewardPicker = Picker("نوع الجائزة");
    private readonly Entry _rewardAmount = Entry("قيمة الجائزة", Keyboard.Numeric);
    private readonly Picker _assetPicker = Picker("أصل منشور من المتجر");
    private readonly Image _assetPreviewImage = new() { HeightRequest = 74, WidthRequest = 74, Aspect = Aspect.AspectFit };
    private readonly Label _assetPreviewTitle = Label("معاينة الأصل المختار", 13, "#F2CB69", true);
    private readonly Label _assetPreviewMeta = Label(string.Empty, 11, "#B9AE98");
    private readonly Label _assetPreviewEmpty = Label("لا توجد أصول منشورة مطابقة لهذا النوع.", 12, "#918B80");
    private readonly Border _assetPreviewCard;
    private readonly Picker _claimModePicker = Picker("طريقة الاستلام");
    private readonly Picker _repeatPicker = Picker("سياسة التكرار");
    private readonly VerticalStackLayout _rulesList = new() { Spacing = 10 };
    private readonly Picker _logicalPicker = Picker("AND / OR");
    private readonly Picker _secondConditionPicker = Picker("شرط ثانٍ اختياري");
    private readonly Entry _secondThreshold = Entry("قيمة الشرط الثاني - اختياري", Keyboard.Numeric);
    private readonly Entry _minimumMatches = Entry("الحد الأدنى للمباريات - اختياري", Keyboard.Numeric);
    private readonly Switch _customWindowSwitch = new();
    private readonly DatePicker _ruleStartDate = new() { Format = "yyyy-MM-dd" };
    private readonly DatePicker _ruleEndDate = new() { Format = "yyyy-MM-dd" };
    private readonly Label _status = Label(string.Empty, 12, "#E9C76B");
    private readonly List<SeasonDefinition> _seasons = new();
    private readonly List<CatalogAssetDisplay> _assets = new();
    private SeasonStory? _story;

    public SeasonContentAdminPage()
    {
        Title = "إدارة محتوى الموسم";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#050607");
        _assetPreviewCard = BuildAssetPreviewCard();
        Build();
        Content = new ScrollView { Content = _root };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = await ApplicationUserService.GetCurrentUserAsync();
        if (user.Role != ApplicationUserRole.Developer)
        {
            _root.Children.Clear();
            _root.Children.Add(Label("غير مصرح. هذه الصفحة متاحة للمطور فقط.", 17, "#E36D6D", true));
            return;
        }
        await LoadAsync();
    }

    private void Build()
    {
        _root.Children.Add(Label("قصة الموسم والتحديات والجوائز", 25, "#F2CB69", true));
        _root.Children.Add(Label("كل البيانات ديناميكية، ولا تظهر للمستخدم قبل نشرها.", 13, "#AAA394"));
        _root.Children.Add(_status);
        _root.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                Label("الموسم", 18, "#F2CB69", true), _seasonPicker,
                Label("بداية الموسم", 12, "#B9AE98"), _startDate,
                Label("نهاية الموسم", 12, "#B9AE98"), _endDate,
                Button("حفظ تواريخ الموسم", SaveSeasonDatesAsync),
                TwoButtons("نسخ الموسم", DuplicateSeasonAsync, "معاينة كلاعب", PreviewAsPlayerAsync),
                Button("التحقق من تكوين الموسم", ValidateSeasonAsync, secondary: true),
                TwoButtons("إلغاء النشر", UnpublishSeasonAsync, "أرشفة الموسم", ArchiveSeasonAsync)
            }
        }));

        _root.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                Label("قصة الموسم", 18, "#F2CB69", true), _storyTitle, _storySynopsis, _storyPrologue, _storyEpilogue,
                Label("الفصول", 15, "#D7B14E", true), _chapterTitle, _chapterBody,
                Button("إضافة فصل", AddChapterAsync, secondary: true), _chapterList,
                Label("الشخصيات", 15, "#D7B14E", true), _characterName, _characterRole,
                Button("إضافة شخصية", AddCharacterAsync, secondary: true),
                Label("الفصائل", 15, "#D7B14E", true), _factionName, _factionDescription,
                Button("إضافة فصيل", AddFactionAsync, secondary: true),
                Label("الخط الزمني", 15, "#D7B14E", true), _timelineTitle, _timelineBody,
                Button("إضافة حدث", AddTimelineAsync, secondary: true), _storySummary,
                TwoButtons("حفظ القصة كمسودة", () => SaveStoryAsync(false), "نشر القصة", () => SaveStoryAsync(true))
            }
        }));

        var conditions = Enum.GetValues<SeasonConditionType>()
            .Where(value => value != SeasonConditionType.CustomCompositeRule)
            .ToList();
        _conditionPicker.ItemsSource = EnumChoices(conditions);
        _secondConditionPicker.ItemsSource = EnumChoices(conditions);
        _logicalPicker.ItemsSource = EnumChoices(Enum.GetValues<CompositeLogicalOperator>());
        _targetPicker.ItemsSource = EnumChoices(Enum.GetValues<SeasonTargetScope>());
        _rewardPicker.ItemsSource = EnumChoices(Enum.GetValues<SeasonRewardType>());
        _claimModePicker.ItemsSource = EnumChoices(Enum.GetValues<SeasonClaimMode>());
        _repeatPicker.ItemsSource = EnumChoices(Enum.GetValues<SeasonRepeatPolicy>());
        _conditionPicker.SelectedIndex = 0;
        _secondConditionPicker.SelectedIndex = 0;
        SelectValue(_logicalPicker, CompositeLogicalOperator.And);
        SelectValue(_targetPicker, SeasonTargetScope.Player);
        SelectValue(_rewardPicker, SeasonRewardType.Coins);
        SelectValue(_claimModePicker, SeasonClaimMode.ManualClaim);
        SelectValue(_repeatPicker, SeasonRepeatPolicy.OncePerSeason);
        _targetPicker.SelectedIndexChanged += (_, _) => RefreshAssetPicker();
        _rewardPicker.SelectedIndexChanged += (_, _) => RefreshAssetPicker();
        _assetPicker.SelectedIndexChanged += (_, _) => RefreshSelectedAssetPreview();

        _root.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                Label("قاعدة تحدٍ وجائزة", 18, "#F2CB69", true),
                _ruleTitle, _ruleDescription, _conditionPicker, _threshold,
                Label("شرط مركب اختياري", 13, "#D7B14E", true),
                _logicalPicker, _secondConditionPicker, _secondThreshold, _minimumMatches,
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { Label("نافذة زمنية مخصصة", 13, "#D7B14E", true), _customWindowSwitch }
                },
                Label("بداية نافذة القاعدة", 12, "#B9AE98"), _ruleStartDate,
                Label("نهاية نافذة القاعدة", 12, "#B9AE98"), _ruleEndDate,
                _targetPicker,
                _rewardPicker, _rewardAmount, _assetPicker, _assetPreviewCard, _claimModePicker, _repeatPicker,
                TwoButtons("حفظ كمسودة", () => SaveRuleAsync(false), "نشر القاعدة", () => SaveRuleAsync(true))
            }
        }));
        _root.Children.Add(Label("القواعد الحالية", 18, "#F2CB69", true));
        _root.Children.Add(_rulesList);
        _root.Children.Add(Label("تشخيص منح الجوائز", 18, "#F2CB69", true));
        _root.Children.Add(Button("تحديث سجل المطالبات", RefreshDiagnosticsAsync, secondary: true));

        _seasonPicker.SelectedIndexChanged += async (_, _) => await LoadSelectedSeasonAsync();
        _customWindowSwitch.Toggled += (_, args) =>
        {
            _ruleStartDate.IsVisible = args.Value;
            _ruleEndDate.IsVisible = args.Value;
        };
        _ruleStartDate.IsVisible = false;
        _ruleEndDate.IsVisible = false;
    }

    private async Task LoadAsync()
    {
        _seasons.Clear();
        _seasons.AddRange(await SeasonExperienceService.LoadDefinitionsAsync());
        _assets.Clear();
        _assets.AddRange(await StoreAssetCatalogService.LoadPublishedRewardAssetsAsync());
        _seasonPicker.ItemsSource = _seasons;
        _seasonPicker.ItemDisplayBinding = new Binding(nameof(SeasonDefinition.TitleAr));
        if (_seasons.Count > 0) _seasonPicker.SelectedIndex = 0;
        else _status.Text = "انشر بطاقة موسم أولاً، ثم عد لإدارة القصة والجوائز.";
        RefreshAssetPicker();
    }

    private SeasonDefinition SelectedSeason() => _seasonPicker.SelectedItem as SeasonDefinition
        ?? throw new InvalidOperationException("اختر موسماً أولاً.");

    private async Task LoadSelectedSeasonAsync()
    {
        if (_seasonPicker.SelectedItem is not SeasonDefinition season) return;
        _startDate.Date = season.StartDateUtc.ToLocalTime().Date;
        _endDate.Date = season.EndDateUtc.ToLocalTime().Date;
        _story = (await SeasonExperienceService.LoadStoriesAsync())
            .Where(item => item.SeasonId.Equals(season.SeasonId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.IsPublished).FirstOrDefault()
            ?? new SeasonStory { SeasonId = season.SeasonId };
        _storyTitle.Text = _story.TitleAr;
        _storySynopsis.Text = _story.SynopsisAr;
        _storyPrologue.Text = _story.PrologueAr;
        _storyEpilogue.Text = _story.EpilogueAr;
        RefreshStorySummary();
        await RefreshRulesAsync();
    }

    private async Task SaveSeasonDatesAsync()
    {
        try
        {
            var season = SelectedSeason();
            var start = _startDate.Date ?? DateTime.Today;
            var end = _endDate.Date ?? start.AddDays(SeasonManager.SeasonDurationDays);
            season.StartDateUtc = start.ToUniversalTime();
            season.EndDateUtc = end.AddDays(1).AddTicks(-1).ToUniversalTime();
            await SeasonExperienceService.SaveSeasonAsync(season, season.IsPublished);
            _status.Text = "تم حفظ تواريخ الموسم.";
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task DuplicateSeasonAsync()
    {
        try
        {
            await SeasonExperienceService.DuplicateSeasonAsync(SelectedSeason().SeasonId);
            _status.Text = "تم إنشاء نسخة مستقلة كمسودة.";
            await LoadAsync();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task PreviewAsPlayerAsync() =>
        await Navigation.PushAsync(new DominoMajlisPRO.GalleryEngine.Pages.SeasonPage());

    private async Task UnpublishSeasonAsync()
    {
        try
        {
            await SeasonExperienceService.UnpublishSeasonAsync(SelectedSeason().SeasonId);
            _status.Text = "تم إلغاء نشر الموسم مع الاحتفاظ ببياناته.";
            await LoadAsync();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task ArchiveSeasonAsync()
    {
        try
        {
            var season = SelectedSeason();
            await SeasonExperienceService.ArchiveSeasonAsync(season.SeasonId);
            await CurrentSeasonAdminService.HidePublishedAsync(season.SeasonId);
            _status.Text = "تمت أرشفة الموسم دون حذف تاريخه أو مطالباته.";
            await LoadAsync();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task SaveStoryAsync(bool publish)
    {
        try
        {
            var story = StoryFromFields();
            _story = await SeasonExperienceService.SaveStoryAsync(story, publish);
            _status.Text = publish ? "تم نشر قصة الموسم." : "تم حفظ مسودة القصة.";
            RefreshStorySummary();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private SeasonStory StoryFromFields()
    {
        var season = SelectedSeason();
        _story ??= new SeasonStory { SeasonId = season.SeasonId };
        _story.SeasonId = season.SeasonId;
        _story.TitleAr = _storyTitle.Text?.Trim() ?? string.Empty;
        _story.SynopsisAr = _storySynopsis.Text?.Trim() ?? string.Empty;
        _story.PrologueAr = _storyPrologue.Text?.Trim() ?? string.Empty;
        _story.EpilogueAr = _storyEpilogue.Text?.Trim() ?? string.Empty;
        return _story;
    }

    private Task AddChapterAsync()
    {
        var story = StoryFromFields();
        if (string.IsNullOrWhiteSpace(_chapterTitle.Text)) { _status.Text = "اكتب عنوان الفصل."; return Task.CompletedTask; }
        story.Chapters.Add(new SeasonStoryChapter
        {
            StoryId = story.StoryId, SortOrder = story.Chapters.Count,
            TitleAr = _chapterTitle.Text.Trim(), BodyAr = _chapterBody.Text?.Trim() ?? string.Empty,
            IsInitiallyVisible = true
        });
        _chapterTitle.Text = _chapterBody.Text = string.Empty;
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private Task AddCharacterAsync()
    {
        var story = StoryFromFields();
        if (string.IsNullOrWhiteSpace(_characterName.Text)) { _status.Text = "اكتب اسم الشخصية."; return Task.CompletedTask; }
        story.Characters.Add(new SeasonStoryCharacter
        {
            NameAr = _characterName.Text.Trim(), RoleAr = _characterRole.Text?.Trim() ?? string.Empty
        });
        _characterName.Text = _characterRole.Text = string.Empty;
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private Task AddFactionAsync()
    {
        var story = StoryFromFields();
        if (string.IsNullOrWhiteSpace(_factionName.Text)) { _status.Text = "اكتب اسم الفصيل."; return Task.CompletedTask; }
        story.Factions.Add(new SeasonStoryFaction
        {
            NameAr = _factionName.Text.Trim(), DescriptionAr = _factionDescription.Text?.Trim() ?? string.Empty
        });
        _factionName.Text = _factionDescription.Text = string.Empty;
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private Task AddTimelineAsync()
    {
        var story = StoryFromFields();
        if (string.IsNullOrWhiteSpace(_timelineTitle.Text)) { _status.Text = "اكتب عنوان الحدث."; return Task.CompletedTask; }
        story.TimelineEntries.Add(new SeasonStoryTimelineEntry
        {
            SortOrder = story.TimelineEntries.Count, TitleAr = _timelineTitle.Text.Trim(),
            BodyAr = _timelineBody.Text?.Trim() ?? string.Empty
        });
        _timelineTitle.Text = _timelineBody.Text = string.Empty;
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private void RefreshStorySummary()
    {
        _storySummary.Text = _story == null ? string.Empty :
            $"الفصول: {_story.Chapters.Count} | الشخصيات: {_story.Characters.Count} | الفصائل: {_story.Factions.Count} | الأحداث: {_story.TimelineEntries.Count}";
        RefreshChapterList();
    }

    private void RefreshChapterList()
    {
        _chapterList.Children.Clear();
        if (_story?.Chapters.Count is not > 0)
            return;

        foreach (var chapter in _story.Chapters.OrderBy(item => item.SortOrder).ToList())
        {
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 6
            };
            row.Add(Label(chapter.TitleAr, 13, "#F2CB69", true), 0, 0);
            row.Add(Button("↑", () => MoveChapterAsync(chapter, -1), true), 1, 0);
            row.Add(Button("↓", () => MoveChapterAsync(chapter, 1), true), 2, 0);
            row.Add(Button("حذف", () => DeleteChapterAsync(chapter), true), 3, 0);
            _chapterList.Children.Add(Card(row));
        }
    }

    private Task MoveChapterAsync(SeasonStoryChapter chapter, int direction)
    {
        if (_story == null)
            return Task.CompletedTask;
        var ordered = _story.Chapters.OrderBy(item => item.SortOrder).ToList();
        var index = ordered.FindIndex(item => item.ChapterId == chapter.ChapterId);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count)
            return Task.CompletedTask;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].SortOrder = i;
        _story.Chapters = ordered;
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private Task DeleteChapterAsync(SeasonStoryChapter chapter)
    {
        _story?.Chapters.RemoveAll(item => item.ChapterId == chapter.ChapterId);
        RefreshStorySummary();
        return Task.CompletedTask;
    }

    private async Task SaveRuleAsync(bool publish)
    {
        try
        {
            var season = SelectedSeason();
            var rewardType = SelectedValue(_rewardPicker, SeasonRewardType.Coins);
            var asset = _assetPicker.SelectedItem as CatalogAssetDisplay;
            var firstCondition = SelectedValue(_conditionPicker, SeasonConditionType.MatchesCompleted);
            var firstThreshold = ParseNumber(_threshold.Text);
            var secondThreshold = ParseNumber(_secondThreshold.Text);
            var minimumMatches = (int)ParseNumber(_minimumMatches.Text);
            var hasCompositeCondition = secondThreshold > 0;
            var rule = new SeasonRewardRule
            {
                SeasonId = season.SeasonId,
                TitleAr = _ruleTitle.Text?.Trim() ?? string.Empty,
                DescriptionAr = _ruleDescription.Text?.Trim() ?? string.Empty,
                ConditionType = hasCompositeCondition ? SeasonConditionType.CustomCompositeRule : firstCondition,
                PrimaryThreshold = hasCompositeCondition ? 1 : firstThreshold,
                MinimumMatches = minimumMatches > 0 ? minimumMatches : null,
                TimeWindowType = _customWindowSwitch.IsToggled
                    ? SeasonTimeWindowType.Custom
                    : SeasonTimeWindowType.Season,
                StartDateUtc = _customWindowSwitch.IsToggled
                    ? (_ruleStartDate.Date ?? DateTime.Today).ToUniversalTime()
                    : null,
                EndDateUtc = _customWindowSwitch.IsToggled
                    ? (_ruleEndDate.Date ?? DateTime.Today).AddDays(1).AddTicks(-1).ToUniversalTime()
                    : null,
                TargetScope = SelectedValue(_targetPicker, SeasonTargetScope.Player),
                RewardType = rewardType,
                RewardAmount = UsesAmount(rewardType) ? (int)ParseNumber(_rewardAmount.Text) : null,
                RewardAssetId = asset?.AssetId,
                RewardStoreTypeId = asset?.AssetType.ToString(),
                ClaimMode = SelectedValue(_claimModePicker, SeasonClaimMode.ManualClaim),
                RepeatPolicy = SelectedValue(_repeatPicker, SeasonRepeatPolicy.OncePerSeason),
                MaxClaims = 1,
                IsActive = true,
                CompositeCondition = hasCompositeCondition
                    ? new SeasonCompositeCondition
                    {
                        Operator = SelectedValue(_logicalPicker, CompositeLogicalOperator.And),
                        Items =
                        {
                            new SeasonCompositeConditionItem { ConditionType = firstCondition, Threshold = firstThreshold },
                            new SeasonCompositeConditionItem
                            {
                                ConditionType = SelectedValue(_secondConditionPicker, SeasonConditionType.Wins),
                                Threshold = secondThreshold
                            }
                        }
                    }
                    : null
            };
            await SeasonExperienceService.SaveRuleAsync(rule, publish);
            _status.Text = publish ? "تم نشر قاعدة الجائزة." : "تم حفظ قاعدة الجائزة كمسودة.";
            _ruleTitle.Text = _ruleDescription.Text = _threshold.Text = _secondThreshold.Text =
                _minimumMatches.Text = _rewardAmount.Text = string.Empty;
            await RefreshRulesAsync();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task RefreshRulesAsync()
    {
        _rulesList.Children.Clear();
        if (_seasonPicker.SelectedItem is not SeasonDefinition season) return;
        var rules = (await SeasonExperienceService.LoadRulesAsync())
            .Where(item => item.SeasonId.Equals(season.SeasonId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.SortOrder).ToList();
        if (rules.Count == 0)
        {
            _rulesList.Children.Add(Label("لا توجد قواعد محفوظة.", 13, "#918B80"));
            return;
        }
        foreach (var rule in rules)
        {
            var toggle = Button(rule.IsPublished ? "إخفاء" : "نشر", async () =>
            {
                await SeasonExperienceService.SetRulePublishedAsync(rule.RewardRuleId, !rule.IsPublished);
                await RefreshRulesAsync();
            }, secondary: true);
            var duplicate = Button("نسخ", async () =>
            {
                await SeasonExperienceService.DuplicateRuleAsync(rule.RewardRuleId);
                await RefreshRulesAsync();
            }, secondary: true);
            var deleteDraft = Button("حذف المسودة", async () =>
            {
                await SeasonExperienceService.DeleteRuleDraftAsync(rule.RewardRuleId);
                await RefreshRulesAsync();
            }, secondary: true);
            deleteDraft.IsVisible = !rule.IsPublished;
            _rulesList.Children.Add(Card(new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    Label(rule.TitleAr, 15, "#F2CB69", true),
                    Label(SeasonExperienceService.DescribeCondition(rule), 12, "#C7BFAF"),
                    Label(rule.IsPublished ? "منشور" : "مسودة / مخفي", 11, rule.IsPublished ? "#62D17A" : "#D59B4B"),
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                        Children = { toggle, duplicate }
                    },
                    deleteDraft
                }
            }));
            Grid.SetColumn(duplicate, 1);
        }
    }

    private async Task RefreshDiagnosticsAsync()
    {
        var claims = await SeasonExperienceService.LoadClaimsAsync();
        var seasonId = (_seasonPicker.SelectedItem as SeasonDefinition)?.SeasonId;
        var relevant = claims.Where(item => string.Equals(item.SeasonId, seasonId, StringComparison.OrdinalIgnoreCase)).ToList();
        var failed = relevant.Count(item => item.GrantStatus == SeasonGrantStatus.Failed);
        var pending = relevant.Count(item => item.GrantStatus == SeasonGrantStatus.Pending);
        _status.Text = $"المطالبات: {relevant.Count} | ناجحة: {relevant.Count - failed - pending} | معلقة: {pending} | فاشلة: {failed}";
    }

    private async Task ValidateSeasonAsync()
    {
        try
        {
            var count = await SeasonExperienceService.ValidateSeasonConfigurationAsync(SelectedSeason().SeasonId);
            _status.Text = $"تم التحقق من الموسم بنجاح: {count} قاعدة صالحة.";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private void RefreshAssetPicker()
    {
        var scope = SelectedValue(_targetPicker, SeasonTargetScope.Player);
        var reward = SelectedValue(_rewardPicker, SeasonRewardType.Coins);
        _assetPicker.IsVisible = !UsesAmount(reward);
        _rewardAmount.IsVisible = UsesAmount(reward);
        var owner = scope == SeasonTargetScope.Team ? StoreProductOwnerScope.Team : StoreProductOwnerScope.Player;
        var expectedAssetType = RewardAssetType(reward);
        var filtered = _assets
            .Where(item => item.OwnerScope == owner)
            .Where(item => expectedAssetType == null || item.AssetType == expectedAssetType)
            .ToList();
        _assetPicker.ItemsSource = filtered;
        _assetPicker.ItemDisplayBinding = new Binding(nameof(CatalogAssetDisplay.DisplayName));
        _assetPicker.SelectedIndex = filtered.Count > 0 ? 0 : -1;
        RefreshSelectedAssetPreview();
    }

    private Border BuildAssetPreviewCard()
    {
        var info = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children = { _assetPreviewTitle, _assetPreviewMeta, _assetPreviewEmpty }
        };
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12,
            FlowDirection = FlowDirection.RightToLeft
        };
        grid.Add(_assetPreviewImage, 0, 0);
        grid.Add(info, 1, 0);

        return Card(grid);
    }

    private void RefreshSelectedAssetPreview()
    {
        var usesAmount = UsesAmount(SelectedValue(_rewardPicker, SeasonRewardType.Coins));
        _assetPreviewCard.IsVisible = !usesAmount;
        if (usesAmount)
            return;

        if (_assetPicker.SelectedItem is not CatalogAssetDisplay asset)
        {
            _assetPreviewImage.IsVisible = false;
            _assetPreviewTitle.Text = "معاينة الأصل المختار";
            _assetPreviewMeta.Text = string.Empty;
            _assetPreviewEmpty.IsVisible = true;
            return;
        }

        var title = string.IsNullOrWhiteSpace(asset.ArabicDisplayName)
            ? asset.DisplayName
            : asset.ArabicDisplayName;
        _assetPreviewTitle.Text = string.IsNullOrWhiteSpace(title) ? asset.AssetId : title;
        _assetPreviewMeta.Text =
            $"{LocalizeAssetType(asset.AssetType)} · {LocalizeOwnerScope(asset.OwnerScope)} · {LocalizeRarity(asset.Rarity)}";
        _assetPreviewEmpty.IsVisible = false;

        var hasImage = !string.IsNullOrWhiteSpace(asset.PreviewImage) &&
                       !RemovedStoreAssetPolicy.IsRemoved(asset.PreviewImage);
        _assetPreviewImage.IsVisible = hasImage;
        _assetPreviewImage.Source = hasImage
            ? InventoryDisplayResolver.ResolveOptionalImageSource(asset.PreviewImage)
            : null;
    }

    private static bool UsesAmount(SeasonRewardType type) => type is SeasonRewardType.Coins or SeasonRewardType.Gems or SeasonRewardType.SeasonXP;
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

    private static string LocalizeOwnerScope(StoreProductOwnerScope scope) => scope switch
    {
        StoreProductOwnerScope.Team => "الفريق",
        StoreProductOwnerScope.Player => "اللاعب",
        _ => scope.ToString()
    };

    private static string LocalizeAssetType(StoreProductAssetType type) => type switch
    {
        StoreProductAssetType.Avatar => "أفاتار",
        StoreProductAssetType.ProfileBackground => "خلفية ملف",
        StoreProductAssetType.Frame => "إطار لاعب",
        StoreProductAssetType.Effect => "تأثير لاعب",
        StoreProductAssetType.PlayerNameEffect => "تأثير اسم لاعب",
        StoreProductAssetType.PlayerNameFrame => "إطار اسم لاعب",
        StoreProductAssetType.Title => "لقب",
        StoreProductAssetType.Badge => "شارة",
        StoreProductAssetType.Emblem => "شعار فريق",
        StoreProductAssetType.TeamColor => "لون فريق",
        StoreProductAssetType.EmblemBackground => "خلفية شعار",
        StoreProductAssetType.TeamEffect => "تأثير فريق",
        StoreProductAssetType.TeamNameEffect => "تأثير اسم فريق",
        StoreProductAssetType.TeamNameFrame => "إطار اسم فريق",
        StoreProductAssetType.Bundle => "حزمة",
        _ => type.ToString()
    };

    private static string LocalizeRarity(string? rarity) =>
        string.IsNullOrWhiteSpace(rarity)
            ? "ندرة عادية"
            : rarity.Trim();
    private static double ParseNumber(string? text) => double.TryParse(text, out var value) ? value : 0;

    private static Grid TwoButtons(string left, Func<Task> leftAction, string right, Func<Task> rightAction)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 9 };
        grid.Add(Button(left, leftAction, true), 0, 0);
        grid.Add(Button(right, rightAction), 1, 0);
        return grid;
    }

    private static Button Button(string text, Func<Task> action, bool secondary = false)
    {
        var button = new Button
        {
            Text = text, HeightRequest = 46, CornerRadius = 12, FontFamily = "Tajawal-Regular",
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb(secondary ? "#1B1915" : "#D5A52B"),
            TextColor = Color.FromArgb(secondary ? "#E8CC80" : "#150F03"),
            BorderColor = Color.FromArgb("#765313"), BorderWidth = secondary ? 1 : 0
        };
        button.Clicked += async (_, _) => { button.IsEnabled = false; try { await action(); } finally { button.IsEnabled = true; } };
        return button;
    }

    private static Border Card(View content) => new()
    {
        Content = content, Padding = 14, BackgroundColor = Color.FromArgb("#0C0E0F"),
        Stroke = Color.FromArgb("#755313"), StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 14 }
    };

    private static Label Label(string text, double size, string color, bool bold = false) => new()
    {
        Text = text, FontFamily = "Tajawal-Regular", FontSize = size,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = Color.FromArgb(color), LineBreakMode = LineBreakMode.WordWrap
    };

    private static Entry Entry(string placeholder, Keyboard? keyboard = null) => new()
    {
        Placeholder = placeholder, Keyboard = keyboard ?? Keyboard.Default,
        FontFamily = "Tajawal-Regular", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#817B70")
    };

    private static Editor Editor(string placeholder) => new()
    {
        Placeholder = placeholder, AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 72,
        FontFamily = "Tajawal-Regular", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#817B70")
    };

    private static Picker Picker(string title) => new()
    {
        Title = title, FontFamily = "Tajawal-Regular", TextColor = Colors.White,
        TitleColor = Color.FromArgb("#817B70")
    };

    private sealed record EnumChoice<T>(T Value, string DisplayName) where T : struct, Enum
    {
        public override string ToString() => DisplayName;
    }

    private static List<EnumChoice<T>> EnumChoices<T>(IEnumerable<T> values)
        where T : struct, Enum =>
        values.Select(value => new EnumChoice<T>(value, LocalizeEnum(value))).ToList();

    private static T SelectedValue<T>(Picker picker, T fallback)
        where T : struct, Enum =>
        picker.SelectedItem switch
        {
            EnumChoice<T> choice => choice.Value,
            T value => value,
            _ => fallback
        };

    private static void SelectValue<T>(Picker picker, T value)
        where T : struct, Enum
    {
        foreach (var item in picker.ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>())
        {
            if (item is EnumChoice<T> choice &&
                EqualityComparer<T>.Default.Equals(choice.Value, value))
            {
                picker.SelectedItem = choice;
                return;
            }
        }

        picker.SelectedItem = value;
    }

    private static string LocalizeEnum<T>(T value)
        where T : struct, Enum =>
        value switch
        {
            CompositeLogicalOperator.And => "كل الشروط",
            CompositeLogicalOperator.Or => "أي شرط",

            SeasonTargetScope.Player => "اللاعب",
            SeasonTargetScope.Team => "الفريق",

            SeasonConditionType.MatchesCompleted => "إكمال مباريات",
            SeasonConditionType.Wins => "تحقيق انتصارات",
            SeasonConditionType.ConsecutiveWins => "انتصارات متتالية",
            SeasonConditionType.SeasonXpEarned => "جمع XP موسمي",
            SeasonConditionType.ReachRank => "الوصول إلى رتبة",
            SeasonConditionType.TrustScoreAtLeast => "ثقة اللاعب على الأقل",
            SeasonConditionType.WinRateAtLeast => "نسبة الفوز على الأقل",
            SeasonConditionType.WinsAgainstDistinctTeams => "الفوز على فرق مختلفة",
            SeasonConditionType.PerfectMatchWin => "فوز مثالي",
            SeasonConditionType.ComebackWin => "فوز بعد عودة",
            SeasonConditionType.ActiveDays => "أيام نشاط",
            SeasonConditionType.ConsecutiveActiveDays => "أيام نشاط متتالية",
            SeasonConditionType.WeeklyMatches => "مباريات أسبوعية",
            SeasonConditionType.WeeklyWins => "انتصارات أسبوعية",
            SeasonConditionType.TopPlacementAtSeasonEnd => "مركز متقدم عند نهاية الموسم",
            SeasonConditionType.ChampionAtSeasonEnd => "بطل الموسم",
            SeasonConditionType.MvpAtSeasonEnd => "أفضل لاعب في الموسم",

            SeasonRewardType.Coins => "عملات",
            SeasonRewardType.Gems => "جواهر",
            SeasonRewardType.SeasonXP => "XP موسمي",
            SeasonRewardType.Avatar => "أفاتار لاعب",
            SeasonRewardType.ProfileBackground => "خلفية ملف اللاعب",
            SeasonRewardType.PlayerFrame => "إطار لاعب",
            SeasonRewardType.PlayerEffect => "تأثير لاعب",
            SeasonRewardType.PlayerNameEffect => "تأثير اسم اللاعب",
            SeasonRewardType.PlayerNameFrame => "إطار اسم اللاعب",
            SeasonRewardType.Title => "لقب",
            SeasonRewardType.Badge => "شارة",
            SeasonRewardType.Emblem => "شعار فريق",
            SeasonRewardType.TeamLivingEmblem => "شعار حي للفريق",
            SeasonRewardType.TeamColor => "لون الفريق",
            SeasonRewardType.EmblemBackground => "خلفية شعار الفريق",
            SeasonRewardType.TeamEffect => "تأثير الفريق",
            SeasonRewardType.TeamNameEffect => "تأثير اسم الفريق",
            SeasonRewardType.TeamNameFrame => "إطار اسم الفريق",
            SeasonRewardType.Bundle => "حزمة",

            SeasonClaimMode.AutoGrant => "منح تلقائي",
            SeasonClaimMode.ManualClaim => "استلام يدوي",

            SeasonRepeatPolicy.OncePerSeason => "مرة واحدة في الموسم",
            SeasonRepeatPolicy.OnceEver => "مرة واحدة دائماً",
            SeasonRepeatPolicy.Repeatable => "قابل للتكرار",
            SeasonRepeatPolicy.Daily => "يومي",
            SeasonRepeatPolicy.Weekly => "أسبوعي",

            _ => value.ToString()
        };
}

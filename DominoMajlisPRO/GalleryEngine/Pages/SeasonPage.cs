using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Pages;

public sealed class SeasonPage : ContentPage
{
    private readonly VerticalStackLayout _content;
    private readonly Label _status;
    private bool _loading;

    public SeasonPage()
    {
        Title = "الموسم";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#050607");
        _status = TextLabel(string.Empty, 13, Color.FromArgb("#E7C979"), true);
        _content = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(14, 18, 14, 110) };
        Content = new ScrollView { Content = _content };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppEvents.SeasonChanged -= OnSeasonChanged;
        AppEvents.SeasonChanged += OnSeasonChanged;
        AppEvents.SeasonProgressChanged -= OnSeasonProgressChanged;
        AppEvents.SeasonProgressChanged += OnSeasonProgressChanged;
        AppEvents.SeasonRewardClaimChanged -= OnSeasonProgressChanged;
        AppEvents.SeasonRewardClaimChanged += OnSeasonProgressChanged;
        _ = LoadAsync();
    }

    protected override void OnDisappearing()
    {
        AppEvents.SeasonChanged -= OnSeasonChanged;
        AppEvents.SeasonProgressChanged -= OnSeasonProgressChanged;
        AppEvents.SeasonRewardClaimChanged -= OnSeasonProgressChanged;
        base.OnDisappearing();
    }

    private void OnSeasonChanged() => _ = LoadAsync();
    private void OnSeasonProgressChanged(string playerId) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            _content.Children.Clear();
            _content.Children.Add(Header());
            _content.Children.Add(_status);
            _status.Text = "جاري تحديث الموسم...";

            var season = await SeasonExperienceService.LoadActiveSeasonAsync();
            if (season == null)
            {
                _status.Text = "لا يوجد موسم منشور ونشط حالياً";
                _content.Children.Add(EmptyCard("سينشر المطور تفاصيل الموسم هنا عند جاهزيتها."));
                return;
            }

            _status.Text = string.Empty;
            _content.Children.Add(SeasonHero(season));
            var story = await SeasonExperienceService.LoadPublishedStoryAsync(season.SeasonId);
            _content.Children.Add(SectionTitle("قصة الموسم"));
            _content.Children.Add(story == null ? EmptyCard("لم يتم نشر قصة الموسم بعد") : StoryCard(story));

            _content.Children.Add(SectionTitle("ملخص ترتيب الموسم"));
            _content.Children.Add(await RankingSummaryAsync());

            _content.Children.Add(SectionTitle("التحديات والجوائز"));
            var rules = await SeasonExperienceService.LoadPublishedRulesAsync(season.SeasonId);
            if (rules.Count == 0)
            {
                _content.Children.Add(EmptyCard("لم يتم نشر جوائز لهذا الموسم بعد"));
            }
            else
            {
                var playerId = await ApplicationUserService.GetCurrentUserPlayerIdAsync();
                foreach (var rule in rules)
                    _content.Children.Add(await RewardCardAsync(rule, playerId));
            }
            var archiveButton = new Button
            {
                Text = "أرشيف المواسم", HeightRequest = 46, CornerRadius = 12,
                BackgroundColor = Color.FromArgb("#171510"), TextColor = Color.FromArgb("#E8C86F"),
                BorderColor = Color.FromArgb("#755313"), BorderWidth = 1, FontFamily = "Tajawal-Regular"
            };
            archiveButton.Clicked += async (_, _) => await Navigation.PushAsync(new SeasonArchivePage());
            _content.Children.Add(archiveButton);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
        finally { _loading = false; }
    }

    private static View Header()
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(TextLabel("الموسم", 26, Color.FromArgb("#F6D06F"), true), 0, 0);
        var back = new Button
        {
            Text = "‹", WidthRequest = 46, HeightRequest = 46, FontSize = 27,
            TextColor = Color.FromArgb("#F6D06F"), BackgroundColor = Color.FromArgb("#12100B"),
            BorderColor = Color.FromArgb("#8A6418"), BorderWidth = 1, CornerRadius = 14
        };
        back.Clicked += async (_, _) =>
        {
            var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation ?? Shell.Current?.Navigation;
            if (navigation != null)
                await navigation.PopAsync();
        };
        grid.Add(back, 1, 0);
        return grid;
    }

    private static View SeasonHero(SeasonDefinition season)
    {
        var days = Math.Max(0, (int)Math.Ceiling((season.EndDateUtc - DateTime.UtcNow).TotalDays));
        var stack = new VerticalStackLayout { Spacing = 7 };
        if (!string.IsNullOrWhiteSpace(season.HeroImagePath))
        {
            stack.Children.Add(new Image
            {
                Source = InventoryDisplayResolver.ResolveImageSource(season.HeroImagePath),
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 190 : 260,
                Aspect = Aspect.AspectFill
            });
        }
        stack.Children.Add(TextLabel($"الموسم {season.SeasonNumber}", 13, Color.FromArgb("#C59B42"), true));
        stack.Children.Add(TextLabel(season.TitleAr, 27, Colors.White, true));
        if (!string.IsNullOrWhiteSpace(season.SubtitleAr))
            stack.Children.Add(TextLabel(season.SubtitleAr, 15, Color.FromArgb("#E7D8B0"), false));
        if (!string.IsNullOrWhiteSpace(season.DescriptionAr))
            stack.Children.Add(TextLabel(season.DescriptionAr, 13, Color.FromArgb("#B8B1A3"), false));
        stack.Children.Add(TextLabel($"متبقٍ {days} يوم", 14, Color.FromArgb("#F6D06F"), true));
        stack.Children.Add(TextLabel($"{season.StartDateUtc.ToLocalTime():yyyy/MM/dd} - {season.EndDateUtc.ToLocalTime():yyyy/MM/dd}", 12, Color.FromArgb("#AFA99B"), false));
        return Card(stack, 18);
    }

    private static View StoryCard(SeasonStory story)
    {
        var stack = new VerticalStackLayout { Spacing = 9 };
        stack.Children.Add(TextLabel(story.TitleAr, 20, Color.FromArgb("#F6D06F"), true));
        if (!string.IsNullOrWhiteSpace(story.SynopsisAr))
            stack.Children.Add(TextLabel(story.SynopsisAr, 14, Color.FromArgb("#E3DED3"), false));
        foreach (var chapter in story.Chapters.OrderBy(item => item.SortOrder).Where(item => item.IsInitiallyVisible))
        {
            stack.Children.Add(TextLabel(chapter.TitleAr, 16, Color.FromArgb("#D6A642"), true));
            stack.Children.Add(TextLabel(chapter.BodyAr, 13, Color.FromArgb("#B8B1A3"), false));
        }
        if (story.Characters.Count > 0)
        {
            stack.Children.Add(TextLabel("الشخصيات", 16, Color.FromArgb("#D6A642"), true));
            foreach (var character in story.Characters)
                stack.Children.Add(TextLabel($"{character.NameAr} - {character.RoleAr}", 13, Color.FromArgb("#D7D0C2"), false));
        }
        if (story.Factions.Count > 0)
        {
            stack.Children.Add(TextLabel("الفصائل", 16, Color.FromArgb("#D6A642"), true));
            foreach (var faction in story.Factions)
                stack.Children.Add(TextLabel($"{faction.NameAr}: {faction.DescriptionAr}", 13, Color.FromArgb("#D7D0C2"), false));
        }
        return Card(stack, 16);
    }

    private async Task<View> RewardCardAsync(SeasonRewardRule rule, string? playerId)
    {
        SeasonRewardProgress? progress = null;
        string? error = null;
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            try { progress = await SeasonExperienceService.EvaluateProgressAsync(rule, playerId); }
            catch (Exception ex) { error = ex.Message; }
        }

        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(TextLabel(rule.TitleAr, 17, Color.FromArgb("#F6D06F"), true));
        stack.Children.Add(TextLabel(SeasonExperienceService.DescribeCondition(rule), 13, Color.FromArgb("#D4C9B0"), false));
        stack.Children.Add(TextLabel(RewardText(rule), 13, Colors.White, true));
        if (!string.IsNullOrWhiteSpace(rule.RewardAssetId))
        {
            var asset = await StoreAssetCatalogService.ResolveAsync(rule.RewardAssetId, rule.RewardStoreTypeId);
            if (asset != null)
            {
                if (asset.AssetType is StoreProductAssetType.PlayerNameEffect or
                    StoreProductAssetType.TeamNameEffect or
                    StoreProductAssetType.PlayerNameFrame or
                    StoreProductAssetType.TeamNameFrame)
                {
                    stack.Children.Add(new IdentityPlateView
                    {
                        DisplayText = "مكافأة الموسم", Preset = asset.TypographyPreset,
                        HeightRequest = 48, MaximumWidthRequest = 320
                    });
                }
                else if (!string.IsNullOrWhiteSpace(asset.PreviewImage))
                {
                    stack.Children.Add(new Image
                    {
                        Source = InventoryDisplayResolver.ResolveImageSource(asset.PreviewImage),
                        HeightRequest = 96, Aspect = Aspect.AspectFit
                    });
                }
            }
        }
        var bar = new ProgressBar
        {
            Progress = progress?.ProgressRatio ?? 0,
            ProgressColor = progress?.IsCompleted == true ? Color.FromArgb("#65D56E") : Color.FromArgb("#E4B52D"),
            BackgroundColor = Color.FromArgb("#26231E"), HeightRequest = 8
        };
        stack.Children.Add(bar);
        stack.Children.Add(TextLabel(progress == null
            ? error ?? "اختر حساب لاعب لمتابعة التقدم"
            : $"{progress.CurrentValue:0} / {progress.RequiredValue:0}", 12, Color.FromArgb("#AFA99B"), false));

        var manual = rule.ClaimMode == SeasonClaimMode.ManualClaim;
        var claim = new Button
        {
            Text = progress?.IsClaimed == true ? "تم الاستلام" : !manual ? "تُمنح تلقائياً عند تحقق الشرط" : progress?.IsClaimable == true ? "استلام الجائزة" : "قيد التقدم",
            IsEnabled = manual && progress?.IsClaimable == true,
            HeightRequest = 46,
            CornerRadius = 12,
            BackgroundColor = progress?.IsClaimable == true ? Color.FromArgb("#D9AA32") : Color.FromArgb("#24221E"),
            TextColor = progress?.IsClaimable == true ? Color.FromArgb("#140F04") : Color.FromArgb("#8D887D"),
            FontFamily = "Tajawal-Regular",
            FontAttributes = FontAttributes.Bold
        };
        claim.Clicked += async (_, _) =>
        {
            claim.IsEnabled = false;
            try
            {
                await SeasonExperienceService.ClaimAsync(rule.RewardRuleId, progress?.TeamId);
                _status.Text = "تمت إضافة الجائزة إلى حسابك بنجاح";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                claim.IsEnabled = true;
            }
        };
        stack.Children.Add(claim);
        return Card(stack, 14);
    }

    private static async Task<View> RankingSummaryAsync()
    {
        var teams = (await TeamProfileService.LoadTeamsAsync())
            .OrderByDescending(item => item.SeasonXP).ThenByDescending(item => item.XP)
            .ThenBy(item => item.TeamId, StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        if (teams.Count == 0) return EmptyCard("لا توجد نتائج موسمية بعد");
        var stack = new VerticalStackLayout { Spacing = 8 };
        for (var index = 0; index < teams.Count; index++)
        {
            var team = teams[index];
            stack.Children.Add(TextLabel($"{index + 1}. {team.TeamName}   {team.SeasonXP:N0} XP", 14,
                index == 0 ? Color.FromArgb("#F6D06F") : Color.FromArgb("#D8D2C7"), index == 0));
        }
        return Card(stack, 14);
    }

    private static string RewardText(SeasonRewardRule rule) => rule.RewardType switch
    {
        SeasonRewardType.Coins => $"{rule.RewardAmount:0} عملة",
        SeasonRewardType.Gems => $"{rule.RewardAmount:0} جوهرة",
        SeasonRewardType.SeasonXP => $"{rule.RewardAmount:0} XP موسمي",
        _ => string.IsNullOrWhiteSpace(rule.DescriptionAr) ? "مقتنى موسمي حصري" : rule.DescriptionAr
    };

    private static View SectionTitle(string text) => TextLabel(text, 20, Color.FromArgb("#F6D06F"), true);
    internal static View EmptyCard(string text) => Card(TextLabel(text, 14, Color.FromArgb("#9D978B"), false), 18);

    internal static Border Card(View content, double padding) => new()
    {
        Content = content,
        Padding = padding,
        BackgroundColor = Color.FromArgb("#0C0E0F"),
        Stroke = Color.FromArgb("#755313"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 16 }
    };

    internal static Label TextLabel(string text, double size, Color color, bool bold) => new()
    {
        Text = text,
        FontFamily = "Tajawal-Regular",
        FontSize = size,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = color,
        HorizontalTextAlignment = TextAlignment.Start,
        LineBreakMode = LineBreakMode.WordWrap
    };
}

public sealed class SeasonArchivePage : ContentPage
{
    private readonly VerticalStackLayout _content = new() { Padding = 14, Spacing = 12 };
    public SeasonArchivePage()
    {
        Title = "أرشيف المواسم";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#050607");
        Content = new ScrollView { Content = _content };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _content.Children.Clear();
        _content.Children.Add(new Label
        {
            Text = "أرشيف المواسم", FontFamily = "Tajawal-Regular", FontSize = 25,
            FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F6D06F")
        });
        var archives = (await SeasonExperienceService.LoadArchivesAsync()).OrderByDescending(item => item.ClosedAtUtc).ToList();
        if (archives.Count == 0)
        {
            _content.Children.Add(SeasonPage.EmptyCard("لا توجد مواسم مكتملة في الأرشيف بعد"));
            return;
        }
        var currentPlayerId = await ApplicationUserService.GetCurrentUserPlayerIdAsync();
        var allRules = await SeasonExperienceService.LoadRulesAsync();
        foreach (var archive in archives)
        {
            var stack = new VerticalStackLayout { Spacing = 6 };
            stack.Children.Add(SeasonPage.TextLabel($"الموسم {archive.SeasonNumber}: {archive.TitleAr}", 17, Color.FromArgb("#F6D06F"), true));
            stack.Children.Add(SeasonPage.TextLabel($"أُغلق في {archive.ClosedAtUtc.ToLocalTime():yyyy/MM/dd}", 12, Color.FromArgb("#AFA99B"), false));
            foreach (var team in archive.TeamStandings.Take(5))
                stack.Children.Add(SeasonPage.TextLabel($"{team.Placement}. {team.TeamName} - {team.SeasonXp:N0} XP", 13, Colors.White, false));
            if (!string.IsNullOrWhiteSpace(currentPlayerId))
            {
                var manualRules = allRules.Where(rule =>
                        rule.IsPublished &&
                        rule.ClaimMode == SeasonClaimMode.ManualClaim &&
                        string.Equals(rule.SeasonId, archive.SeasonId, StringComparison.OrdinalIgnoreCase) &&
                        rule.ConditionType is SeasonConditionType.TopPlacementAtSeasonEnd or
                            SeasonConditionType.ChampionAtSeasonEnd or SeasonConditionType.MvpAtSeasonEnd)
                    .ToList();
                foreach (var rule in manualRules)
                {
                    try
                    {
                        var progress = await SeasonExperienceService.EvaluateProgressAsync(
                            rule,
                            currentPlayerId,
                            allowExpiredWindow: true);
                        if (!progress.IsClaimable)
                            continue;
                        var claim = new Button
                        {
                            Text = $"استلام: {rule.TitleAr}",
                            FontFamily = "Tajawal-Regular",
                            BackgroundColor = Color.FromArgb("#D5A52B"),
                            TextColor = Color.FromArgb("#140F04"),
                            CornerRadius = 10
                        };
                        claim.Clicked += async (_, _) =>
                        {
                            claim.IsEnabled = false;
                            try
                            {
                                await SeasonExperienceService.ClaimAsync(rule.RewardRuleId, progress.TeamId);
                                claim.Text = "تم الاستلام";
                            }
                            catch (Exception ex)
                            {
                                claim.IsEnabled = true;
                                await DisplayAlertAsync("الجائزة", ex.Message, "حسناً");
                            }
                        };
                        stack.Children.Add(claim);
                    }
                    catch
                    {
                        // A malformed historical rule must not hide the archived season.
                    }
                }
            }
            _content.Children.Add(SeasonPage.Card(stack, 14));
        }
    }
}

using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class AvatarsEditorPage : ContentPage
{
    private enum EditorMode
    {
        NewAvatar,
        EditingDraft,
        EditingPublished
    }

    private static readonly Color NewColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");
    private static readonly Color LimitedColor = Color.FromArgb("#F2994A");

    private AvatarRecord? _currentRecord;
    private EditorMode _mode;
    private IReadOnlyList<AvatarRecord> _sheetRecords = Array.Empty<AvatarRecord>();
    private bool _sheetShowsDrafts;
    private bool _assetTargetsThumbnail;

    public AvatarsEditorPage()
    {
        InitializeComponent();

        Configure();
        WirePreviewEvents();
        ApplyTheme();
        ClearFields();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;

        LoadCanonicalInputs();
        await RefreshStatisticsAsync();

        ApplyTheme();
        UpdateLivePreview();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void Configure()
    {
        RarityPicker.SetOptions(CanonicalStoreCatalog.Rarities());
        CurrencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        UnlockTypePicker.SetOptions(CanonicalStoreCatalog.UnlockTypes());
        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());

        UnlockRequirementPicker.SetOptions(CanonicalStoreCatalog.UnlockRequirements());
        TagPicker.SetOptions(CanonicalStoreCatalog.Tags());
        GenderStylePicker.SetOptions(CanonicalStoreCatalog.Styles());

        SeasonIdPicker.SetOptions(CanonicalStoreCatalog.Seasons());
        EventIdPicker.SetOptions(CanonicalStoreCatalog.Events());
        CollectionIdPicker.SetOptions(CanonicalStoreCatalog.Collections());
        AnimationIdPicker.SetOptions(CanonicalStoreCatalog.Animations());
        FrameIdPicker.SetOptions(CanonicalStoreCatalog.Frames());
        GlowEffectPicker.SetOptions(CanonicalStoreCatalog.GlowEffects());
        VersionPicker.SetOptions(CanonicalStoreCatalog.Versions());
    }

    private void LoadCanonicalInputs()
    {
        CategoryPicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        CollectionPicker.SetOptions(CanonicalStoreCatalog.Collections());

        if (_currentRecord != null)
        {
            CategoryPicker.SelectCanonicalId(_currentRecord.CategoryId);
            CollectionPicker.SelectCanonicalId(_currentRecord.Collection);
        }
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        _assetTargetsThumbnail = false;
        await OpenAvatarAssetsAsync();
    }

    private async void OnPickThumbnailClicked(object? sender, EventArgs e)
    {
        _assetTargetsThumbnail = true;
        await OpenAvatarAssetsAsync();
    }

    private async Task OpenAvatarAssetsAsync()
    {
        SheetTitleLabel.Text = "أصول الصور الشخصية";
        FiltersScroll.IsVisible = false;
        ImportAssetButton.IsVisible = true;
        SheetList.Children.Clear();

        var assets =
            await StoreCmsAssetPickerService.ListAssetsAsync(
                StoreCmsAssetSection.Avatars);

        if (assets.Count == 0)
        {
            SheetList.Children.Add(
                new Label
                {
                    Text = "لا توجد أصول صور شخصية بعد",
                    TextColor = GalleryThemeEngine.Current.TextMuted,
                    HorizontalTextAlignment = TextAlignment.Center
                });
        }

        foreach (var path in assets)
            SheetList.Children.Add(CreateAssetRow(path));

        SheetOverlay.IsVisible = true;
    }

    private View CreateAssetRow(string path)
    {
        var select =
            new Button
            {
                Text = "اختيار",
                FontSize = 11
            };

        select.Clicked += (_, _) => SelectAvatarAsset(path);

        var grid =
            new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 72 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

        grid.Add(
            new Image
            {
                Source =
                    InventoryDisplayResolver.ResolveImageSource(path),
                WidthRequest = 72,
                HeightRequest = 72,
                Aspect = Aspect.AspectFill
            },
            0,
            0);

        grid.Add(
            new Label
            {
                Text = System.IO.Path.GetFileName(path),
                TextColor = GalleryThemeEngine.Current.TextSecondary,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalTextAlignment = TextAlignment.Center
            },
            1,
            0);

        grid.Add(select, 2, 0);

        return grid;
    }

    private void SelectAvatarAsset(string path)
    {
        if (_assetTargetsThumbnail)
        {
            ThumbnailPathEntry.Text = path;
            SetPreview(ThumbnailPreview, path);
        }
        else
        {
            ImagePathEntry.Text = path;
            SetPreview(ImagePreview, path);

            if (string.IsNullOrWhiteSpace(ThumbnailPathEntry.Text))
            {
                ThumbnailPathEntry.Text = path;
                SetPreview(ThumbnailPreview, path);
            }
        }

        SheetOverlay.IsVisible = false;
        UpdateLivePreview();
    }

    private async void OnImportAssetClicked(object? sender, EventArgs e)
    {
        var path =
            await StoreCmsAssetPickerService.ImportImageAsync(
                StoreCmsAssetSection.Avatars,
                "استيراد أصل صورة شخصية");

        if (path != null)
            SelectAvatarAsset(path);
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        var saved =
            await AvatarsAdminService.SaveDraftAsync(
                BuildRecord(AvatarStatus.Draft));

        Populate(saved, EditorMode.EditingDraft);

        await RefreshStatisticsAsync();

        await DisplayAlert(
            "المسودة",
            "تم حفظ مسودة الصورة",
            "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (!TryParseNumbers(
                out var price,
                out var priority,
                out var sortOrder))
        {
            return;
        }

        var record =
            BuildRecord(
                AvatarStatus.Published,
                price,
                priority,
                sortOrder);

        if (!AvatarsAdminService.ValidateForPublish(record, out var message))
        {
            ShowError(message);
            return;
        }

        if (_mode == EditorMode.EditingPublished)
            await AvatarsAdminService.UpdatePublishedAsync(record);
        else
            await AvatarsAdminService.PublishAsync(record);

        var confirmation =
            _mode == EditorMode.EditingPublished
                ? "تم حفظ تعديل الصورة"
                : "تم نشر الصورة";

        ClearFields();

        await RefreshStatisticsAsync();

        await DisplayAlert(
            "الصور الشخصية",
            confirmation,
            "حسناً");
    }

    private bool TryParseNumbers(
        out int price,
        out int priority,
        out int sortOrder)
    {
        price = 0;
        priority = 0;
        sortOrder = 0;

        var valid =
            (FreeSwitch.IsToggled || int.TryParse(PriceEntry.Text, out price)) &
            int.TryParse(FeaturedPriorityEntry.Text, out priority) &
            int.TryParse(SortOrderEntry.Text, out sortOrder);

        if (FreeSwitch.IsToggled)
            price = 0;

        if (!valid)
            ShowError("السعر والأولوية والترتيب يجب أن تكون أرقاماً صالحة");

        return valid;
    }

    private async void OnDraftsClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "مسودات الصور";
        _sheetShowsDrafts = true;

        FillSheet(await AvatarsAdminService.LoadAllDraftsAsync());
    }

    private async void OnPublishedClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "الصور المنشورة";
        _sheetShowsDrafts = false;

        FillSheet(await AvatarsAdminService.LoadManagedAsync());
    }

    private void FillSheet(IReadOnlyList<AvatarRecord> records)
    {
        _sheetRecords = records;

        FiltersScroll.IsVisible = true;
        ImportAssetButton.IsVisible = false;

        ApplySheetFilter("All");

        SheetOverlay.IsVisible = true;
    }

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is Button button)
            ApplySheetFilter(button.CommandParameter?.ToString() ?? "All");
    }

    private void ApplySheetFilter(string filter)
    {
        IEnumerable<AvatarRecord> records = _sheetRecords;

        records = filter switch
        {
            "Published" => records.Where(item => item.Status == AvatarStatus.Published),
            "Drafts" => records.Where(item => item.Status == AvatarStatus.Draft),
            "Hidden" => records.Where(item => item.Status == AvatarStatus.Hidden),
            "Featured" => records.Where(item => item.IsFeatured),
            "Free" => records.Where(item => item.IsFree || item.CurrencyType == AvatarCurrencyType.Free),
            "Paid" => records.Where(item => !item.IsFree && item.CurrencyType != AvatarCurrencyType.Free),
            _ => records
        };

        SheetList.Children.Clear();

        var list = records.ToList();

        if (list.Count == 0)
        {
            SheetList.Children.Add(
                new Label
                {
                    Text = "لا توجد صور مطابقة",
                    TextColor = GalleryThemeEngine.Current.TextMuted,
                    HorizontalTextAlignment = TextAlignment.Center
                });
        }
        else
        {
            foreach (var record in list)
                SheetList.Children.Add(CreateRow(record, _sheetShowsDrafts));
        }
    }

    private View CreateRow(AvatarRecord record, bool draft)
    {
        var theme = GalleryThemeEngine.Current;

        var actions =
            new HorizontalStackLayout
            {
                Spacing = 5,
                HorizontalOptions = LayoutOptions.End
            };

        if (draft)
        {
            actions.Children.Add(
                ActionButton(
                    "استئناف التحرير",
                    async () => await ResumeDraftAsync(record.Id)));

            actions.Children.Add(
                ActionButton(
                    "حذف",
                    async () => await DeleteDraftAsync(record.Id)));
        }
        else
        {
            actions.Children.Add(
                ActionButton(
                    "تعديل",
                    async () => await EditPublishedAsync(record.Id)));

            if (record.Status == AvatarStatus.Published)
            {
                actions.Children.Add(
                    ActionButton(
                        "إخفاء",
                        async () => await HideAsync(record.Id)));
            }

            actions.Children.Add(
                ActionButton(
                    "حذف",
                    async () => await DeletePublishedAsync(record.Id)));
        }

        var statusColor =
            record.Status == AvatarStatus.Published
                ? PublishedColor
                : record.Status == AvatarStatus.Hidden
                    ? HiddenColor
                    : record.Id == _currentRecord?.Id
                        ? EditingColor
                        : DraftColor;

        var rarityColor = GetRarityColor(record.Rarity);
        var priceText = FormatPrice(record);

        var details =
            new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    new Label
                    {
                        Text = $"{record.Status} • {record.Rarity}",
                        TextColor = rarityColor,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 11
                    },
                    new Label
                    {
                        Text = DisplayName(record),
                        TextColor = theme.TextPrimary,
                        FontAttributes = FontAttributes.Bold,
                        MaxLines = 1
                    },
                    new Label
                    {
                        Text = draft
                            ? $"{record.Collection} • {record.UpdatedAt.ToLocalTime():yyyy/MM/dd HH:mm}"
                            : $"{record.CategoryId} • {record.Collection} • {priceText} • {record.PublishedAt?.ToLocalTime():yyyy/MM/dd HH:mm}",
                        TextColor = record.IsLimited ? LimitedColor : theme.TextMuted,
                        FontSize = 10,
                        MaxLines = 2
                    },
                    actions
                }
            };

        var grid =
            new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 62 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 9
            };

        grid.Add(
            new Image
            {
                Source = StoreCmsPreviewEngine.ResolveImageSource(
                    string.IsNullOrWhiteSpace(record.ThumbnailPath)
                        ? record.ImagePath
                        : record.ThumbnailPath),
                WidthRequest = 62,
                HeightRequest = 62,
                Aspect = Aspect.AspectFill
            },
            0,
            0);

        grid.Add(details, 1, 0);

        return new Border
        {
            Padding = 10,
            Stroke = statusColor,
            Background = theme.ActionBackground,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = grid
        };
    }

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button =
            new Button
            {
                Text = text,
                FontSize = 10
            };

        button.Clicked += async (_, _) => await action();

        return button;
    }

    private async Task ResumeDraftAsync(string id)
    {
        var record = await AvatarsAdminService.LoadDraftByIdAsync(id);

        if (record == null)
            return;

        Populate(record, EditorMode.EditingDraft);

        SheetOverlay.IsVisible = false;
    }

    private async Task EditPublishedAsync(string id)
    {
        var record =
            (await AvatarsAdminService.LoadManagedAsync())
            .FirstOrDefault(item => item.Id == id);

        if (record == null)
            return;

        Populate(record, EditorMode.EditingPublished);

        SheetOverlay.IsVisible = false;
    }

    private async Task DeleteDraftAsync(string id)
    {
        await AvatarsAdminService.DeleteDraftAsync(id);

        if (_currentRecord?.Id == id)
            ClearFields();

        await RefreshStatisticsAsync();

        OnDraftsClicked(this, EventArgs.Empty);
    }

    private async Task HideAsync(string id)
    {
        await AvatarsAdminService.HidePublishedAsync(id);
        await RefreshStatisticsAsync();

        OnPublishedClicked(this, EventArgs.Empty);
    }

    private async Task DeletePublishedAsync(string id)
    {
        if (!await DisplayAlert(
                "حذف الصورة",
                "هل تريد حذف الصورة المنشورة؟",
                "حذف",
                "إلغاء"))
        {
            return;
        }

        await AvatarsAdminService.DeletePublishedAsync(id);
        await RefreshStatisticsAsync();

        OnPublishedClicked(this, EventArgs.Empty);
    }

    private AvatarRecord BuildRecord(
        AvatarStatus status,
        int? price = null,
        int? priority = null,
        int? sortOrder = null)
    {
        _ = int.TryParse(PriceEntry.Text, out var parsedPrice);
        _ = int.TryParse(FeaturedPriorityEntry.Text, out var parsedPriority);
        _ = int.TryParse(SortOrderEntry.Text, out var parsedSort);

        var currency = ParseEnumPicker(CurrencyPicker, AvatarCurrencyType.Gems);
        var pricing =
            StoreCmsPricingEngine.Normalize(
                price ?? parsedPrice,
                ToCoreCurrency(currency),
                FreeSwitch.IsToggled);

        return new AvatarRecord
        {
            Id = _currentRecord?.Id ?? Guid.NewGuid().ToString(),
            CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = _currentRecord?.PublishedAt,

            NameAr = NameArEntry.Text?.Trim() ?? "",
            NameEn = NameEnEntry.Text?.Trim() ?? "",
            Description = DescriptionEditor.Text?.Trim() ?? "",

            ImagePath = ImagePathEntry.Text?.Trim() ?? "",
            ThumbnailPath = ThumbnailPathEntry.Text?.Trim() ?? "",

            CategoryId = CategoryPicker.SelectedCanonicalId(),
            Collection = CollectionPicker.SelectedCanonicalId(),

            Rarity = ParseEnumPicker(RarityPicker, AvatarRarity.Common),
            CurrencyType = currency,
            Price = pricing.Price,
            IsFree = pricing.IsFree,
            UnlockType = ParseEnumPicker(UnlockTypePicker, AvatarUnlockType.Gems),

            UnlockRequirement = UnlockRequirementPicker.SelectedCanonicalId(),
            Tag = TagPicker.SelectedCanonicalId(),
            GenderOrStyle = GenderStylePicker.SelectedCanonicalId(),

            IsAnimated = AnimatedSwitch.IsToggled,
            IsLimited = LimitedSwitch.IsToggled,
            IsFeatured = FeaturedSwitch.IsToggled,

            FeaturedPriority = priority ?? parsedPriority,
            SortOrder = sortOrder ?? parsedSort,

            SeasonId = SeasonIdPicker.SelectedCanonicalId(),
            EventId = EventIdPicker.SelectedCanonicalId(),
            CollectionId = CollectionIdPicker.SelectedCanonicalId(),
            AnimationId = AnimationIdPicker.SelectedCanonicalId(),
            FrameId = FrameIdPicker.SelectedCanonicalId(),
            GlowEffect = GlowEffectPicker.SelectedCanonicalId(),
            Version = VersionPicker.SelectedCanonicalId(),

            Status = status
        };
    }

    private static T ParseEnumPicker<T>(Picker picker, T fallback)
        where T : struct, Enum
    {
        string value =
            picker.SelectedItem is CanonicalOption option
                ? option.CanonicalId
                : picker.SelectedItem?.ToString() ?? "";

        return Enum.TryParse<T>(value, out var parsed)
            ? parsed
            : fallback;
    }

    private void Populate(AvatarRecord record, EditorMode mode)
    {
        _currentRecord = record;
        _mode = mode;

        NameArEntry.Text = record.NameAr;
        NameEnEntry.Text = record.NameEn;
        DescriptionEditor.Text = record.Description;

        ImagePathEntry.Text = record.ImagePath;
        ThumbnailPathEntry.Text = record.ThumbnailPath;

        CategoryPicker.SelectCanonicalId(record.CategoryId);
        CollectionPicker.SelectCanonicalId(record.Collection);

        RarityPicker.SelectCanonicalId(record.Rarity.ToString());

        var isFree =
            record.IsFree ||
            record.CurrencyType == AvatarCurrencyType.Free;

        CurrencyPicker.SelectCanonicalId(
            record.CurrencyType == AvatarCurrencyType.Coins
                ? AvatarCurrencyType.Coins.ToString()
                : AvatarCurrencyType.Gems.ToString());

        FreeSwitch.IsToggled = isFree;
        PriceEntry.Text = isFree ? "0" : record.Price.ToString();
        PriceEntry.IsEnabled = !isFree;

        UnlockTypePicker.SelectCanonicalId(record.UnlockType.ToString());
        UnlockRequirementPicker.SelectCanonicalId(record.UnlockRequirement);

        TagPicker.SelectCanonicalId(record.Tag);
        GenderStylePicker.SelectCanonicalId(record.GenderOrStyle);

        AnimatedSwitch.IsToggled = record.IsAnimated;
        LimitedSwitch.IsToggled = record.IsLimited;
        FeaturedSwitch.IsToggled = record.IsFeatured;

        FeaturedPriorityEntry.Text = record.FeaturedPriority.ToString();
        SortOrderEntry.Text = record.SortOrder.ToString();

        SeasonIdPicker.SelectCanonicalId(record.SeasonId);
        EventIdPicker.SelectCanonicalId(record.EventId);
        CollectionIdPicker.SelectCanonicalId(record.CollectionId);
        AnimationIdPicker.SelectCanonicalId(record.AnimationId);
        FrameIdPicker.SelectCanonicalId(record.FrameId);
        GlowEffectPicker.SelectCanonicalId(record.GlowEffect);
        VersionPicker.SelectCanonicalId(record.Version);

        StatusPicker.SelectCanonicalId(record.Status.ToString());

        SetPreview(ImagePreview, record.ImagePath);
        SetPreview(ThumbnailPreview, record.ThumbnailPath);

        var published = mode == EditorMode.EditingPublished;

        PublishButton.Text = published ? "حفظ التعديل" : "نشر";
        SaveDraftButton.IsEnabled = !published;

        SetMode(
            published ? "Editing Published" : "Editing Draft",
            published ? "تعديل الصورة المنشورة" : "استكمال المسودة",
            published ? PublishedColor : EditingColor);

        UpdateLivePreview();
    }

    private void ClearFields()
    {
        _currentRecord = null;
        _mode = EditorMode.NewAvatar;

        foreach (var entry in AllEntries())
            entry.Text = "";

        DescriptionEditor.Text = "";

        PriceEntry.Text = "0";
        FeaturedPriorityEntry.Text = "0";
        SortOrderEntry.Text = "0";

        RarityPicker.SelectCanonicalId(AvatarRarity.Common.ToString());
        CurrencyPicker.SelectCanonicalId(AvatarCurrencyType.Gems.ToString());
        UnlockTypePicker.SelectCanonicalId(AvatarUnlockType.Gems.ToString());
        StatusPicker.SelectCanonicalId(AvatarStatus.Draft.ToString());

        CategoryPicker.SelectedIndex = -1;
        CollectionPicker.SelectedIndex = -1;

        UnlockRequirementPicker.SelectCanonicalId("None");
        TagPicker.SelectCanonicalId("None");
        GenderStylePicker.SelectCanonicalId("Normal");

        SeasonIdPicker.SelectCanonicalId("None");
        EventIdPicker.SelectCanonicalId("None");
        CollectionIdPicker.SelectCanonicalId("Default");
        AnimationIdPicker.SelectCanonicalId("None");
        FrameIdPicker.SelectCanonicalId("None");
        GlowEffectPicker.SelectCanonicalId("#D4AF37");
        VersionPicker.SelectCanonicalId("v1");

        AnimatedSwitch.IsToggled = false;
        LimitedSwitch.IsToggled = false;
        FeaturedSwitch.IsToggled = false;
        FreeSwitch.IsToggled = false;

        PriceEntry.IsEnabled = true;

        ImagePreview.IsVisible = false;
        ThumbnailPreview.IsVisible = false;
        ValidationLabel.IsVisible = false;

        PublishButton.Text = "نشر";
        SaveDraftButton.IsEnabled = true;

        SetMode("New Avatar", "جاهز لإضافة صورة جديدة", NewColor);

        UpdateLivePreview();
    }

    private Entry[] AllEntries() =>
        new[]
        {
            NameArEntry,
            NameEnEntry,
            ImagePathEntry,
            ThumbnailPathEntry,
            PriceEntry,
            FeaturedPriorityEntry,
            SortOrderEntry
        };

    private Picker[] AllPickers() =>
        new[]
        {
            CategoryPicker,
            CollectionPicker,
            RarityPicker,
            CurrencyPicker,
            UnlockTypePicker,
            UnlockRequirementPicker,
            TagPicker,
            GenderStylePicker,
            SeasonIdPicker,
            EventIdPicker,
            CollectionIdPicker,
            AnimationIdPicker,
            FrameIdPicker,
            GlowEffectPicker,
            VersionPicker,
            StatusPicker
        };

    private void ShowError(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;

        SetMode("Error", "Missing Required", ErrorColor);
    }

    private void SetMode(string title, string subtitle, Color color)
    {
        ModeTitleLabel.Text = title;
        ModeSubtitleLabel.Text = subtitle;
        ModeDot.Color = color;
        ModeTitleLabel.TextColor = color;
    }

    private static string DisplayName(AvatarRecord record) =>
        !string.IsNullOrWhiteSpace(record.NameAr)
            ? record.NameAr
            : !string.IsNullOrWhiteSpace(record.NameEn)
                ? record.NameEn
                : "Avatar بدون اسم";

    private static Color GetRarityColor(AvatarRarity rarity) =>
        rarity switch
        {
            AvatarRarity.Common => Color.FromArgb("#8A8F98"),
            AvatarRarity.Rare => Color.FromArgb("#2F80ED"),
            AvatarRarity.Epic => Color.FromArgb("#9B51E0"),
            AvatarRarity.Legendary => Color.FromArgb("#D9A441"),
            AvatarRarity.Mythic => Color.FromArgb("#E34B78"),
            AvatarRarity.Immortal => Color.FromArgb("#FFF2C2"),
            _ => Color.FromArgb("#FFF2C2")
        };

    private static StoreCmsCurrency ToCoreCurrency(AvatarCurrencyType currency) =>
        currency == AvatarCurrencyType.Coins
            ? StoreCmsCurrency.Coins
            : StoreCmsCurrency.Gems;

    private static string FormatPrice(AvatarRecord record) =>
        StoreCmsPricingEngine.Format(
            record.Price,
            ToCoreCurrency(record.CurrencyType),
            record.IsFree || record.CurrencyType == AvatarCurrencyType.Free);

    private async Task RefreshStatisticsAsync()
    {
        var stats = await AvatarsAdminService.GetStatisticsAsync();

        PublishedCountLabel.Text = stats.Published.ToString();
        DraftCountLabel.Text = stats.Draft.ToString();
        HiddenCountLabel.Text = stats.Hidden.ToString();
        TotalCountLabel.Text = stats.Total.ToString();
    }

    private void WirePreviewEvents()
    {
        foreach (var entry in AllEntries())
            entry.TextChanged += (_, _) => UpdateLivePreview();

        DescriptionEditor.TextChanged += (_, _) => UpdateLivePreview();

        foreach (var picker in AllPickers())
            picker.SelectedIndexChanged += (_, _) => UpdateLivePreview();

        foreach (var toggle in new[] { AnimatedSwitch, LimitedSwitch, FeaturedSwitch })
            toggle.Toggled += (_, _) => UpdateLivePreview();

        FreeSwitch.Toggled += (_, args) =>
        {
            PriceEntry.IsEnabled = !args.Value;

            if (args.Value)
                PriceEntry.Text = "0";

            UpdateLivePreview();
        };
    }

    private void UpdateLivePreview()
    {
        var rarity = ParseEnumPicker(RarityPicker, AvatarRarity.Common);
        var currency = ParseEnumPicker(CurrencyPicker, AvatarCurrencyType.Gems);

        _ = int.TryParse(PriceEntry.Text, out var price);

        CardPreviewImage.Source =
            StoreCmsPreviewEngine.ResolveImageSource(
                string.IsNullOrWhiteSpace(ThumbnailPathEntry.Text)
                    ? ImagePathEntry.Text
                    : ThumbnailPathEntry.Text);

        PreviewNameLabel.Text =
            !string.IsNullOrWhiteSpace(NameArEntry.Text)
                ? NameArEntry.Text
                : !string.IsNullOrWhiteSpace(NameEnEntry.Text)
                    ? NameEnEntry.Text
                    : "Avatar بدون اسم";

        PreviewRarityLabel.Text =
            RarityPicker.SelectedItem is CanonicalOption rarityOption
                ? rarityOption.DisplayName
                : rarity.ToString();

        PreviewRarityLabel.TextColor = GetRarityColor(rarity);

        PreviewBadgeLabel.Text =
            LimitedSwitch.IsToggled
                ? "محدود"
                : AnimatedSwitch.IsToggled
                    ? "متحرك"
                    : TagPicker.SelectedItem is CanonicalOption tagOption
                        ? tagOption.DisplayName
                        : "";

        PreviewPriceLabel.Text =
            StoreCmsPricingEngine.Format(
                price,
                ToCoreCurrency(currency),
                FreeSwitch.IsToggled);

        PreviewFeaturedRibbon.IsVisible = FeaturedSwitch.IsToggled;

        PreviewVisibilityLabel.Text =
            ParseEnumPicker(StatusPicker, AvatarStatus.Draft) == AvatarStatus.Hidden
                ? "مخفي"
                : "ظاهر";

        var glow = GlowEffectPicker.SelectedCanonicalId();

        try
        {
            PreviewCard.Stroke = Color.FromArgb(glow);
        }
        catch
        {
            PreviewCard.Stroke = GalleryThemeEngine.Current.Stroke;
        }
    }

    private void OnPresetColorClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is string color)
        {
            GlowEffectPicker.SelectCanonicalId(color);
            UpdateLivePreview();
        }
    }

    private static void SetPreview(Image image, string? path)
    {
        image.Source = StoreCmsPreviewEngine.ResolveImageSource(path);
        image.IsVisible = image.Source != null;
    }

    private void OnCloseSheetClicked(object? sender, EventArgs e) =>
        SheetOverlay.IsVisible = false;

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
        else
            await Shell.Current.GoToAsync("..");
    }

    private void ApplyTheme()
    {
        var t = GalleryThemeEngine.Current;

        TitleLabel.TextColor = t.TextPrimary;
        SubtitleLabel.TextColor = t.TextSecondary;

        ModeCard.Background = t.ActionBackground;
        FormPanel.Background = t.ActionBackground;
        StatisticsCard.Background = t.ActionBackground;
        PreviewCard.Background = t.ActionBackground;

        ModeCard.Stroke = t.Stroke;
        FormPanel.Stroke = t.Stroke;
        StatisticsCard.Stroke = t.Stroke;
        SheetPanel.Stroke = t.Stroke;

        SheetPanel.Background = t.CardBackground;

        ModeSubtitleLabel.TextColor = t.TextMuted;
        SheetTitleLabel.TextColor = t.TextPrimary;

        AnimatedLabel.TextColor = t.TextSecondary;
        LimitedLabel.TextColor = t.TextSecondary;
        FeaturedLabel.TextColor = t.TextSecondary;
        FreeLabel.TextColor = t.TextSecondary;

        PublishedCountLabel.TextColor = PublishedColor;
        DraftCountLabel.TextColor = DraftColor;
        HiddenCountLabel.TextColor = HiddenColor;
        TotalCountLabel.TextColor = t.Gold;

        PreviewNameLabel.TextColor = t.TextPrimary;
        PreviewPriceLabel.TextColor = t.Gold;
        PreviewBadgeLabel.TextColor = t.TextMuted;
        PreviewVisibilityLabel.TextColor = t.TextMuted;

        foreach (var entry in AllEntries())
        {
            entry.TextColor = t.TextPrimary;
            entry.PlaceholderColor = t.TextMuted;
        }

        DescriptionEditor.TextColor = t.TextPrimary;
        DescriptionEditor.PlaceholderColor = t.TextMuted;

        foreach (var picker in AllPickers())
        {
            picker.TextColor = t.TextPrimary;
            picker.TitleColor = t.TextMuted;
        }

        UpdateLivePreview();
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme) =>
        ApplyTheme();
}

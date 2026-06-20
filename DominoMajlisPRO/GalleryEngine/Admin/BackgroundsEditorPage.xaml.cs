using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class BackgroundsEditorPage : ContentPage
{
    private enum EditorMode { NewBackground, EditingDraft, EditingPublished }

    private static readonly Color NewColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");
    private static readonly Color LimitedColor = Color.FromArgb("#F2994A");

    private BackgroundRecord? _currentRecord;
    private EditorMode _mode;

    public BackgroundsEditorPage()
    {
        InitializeComponent();
        Configure();
        ApplyTheme();
        ClearFields();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void Configure()
    {
        CategoryPicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        CollectionPicker.SetOptions(CanonicalStoreCatalog.Collections());
        RarityPicker.SetOptions(CanonicalStoreCatalog.Rarities());
        CurrencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        UnlockTypePicker.SetOptions(CanonicalStoreCatalog.UnlockTypes());
        UnlockRequirementPicker.SetOptions(CanonicalStoreCatalog.UnlockRequirements());
        TagPicker.SetOptions(CanonicalStoreCatalog.Tags());
        SeasonIdPicker.SetOptions(CanonicalStoreCatalog.Seasons());
        EventIdPicker.SetOptions(CanonicalStoreCatalog.Events());
        CollectionIdPicker.SetOptions(CanonicalStoreCatalog.Collections());
        VersionPicker.SetOptions(CanonicalStoreCatalog.Versions());
        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        var path = await StoreCmsAssetPickerService.ImportImageAsync(StoreCmsAssetSection.Backgrounds, "اختيار صورة الخلفية");
        if (path == null) return;
        ImagePathEntry.Text = path;
        if (string.IsNullOrWhiteSpace(ThumbnailPathEntry.Text))
            ThumbnailPathEntry.Text = path;
        SetPreview(ImagePreview, path);
        SetPreview(ThumbnailPreview, ThumbnailPathEntry.Text);
    }

    private async void OnPickThumbnailClicked(object? sender, EventArgs e)
    {
        var path = await StoreCmsAssetPickerService.ImportImageAsync(StoreCmsAssetSection.Backgrounds, "اختيار الصورة المصغرة");
        if (path == null) return;
        ThumbnailPathEntry.Text = path;
        SetPreview(ThumbnailPreview, path);
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        var saved = await BackgroundsAdminService.SaveDraftAsync(BuildRecord(BackgroundStatus.Draft));
        Populate(saved, EditorMode.EditingDraft);
        await DisplayAlert("المسودة", "تم حفظ مسودة الخلفية", "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (!TryParseNumbers(out var price, out var priority, out var sortOrder)) return;
        var record = BuildRecord(BackgroundStatus.Published, price, priority, sortOrder);
        if (!BackgroundsAdminService.ValidateForPublish(record, out var message))
        {
            ShowError(message);
            return;
        }

        if (_mode == EditorMode.EditingPublished)
            await BackgroundsAdminService.UpdatePublishedAsync(record);
        else
            await BackgroundsAdminService.PublishAsync(record);

        var confirmation = _mode == EditorMode.EditingPublished ? "تم حفظ تعديل الخلفية" : "تم نشر الخلفية";
        ClearFields();
        await DisplayAlert("الخلفيات", confirmation, "حسناً");
    }

    private bool TryParseNumbers(out int price, out int priority, out int sortOrder)
    {
        price = priority = sortOrder = 0;
        var valid = (FreeSwitch.IsToggled || int.TryParse(PriceEntry.Text, out price))
            & int.TryParse(FeaturedPriorityEntry.Text, out priority)
            & int.TryParse(SortOrderEntry.Text, out sortOrder);
        if (FreeSwitch.IsToggled) price = 0;
        if (!valid) ShowError("السعر والأولوية والترتيب يجب أن تكون أرقاماً صالحة");
        return valid;
    }

    private async void OnDraftsClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "مسودات الخلفيات";
        FillSheet(await BackgroundsAdminService.LoadAllDraftsAsync(), true);
    }

    private async void OnPublishedClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "الخلفيات المنشورة";
        FillSheet(await BackgroundsAdminService.LoadManagedAsync(), false);
    }

    private void FillSheet(IReadOnlyList<BackgroundRecord> records, bool draft)
    {
        SheetList.Children.Clear();
        if (records.Count == 0)
            SheetList.Children.Add(new Label { Text = "لا توجد خلفيات", TextColor = GalleryThemeEngine.Current.TextMuted, HorizontalTextAlignment = TextAlignment.Center });
        else
            foreach (var record in records)
                SheetList.Children.Add(CreateRow(record, draft));
        SheetOverlay.IsVisible = true;
    }

    private View CreateRow(BackgroundRecord record, bool draft)
    {
        var theme = GalleryThemeEngine.Current;
        var actions = new HorizontalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.End };
        if (draft)
        {
            actions.Children.Add(ActionButton("استئناف التحرير", async () => await ResumeDraftAsync(record.Id)));
            actions.Children.Add(ActionButton("حذف", async () => await DeleteDraftAsync(record.Id)));
        }
        else
        {
            actions.Children.Add(ActionButton("تعديل", async () => await EditPublishedAsync(record.Id)));
            if (record.Status == BackgroundStatus.Published)
                actions.Children.Add(ActionButton("إخفاء", async () => await HideAsync(record.Id)));
            actions.Children.Add(ActionButton("حذف", async () => await DeletePublishedAsync(record.Id)));
        }

        var statusColor = record.Status == BackgroundStatus.Published ? PublishedColor
            : record.Status == BackgroundStatus.Hidden ? HiddenColor
            : record.Id == _currentRecord?.Id ? EditingColor : DraftColor;
        var rarityColor = GetRarityColor(record.Rarity);
        var priceText = StoreCmsPricingEngine.Format(record.Price, ToCoreCurrency(record.CurrencyType), record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free);
        var details = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = $"{record.Status} • {record.Rarity}", TextColor = rarityColor, FontAttributes = FontAttributes.Bold, FontSize = 11 },
                new Label { Text = DisplayName(record), TextColor = theme.TextPrimary, FontAttributes = FontAttributes.Bold, MaxLines = 1 },
                new Label { Text = draft ? $"{record.Collection} • {record.UpdatedAt.ToLocalTime():yyyy/MM/dd HH:mm}" : $"{record.Collection} • {priceText} • {record.PublishedAt?.ToLocalTime():yyyy/MM/dd HH:mm}", TextColor = record.IsLimited ? LimitedColor : theme.TextMuted, FontSize = 10, MaxLines = 2 },
                actions
            }
        };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 76 }, new ColumnDefinition { Width = GridLength.Star } }, ColumnSpacing = 9 };
        grid.Add(new Image { Source = StoreCmsPreviewEngine.ResolveImageSource(CardImage(record)), WidthRequest = 76, HeightRequest = 58, Aspect = Aspect.AspectFill }, 0, 0);
        grid.Add(details, 1, 0);
        return new Border { Padding = 10, Stroke = statusColor, Background = theme.ActionBackground, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Content = grid };
    }

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, FontSize = 10 };
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private async Task ResumeDraftAsync(string id)
    {
        var record = await BackgroundsAdminService.LoadDraftByIdAsync(id);
        if (record == null) return;
        Populate(record, EditorMode.EditingDraft);
        SheetOverlay.IsVisible = false;
    }

    private async Task EditPublishedAsync(string id)
    {
        var record = (await BackgroundsAdminService.LoadManagedAsync()).FirstOrDefault(item => item.Id == id);
        if (record == null) return;
        Populate(record, EditorMode.EditingPublished);
        SheetOverlay.IsVisible = false;
    }

    private async Task DeleteDraftAsync(string id)
    {
        await BackgroundsAdminService.DeleteDraftAsync(id);
        if (_currentRecord?.Id == id) ClearFields();
        OnDraftsClicked(this, EventArgs.Empty);
    }

    private async Task HideAsync(string id)
    {
        await BackgroundsAdminService.HidePublishedAsync(id);
        OnPublishedClicked(this, EventArgs.Empty);
    }

    private async Task DeletePublishedAsync(string id)
    {
        if (!await DisplayAlert("حذف الخلفية", "هل تريد حذف الخلفية المنشورة؟", "حذف", "إلغاء")) return;
        await BackgroundsAdminService.DeletePublishedAsync(id);
        OnPublishedClicked(this, EventArgs.Empty);
    }

    private BackgroundRecord BuildRecord(BackgroundStatus status, int? price = null, int? priority = null, int? sortOrder = null)
    {
        _ = int.TryParse(PriceEntry.Text, out var parsedPrice);
        _ = int.TryParse(FeaturedPriorityEntry.Text, out var parsedPriority);
        _ = int.TryParse(SortOrderEntry.Text, out var parsedSort);
        var currency = ParseEnum(CurrencyPicker, BackgroundCurrencyType.Gems);
        var pricing = StoreCmsPricingEngine.Normalize(price ?? parsedPrice, ToCoreCurrency(currency), FreeSwitch.IsToggled);
        return new BackgroundRecord
        {
            Id = _currentRecord?.Id ?? Guid.NewGuid().ToString(),
            CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = _currentRecord?.PublishedAt,
            NameAr = NameArEntry.Text?.Trim() ?? string.Empty,
            NameEn = NameEnEntry.Text?.Trim() ?? string.Empty,
            Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
            ImagePath = ImagePathEntry.Text?.Trim() ?? string.Empty,
            ThumbnailPath = ThumbnailPathEntry.Text?.Trim() ?? string.Empty,
            CategoryId = CategoryPicker.SelectedCanonicalId(),
            Collection = CollectionPicker.SelectedCanonicalId(),
            Rarity = ParseEnum(RarityPicker, BackgroundRarity.Common),
            CurrencyType = currency,
            Price = pricing.Price,
            IsFree = pricing.IsFree,
            UnlockType = ParseEnum(UnlockTypePicker, BackgroundUnlockType.Gems),
            UnlockRequirement = UnlockRequirementPicker.SelectedCanonicalId(),
            Tag = TagPicker.SelectedCanonicalId(),
            IsAnimated = AnimatedSwitch.IsToggled,
            IsLimited = LimitedSwitch.IsToggled,
            IsFeatured = FeaturedSwitch.IsToggled,
            FeaturedPriority = priority ?? parsedPriority,
            SortOrder = sortOrder ?? parsedSort,
            SeasonId = SeasonIdPicker.SelectedCanonicalId(),
            EventId = EventIdPicker.SelectedCanonicalId(),
            CollectionId = CollectionIdPicker.SelectedCanonicalId(),
            Version = VersionPicker.SelectedCanonicalId(),
            Status = status
        };
    }

    private void Populate(BackgroundRecord record, EditorMode mode)
    {
        _currentRecord = record;
        _mode = mode;
        NameArEntry.Text = record.NameAr;
        NameEnEntry.Text = record.NameEn;
        DescriptionEditor.Text = record.Description;
        ImagePathEntry.Text = record.ImagePath;
        ThumbnailPathEntry.Text = record.ThumbnailPath;
        CollectionPicker.SelectCanonicalId(record.Collection);
        RarityPicker.SelectCanonicalId(record.Rarity.ToString());
        var isFree = record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free;
        CurrencyPicker.SelectCanonicalId(record.CurrencyType == BackgroundCurrencyType.Coins ? "Coins" : "Gems");
        FreeSwitch.IsToggled = isFree;
        PriceEntry.Text = isFree ? "0" : record.Price.ToString();
        PriceEntry.IsEnabled = !isFree;
        UnlockTypePicker.SelectCanonicalId(record.UnlockType.ToString());
        UnlockRequirementPicker.SelectCanonicalId(record.UnlockRequirement);
        TagPicker.SelectCanonicalId(record.Tag);
        AnimatedSwitch.IsToggled = record.IsAnimated;
        LimitedSwitch.IsToggled = record.IsLimited;
        FeaturedSwitch.IsToggled = record.IsFeatured;
        FeaturedPriorityEntry.Text = record.FeaturedPriority.ToString();
        SortOrderEntry.Text = record.SortOrder.ToString();
        SeasonIdPicker.SelectCanonicalId(record.SeasonId);
        EventIdPicker.SelectCanonicalId(record.EventId);
        CollectionIdPicker.SelectCanonicalId(record.CollectionId);
        VersionPicker.SelectCanonicalId(record.Version);
        StatusPicker.SelectCanonicalId(record.Status.ToString());
        CategoryPicker.SelectCanonicalId(record.CategoryId == "Ass" ? "Avatar" : record.CategoryId);
        SetPreview(ImagePreview, record.ImagePath);
        SetPreview(ThumbnailPreview, record.ThumbnailPath);
        var published = mode == EditorMode.EditingPublished;
        PublishButton.Text = published ? "حفظ التعديل" : "نشر";
        SaveDraftButton.IsEnabled = !published;
        SetMode(published ? "Editing Published" : "Editing Draft", published ? "تعديل الخلفية المنشورة" : "استكمال المسودة", published ? PublishedColor : EditingColor);
    }

    private void ClearFields()
    {
        _currentRecord = null;
        _mode = EditorMode.NewBackground;
        foreach (var entry in AllEntries()) entry.Text = string.Empty;
        PriceEntry.Text = FeaturedPriorityEntry.Text = SortOrderEntry.Text = "0";
        CategoryPicker.SelectCanonicalId("ProfileBackground");
        CollectionPicker.SelectCanonicalId("Default");
        RarityPicker.SelectCanonicalId("Common");
        CurrencyPicker.SelectCanonicalId("Gems");
        UnlockTypePicker.SelectCanonicalId("Gems");
        UnlockRequirementPicker.SelectCanonicalId("None");
        TagPicker.SelectCanonicalId("None");
        SeasonIdPicker.SelectCanonicalId("None");
        EventIdPicker.SelectCanonicalId("None");
        CollectionIdPicker.SelectCanonicalId("Default");
        VersionPicker.SelectCanonicalId("v1");
        StatusPicker.SelectCanonicalId("Draft");
        AnimatedSwitch.IsToggled = LimitedSwitch.IsToggled = FeaturedSwitch.IsToggled = FreeSwitch.IsToggled = false;
        PriceEntry.IsEnabled = true;
        ImagePreview.IsVisible = ThumbnailPreview.IsVisible = ValidationLabel.IsVisible = false;
        PublishButton.Text = "نشر";
        SaveDraftButton.IsEnabled = true;
        SetMode("New Background", "جاهز لإضافة خلفية جديدة", NewColor);
    }

    private Entry[] AllEntries() => new[] { NameArEntry, NameEnEntry, ImagePathEntry, ThumbnailPathEntry, PriceEntry, FeaturedPriorityEntry, SortOrderEntry };
    private static T ParseEnum<T>(Picker picker, T fallback) where T : struct, Enum => Enum.TryParse<T>(picker.SelectedCanonicalId(), out var value) ? value : fallback;
    private static StoreCmsCurrency ToCoreCurrency(BackgroundCurrencyType currency) => currency == BackgroundCurrencyType.Coins ? StoreCmsCurrency.Coins : StoreCmsCurrency.Gems;
    private static string CardImage(BackgroundRecord record) => string.IsNullOrWhiteSpace(record.ThumbnailPath) ? record.ImagePath : record.ThumbnailPath;
    private static string DisplayName(BackgroundRecord record) => !string.IsNullOrWhiteSpace(record.NameAr) ? record.NameAr : !string.IsNullOrWhiteSpace(record.NameEn) ? record.NameEn : "خلفية بدون اسم";
    private static Color GetRarityColor(BackgroundRarity rarity) => rarity switch { BackgroundRarity.Common => Color.FromArgb("#8A8F98"), BackgroundRarity.Rare => Color.FromArgb("#2F80ED"), BackgroundRarity.Epic => Color.FromArgb("#9B51E0"), BackgroundRarity.Legendary => Color.FromArgb("#D9A441"), BackgroundRarity.Mythic => Color.FromArgb("#E34B78"), _ => Color.FromArgb("#FFF2C2") };
    private static void SetPreview(Image image, string? path) { image.Source = StoreCmsPreviewEngine.ResolveImageSource(path); image.IsVisible = image.Source != null; }
    private void OnFreeToggled(object? sender, ToggledEventArgs e) { PriceEntry.IsEnabled = !e.Value; if (e.Value) PriceEntry.Text = "0"; }
    private void ShowError(string message) { ValidationLabel.Text = message; ValidationLabel.TextColor = ErrorColor; ValidationLabel.IsVisible = true; SetMode("Error", "Missing Required", ErrorColor); }
    private void SetMode(string title, string subtitle, Color color) { ModeTitleLabel.Text = title; ModeSubtitleLabel.Text = subtitle; ModeDot.Color = color; ModeTitleLabel.TextColor = color; }
    private void OnCloseSheetClicked(object? sender, EventArgs e) => SheetOverlay.IsVisible = false;
    private async void OnCancelClicked(object? sender, EventArgs e) { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); else await Shell.Current.GoToAsync(".."); }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        TitleLabel.TextColor = theme.TextPrimary;
        SubtitleLabel.TextColor = theme.TextSecondary;
        ModeCard.Background = FormPanel.Background = theme.ActionBackground;
        ModeCard.Stroke = FormPanel.Stroke = SheetPanel.Stroke = theme.Stroke;
        SheetPanel.Background = theme.CardBackground;
        ModeSubtitleLabel.TextColor = theme.TextMuted;
        SheetTitleLabel.TextColor = theme.TextPrimary;
        AnimatedLabel.TextColor = LimitedLabel.TextColor = FeaturedLabel.TextColor = FreeLabel.TextColor = theme.TextSecondary;
        foreach (var entry in AllEntries()) { entry.TextColor = theme.TextPrimary; entry.PlaceholderColor = theme.TextMuted; }
        DescriptionEditor.TextColor = theme.TextPrimary;
        DescriptionEditor.PlaceholderColor = theme.TextMuted;
        foreach (var picker in new[] { CategoryPicker, CollectionPicker, RarityPicker, CurrencyPicker, UnlockTypePicker, UnlockRequirementPicker, TagPicker, SeasonIdPicker, EventIdPicker, CollectionIdPicker, VersionPicker, StatusPicker }) { picker.TextColor = theme.TextPrimary; picker.TitleColor = theme.TextMuted; }
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme) => ApplyTheme();
}

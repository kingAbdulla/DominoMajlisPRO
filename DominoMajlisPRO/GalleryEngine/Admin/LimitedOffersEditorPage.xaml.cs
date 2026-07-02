using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class LimitedOffersEditorPage : ContentPage
{
    private enum EditorMode { NewOffer, EditingDraft, EditingPublished }
    private static readonly Color NewColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ExpiredColor = Color.FromArgb("#4F535A");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");
    private static readonly Color EndingSoonColor = Color.FromArgb("#F2994A");
    private LimitedOfferRecord? _currentRecord;
    private EditorMode _editorMode;
    private readonly List<DominoMajlisPRO.GalleryEngine.Models.CatalogAssetDisplay> _assetChoices = new();

    public LimitedOffersEditorPage()
    {
        InitializeComponent(); FlowDirection = FlowDirection.RightToLeft; ConfigureFields(); ApplyTheme(); ClearFields();
    }

    protected override async void OnAppearing() { base.OnAppearing(); GalleryThemeEngine.ThemeChanged -= OnThemeChanged; GalleryThemeEngine.ThemeChanged += OnThemeChanged; ApplyTheme(); await LoadAssetChoicesAsync(SelectedAssetId()); }
    protected override void OnDisappearing() { GalleryThemeEngine.ThemeChanged -= OnThemeChanged; base.OnDisappearing(); }

    private void ConfigureFields()
    {
        CurrencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        AssetTypePicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        OwnerScopePicker.SetOptions(CanonicalStoreCatalog.OwnerScopes());
        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());
        StartsDatePicker.Date = DateTime.Today; StartsTimePicker.Time = DateTime.Now.TimeOfDay;
        EndsDatePicker.Date = DateTime.Today.AddDays(7); EndsTimePicker.Time = DateTime.Now.TimeOfDay;
    }

    private void PopulateFields(LimitedOfferRecord record, EditorMode mode)
    {
        _currentRecord = record; _editorMode = mode; TitleEntry.Text = record.Title; SubtitleEntry.Text = record.Subtitle; DescriptionEditor.Text = record.Description;
        ButtonTextEntry.Text = record.ButtonText; ImagePathEntry.Text = record.ImagePath;
        AssetTypePicker.SelectCanonicalId(StoreProductAssetTypeCatalog.TryResolve(record.StoreTypeId, out _) ? record.StoreTypeId : null);
        _ = LoadAssetChoicesAsync(record.AssetId);
        OwnerScopePicker.SelectCanonicalId(record.OwnerScope); ColorHexEntry.Text = record.ColorHex;
        OriginalPriceEntry.Text = record.OriginalPrice.ToString(); DiscountPriceEntry.Text = record.DiscountPrice.ToString();
        CurrencyPicker.SelectCanonicalId(record.CurrencyType.ToString()); StartsDatePicker.Date = record.StartsAt.Date; StartsTimePicker.Time = record.StartsAt.TimeOfDay;
        EndsDatePicker.Date = record.EndsAt.Date; EndsTimePicker.Time = record.EndsAt.TimeOfDay; FeaturedSwitch.IsToggled = record.IsFeatured;
        SortOrderEntry.Text = record.SortOrder.ToString(); StatusPicker.SelectCanonicalId(record.Status.ToString()); ApplyPreviewImage(record.ImagePath);
        var editingPublished = mode == EditorMode.EditingPublished;
        PublishButton.Text = editingPublished ? "حفظ التعديل" : "نشر";
        SaveDraftButton.IsEnabled = !editingPublished;
        SetMode(editingPublished ? "Editing Published" : "Editing Draft", editingPublished ? "تعديل العرض المنشور" : "استكمال تعديل مسودة عرض", editingPublished ? PublishedColor : EditingColor); UpdateOfferSummary();
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "اختيار صورة العرض", FileTypes = FilePickerFileType.Images });
        if (result == null) return; ImagePathEntry.Text = result.FullPath ?? result.FileName; ApplyPreviewImage(ImagePathEntry.Text);
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        var saved = await LimitedOffersAdminService.SaveDraftAsync(BuildRecord(LimitedOfferStatus.Draft)); PopulateFields(saved, EditorMode.EditingDraft);
        ValidationLabel.IsVisible = false; await DisplayAlert("حفظ كمسودة", "تم حفظ مسودة العرض بنجاح", "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (!TryParseRequiredNumbers(out var original, out var discount, out var sortOrder)) return;
        var record = BuildRecord(LimitedOfferStatus.Published, original, discount, sortOrder);
        if (!LimitedOffersAdminService.ValidateForPublish(record, out var message)) { ShowError(message); return; }
        if (_editorMode == EditorMode.EditingPublished)
            await LimitedOffersAdminService.UpdatePublishedAsync(record);
        else
            await LimitedOffersAdminService.PublishAsync(record);
        var messageText = _editorMode == EditorMode.EditingPublished ? "تم حفظ تعديل العرض بنجاح" : "تم نشر العرض بنجاح";
        ClearFields(); await DisplayAlert("نشر", messageText, "حسناً");
    }

    private async void OnOpenDraftsClicked(object? sender, EventArgs e) => await OpenDraftsAsync();
    private async void OnOpenPublishedClicked(object? sender, EventArgs e) => await OpenManagedAsync();

    private async void OnAuditMalformedClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "تدقيق المنتجات غير الصالحة";
        SheetList.Children.Clear();
        var records = (await LimitedOffersAdminService.LoadAllDraftsAsync())
            .Concat(await LimitedOffersAdminService.LoadManagedOffersAsync())
            .Where(LimitedOffersAdminService.IsMalformed)
            .ToList();
        if (records.Count == 0)
            SheetList.Children.Add(EmptyLabel("لا توجد منتجات غير صالحة"));
        else
            foreach (var item in records)
                SheetList.Children.Add(CreateMalformedRow(item));
        SheetOverlay.IsVisible = true;
    }

    private View CreateMalformedRow(LimitedOfferRecord item)
    {
        var edit = new Button { Text = "إعادة تعيين نوع الأصل", FontSize = 11 };
        edit.Clicked += async (_, _) =>
        {
            if (item.Status == LimitedOfferStatus.Draft)
                await ResumeAsync(LimitedOffersAdminService.GetAssetId(item));
            else
            {
                var draft = await LimitedOffersAdminService.CreateDraftFromPublishedAsync(LimitedOffersAdminService.GetAssetId(item));
                if (draft != null)
                    PopulateFields(draft, EditorMode.EditingDraft);
                SheetOverlay.IsVisible = false;
            }
        };
        var container = new VerticalStackLayout { Spacing = 5 };
        container.Children.Add(new Label
        {
            Text = $"ProductId: {item.ProductId}\nAssetId: {item.AssetId}",
            TextColor = ErrorColor,
            FontSize = 11
        });
        container.Children.Add(edit);
        return new Border
        {
            Padding = 10,
            Stroke = ErrorColor,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = container
        };
    }

    private async Task OpenDraftsAsync()
    {
        SheetTitleLabel.Text = "مسودات العروض"; SheetList.Children.Clear(); var drafts = await LimitedOffersAdminService.LoadAllDraftsAsync();
        if (drafts.Count == 0) SheetList.Children.Add(EmptyLabel("لا توجد مسودات عروض"));
        else foreach (var item in drafts) SheetList.Children.Add(CreateRow(item, true));
        SheetOverlay.IsVisible = true;
    }

    private async Task OpenManagedAsync()
    {
        SheetTitleLabel.Text = "العروض المنشورة"; SheetList.Children.Clear(); var items = await LimitedOffersAdminService.LoadManagedOffersAsync();
        if (items.Count == 0) SheetList.Children.Add(EmptyLabel("لا توجد عروض منشورة"));
        else foreach (var item in items) SheetList.Children.Add(CreateRow(item, false));
        SheetOverlay.IsVisible = true;
    }

    private Label EmptyLabel(string text) => new() { Text = text, TextColor = GalleryThemeEngine.Current.TextMuted, FontSize = 14, HorizontalTextAlignment = TextAlignment.Center };

    private View CreateRow(LimitedOfferRecord item, bool draft)
    {
        var theme = GalleryThemeEngine.Current; var buttons = new List<Button>();
        if (draft)
        {
            var resume = new Button { Text = "استئناف التحرير", FontSize = 11 }; resume.Clicked += async (_, _) => await ResumeAsync(LimitedOffersAdminService.GetAssetId(item));
            var deleteDraft = new Button { Text = "حذف", FontSize = 11 }; deleteDraft.Clicked += async (_, _) => await DeleteDraftAsync(LimitedOffersAdminService.GetAssetId(item)); buttons.Add(resume); buttons.Add(deleteDraft);
        }
        else
        {
            var edit = new Button { Text = "تعديل", FontSize = 10 }; edit.Clicked += async (_, _) => await EditPublishedAsync(LimitedOffersAdminService.GetAssetId(item));
            var hide = new Button { Text = "إخفاء", FontSize = 10 }; hide.Clicked += async (_, _) => await HideAsync(LimitedOffersAdminService.GetAssetId(item));
            var expire = new Button { Text = "إنهاء", FontSize = 10 }; expire.Clicked += async (_, _) => await ExpireAsync(LimitedOffersAdminService.GetAssetId(item));
            var delete = new Button { Text = "حذف", FontSize = 10 }; delete.Clicked += async (_, _) => await DeletePublishedAsync(LimitedOffersAdminService.GetAssetId(item)); buttons.Add(edit); buttons.Add(hide); buttons.Add(expire); buttons.Add(delete);
        }

        var statusColor = GetStatusColor(item); var actions = new Grid { ColumnSpacing = 5 };
        for (int i = 0; i < buttons.Count; i++) { actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); actions.Add(buttons[i], i, 0); }
        var status = new Label { Text = item.Status.ToString(), TextColor = statusColor, FontAttributes = FontAttributes.Bold, FontSize = 11, HorizontalTextAlignment = TextAlignment.End };
        var title = new Label { Text = string.IsNullOrWhiteSpace(item.Title) ? "عرض بدون عنوان" : item.Title, TextColor = theme.TextPrimary, FontAttributes = FontAttributes.Bold, FontSize = 15, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, HorizontalTextAlignment = TextAlignment.End };
        var meta = new Label { Text = draft ? item.UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : $"{item.OriginalPrice} → {item.DiscountPrice} ({item.DiscountPercent}%) {item.CurrencyType}\n{item.StartsAt:yyyy/MM/dd HH:mm} - {item.EndsAt:yyyy/MM/dd HH:mm}", TextColor = theme.TextMuted, FontSize = 10, MaxLines = 3, HorizontalTextAlignment = TextAlignment.End };
        var stack = new VerticalStackLayout { Spacing = 4, Children = { status, title, meta, actions } };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 62 }, new ColumnDefinition { Width = GridLength.Star } }, ColumnSpacing = 10 };
        grid.Add(new Border { WidthRequest = 62, HeightRequest = 62, Stroke = theme.Stroke, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Content = new Image { Source = ResolveImage(item.ImagePath), Aspect = Aspect.AspectFill } }, 0, 0); grid.Add(stack, 1, 0);
        return new Border { Padding = 10, Stroke = theme.Stroke, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Background = theme.ActionBackground, Content = grid };
    }

    private Color GetStatusColor(LimitedOfferRecord item)
    {
        if (item.Status == LimitedOfferStatus.Published && item.EndsAt <= DateTime.Now.AddHours(24)) return EndingSoonColor;
        return item.Status switch { LimitedOfferStatus.Draft => _currentRecord != null && LimitedOffersAdminService.GetAssetId(item) == LimitedOffersAdminService.GetAssetId(_currentRecord) ? EditingColor : DraftColor, LimitedOfferStatus.Published => PublishedColor, LimitedOfferStatus.Hidden => HiddenColor, _ => ExpiredColor };
    }

    private async Task ResumeAsync(string assetId) { var item = await LimitedOffersAdminService.LoadDraftByIdAsync(assetId); if (item != null) { PopulateFields(item, EditorMode.EditingDraft); SheetOverlay.IsVisible = false; } }
    private async Task DeleteDraftAsync(string assetId) { await LimitedOffersAdminService.DeleteDraftAsync(assetId); if (_currentRecord != null && LimitedOffersAdminService.GetAssetId(_currentRecord) == assetId) ClearFields(); await OpenDraftsAsync(); }
    private async Task EditPublishedAsync(string assetId) { var item = (await LimitedOffersAdminService.LoadPublishedAsync()).FirstOrDefault(record => LimitedOffersAdminService.GetAssetId(record) == assetId); if (item != null) { PopulateFields(item, EditorMode.EditingPublished); SheetOverlay.IsVisible = false; } }
    private async Task HideAsync(string assetId) { await LimitedOffersAdminService.HidePublishedAsync(assetId); await OpenManagedAsync(); }
    private async Task ExpireAsync(string assetId) { await LimitedOffersAdminService.ExpireOfferAsync(assetId); await OpenManagedAsync(); }
    private async Task DeletePublishedAsync(string assetId) { if (await DisplayAlert("حذف النشر", "هل تريد حذف العرض؟", "حذف", "إلغاء")) { await LimitedOffersAdminService.DeletePublishedAsync(assetId); await OpenManagedAsync(); } }
    private void OnCloseSheetClicked(object? sender, EventArgs e) => SheetOverlay.IsVisible = false;
    private void OnCloseSheetTapped(object? sender, TappedEventArgs e) => SheetOverlay.IsVisible = false;
    private void OnCancelTapped(object? sender, EventArgs e) => _ = CloseAsync();
    private async Task CloseAsync() { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); else await Shell.Current.GoToAsync(".."); }

    private bool TryParseRequiredNumbers(out int original, out int discount, out int sortOrder)
    {
        original = 0;
        discount = 0;
        var currency = GetSelectedCurrency();
        var originalValid = currency == LimitedOfferCurrencyType.Free || int.TryParse(OriginalPriceEntry.Text, out original);
        var discountValid = currency == LimitedOfferCurrencyType.Free || int.TryParse(DiscountPriceEntry.Text, out discount);
        if (currency == LimitedOfferCurrencyType.Free) original = discount = 0;
        var sortOrderValid = int.TryParse(SortOrderEntry.Text, out sortOrder);
        var valid = originalValid && discountValid && sortOrderValid;
        if (!valid) ShowError("الأسعار وترتيب العرض يجب أن تكون أرقاماً صالحة"); return valid;
    }

    private LimitedOfferRecord BuildRecord(LimitedOfferStatus status, int? original = null, int? discount = null, int? sort = null)
    {
        var currency = GetSelectedCurrency();
        _ = int.TryParse(OriginalPriceEntry.Text, out var originalValue); _ = int.TryParse(DiscountPriceEntry.Text, out var discountValue); _ = int.TryParse(SortOrderEntry.Text, out var sortValue);
        var finalOriginal = currency == LimitedOfferCurrencyType.Free ? 0 : original ?? originalValue;
        var finalDiscount = currency == LimitedOfferCurrencyType.Free ? 0 : discount ?? discountValue;
        var discountPercent = CalculateDiscountPercent(finalOriginal, finalDiscount, currency);
        var startDate = StartsDatePicker.Date ?? DateTime.Today; var endDate = EndsDatePicker.Date ?? DateTime.Today.AddDays(7);
        var productId = !string.IsNullOrWhiteSpace(_currentRecord?.ProductId) ? _currentRecord.ProductId : !string.IsNullOrWhiteSpace(_currentRecord?.Id) ? _currentRecord.Id : Guid.NewGuid().ToString();
        var storeTypeId = AssetTypePicker.SelectedCanonicalId();
        var ownerScope = StoreProductAssetTypeCatalog.TryResolve(storeTypeId, out var assetType) ? StoreProductAssetTypeCatalog.GetOwnerScope(assetType).ToString() : string.Empty;
        return new LimitedOfferRecord { Id = productId, ProductId = productId, AssetId = SelectedAssetId(), StoreTypeId = storeTypeId, OwnerScope = ownerScope, ColorHex = ColorHexEntry.Text?.Trim() ?? "", CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow, PublishedAt = _currentRecord?.PublishedAt,
            Title = TitleEntry.Text?.Trim() ?? "", Subtitle = SubtitleEntry.Text?.Trim() ?? "", Description = DescriptionEditor.Text?.Trim() ?? "", ButtonText = ButtonTextEntry.Text?.Trim() ?? "", ImagePath = ImagePathEntry.Text?.Trim() ?? "", Category = _currentRecord?.Category ?? "",
            OriginalPrice = finalOriginal, DiscountPrice = finalDiscount, DiscountPercent = discountPercent, CurrencyType = currency, IsFree = currency == LimitedOfferCurrencyType.Free || finalDiscount == 0,
            StartsAt = startDate.Date + (StartsTimePicker.Time ?? TimeSpan.Zero), EndsAt = endDate.Date + (EndsTimePicker.Time ?? TimeSpan.Zero), IsFeatured = FeaturedSwitch.IsToggled, SortOrder = sort ?? sortValue, Status = status };
    }

    private void ShowError(string message) { ValidationLabel.Text = message; ValidationLabel.TextColor = ErrorColor; ValidationLabel.IsVisible = true; SetMode("Error", "Missing Required", ErrorColor); }
    private void ClearFields() { _currentRecord = null; _editorMode = EditorMode.NewOffer; PublishButton.Text = "نشر"; SaveDraftButton.IsEnabled = true; TitleEntry.Text = SubtitleEntry.Text = DescriptionEditor.Text = ButtonTextEntry.Text = ImagePathEntry.Text = ColorHexEntry.Text = ""; AssetIdPicker.SelectedIndex = -1; AssetTypePicker.SelectedIndex = -1; OwnerScopePicker.SelectedIndex = -1; OriginalPriceEntry.Text = DiscountPriceEntry.Text = SortOrderEntry.Text = "0"; CurrencyPicker.SelectCanonicalId("Gems"); StartsDatePicker.Date = DateTime.Today; StartsTimePicker.Time = DateTime.Now.TimeOfDay; EndsDatePicker.Date = DateTime.Today.AddDays(7); EndsTimePicker.Time = DateTime.Now.TimeOfDay; FeaturedSwitch.IsToggled = false; StatusPicker.SelectCanonicalId("Draft"); PreviewImage.Source = null; PreviewImage.IsVisible = false; ValidationLabel.IsVisible = false; SetMode("New Offer", "جاهز لإضافة عرض جديد", NewColor); UpdateOfferSummary(); }
    private LimitedOfferCurrencyType GetSelectedCurrency() => Enum.TryParse<LimitedOfferCurrencyType>(CurrencyPicker.SelectedCanonicalId(), out var currency) ? currency : LimitedOfferCurrencyType.Gems;
    private static int CalculateDiscountPercent(int original, int discount, LimitedOfferCurrencyType currency) => currency == LimitedOfferCurrencyType.Free ? 100 : original > 0 ? (int)Math.Round((original - discount) * 100d / original, MidpointRounding.AwayFromZero) : 0;
    private void OnOfferSummaryChanged(object? sender, EventArgs e) => UpdateOfferSummary();
    private void OnFeaturedToggled(object? sender, ToggledEventArgs e) => UpdateOfferSummary();
    private void UpdateOfferSummary()
    {
        var currency = GetSelectedCurrency();
        _ = int.TryParse(OriginalPriceEntry.Text, out var original);
        _ = int.TryParse(DiscountPriceEntry.Text, out var discount);
        if (currency == LimitedOfferCurrencyType.Free) original = discount = 0;
        var percent = CalculateDiscountPercent(original, discount, currency);
        var saving = Math.Max(0, original - discount);
        OfferSummaryLabel.Text = currency == LimitedOfferCurrencyType.Free
            ? "السعر الأصلي: Free | سعر الخصم: Free | الخصم: 100% | التوفير: Free | العملة: Free"
            : $"السعر الأصلي: {original} | سعر الخصم: {discount} | الخصم: {percent}% | التوفير: {saving} | العملة: {currency}";
        FeaturedSummaryLabel.Text = $"مميز: {(FeaturedSwitch.IsToggled ? "نعم" : "لا")}";
    }
    private void ApplyPreviewImage(string? path) { var source = ResolveImage(path); PreviewImage.Source = source; PreviewImage.IsVisible = source != null; }
    private static ImageSource? ResolveImage(string? path) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(path);
    private void SetMode(string title, string subtitle, Color color) { ModeTitleLabel.Text = title; ModeSubtitleLabel.Text = subtitle; ModeDot.Color = color; ModeTitleLabel.TextColor = color; }

    private void ApplyTheme()
    {
        var t = GalleryThemeEngine.Current; BackButtonFrame.Background = t.CardBackground; BackButtonFrame.Stroke = t.Stroke; BackButtonLabel.TextColor = t.Gold; TitleLabel.TextColor = t.TextPrimary; SubtitleLabel.TextColor = t.TextSecondary; ModeCard.Background = t.ActionBackground; ModeCard.Stroke = t.Stroke; ModeSubtitleLabel.TextColor = t.TextMuted; FormPanel.Background = t.ActionBackground; FormPanel.Stroke = t.Stroke; OfferSummaryCard.Background = t.CardBackground; OfferSummaryCard.Stroke = t.Stroke; OfferSummaryTitleLabel.TextColor = t.Gold; OfferSummaryLabel.TextColor = FeaturedSummaryLabel.TextColor = t.TextSecondary; StartsLabel.TextColor = EndsLabel.TextColor = FeaturedLabel.TextColor = t.TextSecondary; SheetPanel.Background = t.CardBackground; SheetPanel.Stroke = t.Stroke; SheetTitleLabel.TextColor = t.TextPrimary; ApplyInputTheme();
    }
    private void ApplyInputTheme()
    {
        var t = GalleryThemeEngine.Current; foreach (var entry in new[] { TitleEntry, SubtitleEntry, ButtonTextEntry, ImagePathEntry, ColorHexEntry, OriginalPriceEntry, DiscountPriceEntry, SortOrderEntry }) { entry.TextColor = t.TextPrimary; entry.PlaceholderColor = t.TextMuted; } DescriptionEditor.TextColor = t.TextPrimary; DescriptionEditor.PlaceholderColor = t.TextMuted; CurrencyPicker.TextColor = StatusPicker.TextColor = AssetTypePicker.TextColor = AssetIdPicker.TextColor = OwnerScopePicker.TextColor = StartsDatePicker.TextColor = EndsDatePicker.TextColor = StartsTimePicker.TextColor = EndsTimePicker.TextColor = t.TextPrimary; CurrencyPicker.TitleColor = StatusPicker.TitleColor = AssetTypePicker.TitleColor = AssetIdPicker.TitleColor = OwnerScopePicker.TitleColor = t.TextMuted; ValidationLabel.TextColor = ErrorColor;
    }
    private void OnThemeChanged(object? sender, GalleryTheme theme) => ApplyTheme();
    private async void OnAssetTypeChanged(object? sender, EventArgs e)
    {
        var selected = AssetTypePicker.SelectedCanonicalId();
        OwnerScopePicker.SelectCanonicalId(StoreProductAssetTypeCatalog.TryResolve(selected, out var type) ? StoreProductAssetTypeCatalog.GetOwnerScope(type).ToString() : string.Empty);
        await LoadAssetChoicesAsync();
    }

    private async Task LoadAssetChoicesAsync(string? selectedAssetId = null)
    {
        selectedAssetId ??= SelectedAssetId();
        _assetChoices.Clear();
        if (StoreProductAssetTypeCatalog.TryResolve(
                AssetTypePicker.SelectedCanonicalId(),
                out var selectedType))
        {
            _assetChoices.AddRange((await StoreAssetCatalogService.LoadAsync())
                .Where(asset => asset.AssetType == selectedType));
        }

        AssetIdPicker.ItemsSource = _assetChoices
            .Select(asset => $"{asset.DisplayName} • {asset.AssetId}")
            .ToList();
        AssetIdPicker.SelectedIndex = _assetChoices.FindIndex(asset =>
            CanonicalAssetIdentityService.SameAssetId(asset.AssetId, selectedAssetId));
    }

    private string SelectedAssetId() =>
        AssetIdPicker.SelectedIndex >= 0 &&
        AssetIdPicker.SelectedIndex < _assetChoices.Count
            ? _assetChoices[AssetIdPicker.SelectedIndex].AssetId
            : string.Empty;
}

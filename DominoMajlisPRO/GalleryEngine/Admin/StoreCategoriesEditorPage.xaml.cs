using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class StoreCategoriesEditorPage : ContentPage
{
    private enum EditorMode { NewCategory, EditingDraft, EditingPublished }
    private static readonly Color NewColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");
    private StoreCategoryRecord? _currentRecord;
    private EditorMode _mode;

    public StoreCategoriesEditorPage()
    {
        InitializeComponent();
        CategoryPicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        CollectionPicker.SetOptions(CanonicalStoreCatalog.Collections());
        SeasonPicker.SetOptions(CanonicalStoreCatalog.Seasons());
        AccentColorPicker.SetOptions(CanonicalStoreCatalog.GlowEffects());
        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());
        ApplyTheme();
        ClearFields();
    }

    protected override void OnAppearing()
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

    private async void OnPickIconClicked(object? sender, EventArgs e)
    {
        var file = await PickImageAsync("اختيار أيقونة التصنيف");
        if (file == null) return;
        IconPathEntry.Text = file;
        SetPreview(IconPreview, file);
    }

    private async void OnPickBannerClicked(object? sender, EventArgs e)
    {
        var file = await PickImageAsync("اختيار غلاف التصنيف");
        if (file == null) return;
        BannerPathEntry.Text = file;
        SetPreview(BannerPreview, file);
    }

    private static async Task<string?> PickImageAsync(string title)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title, FileTypes = FilePickerFileType.Images });
        return result?.FullPath ?? result?.FileName;
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        var saved = await StoreCategoriesAdminService.SaveDraftAsync(BuildRecord(StoreCategoryStatus.Draft));
        Populate(saved, EditorMode.EditingDraft);
        await DisplayAlert("المسودة", "تم حفظ مسودة التصنيف", "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(DisplayOrderEntry.Text, out var order)) { ShowError("ترتيب العرض يجب أن يكون رقماً صالحاً"); return; }
        var record = BuildRecord(StoreCategoryStatus.Published, order);
        if (!StoreCategoriesAdminService.ValidateForPublish(record, out var message)) { ShowError(message); return; }
        if (_mode == EditorMode.EditingPublished) await StoreCategoriesAdminService.UpdatePublishedAsync(record);
        else await StoreCategoriesAdminService.PublishAsync(record);
        ClearFields();
        await DisplayAlert("التصنيفات", "تم حفظ التصنيف المنشور", "حسناً");
    }

    private async void OnDraftsClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "مسودات التصنيفات";
        FillSheet(await StoreCategoriesAdminService.LoadAllDraftsAsync(), true);
    }

    private async void OnPublishedClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "التصنيفات المنشورة";
        FillSheet(await StoreCategoriesAdminService.LoadManagedAsync(), false);
    }

    private void FillSheet(IReadOnlyList<StoreCategoryRecord> records, bool draft)
    {
        SheetList.Children.Clear();
        if (records.Count == 0)
            SheetList.Children.Add(new Label { Text = "لا توجد تصنيفات", TextColor = GalleryThemeEngine.Current.TextMuted, HorizontalTextAlignment = TextAlignment.Center });
        else
            foreach (var record in records) SheetList.Children.Add(CreateRow(record, draft));
        SheetOverlay.IsVisible = true;
    }

    private View CreateRow(StoreCategoryRecord record, bool draft)
    {
        var theme = GalleryThemeEngine.Current;
        var actions = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.End };
        if (draft)
        {
            actions.Children.Add(ActionButton("استئناف التحرير", async () => await ResumeDraftAsync(record.Id)));
            actions.Children.Add(ActionButton("حذف", async () => await DeleteDraftAsync(record.Id)));
        }
        else
        {
            actions.Children.Add(ActionButton("تعديل", async () => await EditPublishedAsync(record.Id)));
            if (record.Status == StoreCategoryStatus.Published) actions.Children.Add(ActionButton("إخفاء", async () => await HideAsync(record.Id)));
            actions.Children.Add(ActionButton("حذف", async () => await DeletePublishedAsync(record.Id)));
        }
        var statusColor = record.Status switch { StoreCategoryStatus.Published => PublishedColor, StoreCategoryStatus.Hidden => HiddenColor, _ => DraftColor };
        var details = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = record.Status.ToString(), TextColor = statusColor, FontSize = 11, FontAttributes = FontAttributes.Bold },
                new Label { Text = DisplayName(record), TextColor = theme.TextPrimary, FontAttributes = FontAttributes.Bold, MaxLines = 1 },
                new Label { Text =
$"{CanonicalStoreCatalog.GetCategoryDisplayName(record.Category)} • " +
$"{CanonicalStoreCatalog.GetCollectionDisplayName(record.Collection)} • " +
$"{CanonicalStoreCatalog.GetSeasonDisplayName(record.SeasonId)}", TextColor = theme.TextMuted, FontSize = 10 },
                actions
            }
        };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 58 }, new ColumnDefinition { Width = GridLength.Star } }, ColumnSpacing = 10 };
        grid.Add(new Image { Source = ResolveImage(record.IconPath), Aspect = Aspect.AspectFill, HeightRequest = 58, WidthRequest = 58 }, 0, 0);
        grid.Add(details, 1, 0);
        return new Border { Padding = 10, Background = theme.ActionBackground, Stroke = theme.Stroke, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Content = grid };
    }

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, FontSize = 10 };
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private async Task ResumeDraftAsync(string id)
    {
        var record = await StoreCategoriesAdminService.LoadDraftByIdAsync(id);
        if (record == null) return;
        Populate(record, EditorMode.EditingDraft);
        SheetOverlay.IsVisible = false;
    }

    private async Task EditPublishedAsync(string id)
    {
        var record = (await StoreCategoriesAdminService.LoadPublishedAsync()).FirstOrDefault(item => item.Id == id);
        if (record == null) return;
        Populate(record, EditorMode.EditingPublished);
        SheetOverlay.IsVisible = false;
    }

    private async Task DeleteDraftAsync(string id) { await StoreCategoriesAdminService.DeleteDraftAsync(id); OnDraftsClicked(this, EventArgs.Empty); }
    private async Task HideAsync(string id) { await StoreCategoriesAdminService.HidePublishedAsync(id); OnPublishedClicked(this, EventArgs.Empty); }
    private async Task DeletePublishedAsync(string id)
    {
        if (!await DisplayAlert("حذف التصنيف", "هل تريد حذف التصنيف المنشور؟", "حذف", "إلغاء")) return;
        await StoreCategoriesAdminService.DeletePublishedAsync(id);
        OnPublishedClicked(this, EventArgs.Empty);
    }

    private StoreCategoryRecord BuildRecord(StoreCategoryStatus status, int? order = null)
    {
        _ = int.TryParse(DisplayOrderEntry.Text, out var parsedOrder);
        return new StoreCategoryRecord
        {
            Id = _currentRecord?.Id ?? Guid.NewGuid().ToString(), CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = _currentRecord?.PublishedAt, NameAr = NameArEntry.Text?.Trim() ?? string.Empty,
            NameEn = NameEnEntry.Text?.Trim() ?? string.Empty, Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
            IconPath = IconPathEntry.Text?.Trim() ?? string.Empty, BannerPath = BannerPathEntry.Text?.Trim() ?? string.Empty,
            AccentColor = AccentColorPicker.SelectedCanonicalId(),
            Category = CategoryPicker.SelectedCanonicalId(),
            Collection = CollectionPicker.SelectedCanonicalId(),
            SeasonId = SeasonPicker.SelectedCanonicalId(),
            DisplayOrder = order ?? parsedOrder, IsVisible = VisibleSwitch.IsToggled, IsFeatured = FeaturedSwitch.IsToggled,
            ItemCount = _currentRecord?.ItemCount ?? 0, Status = status
        };
    }

    private void Populate(StoreCategoryRecord record, EditorMode mode)
    {
        _currentRecord = record; _mode = mode;
        NameArEntry.Text = record.NameAr; NameEnEntry.Text = record.NameEn; DescriptionEditor.Text = record.Description;
        IconPathEntry.Text = record.IconPath; BannerPathEntry.Text = record.BannerPath; AccentColorPicker.SelectCanonicalId(record.AccentColor);
        CategoryPicker.SelectCanonicalId(record.Category); CollectionPicker.SelectCanonicalId(record.Collection); SeasonPicker.SelectCanonicalId(record.SeasonId);
        DisplayOrderEntry.Text = record.DisplayOrder.ToString(); VisibleSwitch.IsToggled = record.IsVisible; FeaturedSwitch.IsToggled = record.IsFeatured;
        StatusPicker.SelectCanonicalId(record.Status.ToString()); SetPreview(IconPreview, record.IconPath); SetPreview(BannerPreview, record.BannerPath);
        var published = mode == EditorMode.EditingPublished; PublishButton.Text = published ? "حفظ التعديل" : "نشر"; SaveDraftButton.IsEnabled = !published;
        SetMode(published ? "Editing Published" : "Editing Draft", published ? "تعديل التصنيف المنشور" : "استكمال المسودة", published ? PublishedColor : EditingColor);
    }

    private void ClearFields()
    {
        _currentRecord = null; _mode = EditorMode.NewCategory;
        NameArEntry.Text = NameEnEntry.Text = DescriptionEditor.Text = IconPathEntry.Text = BannerPathEntry.Text = string.Empty;
        CategoryPicker.SelectCanonicalId("Avatar");
        CollectionPicker.SelectCanonicalId("Default");
        SeasonPicker.SelectCanonicalId("None");
        AccentColorPicker.SelectCanonicalId("#D4AF37"); DisplayOrderEntry.Text = "0";
        VisibleSwitch.IsToggled = true; FeaturedSwitch.IsToggled = false; StatusPicker.SelectCanonicalId(StoreCategoryStatus.Draft.ToString());
        IconPreview.IsVisible = BannerPreview.IsVisible = ValidationLabel.IsVisible = false; PublishButton.Text = "نشر"; SaveDraftButton.IsEnabled = true;
        SetMode("New Category", "جاهز لإضافة تصنيف جديد", NewColor);
    }

    private void ShowError(string message) { ValidationLabel.Text = message; ValidationLabel.IsVisible = true; SetMode("Error", "Missing Required", ErrorColor); }
    private void SetMode(string title, string subtitle, Color color) { ModeTitleLabel.Text = title; ModeSubtitleLabel.Text = subtitle; ModeDot.Color = color; ModeTitleLabel.TextColor = color; }
    private static string DisplayName(StoreCategoryRecord record) => !string.IsNullOrWhiteSpace(record.NameAr) ? record.NameAr : record.NameEn;
    private static ImageSource? ResolveImage(string? path) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(path);
    private static void SetPreview(Image image, string? path) { image.Source = ResolveImage(path); image.IsVisible = image.Source != null; }
    private void OnCloseSheetClicked(object? sender, EventArgs e) => SheetOverlay.IsVisible = false;
    private async void OnCancelClicked(object? sender, EventArgs e) { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); else await Shell.Current.GoToAsync(".."); }
    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        TitleLabel.TextColor = theme.TextPrimary; SubtitleLabel.TextColor = theme.TextSecondary;
        ModeCard.Background = FormPanel.Background = theme.ActionBackground; ModeCard.Stroke = FormPanel.Stroke = SheetPanel.Stroke = theme.Stroke;
        SheetPanel.Background = theme.CardBackground; ModeSubtitleLabel.TextColor = theme.TextMuted; SheetTitleLabel.TextColor = theme.TextPrimary;
        VisibleLabel.TextColor = FeaturedLabel.TextColor = theme.TextSecondary;
        foreach (var picker in new[] { CategoryPicker, CollectionPicker, SeasonPicker, AccentColorPicker, StatusPicker })
        {
            picker.TextColor = theme.TextPrimary;
            picker.TitleColor = theme.TextMuted;
        }
        foreach (var entry in new[] { NameArEntry, NameEnEntry, IconPathEntry, BannerPathEntry, DisplayOrderEntry })
        {
            entry.TextColor = theme.TextPrimary;
            entry.PlaceholderColor = theme.TextMuted;
        }
        DescriptionEditor.TextColor = theme.TextPrimary; DescriptionEditor.PlaceholderColor = theme.TextMuted; 
    }
    private void OnThemeChanged(object? sender, GalleryTheme theme) => ApplyTheme();
}

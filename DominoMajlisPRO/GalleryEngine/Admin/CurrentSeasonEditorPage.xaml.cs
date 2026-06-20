using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class CurrentSeasonEditorPage : ContentPage
{
    private static readonly Color NewSeasonColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingDraftColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");

    private readonly StoreTextLimitRule _textLimits;
    private readonly StoreImageRule _imageRule;
    private CurrentSeasonRecord? _currentRecord;
    private CurrentSeasonRecord? _publishedRecord;
    private bool _editingManagedRecord;

    public CurrentSeasonEditorPage()
    {
        InitializeComponent();
        FlowDirection = FlowDirection.RightToLeft;

        _textLimits = StoreTextLimitRule.ForTemplate(StoreCardTemplateType.SeasonHero);
        _imageRule = StoreImageRule.ForTemplate(StoreCardTemplateType.SeasonHero);

        ConfigureFields();
        ApplyTheme();
        ClearFieldsForNewEntry();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        await RefreshPublishedCardAsync();
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void ConfigureFields()
    {
        SeasonNameEntry.MaxLength = _textLimits.TitleMaxLength ?? 32;
        SeasonTitleEntry.MaxLength = _textLimits.SubtitleMaxLength ?? 44;
        DescriptionEditor.MaxLength = _textLimits.DescriptionMaxLength ?? 120;
        ButtonTextEntry.MaxLength = _textLimits.ButtonTextMaxLength ?? 18;
        ApplyInputTheme();

        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());
        StatusPicker.SelectCanonicalId(StoreContentStatus.Draft.ToString());
    }

    private async Task RefreshPublishedCardAsync()
    {
        _publishedRecord = await CurrentSeasonAdminService.LoadPublishedAsync();
        PublishedCard.IsVisible = _publishedRecord != null;
        if (_publishedRecord == null)
            return;

        PublishedTitleLabel.Text = string.IsNullOrWhiteSpace(_publishedRecord.Title)
            ? "مسودة بدون عنوان"
            : _publishedRecord.Title;
        PublishedDateLabel.Text = _publishedRecord.PublishedAt.HasValue
            ? $"نشر: {_publishedRecord.PublishedAt.Value.ToLocalTime():yyyy/MM/dd HH:mm}"
            : "Published";
        PublishedImage.Source = ResolveImageSource(_publishedRecord.ImagePath);
    }

    private void PopulateFields(CurrentSeasonRecord record, bool editingDraft, bool editingManaged = false)
    {
        _currentRecord = record;
        _editingManagedRecord = editingManaged;

        SeasonNameEntry.Text = record.Title;
        SeasonTitleEntry.Text = record.Subtitle;
        DescriptionEditor.Text = record.Description;
        ButtonTextEntry.Text = record.ButtonText;
        ImagePathEntry.Text = record.ImagePath;
        StatusPicker.SelectCanonicalId(record.Status.ToString());

        ApplyPreviewImage(record.ImagePath);
        UpdateCountersAndPreview();
        PublishButton.IsVisible = !editingManaged;
        SaveChangesButton.IsVisible = editingManaged;
        SetEditorMode(
            editingManaged ? "Editing Published" : editingDraft ? "Editing Draft" : "Draft",
            editingManaged ? "تعديل موسم منشور بنفس الهوية" : editingDraft ? "استكمال تعديل مسودة محفوظة" : "مسودة",
            editingManaged ? PublishedColor : editingDraft ? EditingDraftColor : DraftColor);
    }

    private void OnTextFieldChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateCountersAndPreview();
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "اختيار صورة الموسم",
            FileTypes = FilePickerFileType.Images
        });

        if (result == null)
            return;

        ImagePathEntry.Text = result.FullPath ?? result.FileName;
        ApplyPreviewImage(ImagePathEntry.Text);
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        if (!ValidateTextLimits())
            return;

        var saved = await CurrentSeasonAdminService.SaveDraftAsync(BuildRecordFromFields(StoreContentStatus.Draft));
        PopulateFields(saved, true);
        ValidationLabel.IsVisible = false;
        await DisplayAlert("حفظ كمسودة", "تم حفظ مسودة الموسم الحالي بنجاح", "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (!ValidateTextLimits())
            return;

        var record = BuildRecordFromFields(StoreContentStatus.Published);
        if (!CurrentSeasonAdminService.ValidateForPublish(record, out var message))
        {
            ShowValidation(message);
            SetEditorMode("Error", "Missing Required", ErrorColor);
            return;
        }

        await CurrentSeasonAdminService.PublishAsync(record);
        ClearFieldsForNewEntry();
        await RefreshPublishedCardAsync();
        ValidationLabel.IsVisible = false;
        await DisplayAlert("نشر", "تم نشر بيانات الموسم الحالي بنجاح", "حسناً");
    }

    private async void OnSaveChangesClicked(object? sender, EventArgs e)
    {
        if (!_editingManagedRecord || _currentRecord == null || !ValidateTextLimits())
            return;

        var record = BuildRecordFromFields(_currentRecord.Status);
        if (!CurrentSeasonAdminService.ValidateForPublish(record, out var message))
        {
            ShowValidation(message);
            SetEditorMode("Error", "Missing Required", ErrorColor);
            return;
        }

        await CurrentSeasonAdminService.UpdateManagedAsync(record);
        ClearFieldsForNewEntry();
        await RefreshPublishedCardAsync();
        ValidationLabel.IsVisible = false;
        await DisplayAlert("حفظ التعديل", "تم تحديث الموسم بنفس الهوية بنجاح", "حسناً");
    }

    private async void OnOpenDraftsClicked(object? sender, EventArgs e)
    {
        await OpenDraftsSheetAsync();
    }

    private async Task OpenDraftsSheetAsync()
    {
        DraftsList.Children.Clear();
        var drafts = await CurrentSeasonAdminService.LoadAllDraftsAsync();

        if (drafts.Count == 0)
        {
            DraftsList.Children.Add(new Label
            {
                Text = "لا توجد مسودات محفوظة حالياً",
                FontFamily = "Tajawal-Regular",
                FontSize = 14,
                TextColor = GalleryThemeEngine.Current.TextMuted,
                HorizontalTextAlignment = TextAlignment.Center
            });
        }
        else
        {
            foreach (var draft in drafts)
                DraftsList.Children.Add(CreateDraftRow(draft));
        }

        DraftsSheetOverlay.IsVisible = true;
    }

    private View CreateDraftRow(CurrentSeasonRecord draft)
    {
        var theme = GalleryThemeEngine.Current;
        var image = new Image
        {
            Source = ResolveImageSource(draft.ImagePath),
            Aspect = Aspect.AspectFill,
            WidthRequest = 62,
            HeightRequest = 62
        };

        var imageFrame = new Border
        {
            WidthRequest = 62,
            HeightRequest = 62,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = image
        };

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(draft.Title) ? "مسودة بدون عنوان" : draft.Title,
            FontFamily = "Tajawal-Regular",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End
        };

        var meta = new Label
        {
            Text = draft.UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
            FontFamily = "Tajawal-Regular",
            FontSize = 11,
            TextColor = theme.TextMuted,
            HorizontalTextAlignment = TextAlignment.End
        };

        var status = new Label
        {
            Text = draft.Id == _currentRecord?.Id ? "Editing Draft" : "Draft",
            FontFamily = "Tajawal-Regular",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = draft.Id == _currentRecord?.Id ? EditingDraftColor : DraftColor,
            HorizontalTextAlignment = TextAlignment.End
        };

        var resume = new Button { Text = "استئناف التحرير", FontSize = 12 };
        resume.Clicked += async (_, _) => await ResumeDraftAsync(draft.Id);

        var delete = new Button { Text = "حذف", FontSize = 12 };
        delete.Clicked += async (_, _) => await DeleteDraftAsync(draft.Id);

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8
        };
        actions.Add(resume, 0, 0);
        actions.Add(delete, 1, 0);

        var content = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { status, title, meta, actions }
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        row.Add(imageFrame, 0, 0);
        row.Add(content, 1, 0);

        return new Border
        {
            Padding = 10,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Background = theme.ActionBackground,
            Content = row
        };
    }

    private async Task ResumeDraftAsync(string id)
    {
        var draft = await CurrentSeasonAdminService.LoadDraftByIdAsync(id);
        if (draft == null)
        {
            await DisplayAlert("المسودات", "تعذر العثور على المسودة", "حسناً");
            return;
        }

        PopulateFields(draft, true);
        DraftsSheetOverlay.IsVisible = false;
    }

    private async Task DeleteDraftAsync(string id)
    {
        await CurrentSeasonAdminService.DeleteDraftAsync(id);
        if (_currentRecord?.Id == id)
            ClearFieldsForNewEntry();
        await OpenDraftsSheetAsync();
    }

    private async void OnOpenPublishedSeasonsClicked(object? sender, EventArgs e)
    {
        await OpenPublishedSeasonsSheetAsync();
    }

    private async Task OpenPublishedSeasonsSheetAsync()
    {
        PublishedSeasonsList.Children.Clear();
        var seasons = await CurrentSeasonAdminService.LoadManagedAsync();
        if (seasons.Count == 0)
        {
            PublishedSeasonsList.Children.Add(new Label
            {
                Text = "لا توجد مواسم منشورة حالياً",
                FontFamily = "Tajawal-Regular",
                FontSize = 14,
                TextColor = GalleryThemeEngine.Current.TextMuted,
                HorizontalTextAlignment = TextAlignment.Center
            });
        }
        else
        {
            foreach (var season in seasons)
                PublishedSeasonsList.Children.Add(CreatePublishedSeasonRow(season));
        }

        PublishedSeasonsSheetOverlay.IsVisible = true;
    }

    private View CreatePublishedSeasonRow(CurrentSeasonRecord season)
    {
        var theme = GalleryThemeEngine.Current;
        var identity = CurrentSeasonAdminService.GetIdentity(season);
        var image = new Image
        {
            Source = ResolveImageSource(season.ImagePath),
            Aspect = Aspect.AspectFill,
            WidthRequest = 62,
            HeightRequest = 62
        };

        var imageFrame = new Border
        {
            WidthRequest = 62,
            HeightRequest = 62,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = image
        };

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(season.Title) ? "موسم بدون عنوان" : season.Title,
            FontFamily = "Tajawal-Regular",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End
        };

        var status = new Label
        {
            Text = season.Status == StoreContentStatus.Hidden ? "مخفي / مؤرشف" : "منشور",
            FontFamily = "Tajawal-Regular",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = season.Status == StoreContentStatus.Hidden ? HiddenColor : PublishedColor,
            HorizontalTextAlignment = TextAlignment.End
        };

        var meta = new Label
        {
            Text = season.PublishedAt?.ToLocalTime().ToString("yyyy/MM/dd HH:mm") ?? season.UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
            FontFamily = "Tajawal-Regular",
            FontSize = 11,
            TextColor = theme.TextMuted,
            HorizontalTextAlignment = TextAlignment.End
        };

        var edit = new Button { Text = "تعديل", FontSize = 12 };
        edit.Clicked += async (_, _) => await EditManagedSeasonAsync(identity);
        var hide = new Button { Text = season.Status == StoreContentStatus.Hidden ? "مؤرشف" : "إخفاء", FontSize = 12, IsEnabled = season.Status == StoreContentStatus.Published };
        hide.Clicked += async (_, _) => await HideManagedSeasonAsync(identity);
        var delete = new Button { Text = "حذف", FontSize = 12 };
        delete.Clicked += async (_, _) => await DeleteManagedSeasonAsync(identity);

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8
        };
        actions.Add(edit, 0, 0);
        actions.Add(hide, 1, 0);
        actions.Add(delete, 2, 0);

        var content = new VerticalStackLayout { Spacing = 4, Children = { status, title, meta, actions } };
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        row.Add(imageFrame, 0, 0);
        row.Add(content, 1, 0);
        return new Border
        {
            Padding = 10,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Background = theme.ActionBackground,
            Content = row
        };
    }

    private async Task EditManagedSeasonAsync(string id)
    {
        var season = await CurrentSeasonAdminService.LoadManagedByIdAsync(id);
        if (season == null)
            return;

        PopulateFields(season, false, true);
        PublishedSeasonsSheetOverlay.IsVisible = false;
    }

    private async Task HideManagedSeasonAsync(string id)
    {
        await CurrentSeasonAdminService.HidePublishedAsync(id);
        await RefreshPublishedCardAsync();
        await OpenPublishedSeasonsSheetAsync();
        if (_currentRecord != null && CurrentSeasonAdminService.GetIdentity(_currentRecord) == id)
            ClearFieldsForNewEntry();
    }

    private async Task DeleteManagedSeasonAsync(string id)
    {
        var confirm = await DisplayAlert("حذف الموسم", "هل تريد حذف هذا الموسم؟", "حذف", "إلغاء");
        if (!confirm)
            return;

        await CurrentSeasonAdminService.DeletePublishedAsync(id);
        await RefreshPublishedCardAsync();
        await OpenPublishedSeasonsSheetAsync();
        if (_currentRecord != null && CurrentSeasonAdminService.GetIdentity(_currentRecord) == id)
            ClearFieldsForNewEntry();
    }

    private async void OnEditPublishedClicked(object? sender, EventArgs e)
    {
        var published = _publishedRecord ?? await CurrentSeasonAdminService.LoadPublishedAsync();
        if (published == null)
            return;

        PopulateFields(published, false, true);
    }

    private async void OnHidePublishedClicked(object? sender, EventArgs e)
    {
        if (_publishedRecord != null)
            await CurrentSeasonAdminService.HidePublishedAsync(CurrentSeasonAdminService.GetIdentity(_publishedRecord));
        await RefreshPublishedCardAsync();
        ClearFieldsForNewEntry();
    }

    private async void OnDeletePublishedClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("حذف النشر", "هل تريد حذف الموسم المنشور؟", "حذف", "إلغاء");
        if (!confirm)
            return;

        if (_publishedRecord != null)
            await CurrentSeasonAdminService.DeletePublishedAsync(CurrentSeasonAdminService.GetIdentity(_publishedRecord));
        await RefreshPublishedCardAsync();
        ClearFieldsForNewEntry();
    }

    private void OnNewSeasonClicked(object? sender, EventArgs e)
    {
        ClearFieldsForNewEntry();
    }

    private void OnCloseDraftsSheetClicked(object? sender, EventArgs e)
    {
        DraftsSheetOverlay.IsVisible = false;
    }

    private void OnCloseDraftsSheetTapped(object? sender, TappedEventArgs e)
    {
        DraftsSheetOverlay.IsVisible = false;
    }

    private void OnClosePublishedSeasonsSheetClicked(object? sender, EventArgs e)
    {
        PublishedSeasonsSheetOverlay.IsVisible = false;
    }

    private void OnClosePublishedSeasonsSheetTapped(object? sender, TappedEventArgs e)
    {
        PublishedSeasonsSheetOverlay.IsVisible = false;
    }

    private async void OnCancelTapped(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private CurrentSeasonRecord BuildRecordFromFields(StoreContentStatus status)
    {
        var existing = _currentRecord;

        return new CurrentSeasonRecord
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            SeasonId = existing?.SeasonId ?? string.Empty,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = existing?.PublishedAt,
            Title = SeasonNameEntry.Text?.Trim() ?? string.Empty,
            Subtitle = SeasonTitleEntry.Text?.Trim() ?? string.Empty,
            Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
            ButtonText = ButtonTextEntry.Text?.Trim() ?? string.Empty,
            ImagePath = ImagePathEntry.Text?.Trim() ?? string.Empty,
            IsVisible = existing?.IsVisible ?? true,
            SortOrder = existing?.SortOrder ?? 0,
            StartsAt = existing?.StartsAt,
            EndsAt = existing?.EndsAt,
            Status = status
        };
    }

    private bool ValidateTextLimits()
    {
        var item = new StoreAdminContentItem
        {
            Title = SeasonNameEntry.Text ?? string.Empty,
            Subtitle = SeasonTitleEntry.Text ?? string.Empty,
            Description = DescriptionEditor.Text ?? string.Empty
        };

        if ((ButtonTextEntry.Text ?? string.Empty).Length > (_textLimits.ButtonTextMaxLength ?? int.MaxValue))
        {
            ShowValidation(_textLimits.ValidationMessage);
            return false;
        }

        if (!StoreAdminService.ValidateText(item, _textLimits, out var message))
        {
            ShowValidation(message);
            return false;
        }

        ValidationLabel.IsVisible = false;
        return true;
    }

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }

    private void UpdateCountersAndPreview()
    {
        SeasonNameCounterLabel.Text = BuildCounter(SeasonNameEntry.Text, _textLimits.TitleMaxLength);
        SeasonTitleCounterLabel.Text = BuildCounter(SeasonTitleEntry.Text, _textLimits.SubtitleMaxLength);
        DescriptionCounterLabel.Text = BuildCounter(DescriptionEditor.Text, _textLimits.DescriptionMaxLength);
        ButtonTextCounterLabel.Text = BuildCounter(ButtonTextEntry.Text, _textLimits.ButtonTextMaxLength);

        PreviewTitleLabel.Text = string.IsNullOrWhiteSpace(SeasonNameEntry.Text)
            ? "اسم الموسم"
            : SeasonNameEntry.Text;

        PreviewSubtitleLabel.Text = string.IsNullOrWhiteSpace(SeasonTitleEntry.Text)
            ? "العنوان"
            : SeasonTitleEntry.Text;

        PreviewDescriptionLabel.Text = string.IsNullOrWhiteSpace(DescriptionEditor.Text)
            ? "الوصف"
            : DescriptionEditor.Text;
    }

    private void ApplyPreviewImage(string? imagePath)
    {
        var source = ResolveImageSource(imagePath);
        SeasonImagePreview.Source = source;
        SeasonImagePreview.Aspect = _imageRule.ImageFit;
        SeasonImagePreview.IsVisible = source != null;
    }

    private static ImageSource? ResolveImageSource(string? imagePath)
        => InventoryDisplayResolver.ResolveOptionalImageSource(
            imagePath);

    private static string BuildCounter(string? value, int? maxLength)
    {
        var current = value?.Length ?? 0;
        return maxLength.HasValue ? $"{current}/{maxLength.Value}" : current.ToString();
    }

    private void ClearFieldsForNewEntry()
    {
        _currentRecord = null;
        _editingManagedRecord = false;
        SeasonNameEntry.Text = string.Empty;
        SeasonTitleEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ButtonTextEntry.Text = string.Empty;
        ImagePathEntry.Text = string.Empty;
        StatusPicker.SelectCanonicalId(StoreContentStatus.Draft.ToString());
        SeasonImagePreview.Source = null;
        SeasonImagePreview.IsVisible = false;
        ValidationLabel.IsVisible = false;
        PublishButton.IsVisible = true;
        SaveChangesButton.IsVisible = false;
        UpdateCountersAndPreview();
        SetEditorMode("New Season", "جاهز لإنشاء موسم جديد", NewSeasonColor);
    }

    private void SetEditorMode(string title, string subtitle, Color color)
    {
        ModeTitleLabel.Text = title;
        ModeSubtitleLabel.Text = subtitle;
        ModeDot.Color = color;
        ModeTitleLabel.TextColor = color;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        BackButtonFrame.Background = theme.CardBackground;
        BackButtonFrame.Stroke = theme.Stroke;
        BackButtonLabel.TextColor = theme.Gold;
        TitleLabel.TextColor = theme.TextPrimary;
        SubtitleLabel.TextColor = theme.TextSecondary;

        ModeCard.Background = theme.ActionBackground;
        ModeCard.Stroke = theme.Stroke;
        ModeSubtitleLabel.TextColor = theme.TextMuted;

        PublishedCard.Background = theme.ActionBackground;
        PublishedCard.Stroke = theme.Stroke;
        PublishedStatusLabel.TextColor = PublishedColor;
        PublishedTitleLabel.TextColor = theme.TextPrimary;
        PublishedDateLabel.TextColor = theme.TextMuted;

        PreviewCard.Background = theme.CardBackground;
        PreviewCard.Stroke = theme.Stroke;
        PreviewOverlay.Background = theme.ActionBackground;
        PreviewTitleLabel.TextColor = theme.TextPrimary;
        PreviewSubtitleLabel.TextColor = theme.TextSecondary;
        PreviewDescriptionLabel.TextColor = theme.TextSecondary;

        FormPanel.Background = theme.ActionBackground;
        FormPanel.Stroke = theme.Stroke;
        ValidationLabel.TextColor = ErrorColor;

        DraftsSheet.Background = theme.CardBackground;
        DraftsSheet.Stroke = theme.Stroke;
        DraftsSheetTitleLabel.TextColor = theme.TextPrimary;

        PublishedSeasonsSheet.Background = theme.CardBackground;
        PublishedSeasonsSheet.Stroke = theme.Stroke;
        PublishedSeasonsSheetTitleLabel.TextColor = theme.TextPrimary;

        ApplyInputTheme();

        var counterColor = theme.TextMuted;
        SeasonNameCounterLabel.TextColor = counterColor;
        SeasonTitleCounterLabel.TextColor = counterColor;
        DescriptionCounterLabel.TextColor = counterColor;
        ButtonTextCounterLabel.TextColor = counterColor;
    }

    private void ApplyInputTheme()
    {
        var theme = GalleryThemeEngine.Current;
        var inputText = theme.TextPrimary;
        var placeholder = theme.TextMuted;

        SeasonNameEntry.TextColor = inputText;
        SeasonNameEntry.PlaceholderColor = placeholder;
        SeasonTitleEntry.TextColor = inputText;
        SeasonTitleEntry.PlaceholderColor = placeholder;
        DescriptionEditor.TextColor = inputText;
        DescriptionEditor.PlaceholderColor = placeholder;
        ButtonTextEntry.TextColor = inputText;
        ButtonTextEntry.PlaceholderColor = placeholder;
        ImagePathEntry.TextColor = inputText;
        ImagePathEntry.PlaceholderColor = placeholder;
        StatusPicker.TextColor = inputText;
        StatusPicker.TitleColor = placeholder;
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }
}

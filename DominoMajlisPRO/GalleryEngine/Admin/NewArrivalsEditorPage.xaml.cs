using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class NewArrivalsEditorPage : ContentPage
{
    private enum EditorMode { NewItem, EditingDraft, EditingPublished }
    private static readonly Color NewItemColor = Color.FromArgb("#2F80ED");
    private static readonly Color DraftColor = Color.FromArgb("#D9A441");
    private static readonly Color EditingDraftColor = Color.FromArgb("#9B51E0");
    private static readonly Color PublishedColor = Color.FromArgb("#27AE60");
    private static readonly Color HiddenColor = Color.FromArgb("#7E8490");
    private static readonly Color ErrorColor = Color.FromArgb("#D84A4A");

    private NewArrivalRecord? _currentRecord;
    private EditorMode _editorMode;
    private readonly List<DominoMajlisPRO.GalleryEngine.Models.CatalogAssetDisplay> _assetChoices = new();

    public NewArrivalsEditorPage()
    {
        InitializeComponent();
        FlowDirection = FlowDirection.RightToLeft;
        ConfigureFields();
        ApplyTheme();
        ClearFieldsForNewItem();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        await LoadAssetChoicesAsync(SelectedAssetId());
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void ConfigureFields()
    {
        CurrencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        AssetTypePicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        OwnerScopePicker.SetOptions(CanonicalStoreCatalog.OwnerScopes());
        StatusPicker.SetOptions(CanonicalStoreCatalog.PublishStates());
    }

    private void PopulateFields(NewArrivalRecord record, EditorMode mode)
    {
        _currentRecord = record;
        _editorMode = mode;
        TitleEntry.Text = record.Title;
        SubtitleEntry.Text = record.Subtitle;
        DescriptionEditor.Text = record.Description;
        ButtonTextEntry.Text = record.ButtonText;
        ImagePathEntry.Text = record.ImagePath;
        AssetTypePicker.SelectCanonicalId(StoreProductAssetTypeCatalog.TryResolve(record.StoreTypeId, out _) ? record.StoreTypeId : null);
        _ = LoadAssetChoicesAsync(record.AssetId);
        OwnerScopePicker.SelectCanonicalId(record.OwnerScope);
        ColorHexEntry.Text = record.ColorHex;
        PriceEntry.Text = record.Price.ToString();
        CurrencyPicker.SelectCanonicalId(record.CurrencyType.ToString());
        FeaturedSwitch.IsToggled = record.IsFeatured;
        SortOrderEntry.Text = record.SortOrder.ToString();
        StatusPicker.SelectCanonicalId(record.Status.ToString());
        ApplyPreviewImage(record.ImagePath);
        var editingPublished = mode == EditorMode.EditingPublished;
        PublishButton.Text = editingPublished ? "حفظ التعديل" : "نشر";
        SaveDraftButton.IsEnabled = !editingPublished;
        SetEditorMode(editingPublished ? "Editing Published" : "Editing Draft", editingPublished ? "تعديل العنصر المنشور" : "استكمال تعديل مسودة", editingPublished ? PublishedColor : EditingDraftColor);
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "اختيار صورة العنصر",
            FileTypes = FilePickerFileType.Images
        });

        if (result == null)
            return;

        ImagePathEntry.Text = result.FullPath ?? result.FileName;
        ApplyPreviewImage(ImagePathEntry.Text);
    }

    private async void OnSaveDraftClicked(object? sender, EventArgs e)
    {
        var saved = await NewArrivalsAdminService.SaveDraftAsync(BuildRecordFromFields(NewArrivalStatus.Draft));
        PopulateFields(saved, EditorMode.EditingDraft);
        ValidationLabel.IsVisible = false;
        await DisplayAlert("حفظ كمسودة", "تم حفظ مسودة العنصر بنجاح", "حسناً");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        var record = BuildRecordFromFields(NewArrivalStatus.Published);
        if (!NewArrivalsAdminService.ValidateForPublish(record, out var message))
        {
            ShowValidation(message);
            SetEditorMode("Error", "Missing Required", ErrorColor);
            return;
        }

        if (_editorMode == EditorMode.EditingPublished)
            await NewArrivalsAdminService.UpdatePublishedAsync(record);
        else
            await NewArrivalsAdminService.PublishAsync(record);
        var messageText = _editorMode == EditorMode.EditingPublished ? "تم حفظ تعديل العنصر بنجاح" : "تم نشر العنصر بنجاح";
        ClearFieldsForNewItem();
        await DisplayAlert("نشر", messageText, "حسناً");
    }

    private async void OnOpenDraftsClicked(object? sender, EventArgs e)
    {
        await OpenDraftsSheetAsync();
    }

    private async void OnOpenPublishedClicked(object? sender, EventArgs e)
    {
        await OpenPublishedSheetAsync();
    }

    private async void OnAuditMalformedClicked(object? sender, EventArgs e)
    {
        SheetTitleLabel.Text = "تدقيق المنتجات غير الصالحة";
        SheetList.Children.Clear();
        var malformed = (await NewArrivalsAdminService.LoadAuditAsync())
            .Where(NewArrivalsAdminService.IsMalformed)
            .ToList();
        if (malformed.Count == 0)
            SheetList.Children.Add(CreateEmptySheetLabel("لا توجد منتجات غير صالحة"));
        else
            foreach (var record in malformed)
                SheetList.Children.Add(CreateMalformedRow(record));
        SheetOverlay.IsVisible = true;
    }

    private View CreateMalformedRow(NewArrivalRecord record)
    {
        var edit = new Button { Text = "إعادة تعيين نوع الأصل", FontSize = 11 };
        edit.Clicked += async (_, _) =>
        {
            if (record.Status == NewArrivalStatus.Draft)
                await ResumeDraftAsync(NewArrivalsAdminService.GetAssetId(record));
            else
            {
                var draft = await NewArrivalsAdminService.CreateDraftFromPublishedAsync(NewArrivalsAdminService.GetAssetId(record));
                if (draft != null)
                    PopulateFields(draft, EditorMode.EditingDraft);
                SheetOverlay.IsVisible = false;
            }
        };
        return new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                new Label
                {
                    Text = $"ProductId: {record.ProductId}\nAssetId: {record.AssetId}",
                    TextColor = ErrorColor,
                    FontSize = 11
                },
                CreateRecordRow(record, ErrorColor, edit)
            }
        };
    }

    private async Task OpenDraftsSheetAsync()
    {
        SheetTitleLabel.Text = "المسودات";
        SheetList.Children.Clear();
        var drafts = await NewArrivalsAdminService.LoadAllDraftsAsync();

        if (drafts.Count == 0)
            SheetList.Children.Add(CreateEmptySheetLabel("لا توجد مسودات محفوظة حالياً"));
        else
            foreach (var draft in drafts)
                SheetList.Children.Add(CreateDraftRow(draft));

        SheetOverlay.IsVisible = true;
    }

    private async Task OpenPublishedSheetAsync()
    {
        SheetTitleLabel.Text = "العناصر المنشورة";
        SheetList.Children.Clear();
        var published = await NewArrivalsAdminService.LoadPublishedAsync();

        if (published.Count == 0)
            SheetList.Children.Add(CreateEmptySheetLabel("لا توجد عناصر منشورة حالياً"));
        else
            foreach (var item in published)
                SheetList.Children.Add(CreatePublishedRow(item));

        SheetOverlay.IsVisible = true;
    }

    private Label CreateEmptySheetLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = 14,
            TextColor = GalleryThemeEngine.Current.TextMuted,
            HorizontalTextAlignment = TextAlignment.Center
        };
    }

    private View CreateDraftRow(NewArrivalRecord record)
    {
        var resume = new Button { Text = "استئناف التحرير", FontSize = 12 };
        resume.Clicked += async (_, _) => await ResumeDraftAsync(NewArrivalsAdminService.GetAssetId(record));

        var delete = new Button { Text = "حذف", FontSize = 12 };
        delete.Clicked += async (_, _) => await DeleteDraftAsync(NewArrivalsAdminService.GetAssetId(record));

        return CreateRecordRow(record, _currentRecord != null && NewArrivalsAdminService.GetAssetId(record) == NewArrivalsAdminService.GetAssetId(_currentRecord) ? EditingDraftColor : DraftColor, resume, delete);
    }

    private View CreatePublishedRow(NewArrivalRecord record)
    {
        var edit = new Button { Text = "تعديل", FontSize = 12 };
        edit.Clicked += async (_, _) => await EditPublishedAsync(NewArrivalsAdminService.GetAssetId(record));

        var hide = new Button { Text = "إخفاء", FontSize = 12 };
        hide.Clicked += async (_, _) => await HidePublishedAsync(NewArrivalsAdminService.GetAssetId(record));

        var delete = new Button { Text = "حذف النشر", FontSize = 12 };
        delete.Clicked += async (_, _) => await DeletePublishedAsync(NewArrivalsAdminService.GetAssetId(record));

        return CreateRecordRow(record, PublishedColor, edit, hide, delete);
    }

    private View CreateRecordRow(NewArrivalRecord record, Color statusColor, params Button[] buttons)
    {
        var theme = GalleryThemeEngine.Current;
        var imageFrame = new Border
        {
            WidthRequest = 62,
            HeightRequest = 62,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new Image
            {
                Source = ResolveImageSource(record.ImagePath),
                Aspect = Aspect.AspectFill,
                WidthRequest = 62,
                HeightRequest = 62
            }
        };

        var status = new Label
        {
            Text = record.Status.ToString(),
            TextColor = statusColor,
            FontAttributes = FontAttributes.Bold,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.End
        };

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(record.Title) ? "مسودة بدون عنوان" : record.Title,
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
            Text = record.Status == NewArrivalStatus.Published
                ? $"{record.Price} {record.CurrencyType} • {record.PublishedAt?.ToLocalTime():yyyy/MM/dd HH:mm}"
                : record.UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
            TextColor = theme.TextMuted,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.End
        };

        var actions = new Grid { ColumnSpacing = 8 };
        for (int i = 0; i < buttons.Length; i++)
        {
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            actions.Add(buttons[i], i, 0);
        }

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

    private async Task ResumeDraftAsync(string assetId)
    {
        var draft = await NewArrivalsAdminService.LoadDraftByIdAsync(assetId);
        if (draft == null)
        {
            await DisplayAlert("المسودات", "تعذر العثور على المسودة", "حسناً");
            return;
        }

        PopulateFields(draft, EditorMode.EditingDraft);
        SheetOverlay.IsVisible = false;
    }

    private async Task DeleteDraftAsync(string assetId)
    {
        await NewArrivalsAdminService.DeleteDraftAsync(assetId);
        if (_currentRecord != null && NewArrivalsAdminService.GetAssetId(_currentRecord) == assetId)
            ClearFieldsForNewItem();
        await OpenDraftsSheetAsync();
    }

    private async Task EditPublishedAsync(string assetId)
    {
        var item = (await NewArrivalsAdminService.LoadPublishedAsync()).FirstOrDefault(record => NewArrivalsAdminService.GetAssetId(record) == assetId);
        if (item == null)
            return;

        PopulateFields(item, EditorMode.EditingPublished);
        SheetOverlay.IsVisible = false;
    }

    private async Task HidePublishedAsync(string assetId)
    {
        await NewArrivalsAdminService.HidePublishedAsync(assetId);
        await OpenPublishedSheetAsync();
    }

    private async Task DeletePublishedAsync(string assetId)
    {
        var confirm = await DisplayAlert("حذف النشر", "هل تريد حذف العنصر المنشور؟", "حذف", "إلغاء");
        if (!confirm)
            return;

        await NewArrivalsAdminService.DeletePublishedAsync(assetId);
        await OpenPublishedSheetAsync();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ValidationLabel.IsVisible = false;
    }

    private void OnCloseSheetClicked(object? sender, EventArgs e)
    {
        SheetOverlay.IsVisible = false;
    }

    private void OnCloseSheetTapped(object? sender, TappedEventArgs e)
    {
        SheetOverlay.IsVisible = false;
    }

    private void ClearFieldsForNewItem()
    {
        _currentRecord = null;
        _editorMode = EditorMode.NewItem;
        PublishButton.Text = "نشر";
        SaveDraftButton.IsEnabled = true;
        TitleEntry.Text = string.Empty;
        SubtitleEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ButtonTextEntry.Text = string.Empty;
        ImagePathEntry.Text = string.Empty;
        AssetIdPicker.SelectedIndex = -1;
        AssetTypePicker.SelectedIndex = -1;
        OwnerScopePicker.SelectedIndex = -1;
        ColorHexEntry.Text = string.Empty;
        PriceEntry.Text = "0";
        CurrencyPicker.SelectCanonicalId("Gems");
        FeaturedSwitch.IsToggled = false;
        SortOrderEntry.Text = "0";
        StatusPicker.SelectCanonicalId("Draft");
        PreviewImage.Source = null;
        PreviewImage.IsVisible = false;
        ValidationLabel.IsVisible = false;
        SetEditorMode("New Item", "جاهز لإضافة عنصر جديد", NewItemColor);
    }

    private void OnCancelTapped(object? sender, EventArgs e)
    {
        _ = CloseAsync();
    }

    private async Task CloseAsync()
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private NewArrivalRecord BuildRecordFromFields(NewArrivalStatus status)
    {
        var currency = Enum.TryParse<NewArrivalCurrencyType>(CurrencyPicker.SelectedCanonicalId(), out var parsedCurrency)
            ? parsedCurrency
            : NewArrivalCurrencyType.Gems;

        _ = int.TryParse(PriceEntry.Text, out var price);
        _ = int.TryParse(SortOrderEntry.Text, out var sortOrder);

        var productId = !string.IsNullOrWhiteSpace(_currentRecord?.ProductId)
            ? _currentRecord.ProductId
            : !string.IsNullOrWhiteSpace(_currentRecord?.Id)
                ? _currentRecord.Id
                : Guid.NewGuid().ToString();
        var storeTypeId = AssetTypePicker.SelectedCanonicalId();
        var ownerScope = StoreProductAssetTypeCatalog.TryResolve(storeTypeId, out var assetType)
            ? StoreProductAssetTypeCatalog.GetOwnerScope(assetType).ToString()
            : string.Empty;

        return new NewArrivalRecord
        {
            Id = productId,
            ProductId = productId,
            AssetId = SelectedAssetId(),
            StoreTypeId = storeTypeId,
            OwnerScope = ownerScope,
            ColorHex = ColorHexEntry.Text?.Trim() ?? string.Empty,
            CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = _currentRecord?.PublishedAt,
            Title = TitleEntry.Text?.Trim() ?? string.Empty,
            Subtitle = SubtitleEntry.Text?.Trim() ?? string.Empty,
            Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
            ButtonText = ButtonTextEntry.Text?.Trim() ?? string.Empty,
            ImagePath = ImagePathEntry.Text?.Trim() ?? string.Empty,
            Category = _currentRecord?.Category ?? string.Empty,
            Price = currency == NewArrivalCurrencyType.Free ? 0 : price,
            CurrencyType = currency,
            IsFree = currency == NewArrivalCurrencyType.Free || price == 0,
            IsFeatured = FeaturedSwitch.IsToggled,
            SortOrder = sortOrder,
            Status = status
        };
    }

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }

    private void ApplyPreviewImage(string? imagePath)
    {
        var source = ResolveImageSource(imagePath);
        PreviewImage.Source = source;
        PreviewImage.IsVisible = source != null;
    }

    private static ImageSource? ResolveImageSource(string? imagePath)
        => InventoryDisplayResolver.ResolveOptionalImageSource(
            imagePath);

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
        FormPanel.Background = theme.ActionBackground;
        FormPanel.Stroke = theme.Stroke;
        FeaturedLabel.TextColor = theme.TextSecondary;
        SheetPanel.Background = theme.CardBackground;
        SheetPanel.Stroke = theme.Stroke;
        SheetTitleLabel.TextColor = theme.TextPrimary;
        ApplyInputTheme();
    }

    private void ApplyInputTheme()
    {
        var theme = GalleryThemeEngine.Current;
        var inputText = theme.TextPrimary;
        var placeholder = theme.TextMuted;

        foreach (var entry in new[] { TitleEntry, SubtitleEntry, ButtonTextEntry, ImagePathEntry, ColorHexEntry, PriceEntry, SortOrderEntry })
        {
            entry.TextColor = inputText;
            entry.PlaceholderColor = placeholder;
        }

        DescriptionEditor.TextColor = inputText;
        DescriptionEditor.PlaceholderColor = placeholder;
        CurrencyPicker.TextColor = inputText;
        CurrencyPicker.TitleColor = placeholder;
        StatusPicker.TextColor = inputText;
        StatusPicker.TitleColor = placeholder;
        AssetTypePicker.TextColor = inputText;
        AssetTypePicker.TitleColor = placeholder;
        AssetIdPicker.TextColor = inputText;
        AssetIdPicker.TitleColor = placeholder;
        OwnerScopePicker.TextColor = inputText;
        OwnerScopePicker.TitleColor = placeholder;
        ValidationLabel.TextColor = ErrorColor;
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }

    private async void OnAssetTypeChanged(object? sender, EventArgs e)
    {
        var selected = AssetTypePicker.SelectedCanonicalId();
        OwnerScopePicker.SelectCanonicalId(StoreProductAssetTypeCatalog.TryResolve(selected, out var type)
            ? StoreProductAssetTypeCatalog.GetOwnerScope(type).ToString()
            : string.Empty);
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


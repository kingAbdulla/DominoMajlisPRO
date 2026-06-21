using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class EmblemsManagerPage : SpecializedStoreManagerPage
{
    public EmblemsManagerPage() : base(SpecializedStoreManagerDefinition.Emblems) { }
}

public sealed class EffectsManagerPage : SpecializedStoreManagerPage
{
    public EffectsManagerPage() : base(SpecializedStoreManagerDefinition.Effects) { }
}

public sealed class EmblemBackgroundsManagerPage : SpecializedStoreManagerPage
{
    public EmblemBackgroundsManagerPage() : base(SpecializedStoreManagerDefinition.EmblemBackgrounds) { }
}

public sealed class FramesManagerPage : SpecializedStoreManagerPage
{
    public FramesManagerPage() : base(SpecializedStoreManagerDefinition.Frames) { }
}

public sealed class TitlesManagerPage : SpecializedStoreManagerPage
{
    public TitlesManagerPage() : base(SpecializedStoreManagerDefinition.Titles) { }
}

public sealed class BundlesManagerPage : SpecializedStoreManagerPage
{
    public BundlesManagerPage() : base(SpecializedStoreManagerDefinition.Bundles) { }
}

public sealed class TeamColorsManagerPage : SpecializedStoreManagerPage
{
    public TeamColorsManagerPage() : base(SpecializedStoreManagerDefinition.TeamColors) { }
}

public class SpecializedStoreManagerPage : ContentPage
{
    private readonly SpecializedStoreManagerDefinition _definition;
    private readonly Entry _titleEntry = new() { Placeholder = "ط§ظ„ط¹ظ†ظˆط§ظ† / ط§ظ„ط§ط³ظ…" };
    private readonly Editor _descriptionEditor = new() { Placeholder = "ط§ظ„ظˆطµظپ", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 82 };
    private readonly Entry _imageEntry = new() { Placeholder = "ط§ظ„طµظˆط±ط©", IsReadOnly = true };
    private readonly Image _previewImage = new() { HeightRequest = 130, Aspect = Aspect.AspectFit, IsVisible = false };
    private readonly Picker _assetIdPicker = new() { Title = "ظ…ط¹ط±ظ‘ظپ ط§ظ„ط£طµظ„ AssetId" };
    private readonly Picker _assetTypePicker = new() { Title = "Asset Type / ظ†ظˆط¹ ط§ظ„ط£طµظ„" };
    private readonly Picker _categoryPicker = new() { Title = "ط§ظ„طھطµظ†ظٹظپ" };
    private readonly Entry _priceEntry = new() { Placeholder = "ط§ظ„ط³ط¹ط±", Keyboard = Keyboard.Numeric };
    private readonly Picker _currencyPicker = new() { Title = "ط§ظ„ط¹ظ…ظ„ط©" };
    private readonly Picker _effectTypePicker = new() { Title = "ظ†ظˆط¹ ط§ظ„طھط£ط«ظٹط±" };
    private readonly Picker _animationTypePicker = new() { Title = "ظ†ظˆط¹ ط§ظ„ط­ط±ظƒط©" };
    private readonly Entry _durationEntry = new() { Placeholder = "ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ظ…ظ„ظ„ظٹ ط«ط§ظ†ظٹط©", Keyboard = Keyboard.Numeric };
    private readonly Picker _equipTargetPicker = new() { Title = "ظ‡ط¯ظپ ط§ظ„طھط¬ظ‡ظٹط²" };
    private readonly Editor _bundleAssetsEditor = new() { Placeholder = "AssetType:AssetId â€” ط£طµظ„ ظˆط§ط­ط¯ ظپظٹ ظƒظ„ ط³ط·ط±", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 92 };
    private readonly Entry _discountEntry = new() { Placeholder = "ظ†ط³ط¨ط© ط§ظ„ط®طµظ…", Keyboard = Keyboard.Numeric };
    private readonly Entry _colorHexEntry = new() { Placeholder = "ظ„ظˆظ† ط§ظ„ظپط±ظٹظ‚ #RRGGBB" };
    private readonly Label _validationLabel = new() { FontSize = 11, IsVisible = false, HorizontalTextAlignment = TextAlignment.End };
    private readonly Label _modeTitle = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End };
    private readonly Label _modeSubtitle = new() { FontSize = 11, HorizontalTextAlignment = TextAlignment.End };
    private readonly BoxView _modeDot = new() { WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, VerticalOptions = LayoutOptions.Center };
    private readonly Border _modeCard = Panel();
    private readonly Border _formPanel = Panel();
    private readonly List<DominoMajlisPRO.GalleryEngine.Models.CatalogAssetDisplay> _assetChoices = new();
    private NewArrivalRecord? _currentRecord;
    private bool _editingPublished;

    protected SpecializedStoreManagerPage(SpecializedStoreManagerDefinition definition)
    {
        _definition = definition;
        Title = definition.Title;
        BackgroundColor = Color.FromArgb("#030303");
        FlowDirection = FlowDirection.RightToLeft;
        NavigationPage.SetHasNavigationBar(this, false);
        BuildPage();
        ConfigureControls();
        ApplyTheme();
        ClearFields();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        await LoadAssetChoicesAsync();
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void BuildPage()
    {
        var back = new Border { WidthRequest = 42, HeightRequest = 42, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Content = new Label { Text = "â€¹", FontSize = 28, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        var heading = new VerticalStackLayout { Spacing = 1, Children = { new Label { Text = _definition.Title, FontFamily = "Tajawal-Regular", FontSize = 25, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End }, new Label { Text = _definition.Subtitle, FontFamily = "Tajawal-Regular", FontSize = 12, HorizontalTextAlignment = TextAlignment.End } } };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
        header.Add(back, 0); header.Add(heading, 1);

        var modeGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        modeGrid.Add(_modeDot, 0); modeGrid.Add(new VerticalStackLayout { Spacing = 1, Children = { _modeTitle, _modeSubtitle } }, 1);
        _modeCard.Content = modeGrid;

        var imageRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 10 };
        imageRow.Add(_imageEntry, 0);
        var pickImage = new Button { Text = _definition.IsBundle ? "ط§ط®طھظٹط§ط± ط£ظٹظ‚ظˆظ†ط©" : "ط§ط®طھظٹط§ط± طµظˆط±ط©" };
        pickImage.Clicked += OnPickImageClicked;
        imageRow.Add(pickImage, 1);

        var form = new VerticalStackLayout { Spacing = 10, Children = { _titleEntry, _descriptionEditor, imageRow, _previewImage, _assetTypePicker, _categoryPicker } };
        if (_definition.IsEffect)
        {
            form.Children.Add(_effectTypePicker);
            form.Children.Add(_animationTypePicker);
            form.Children.Add(_durationEntry);
            form.Children.Add(_equipTargetPicker);
        }
        if (_definition.IsBundle)
        {
            form.Children.Add(_bundleAssetsEditor);
            form.Children.Add(_discountEntry);
        }
        if (_definition.IsTeamColor)
            form.Children.Add(_colorHexEntry);
        var priceRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        priceRow.Add(_priceEntry, 0); priceRow.Add(_currencyPicker, 1);
        form.Children.Add(priceRow);
        form.Children.Add(_validationLabel);
        _formPanel.Content = form;

        var actions = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }, ColumnSpacing = 10, RowSpacing = 10 };
        actions.Add(ActionButton("ط­ظپط¸ ظƒظ…ط³ظˆط¯ط©", async () => await SaveDraftAsync()), 0, 0);
        actions.Add(ActionButton("ظ†ط´ط±", async () => await PublishAsync()), 1, 0);
        actions.Add(ActionButton("ط¥ظ„ط؛ط§ط،", () => { ClearFields(); return Task.CompletedTask; }), 0, 1);
        actions.Add(ActionButton("ط§ظ„ظ…ط³ظˆط¯ط§طھ", async () => await SelectRecordAsync(false)), 1, 1);
        var published = ActionButton("ط§ظ„ط¹ظ†ط§طµط± ط§ظ„ظ…ظ†ط´ظˆط±ط©", async () => await SelectRecordAsync(true));
        actions.Add(published, 0, 2); Grid.SetColumnSpan(published, 2);

        var content = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 28), Spacing = 14, Children = { header, _modeCard, _formPanel, actions } };
        Content = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never, Content = content };
    }

    private void ConfigureControls()
    {
        _assetTypePicker.SetOptions(
            CanonicalStoreCatalog.DefaultCategoriesForAdmin()
                .Where(option =>
                    StoreProductAssetTypeCatalog.TryResolve(option.CanonicalId, out var type) &&
                    _definition.AllowedTypes.Contains(type)));
        _assetTypePicker.SelectedIndexChanged += async (_, _) => await LoadAssetChoicesAsync();
        if (_definition.AllowedTypes.Count == 1) _assetTypePicker.SelectedIndex = 0;
        _categoryPicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        _currencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        _effectTypePicker.SetOptions(CanonicalStoreCatalog.EffectTypes());
        _animationTypePicker.SetOptions(CanonicalStoreCatalog.AnimationTypes());
        _equipTargetPicker.SetOptions(CanonicalStoreCatalog.EquipTargets());
        _bundleAssetsEditor.IsReadOnly = true;
        _bundleAssetsEditor.Focused += OnBundleAssetsFocused;
    }

    private async void OnBundleAssetsFocused(object? sender, FocusEventArgs e)
    {
        if (!_definition.IsBundle)
            return;

        _bundleAssetsEditor.Unfocus();
        var assets = await StoreAssetCatalogService.LoadAsync();
        var labels = assets
            .Select(asset => $"{asset.DisplayName} â€¢ {asset.AssetType}")
            .ToArray();
        var selected = await DisplayActionSheetAsync("ط§ط®طھظٹط§ط± ط£طµظ„ ظ„ظ„ط­ط²ظ…ط©", "ط¥ظ„ط؛ط§ط،", null, labels);
        var index = Array.IndexOf(labels, selected);
        if (index < 0)
            return;

        var asset = assets[index];
        var value = $"{asset.AssetType}:{asset.AssetId}";
        var current = _bundleAssetsEditor.Text?
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList()
            ?? new List<string>();
        if (!current.Contains(value, StringComparer.OrdinalIgnoreCase))
            current.Add(value);
        _bundleAssetsEditor.Text = string.Join(Environment.NewLine, current);
    }

    private async Task LoadAssetChoicesAsync()
    {
        var selectedId = SelectedAssetId();
        _assetChoices.Clear();
        if (StoreProductAssetTypeCatalog.TryResolve(_assetTypePicker.SelectedCanonicalId(), out var selectedType))
            _assetChoices.AddRange(
                (await StoreAssetCatalogService.LoadAsync())
                    .Where(asset => asset.AssetType == selectedType)
                    .GroupBy(asset => asset.AssetId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(asset => asset.DisplayName, StringComparer.CurrentCultureIgnoreCase));
        _assetIdPicker.ItemsSource = _assetChoices
            .Select(asset => $"{asset.DisplayName} â€¢ {asset.AssetType}")
            .ToList();
        _assetIdPicker.SelectedIndex = _assetChoices.FindIndex(asset => string.Equals(asset.AssetId, selectedId, StringComparison.OrdinalIgnoreCase));
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = _definition.IsBundle ? "ط§ط®طھظٹط§ط± ط£ظٹظ‚ظˆظ†ط© ط§ظ„ط­ط²ظ…ط©" : "ط§ط®طھظٹط§ط± طµظˆط±ط© ط§ظ„ط£طµظ„", FileTypes = FilePickerFileType.Images });
        if (result == null) return;
        _imageEntry.Text = result.FullPath ?? result.FileName;
        _previewImage.Source =
            InventoryDisplayResolver.ResolveOptionalImageSource(
                _imageEntry.Text);
        _previewImage.IsVisible = _previewImage.Source != null;
    }

    private async Task SaveDraftAsync()
    {
        if (!TryBuildRecord(out var record, validateForPublish: false)) return;
        _currentRecord = await NewArrivalsAdminService.SaveDraftAsync(record);
        _editingPublished = false;
        SetMode("Editing Draft", "طھظ… ط­ظپط¸ ط§ظ„ظ…ط³ظˆط¯ط©", Color.FromArgb("#9B51E0"));
    }

    private async Task PublishAsync()
    {
        if (!TryBuildRecord(out var record, validateForPublish: true)) return;
        if (!NewArrivalsAdminService.ValidateForPublish(record, out var message)) { ShowError(message); return; }
        _currentRecord = _editingPublished ? await NewArrivalsAdminService.UpdatePublishedAsync(record) : await NewArrivalsAdminService.PublishAsync(record);
        ClearFields();
        SetMode("Published", "طھظ… ط§ظ„ظ†ط´ط± ظˆط§ظ„ظ†ظ…ظˆط°ط¬ ط¬ط§ظ‡ط² ظ„ط¹ظ†طµط± ط¬ط¯ظٹط¯", Color.FromArgb("#27AE60"));
    }

    private bool TryBuildRecord(out NewArrivalRecord record, bool validateForPublish)
    {
        record = new NewArrivalRecord();
        if (!StoreProductAssetTypeCatalog.TryResolve(_assetTypePicker.SelectedCanonicalId(), out var assetType) || !_definition.AllowedTypes.Contains(assetType)) { ShowError("ظ†ظˆط¹ ط§ظ„ط£طµظ„ ط؛ظٹط± ظ…ط³ظ…ظˆط­ ظپظٹ ظ‡ط°ط§ ط§ظ„ظ…ط¯ظٹط±"); return false; }
        _ = int.TryParse(_priceEntry.Text, out var price);
        _ = int.TryParse(_durationEntry.Text, out var duration);
        _ = int.TryParse(_discountEntry.Text, out var discount);
        var currency = Enum.TryParse<NewArrivalCurrencyType>(_currencyPicker.SelectedCanonicalId(), out var parsedCurrency) ? parsedCurrency : NewArrivalCurrencyType.Gems;
        var bundleAssets = _bundleAssetsEditor.Text?.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>();
        if (_definition.IsEffect && (string.IsNullOrWhiteSpace(_effectTypePicker.SelectedCanonicalId()) || string.IsNullOrWhiteSpace(_animationTypePicker.SelectedCanonicalId()) || duration <= 0 || string.IsNullOrWhiteSpace(_equipTargetPicker.SelectedCanonicalId()))) { ShowError("ط£ظƒظ…ظ„ ظ†ظˆط¹ ط§ظ„طھط£ط«ظٹط± ظˆط§ظ„ط­ط±ظƒط© ظˆط§ظ„ظ…ط¯ط© ظˆظ‡ط¯ظپ ط§ظ„طھط¬ظ‡ظٹط²"); return false; }
        if (_definition.IsBundle && (bundleAssets.Count < 2 || string.IsNullOrWhiteSpace(_imageEntry.Text))) { ShowError("ط§ظ„ط­ط²ظ…ط© طھط­طھط§ط¬ ط£طµظ„ظٹظ† ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„ ظˆط£ظٹظ‚ظˆظ†ط©"); return false; }
        if (_definition.IsBundle && bundleAssets.Any(item => !ValidBundleComponent(item))) { ShowError("طµظٹط؛ط© ظ…ظƒظˆظ†ط§طھ ط§ظ„ط­ط²ظ…ط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† AssetType:AssetId"); return false; }
        if (validateForPublish && string.IsNullOrWhiteSpace(_titleEntry.Text)) { ShowError("ط§ظ„ط¹ظ†ظˆط§ظ† ظ…ط·ظ„ظˆط¨"); return false; }
        var productId = _currentRecord?.ProductId ?? Guid.NewGuid().ToString();
        var assetId = _currentRecord?.AssetId;
        if (string.IsNullOrWhiteSpace(assetId))
            assetId = GenerateAssetId(assetType, _titleEntry.Text);
        record = new NewArrivalRecord { Id = productId, ProductId = productId, AssetId = assetId, StoreTypeId = assetType.ToString(), OwnerScope = StoreProductAssetTypeCatalog.GetOwnerScope(assetType).ToString(), Title = _titleEntry.Text?.Trim() ?? string.Empty, Description = _descriptionEditor.Text?.Trim() ?? string.Empty, ButtonText = "ط¹ط±ط¶", ImagePath = _imageEntry.Text?.Trim() ?? string.Empty, ColorHex = _definition.IsTeamColor ? _colorHexEntry.Text?.Trim() ?? string.Empty : string.Empty, Category = _categoryPicker.SelectedCanonicalId(), Price = currency == NewArrivalCurrencyType.Free ? 0 : price, CurrencyType = currency, IsFree = currency == NewArrivalCurrencyType.Free || price == 0, EffectType = _effectTypePicker.SelectedCanonicalId(), AnimationType = _animationTypePicker.SelectedCanonicalId(), DurationMilliseconds = duration, EquipTarget = _equipTargetPicker.SelectedCanonicalId(), BundleAssetIds = bundleAssets, DiscountPercent = Math.Clamp(discount, 0, 100), Status = _editingPublished ? NewArrivalStatus.Published : NewArrivalStatus.Draft, CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow, PublishedAt = _currentRecord?.PublishedAt };
        _validationLabel.IsVisible = false;
        return true;
    }

    private async Task SelectRecordAsync(bool published)
    {
        var records = published
            ? await NewArrivalsAdminService.LoadManagedAsync()
            : await NewArrivalsAdminService.LoadAllDraftsAsync();

        var scoped = records
            .Where(record => _definition.AllowedTypes.Any(type =>
                string.Equals(record.StoreTypeId, type.ToString(), StringComparison.Ordinal)))
            .ToList();

        if (scoped.Count == 0)
        {
            await DisplayAlertAsync(_definition.Title, "ظ„ط§ طھظˆط¬ط¯ ط¹ظ†ط§طµط±", "ط­ط³ظ†ط§ظ‹");
            return;
        }

        var record =
            await DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetPickerSheet.ShowAsync(
                this,
                scoped,
                _definition.Title);

        if (record == null)
            return;
        var action =
            await DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsSheet.ShowAsync(
                this,
                record,
                published,
                _definition.Title);

        if (action == DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsAction.Edit)
            await PopulateAsync(record, published);
        else if (action == DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsAction.Hide)
            await NewArrivalsAdminService.HidePublishedAsync(record.AssetId);
        else if (action == DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsAction.Restore)
            await NewArrivalsAdminService.RestorePublishedAsync(record.AssetId);
        else if (action == DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsAction.DeleteDraft)
            await NewArrivalsAdminService.DeleteDraftAsync(record.AssetId);
        else if (action == DominoMajlisPRO.GalleryEngine.Admin.Components.AdminAssetDetailsAction.DeletePublished)
            await NewArrivalsAdminService.DeletePublishedAsync(record.AssetId);
    }

    private async Task PopulateAsync(NewArrivalRecord record, bool published)
    {
        _currentRecord = record; _editingPublished = published;
        _titleEntry.Text = record.Title; _descriptionEditor.Text = record.Description; _imageEntry.Text = record.ImagePath; _assetTypePicker.SelectCanonicalId(record.StoreTypeId); _priceEntry.Text = record.Price.ToString(); _currencyPicker.SelectCanonicalId(record.CurrencyType.ToString()); _colorHexEntry.Text = record.ColorHex;
        _effectTypePicker.SelectCanonicalId(record.EffectType); _animationTypePicker.SelectCanonicalId(record.AnimationType); _durationEntry.Text = record.DurationMilliseconds.ToString(); _equipTargetPicker.SelectCanonicalId(record.EquipTarget); _bundleAssetsEditor.Text = string.Join(Environment.NewLine, record.BundleAssetIds ?? new List<string>()); _discountEntry.Text = record.DiscountPercent.ToString();
        if (!string.IsNullOrWhiteSpace(record.ImagePath)) { _previewImage.Source = InventoryDisplayResolver.ResolveOptionalImageSource(record.ImagePath); _previewImage.IsVisible = _previewImage.Source != null; }
        await LoadAssetChoicesAsync();
        _assetIdPicker.SelectedIndex = _assetChoices.FindIndex(asset => string.Equals(asset.AssetId, record.AssetId, StringComparison.OrdinalIgnoreCase));
        _categoryPicker.SelectCanonicalId(record.Category == "Ass" ? "Avatar" : record.Category);
        SetMode(published ? "Editing Published" : "Editing Draft", published ? "طھط¹ط¯ظٹظ„ ط£طµظ„ ظ…ظ†ط´ظˆط±" : "ط§ط³طھظƒظ…ط§ظ„ ط§ظ„ظ…ط³ظˆط¯ط©", published ? Color.FromArgb("#27AE60") : Color.FromArgb("#9B51E0"));
    }

    private void ClearFields()
    {
        _currentRecord = null; _editingPublished = false;
        _titleEntry.Text = _descriptionEditor.Text = _imageEntry.Text = _priceEntry.Text = _durationEntry.Text = _bundleAssetsEditor.Text = _discountEntry.Text = _colorHexEntry.Text = string.Empty;
        _assetIdPicker.SelectedIndex = -1;
        _previewImage.Source = null; _previewImage.IsVisible = false; _categoryPicker.SelectCanonicalId(_definition.AllowedTypes[0].ToString()); _currencyPicker.SelectCanonicalId("Gems");
        if (_definition.AllowedTypes.Count == 1) _assetTypePicker.SelectedIndex = 0; else _assetTypePicker.SelectedIndex = -1;
        _effectTypePicker.SelectedIndex = _animationTypePicker.SelectedIndex = _equipTargetPicker.SelectedIndex = -1;
        _validationLabel.IsVisible = false;
        SetMode("New Product", "ط¬ط§ظ‡ط² ظ„ط¥ط¶ط§ظپط© ط£طµظ„ ط¬ط¯ظٹط¯", Color.FromArgb("#2F80ED"));
    }

    private static bool ValidBundleComponent(string value)
    {
        var parts = value.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && StoreProductAssetTypeCatalog.TryResolve(parts[0], out _) && !string.IsNullOrWhiteSpace(parts[1]);
    }

    private static string GenerateAssetId(
        StoreProductAssetType assetType,
        string? title)
    {
        var slug = new string((title ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(character =>
                char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        slug = slug.Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "asset";

        var typeSlug = assetType switch
        {
            StoreProductAssetType.ProfileBackground => "profile-background",
            StoreProductAssetType.EmblemBackground => "emblem-background",
            StoreProductAssetType.TeamColor => "team-color",
            _ => assetType.ToString().ToLowerInvariant()
        };
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{typeSlug}-{slug}-{suffix}";
    }

    private string SelectedAssetId() => _assetIdPicker.SelectedIndex >= 0 && _assetIdPicker.SelectedIndex < _assetChoices.Count ? _assetChoices[_assetIdPicker.SelectedIndex].AssetId : string.Empty;
    private void ShowError(string message) { _validationLabel.Text = message; _validationLabel.IsVisible = true; }
    private void SetMode(string title, string subtitle, Color color) { _modeTitle.Text = title; _modeSubtitle.Text = subtitle; _modeDot.Color = color; }
    private void OnThemeChanged(object? sender, GalleryTheme theme) => ApplyTheme();

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        _modeCard.Background = _formPanel.Background = theme.ActionBackground; _modeCard.Stroke = _formPanel.Stroke = theme.Stroke; _modeTitle.TextColor = theme.TextPrimary; _modeSubtitle.TextColor = theme.TextMuted; _validationLabel.TextColor = Color.FromArgb("#D84A4A");
        foreach (var entry in new[] { _titleEntry, _imageEntry, _priceEntry, _durationEntry, _discountEntry, _colorHexEntry }) { entry.TextColor = theme.TextPrimary; entry.PlaceholderColor = theme.TextMuted; }
        foreach (var editor in new[] { _descriptionEditor, _bundleAssetsEditor }) { editor.TextColor = theme.TextPrimary; editor.PlaceholderColor = theme.TextMuted; }
        foreach (var picker in new[] { _assetIdPicker, _assetTypePicker, _categoryPicker, _currencyPicker, _effectTypePicker, _animationTypePicker, _equipTargetPicker }) { picker.TextColor = theme.TextPrimary; picker.TitleColor = theme.TextMuted; }
    }

    private static Border Panel() => new() { Padding = 12, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 } };
    private static Button ActionButton(string text, Func<Task> action) { var button = new Button { Text = text }; button.Clicked += async (_, _) => await action(); return button; }
}

public sealed record SpecializedStoreManagerDefinition(string Title, string Subtitle, IReadOnlyList<StoreProductAssetType> AllowedTypes, bool IsEffect = false, bool IsBundle = false, bool IsTeamColor = false)
{
    public static SpecializedStoreManagerDefinition Emblems { get; } = new("ط§ظ„ط´ط¹ط§ط±ط§طھ", "ظ†ط´ط± ط´ط¹ط§ط±ط§طھ ط§ظ„ظپط±ظ‚", new[] { StoreProductAssetType.Emblem });
    public static SpecializedStoreManagerDefinition EmblemBackgrounds { get; } = new("ط®ظ„ظپظٹط§طھ ط§ظ„ط´ط¹ط§ط±ط§طھ", "ظ†ط´ط± ط®ظ„ظپظٹط§طھ ظ‡ظˆظٹط© ط´ط¹ط§ط±ط§طھ ط§ظ„ظپط±ظ‚", new[] { StoreProductAssetType.EmblemBackground });
    public static SpecializedStoreManagerDefinition Effects { get; } = new("ط§ظ„طھط£ط«ظٹط±ط§طھ", "ظ†ط´ط± ظ…ط¤ط«ط±ط§طھ ط§ظ„ظ„ط§ط¹ط¨ ط§ظ„ظ‚ط§ط¨ظ„ط© ظ„ظ„طھط¬ظ‡ظٹط²", new[] { StoreProductAssetType.Effect }, IsEffect: true);
    public static SpecializedStoreManagerDefinition Frames { get; } = new("ط§ظ„ط¥ط·ط§ط±ط§طھ", "ظ†ط´ط± ط¥ط·ط§ط±ط§طھ ظ‡ظˆظٹط© ط§ظ„ظ„ط§ط¹ط¨", new[] { StoreProductAssetType.Frame });
    public static SpecializedStoreManagerDefinition Titles { get; } = new("ط§ظ„ط£ظ„ظ‚ط§ط¨", "ظ†ط´ط± ط£ظ„ظ‚ط§ط¨ ظ‡ظˆظٹط© ط§ظ„ظ„ط§ط¹ط¨", new[] { StoreProductAssetType.Title });
    public static SpecializedStoreManagerDefinition Bundles { get; } = new("ط§ظ„ط­ط²ظ…", "ظ†ط´ط± ط­ط²ظ… ظ…طھط¹ط¯ط¯ط© ط§ظ„ط£طµظˆظ„", new[] { StoreProductAssetType.Bundle }, IsBundle: true);
    public static SpecializedStoreManagerDefinition TeamColors { get; } = new("ط£ظ„ظˆط§ظ† ط§ظ„ظپط±ظ‚", "ظ†ط´ط± ط£ظ„ظˆط§ظ† ظ‡ظˆظٹط© ط§ظ„ظپط±ظ‚", new[] { StoreProductAssetType.TeamColor }, IsTeamColor: true);
}


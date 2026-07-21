using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class TypographyManagerPage : ContentPage
{
    private readonly Entry _titleEntry = new() { Placeholder = "العنوان" };
    private readonly Editor _descriptionEditor = new() { Placeholder = "الوصف", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 72 };
    private readonly Picker _assetTypePicker = new() { Title = "نوع الأصل" };
    private readonly Picker _categoryPicker = new() { Title = "التصنيف" };
    private readonly Entry _priceEntry = new() { Placeholder = "السعر", Keyboard = Keyboard.Numeric };
    private readonly Picker _currencyPicker = new() { Title = "العملة" };
    private readonly Picker _equipTargetPicker = new() { Title = "هدف التجهيز" };
    private readonly Picker _fontPicker = new() { Title = "الخط" };
    private readonly Slider _fontSizeSlider = new() { Minimum = 12, Maximum = 34, Value = 18 };
    private readonly Picker _materialPicker = new() { Title = "Material" };
    private readonly Picker _lightingPicker = new() { Title = "Lighting" };
    private readonly Picker _depthPicker = new() { Title = "Depth" };
    private readonly Picker _motionPicker = new() { Title = "Motion" };
    private readonly Picker _particlePicker = new() { Title = "Particles" };
    private readonly Picker _distortionPicker = new() { Title = "Distortion" };
    private readonly Picker _framePicker = new() { Title = "Frame style" };
    private readonly Slider _thicknessSlider = new() { Minimum = 0.8, Maximum = 4, Value = 1.4 };
    private readonly Entry _primaryColorEntry = new() { Placeholder = "#FFD76A", Text = "#FFD76A" };
    private readonly Entry _secondaryColorEntry = new() { Placeholder = "#2A1B08", Text = "#2A1B08" };
    private readonly Slider _opacitySlider = new() { Minimum = 0.35, Maximum = 1, Value = 1 };
    private readonly Slider _scaleSlider = new() { Minimum = 0.8, Maximum = 1.35, Value = 1 };
    private readonly Slider _speedSlider = new() { Minimum = 0.5, Maximum = 2, Value = 1 };
    private readonly Slider _intensitySlider = new() { Minimum = 0.2, Maximum = 1.6, Value = 1 };
    private readonly Slider _metalnessSlider = UnitSlider(0.65);
    private readonly Slider _roughnessSlider = UnitSlider(0.28);
    private readonly Slider _specularSlider = UnitSlider(0.72);
    private readonly Slider _glossSlider = UnitSlider(0.62);
    private readonly Slider _reflectionSlider = UnitSlider(0.55);
    private readonly Slider _depthAmountSlider = UnitSlider(0.35);
    private readonly Slider _brightnessSlider = UnitSlider(0.68);
    private readonly IdentityPlateView _playerPreview = new() { HeightRequest = 44, MaximumWidthRequest = 360, RenderingContext = NameSurfaceRenderingContext.DeveloperPreview };
    private readonly IdentityPlateView _teamPreview = new() { HeightRequest = 44, MaximumWidthRequest = 360, RenderingContext = NameSurfaceRenderingContext.DeveloperPreview };
    private readonly Label _validationLabel = new() { TextColor = Color.FromArgb("#FF6B6B"), FontSize = 12, IsVisible = false, HorizontalTextAlignment = TextAlignment.End };
    private NewArrivalRecord? _currentRecord;
    private bool _editingPublished;

    public TypographyManagerPage()
    {
        Title = "أسماء و إطارات الهوية";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#050505");
        NavigationPage.SetHasNavigationBar(this, false);
        ConfigureControls();
        BuildPage();
        RefreshPreview();
    }

    private void ConfigureControls()
    {
        SetPicker(_assetTypePicker, new[]
        {
            StoreProductAssetType.PlayerNameEffect.ToString(),
            StoreProductAssetType.TeamNameEffect.ToString(),
            StoreProductAssetType.PlayerNameFrame.ToString(),
            StoreProductAssetType.TeamNameFrame.ToString()
        });
        SetPicker(_categoryPicker, new[]
        {
            "PlayerNameEffect", "TeamNameEffect", "PlayerNameFrame", "TeamNameFrame"
        });
        SetPicker(_currencyPicker, Enum.GetNames<NewArrivalCurrencyType>());
        SetPicker(_equipTargetPicker, new[] { "PlayerName", "TeamName" });
        SetPicker(_fontPicker, TypographyFontCatalog.FontFamilies);
        SetPicker(_materialPicker, TypographyPresetCatalog.Materials);
        SetPicker(_lightingPicker, TypographyPresetCatalog.Lighting);
        SetPicker(_depthPicker, TypographyPresetCatalog.Depth);
        SetPicker(_motionPicker, TypographyPresetCatalog.Motion);
        SetPicker(_particlePicker, TypographyPresetCatalog.Particles);
        SetPicker(_distortionPicker, TypographyPresetCatalog.Distortions);
        SetPicker(_framePicker, TypographyPresetCatalog.Frames);

        _assetTypePicker.SelectedIndex = 0;
        _categoryPicker.SelectedIndex = 0;
        _currencyPicker.SelectedIndex = 1;
        _equipTargetPicker.SelectedIndex = 0;
        _fontPicker.SelectedItem = TypographyFontCatalog.DefaultFontFamily;
        _materialPicker.SelectedIndex = 0;
        _lightingPicker.SelectedIndex = 0;
        _depthPicker.SelectedIndex = 1;
        _motionPicker.SelectedIndex = 0;
        _particlePicker.SelectedIndex = 0;
        _distortionPicker.SelectedIndex = 0;
        _framePicker.SelectedIndex = 0;

        _assetTypePicker.SelectedIndexChanged += (_, _) => SyncEquipTarget();
        _categoryPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        foreach (var picker in new[] { _fontPicker, _materialPicker, _lightingPicker, _depthPicker, _motionPicker, _particlePicker, _distortionPicker, _framePicker })
            picker.SelectedIndexChanged += (_, _) => RefreshPreview();
        foreach (var slider in new[] { _fontSizeSlider, _thicknessSlider, _opacitySlider, _scaleSlider, _speedSlider, _intensitySlider, _metalnessSlider, _roughnessSlider, _specularSlider, _glossSlider, _reflectionSlider, _depthAmountSlider, _brightnessSlider })
            slider.ValueChanged += (_, _) => RefreshPreview();
        _primaryColorEntry.TextChanged += (_, _) => RefreshPreview();
        _secondaryColorEntry.TextChanged += (_, _) => RefreshPreview();
        _titleEntry.TextChanged += (_, _) => RefreshPreview();
        _descriptionEditor.TextChanged += (_, _) => RefreshPreview();
        ApplyInputColors();
    }

    private void BuildPage()
    {
        var back = new Button { Text = "‹", FontSize = 26, WidthRequest = 44, HeightRequest = 44, Padding = 0 };
        back.Clicked += async (_, _) => await Navigation.PopAsync();

        var heading = new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = "أسماء و إطارات الهوية", FontFamily = "Tajawal-Regular", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.End },
                new Label { Text = "نشر تأثيرات وإطارات أسماء اللاعبين والفرق", FontFamily = "Tajawal-Regular", FontSize = 12, TextColor = Color.FromArgb("#B8A77D"), HorizontalTextAlignment = TextAlignment.End }
            }
        };

        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12
        };
        header.Add(back, 0, 0);
        header.Add(heading, 1, 0);

        var preview = Panel(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "معاينة مباشرة", FontFamily = "Tajawal-Regular", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD76A"), HorizontalTextAlignment = TextAlignment.End },
                _playerPreview,
                _teamPreview
            }
        });

        var form = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                _titleEntry,
                _descriptionEditor,
                _assetTypePicker,
                _categoryPicker,
                Two(_priceEntry, _currencyPicker),
                _equipTargetPicker,
                _fontPicker,
                Labeled("حجم الخط", _fontSizeSlider),
                _materialPicker,
                _lightingPicker,
                _depthPicker,
                _motionPicker,
                _particlePicker,
                _distortionPicker,
                _framePicker,
                Labeled("سماكة / كثافة الإطار", _thicknessSlider),
                Two(_primaryColorEntry, _secondaryColorEntry),
                Labeled("الشفافية", _opacitySlider),
                Labeled("الحجم", _scaleSlider),
                Labeled("السرعة", _speedSlider),
                Labeled("الكثافة", _intensitySlider),
                Labeled("المعدنية", _metalnessSlider),
                Labeled("الخشونة", _roughnessSlider),
                Labeled("الانعكاس اللامع", _specularSlider),
                Labeled("اللمعان", _glossSlider),
                Labeled("الانعكاس", _reflectionSlider),
                Labeled("العمق", _depthAmountSlider),
                Labeled("الإضاءة", _brightnessSlider),
                _validationLabel
            }
        };

        var actions = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            RowSpacing = 10
        };
        actions.Add(ActionButton("حفظ كمسودة", async () => await SaveAsync(false)), 0, 0);
        actions.Add(ActionButton("نشر", async () => await SaveAsync(true)), 1, 0);
        actions.Add(ActionButton("المنشور", async () => await SelectRecordAsync(true)), 0, 1);
        actions.Add(ActionButton("المسودات", async () => await SelectRecordAsync(false)), 1, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children = { header, preview, Panel(form), actions }
            }
        };
    }

    private async Task SaveAsync(bool publish)
    {
        if (!TryBuildRecord(publish, out var record))
            return;

        if (publish && !NewArrivalsAdminService.ValidateForPublish(record, out var message))
        {
            ShowError(message);
            return;
        }

        _currentRecord = publish
            ? _editingPublished
                ? await NewArrivalsAdminService.UpdatePublishedAsync(record)
                : await NewArrivalsAdminService.PublishAsync(record)
            : await NewArrivalsAdminService.SaveDraftAsync(record);
        _editingPublished = publish;
        _validationLabel.IsVisible = false;
        await DisplayAlertAsync(Title, publish ? "تم النشر" : "تم حفظ المسودة", "حسناً");
    }

    private bool TryBuildRecord(bool publish, out NewArrivalRecord record)
    {
        record = new NewArrivalRecord();
        if (!StoreProductAssetTypeCatalog.TryResolve(Selected(_assetTypePicker), out var type))
        {
            ShowError("نوع الأصل مطلوب.");
            return false;
        }
        if (!TypographyFontCatalog.FontFamilies.Contains(Selected(_fontPicker), StringComparer.Ordinal))
        {
            ShowError("اختر خطاً من الكتالوج فقط.");
            return false;
        }

        var expectedTarget = type is StoreProductAssetType.PlayerNameEffect or StoreProductAssetType.PlayerNameFrame
            ? "PlayerName"
            : "TeamName";
        if (!string.Equals(Selected(_equipTargetPicker), expectedTarget, StringComparison.Ordinal))
        {
            ShowError("هدف التجهيز لا يطابق نوع الأصل.");
            return false;
        }

        if (publish && string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            ShowError("العنوان مطلوب.");
            return false;
        }

        _ = int.TryParse(_priceEntry.Text, out var price);
        var currency = Enum.TryParse<NewArrivalCurrencyType>(Selected(_currencyPicker), out var parsed)
            ? parsed
            : NewArrivalCurrencyType.Gems;
        var productId = _currentRecord?.ProductId;
        if (string.IsNullOrWhiteSpace(productId))
            productId = Guid.NewGuid().ToString();

        record = new NewArrivalRecord
        {
            Id = productId,
            ProductId = productId,
            AssetId = _currentRecord?.AssetId ?? GenerateAssetId(type, _titleEntry.Text),
            StoreTypeId = type.ToString(),
            OwnerScope = StoreProductAssetTypeCatalog.GetOwnerScope(type).ToString(),
            Title = _titleEntry.Text?.Trim() ?? string.Empty,
            Description = _descriptionEditor.Text?.Trim() ?? string.Empty,
            ButtonText = "عرض",
            ImagePath = string.Empty,
            Category = Selected(_categoryPicker),
            Price = currency == NewArrivalCurrencyType.Free ? 0 : Math.Max(0, price),
            CurrencyType = currency,
            IsFree = currency == NewArrivalCurrencyType.Free || price <= 0,
            EquipTarget = expectedTarget,
            TypographyPreset = BuildPreset(type),
            Status = publish ? NewArrivalStatus.Published : NewArrivalStatus.Draft,
            CreatedAt = _currentRecord?.CreatedAt ?? DateTime.UtcNow,
            PublishedAt = publish ? DateTime.UtcNow : _currentRecord?.PublishedAt
        };
        return true;
    }

    private async Task SelectRecordAsync(bool published)
    {
        var allowed = Canonical.StoreManagerAssetTypeScopes.ForSection("name-effects");
        var records = (published
                ? await NewArrivalsAdminService.LoadManagedAsync()
                : await NewArrivalsAdminService.LoadAllDraftsAsync())
            .Where(record => allowed.Contains(record.StoreTypeId, StringComparer.Ordinal))
            .ToList();
        if (records.Count == 0)
        {
            await DisplayAlertAsync(Title, "لا توجد عناصر.", "حسناً");
            return;
        }
        var selected = await DisplayActionSheetAsync("اختيار عنصر", "إلغاء", null, records.Select(record => record.Title).ToArray());
        var record = records.FirstOrDefault(item => item.Title == selected);
        if (record == null)
            return;
        Populate(record, published);
    }

    private void Populate(NewArrivalRecord record, bool published)
    {
        _currentRecord = record;
        _editingPublished = published;
        _titleEntry.Text = record.Title;
        _descriptionEditor.Text = record.Description;
        Select(_assetTypePicker, record.StoreTypeId);
        Select(_categoryPicker, record.Category);
        Select(_currencyPicker, record.CurrencyType.ToString());
        _priceEntry.Text = record.Price.ToString();
        Select(_equipTargetPicker, record.EquipTarget);
        ApplyPreset(record.TypographyPreset);
    }

    private TypographyIdentityPreset BuildPreset() =>
        BuildPreset(ResolveSelectedAssetType());

    private TypographyIdentityPreset BuildPreset(StoreProductAssetType? assetType)
    {
        var frameStyle = Selected(_framePicker);
        var lighting = Selected(_lightingPicker);
        var motion = Selected(_motionPicker);
        var particles = Selected(_particlePicker);
        var distortion = Selected(_distortionPicker);
        if (assetType is StoreProductAssetType.PlayerNameEffect or
            StoreProductAssetType.TeamNameEffect)
        {
            frameStyle = "None";
            if (IsNone(motion) && IsNone(particles) && IsNone(distortion))
            {
                lighting = "MovingHighlight";
                motion = "Breath";
                particles = "TinySparks";
            }
        }

        return new TypographyIdentityPreset
        {
            FontFamily = Selected(_fontPicker),
            FontSize = _fontSizeSlider.Value,
            MaterialPreset = Selected(_materialPicker),
            LightingPreset = lighting,
            DepthPreset = Selected(_depthPicker),
            MotionPreset = motion,
            ParticlePreset = particles,
            DistortionPreset = distortion,
            FrameStylePreset = frameStyle,
            FrameThickness = _thicknessSlider.Value,
            PrimaryColor = _primaryColorEntry.Text ?? "#FFD76A",
            SecondaryColor = _secondaryColorEntry.Text ?? "#2A1B08",
            Opacity = _opacitySlider.Value,
            Scale = _scaleSlider.Value,
            Speed = _speedSlider.Value,
            Intensity = _intensitySlider.Value,
            Metalness = _metalnessSlider.Value,
            Roughness = _roughnessSlider.Value,
            Specular = _specularSlider.Value,
            Gloss = _glossSlider.Value,
            Reflection = _reflectionSlider.Value,
            Depth = _depthAmountSlider.Value,
            Brightness = _brightnessSlider.Value
        }.Normalized();
    }

    private static bool IsNone(string? value) =>
        string.Equals(value?.Trim(), "None", StringComparison.OrdinalIgnoreCase);

    private void ApplyPreset(TypographyIdentityPreset? source)
    {
        var preset = (source ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        Select(_fontPicker, preset.FontFamily);
        _fontSizeSlider.Value = preset.FontSize;
        Select(_materialPicker, preset.MaterialPreset);
        Select(_lightingPicker, preset.LightingPreset);
        Select(_depthPicker, preset.DepthPreset);
        Select(_motionPicker, preset.MotionPreset);
        Select(_particlePicker, preset.ParticlePreset);
        Select(_distortionPicker, preset.DistortionPreset);
        Select(_framePicker, preset.FrameStylePreset);
        _thicknessSlider.Value = preset.FrameThickness;
        _primaryColorEntry.Text = preset.PrimaryColor;
        _secondaryColorEntry.Text = preset.SecondaryColor;
        _opacitySlider.Value = preset.Opacity;
        _scaleSlider.Value = preset.Scale;
        _speedSlider.Value = preset.Speed;
        _intensitySlider.Value = preset.Intensity;
        _metalnessSlider.Value = preset.Metalness;
        _roughnessSlider.Value = preset.Roughness;
        _specularSlider.Value = preset.Specular;
        _glossSlider.Value = preset.Gloss;
        _reflectionSlider.Value = preset.Reflection;
        _depthAmountSlider.Value = preset.Depth;
        _brightnessSlider.Value = preset.Brightness;
        RefreshPreview();
    }

    private void SyncEquipTarget()
    {
        var selected = Selected(_assetTypePicker);
        if (selected.StartsWith("Team", StringComparison.Ordinal))
        {
            Select(_equipTargetPicker, "TeamName");
            if (!Selected(_categoryPicker).StartsWith("Team", StringComparison.Ordinal))
                Select(_categoryPicker, selected);
        }
        else
        {
            Select(_equipTargetPicker, "PlayerName");
            if (!Selected(_categoryPicker).StartsWith("Player", StringComparison.Ordinal))
                Select(_categoryPicker, selected);
        }
        if (selected is nameof(StoreProductAssetType.PlayerNameEffect) or
            nameof(StoreProductAssetType.TeamNameEffect))
            Select(_framePicker, "None");
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_titleEntry != null)
        {
            var type = ResolveSelectedAssetType();
            var livePreset = BuildPreset(type);
            var title = string.IsNullOrWhiteSpace(_titleEntry.Text)
                ? "العنوان"
                : _titleEntry.Text.Trim();
        var isTeam = type is StoreProductAssetType.TeamNameEffect or
            StoreProductAssetType.TeamNameFrame;
        _playerPreview.IsVisible = true;
        _teamPreview.IsVisible = true;
        _playerPreview.Opacity = isTeam ? 0.46 : 1;
        _teamPreview.Opacity = isTeam ? 1 : 0.46;
        _playerPreview.Bind(isTeam ? "Abdulla" : title, livePreset);
        _teamPreview.Bind(isTeam ? title : "Alosh Team", livePreset);
        return;
        }
        var preset = BuildPreset();
        _playerPreview.Bind("اللاعب الذهبي الطويل", preset);
        _teamPreview.Bind("فريق المجالس الملكية", preset);
    }

    private StoreProductAssetType? ResolveSelectedAssetType() =>
        StoreProductAssetTypeCatalog.TryResolve(
            Selected(_assetTypePicker),
            out var type)
            ? type
            : null;

    private void ApplyInputColors()
    {
        var text = Color.FromArgb("#FFF4D2");
        var muted = Color.FromArgb("#8F7A55");
        foreach (var entry in new[]
                 { _titleEntry, _priceEntry, _primaryColorEntry, _secondaryColorEntry })
        {
            entry.TextColor = text;
            entry.PlaceholderColor = muted;
        }

        _descriptionEditor.TextColor = text;
        _descriptionEditor.PlaceholderColor = muted;
        foreach (var picker in new[]
                 {
                     _assetTypePicker, _categoryPicker, _currencyPicker,
                     _equipTargetPicker, _fontPicker, _materialPicker,
                     _lightingPicker, _depthPicker, _motionPicker,
                     _particlePicker, _distortionPicker, _framePicker
                 })
        {
            picker.TextColor = text;
            picker.TitleColor = muted;
        }
    }

    private static Border Panel(View content) => new()
    {
        Padding = 12,
        Stroke = Color.FromArgb("#4A3A17"),
        Background = Color.FromArgb("#12100A"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Content = content
    };

    private static Grid Two(View left, View right)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        grid.Add(left, 0, 0);
        grid.Add(right, 1, 0);
        return grid;
    }

    private static VerticalStackLayout Labeled(string title, View control) => new()
    {
        Spacing = 3,
        Children =
        {
            new Label { Text = title, FontFamily = "Tajawal-Regular", FontSize = 11, TextColor = Color.FromArgb("#B8A77D"), HorizontalTextAlignment = TextAlignment.End },
            control
        }
    };

    private static Slider UnitSlider(double value) => new()
    {
        Minimum = 0,
        Maximum = 1,
        Value = value
    };

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, FontFamily = "Tajawal-Regular" };
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private static void SetPicker(Picker picker, IEnumerable<string> values) =>
        picker.ItemsSource = values.ToList();

    private static string Selected(Picker picker) =>
        picker.SelectedItem?.ToString() ?? string.Empty;

    private static void Select(Picker picker, string? value)
    {
        var items = picker.ItemsSource?.Cast<object>().Select(item => item.ToString()).ToList() ?? new List<string?>();
        var index = items.FindIndex(item => string.Equals(item, value, StringComparison.Ordinal));
        picker.SelectedIndex = index;
    }

    private void ShowError(string message)
    {
        _validationLabel.Text = message;
        _validationLabel.IsVisible = true;
    }

    private static string GenerateAssetId(StoreProductAssetType assetType, string? title)
    {
        var seed = new string((title ?? "name-asset").Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(seed))
            seed = "name-asset";
        return $"{assetType.ToString().ToLowerInvariant()}-{seed.ToLowerInvariant()}-{Guid.NewGuid():N}"[..Math.Min(54, $"{assetType.ToString().ToLowerInvariant()}-{seed.ToLowerInvariant()}-{Guid.NewGuid():N}".Length)];
    }
}

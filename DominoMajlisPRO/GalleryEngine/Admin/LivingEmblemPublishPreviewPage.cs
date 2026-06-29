using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.LivingVisualPlatform.Controls;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class LivingEmblemPublishPreviewPage : ContentPage
{
    private readonly LivingEmblemImporter _importer = new();
    private readonly Entry _packagePathEntry = new()
    {
        Placeholder = "Living Emblem package path",
        Text = LivingEmblemPackagePaths.DefaultProductionPackagePath
    };
    private readonly Entry _titleEntry = new()
    {
        Placeholder = "Living emblem name",
        Text = "Living Emblem Package"
    };
    private readonly Editor _descriptionEditor = new()
    {
        Placeholder = "Description",
        Text = "Validated Filament GLB package preview before publishing",
        AutoSize = EditorAutoSizeOption.TextChanges,
        HeightRequest = 80
    };
    private readonly Entry _fallbackEntry = new()
    {
        Placeholder = "Fallback thumbnail PNG",
        Text = "LivingEmblems/production_default/fallback.png"
    };
    private readonly Entry _priceEntry = new() { Placeholder = "Price", Text = "0", Keyboard = Keyboard.Numeric };
    private readonly Picker _currencyPicker = new() { Title = "Currency" };
    private readonly ContentView _previewHost = new() { HeightRequest = 220 };
    private readonly Label _statusLabel = new() { FontSize = 12, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _diagnosticsLabel = new() { FontSize = 11, HorizontalTextAlignment = TextAlignment.Start };
    private readonly Label _validationLabel = new() { FontSize = 12, IsVisible = false, HorizontalTextAlignment = TextAlignment.End };
    private LivingEmblemPackage? _validatedPackage;
    private bool _previewGenerated;

    public LivingEmblemPublishPreviewPage()
    {
        Title = "Living Emblem Package";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#030303");
        NavigationPage.SetHasNavigationBar(this, false);
        _currencyPicker.ItemsSource = Enum.GetNames(typeof(NewArrivalCurrencyType)).ToList();
        _currencyPicker.SelectedItem = NewArrivalCurrencyType.Free.ToString();
        BuildPage();
        ResetPreview();
    }

    private void BuildPage()
    {
        var back = Button("Back", async () => await Navigation.PopAsync());
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        header.Add(back, 0);
        header.Add(new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = "Living Emblem Package", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD966"), HorizontalTextAlignment = TextAlignment.End },
                new Label { Text = "Import Validation Tool", FontSize = 12, TextColor = Color.FromArgb("#A88E45"), HorizontalTextAlignment = TextAlignment.End }
            }
        }, 1);

        var priceRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        priceRow.Add(_priceEntry, 0);
        priceRow.Add(_currencyPicker, 1);

        var previewCard = Panel(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Production living preview", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD966"), HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "This card renders the validated package through LivingVisualHost and Filament.", FontSize = 11, TextColor = Color.FromArgb("#A88E45"), HorizontalTextAlignment = TextAlignment.Center },
                _previewHost,
                _statusLabel
            }
        });

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(Button("Validate Package", ValidatePackageAsync), 0);
        actions.Add(Button("Generate Filament Preview", GeneratePreviewAsync), 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    header,
                    Panel(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            _packagePathEntry,
                            _titleEntry,
                            _descriptionEditor,
                            new Label { Text = "Fallback image is thumbnail/fallback metadata only. It is not the living emblem.", FontSize = 11, TextColor = Color.FromArgb("#8C7A3E"), HorizontalTextAlignment = TextAlignment.End },
                            _fallbackEntry,
                            priceRow
                        }
                    }),
                    previewCard,
                    _diagnosticsLabel,
                    _validationLabel,
                    actions,
                    Button("Publish approved package", PublishAsync)
                }
            }
        };
    }

    private void ResetPreview()
    {
        _previewHost.Content = new Label
        {
            Text = "Validate a package, then generate the Filament preview",
            TextColor = Color.FromArgb("#8C7A3E"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        _statusLabel.Text = "Import package | Validate manifest | Render through LivingVisualHost";
        _statusLabel.TextColor = Color.FromArgb("#A88E45");
        _diagnosticsLabel.Text = string.Empty;
    }

    private async Task ValidatePackageAsync()
    {
        _validationLabel.IsVisible = false;
        _previewGenerated = false;
        var result = await _importer.ImportAsync(_packagePathEntry.Text ?? string.Empty);
        LivingEmblemPackageValidator.ValidateProductionImport(result);
        _validatedPackage = result.Package;
        RenderDiagnostics(result);

        if (!result.IsValid || result.Package == null)
        {
            _statusLabel.Text = "Package rejected. Fix diagnostics before preview or publish.";
            _statusLabel.TextColor = Color.FromArgb("#D84A4A");
            _validatedPackage = null;
            return;
        }

        var package = result.Package;
        _titleEntry.Text = package.Manifest.DisplayName;
        _descriptionEditor.Text = string.IsNullOrWhiteSpace(package.Metadata.ArtStatus)
            ? "Validated Filament GLB package preview before publishing"
            : package.Metadata.ArtStatus;
        _fallbackEntry.Text = package.ResolvedFallbackPath;
        _statusLabel.Text =
            $"Package valid | {package.Manifest.PackageId} | {package.Manifest.Backend} | {package.ResolvedGlbPath}";
        _statusLabel.TextColor = Color.FromArgb("#27AE60");
    }

    private async Task GeneratePreviewAsync()
    {
        _validationLabel.IsVisible = false;
        var package = await EnsureValidatedPackageAsync();
        if (package == null)
            return;

        StoreCatalogLivingVisualManifestProvider.RegisterDeveloperPreviewPackage(package);
        _fallbackEntry.Text = package.ResolvedFallbackPath;
        _previewHost.Content = new LivingVisualHost
        {
            AssetId = package.Manifest.AssetId,
            StaticFallbackImage = package.ResolvedFallbackPath,
            ApplicationUserId = string.Empty,
            PlayerId = string.Empty,
            TeamId = string.Empty,
            DisplayLocation = LivingVisualDisplayLocation.StorePreview,
            IsDeveloperPreview = true,
            IsStorePreview = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        _previewGenerated = true;
        _statusLabel.Text = "Preview generated through LivingVisualHost from validated package metadata.";
        _statusLabel.TextColor = Color.FromArgb("#FFD966");
    }

    private async Task PublishAsync()
    {
        var package = await EnsureValidatedPackageAsync();
        if (package == null)
            return;

        if (!_previewGenerated)
        {
            ShowError("Generate and inspect the Filament preview before publishing.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            ShowError("Living emblem name is required.");
            return;
        }

        _ = int.TryParse(_priceEntry.Text, out var price);
        var currency = Enum.TryParse<NewArrivalCurrencyType>(_currencyPicker.SelectedItem?.ToString(), out var parsed)
            ? parsed
            : NewArrivalCurrencyType.Free;
        var productId = "product_" + SanitizeId(package.Manifest.PackageId);
        var record = new NewArrivalRecord
        {
            Id = productId,
            ProductId = productId,
            AssetId = package.Manifest.AssetId,
            StoreTypeId = StoreProductAssetType.TeamLivingEmblem.ToString(),
            OwnerScope = StoreProductOwnerScope.Player.ToString(),
            Title = _titleEntry.Text.Trim(),
            Subtitle = "Production Package Preview",
            Description = string.IsNullOrWhiteSpace(_descriptionEditor.Text)
                ? "Living Legendary Emblem package."
                : _descriptionEditor.Text.Trim(),
            ButtonText = "Preview",
            ImagePath = package.ResolvedFallbackPath,
            Category = StoreProductAssetType.TeamLivingEmblem.ToString(),
            EffectType = "LivingVisual",
            AnimationType = package.Behavior.ProfileId,
            EquipTarget = "TeamEmblem",
            LivingVisualScope = LivingVisualAssetScope.TeamEmblem.ToString(),
            LivingVisualKind = LivingVisualAssetKind.LivingLegendaryEmblem.ToString(),
            LivingPackageId = package.Manifest.PackageId,
            LivingPackageManifestPath = package.ManifestPath,
            LivingPackagePath = package.PackageRootPath,
            PreferredBackend = package.Manifest.Backend,
            FallbackPolicy = "StaticFallback",
            LivingVisualVersion = package.Manifest.Version,
            LivingPackageVersion = package.Manifest.Version,
            Rarity = "Legendary",
            Price = currency == NewArrivalCurrencyType.Free ? 0 : price,
            CurrencyType = currency,
            IsFree = currency == NewArrivalCurrencyType.Free || price == 0,
            IsFeatured = true,
            Status = NewArrivalStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await NewArrivalsAdminService.PublishAsync(record);
            _validationLabel.TextColor = Color.FromArgb("#27AE60");
            _validationLabel.Text = "Published package metadata. It will appear as an Emblem after acquisition.";
            _validationLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task<LivingEmblemPackage?> EnsureValidatedPackageAsync()
    {
        if (_validatedPackage != null)
            return _validatedPackage;

        await ValidatePackageAsync();
        if (_validatedPackage == null)
            ShowError("Import and validate a package before continuing.");
        return _validatedPackage;
    }

    private void RenderDiagnostics(LivingEmblemPackageImportResult result)
    {
        _diagnosticsLabel.Text = string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(item => $"{item.Severity}: {item.Code} - {item.Message}"));
        _diagnosticsLabel.TextColor = result.IsValid ? Color.FromArgb("#27AE60") : Color.FromArgb("#D84A4A");
    }

    private void ShowError(string message)
    {
        _validationLabel.TextColor = Color.FromArgb("#D84A4A");
        _validationLabel.Text = message;
        _validationLabel.IsVisible = true;
    }

    private static string SanitizeId(string value) =>
        string.Concat(value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')).Trim('_');

    private static Border Panel(View content) => new()
    {
        Padding = 12,
        StrokeThickness = 1,
        Stroke = Color.FromArgb("#5A4211"),
        Background = Color.FromArgb("#101010"),
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Content = content
    };

    private static Button Button(string text, Func<Task> action)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await action();
        return button;
    }
}

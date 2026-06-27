using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Controls;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class LivingEmblemPublishPreviewPage : ContentPage
{
    private const string DefaultFallbackImage = "shield_3d.png";
    private readonly Entry _titleEntry = new() { Placeholder = "Living emblem name", Text = "Living Filament Backend Probe" };
    private readonly Editor _descriptionEditor = new() { Placeholder = "Description", Text = "Temporary proof asset for the Living Visual Platform Filament backend.", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 80 };
    private readonly Entry _fallbackEntry = new() { Placeholder = "Fallback thumbnail PNG", Text = DefaultFallbackImage };
    private readonly Entry _priceEntry = new() { Placeholder = "Price", Text = "0", Keyboard = Keyboard.Numeric };
    private readonly Picker _currencyPicker = new() { Title = "Currency" };
    private readonly ContentView _previewHost = new() { HeightRequest = 220 };
    private readonly Label _statusLabel = new() { FontSize = 12, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _validationLabel = new() { FontSize = 12, IsVisible = false, HorizontalTextAlignment = TextAlignment.End };
    private bool _previewGenerated;

    public LivingEmblemPublishPreviewPage()
    {
        Title = "Living Emblems";
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
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        header.Add(back, 0);
        header.Add(new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = "Living Emblems", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD966"), HorizontalTextAlignment = TextAlignment.End },
                new Label { Text = "Production preview: LivingVisualHost -> Filament -> GLB.", FontSize = 12, TextColor = Color.FromArgb("#A88E45"), HorizontalTextAlignment = TextAlignment.End }
            }
        }, 1);

        var priceRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        priceRow.Add(_priceEntry, 0);
        priceRow.Add(_currencyPicker, 1);

        var previewCard = Panel(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Production living preview", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD966"), HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "This card uses the same LivingVisualHost path used by players. If it shows only PNG, Filament is not accepted yet.", FontSize = 11, TextColor = Color.FromArgb("#A88E45"), HorizontalTextAlignment = TextAlignment.Center },
                _previewHost,
                _statusLabel
            }
        });

        var actions = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        actions.Add(Button("Generate Filament preview", GeneratePreviewAsync), 0);
        actions.Add(Button("Publish approved preview", PublishAsync), 1);

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
                            _titleEntry,
                            _descriptionEditor,
                            new Label { Text = "Fallback image is thumbnail/fallback only. It is not the living emblem.", FontSize = 11, TextColor = Color.FromArgb("#8C7A3E"), HorizontalTextAlignment = TextAlignment.End },
                            _fallbackEntry,
                            priceRow
                        }
                    }),
                    previewCard,
                    _validationLabel,
                    actions
                }
            }
        };
    }

    private void ResetPreview()
    {
        _previewHost.Content = new Label { Text = "Generate the Filament preview before publishing", TextColor = Color.FromArgb("#8C7A3E"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        _statusLabel.Text = "AssetId auto | Backend Filament | GLB living_visual_backend_probe.glb";
        _statusLabel.TextColor = Color.FromArgb("#A88E45");
    }

    private Task GeneratePreviewAsync()
    {
        _validationLabel.IsVisible = false;
        var fallback = NormalizeFallbackImage(_fallbackEntry.Text);
        _fallbackEntry.Text = fallback;
        _previewHost.Content = new LivingVisualHost
        {
            AssetId = StoreAssetCatalogService.LivingFilamentBackendProbeAssetId,
            StaticFallbackImage = fallback,
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
        _statusLabel.Text = "Preview generated through LivingVisualHost. Approve only if Filament/GLB is visible, not fallback PNG.";
        _statusLabel.TextColor = Color.FromArgb("#FFD966");
        return Task.CompletedTask;
    }

    private async Task PublishAsync()
    {
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
        var currency = Enum.TryParse<NewArrivalCurrencyType>(_currencyPicker.SelectedItem?.ToString(), out var parsed) ? parsed : NewArrivalCurrencyType.Free;
        var fallback = NormalizeFallbackImage(_fallbackEntry.Text);
        _fallbackEntry.Text = fallback;
        var productId = "product_living_filament_backend_probe";
        var record = new NewArrivalRecord
        {
            Id = productId,
            ProductId = productId,
            AssetId = StoreAssetCatalogService.LivingFilamentBackendProbeAssetId,
            StoreTypeId = StoreProductAssetType.Emblem.ToString(),
            OwnerScope = StoreProductOwnerScope.Player.ToString(),
            Title = _titleEntry.Text.Trim(),
            Subtitle = "Living Emblems",
            Description = string.IsNullOrWhiteSpace(_descriptionEditor.Text) ? "Living Legendary Emblem." : _descriptionEditor.Text.Trim(),
            ButtonText = "Preview",
            ImagePath = fallback,
            Category = StoreProductAssetType.Emblem.ToString(),
            EffectType = "LivingVisual",
            AnimationType = "FilamentBackendProbe",
            EquipTarget = "TeamEmblem",
            LivingVisualScope = LivingVisualAssetScope.TeamEmblem.ToString(),
            LivingVisualKind = LivingVisualAssetKind.LivingLegendaryEmblem.ToString(),
            LivingPackagePath = "living_visual_backend_probe.glb",
            PreferredBackend = LivingRendererBackend.Filament.ToString(),
            FallbackPolicy = "StaticFallback",
            LivingVisualVersion = "filament-backend-probe-1",
            Rarity = "BackendProbe",
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
            _validationLabel.Text = "Published. It will appear as an Emblem after acquisition.";
            _validationLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static string NormalizeFallbackImage(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return DefaultFallbackImage;

        var lower = text.ToLowerInvariant();
        if (lower.EndsWith(".png", StringComparison.Ordinal) || lower.EndsWith(".jpg", StringComparison.Ordinal) || lower.EndsWith(".jpeg", StringComparison.Ordinal) || lower.EndsWith(".webp", StringComparison.Ordinal))
            return text;

        return DefaultFallbackImage;
    }

    private void ShowError(string message)
    {
        _validationLabel.TextColor = Color.FromArgb("#D84A4A");
        _validationLabel.Text = message;
        _validationLabel.IsVisible = true;
    }

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

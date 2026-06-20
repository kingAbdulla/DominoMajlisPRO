using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class InventoryAuditPage : ContentPage
{
    private readonly Label _healthLabel = new();
    private readonly Label _countsLabel = new();
    private readonly Label _emptyLabel = new();
    private readonly VerticalStackLayout _rows = new() { Spacing = 8 };
    private readonly Grid _editorOverlay = new() { IsVisible = false, BackgroundColor = Color.FromArgb("#99000000") };
    private readonly Label _editorTitle = new();
    private readonly Label _selectedAssetLabel = new();
    private readonly Entry _catalogSearch = new() { Placeholder = "Search registered Asset Catalog" };
    private readonly Picker _assetTypePicker = new() { Title = "Canonical Asset Type" };
    private readonly Picker _ownerScopePicker = new() { Title = "OwnerScope" };
    private readonly VerticalStackLayout _catalogRows = new() { Spacing = 6 };
    private readonly Button _saveButton = new() { Text = "Save repair" };
    private InventoryAuditReport? _report;
    private InventoryAuditItem? _editingItem;
    private RegisteredStoreAsset? _selectedAsset;

    public InventoryAuditPage()
    {
        Title = "Inventory Audit";
        BackgroundColor = Color.FromArgb("#050505");
        FlowDirection = FlowDirection.LeftToRight;
        Content = BuildPage();
        _assetTypePicker.SetOptions(CanonicalStoreCatalog.DefaultCategoriesForAdmin());
        _ownerScopePicker.SetOptions(CanonicalStoreCatalog.OwnerScopes());
        _catalogSearch.TextChanged += (_, _) => FillCatalog();
        _assetTypePicker.SelectedIndexChanged += (_, _) => FillCatalog();
        _saveButton.Clicked += OnSaveClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AppEvents.StoreProgressChanged -= OnInventoryChanged;
        AppEvents.StoreProgressChanged += OnInventoryChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
        StoreAssetQueryService.PublishedContentChanged += OnPublishedContentChanged;
        await RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        AppEvents.StoreProgressChanged -= OnInventoryChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
        base.OnDisappearing();
    }

    private void OnInventoryChanged(string playerId)
    {
        _ = playerId;
        _ = RefreshAsync();
    }

    private void OnTeamAssetsChanged(string teamId)
    {
        _ = teamId;
        _ = RefreshAsync();
    }

    private void OnPublishedContentChanged() => _ = RefreshAsync();

    private View BuildPage()
    {
        var back = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, FontSize = 26 };
        back.Clicked += async (_, _) => await Navigation.PopAsync();
        var repairAll = new Button { Text = "Repair All Safe Items", BackgroundColor = Color.FromArgb("#B38B2E"), TextColor = Colors.Black };
        repairAll.Clicked += OnRepairAllClicked;
        var refresh = new Button { Text = "Refresh" };
        refresh.Clicked += async (_, _) => await RefreshAsync();

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        header.Add(back, 0, 0);
        header.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = "Inventory Audit", TextColor = Colors.White, FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Published products × registered Asset Catalog", TextColor = Color.FromArgb("#AAAAAA"), FontSize = 11 }
            }
        }, 1, 0);
        header.Add(refresh, 2, 0);

        var summary = Panel(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { _healthLabel, _countsLabel, repairAll }
        });

        var tableHeader = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.2, GridUnitType.Star))
            },
            ColumnSpacing = 5
        };
        AddHeader(tableHeader, "Product Name", 0);
        AddHeader(tableHeader, "ProductId", 1);
        AddHeader(tableHeader, "AssetId", 2);
        AddHeader(tableHeader, "Current Asset Type", 3);
        AddHeader(tableHeader, "Status", 4);

        _emptyLabel.Text = "No published products found.";
        _emptyLabel.TextColor = Color.FromArgb("#AAAAAA");
        _emptyLabel.HorizontalTextAlignment = TextAlignment.Center;

        var scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(14, 16, 14, 28),
                Spacing = 12,
                Children = { header, summary, tableHeader, _emptyLabel, _rows }
            }
        };

        var root = new Grid();
        root.Add(scroll);
        root.Add(BuildEditor());
        return root;
    }

    private View BuildEditor()
    {
        var close = new Button { Text = "Close" };
        close.Clicked += (_, _) => CloseEditor();
        var editorHeader = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        editorHeader.Add(_editorTitle, 0, 0);
        editorHeader.Add(close, 1, 0);
        _editorTitle.FontSize = 20;
        _editorTitle.FontAttributes = FontAttributes.Bold;
        _editorTitle.TextColor = Colors.White;
        _selectedAssetLabel.TextColor = Color.FromArgb("#D8B75B");
        _selectedAssetLabel.LineBreakMode = LineBreakMode.TailTruncation;

        var catalogScroll = new ScrollView
        {
            MaximumHeightRequest = 280,
            Content = _catalogRows
        };
        var content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                editorHeader,
                new Label { Text = "AssetId cannot be typed. Select it from the registered catalog.", TextColor = Color.FromArgb("#AAAAAA"), FontSize = 11 },
                _assetTypePicker,
                _catalogSearch,
                _selectedAssetLabel,
                catalogScroll,
                _ownerScopePicker,
                _saveButton
            }
        };
        var panel = new Border
        {
            Padding = 16,
            Margin = new Thickness(10),
            VerticalOptions = LayoutOptions.End,
            Background = Color.FromArgb("#171717"),
            Stroke = Color.FromArgb("#4A4A4A"),
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = content
        };
        _editorOverlay.Add(panel);
        return _editorOverlay;
    }

    private async Task RefreshAsync()
    {
        try
        {
            _report = await InventoryAuditService.ScanAsync();
            var summary = _report.Summary;
            _healthLabel.Text = $"Catalog Health: {summary.HealthyPercent:0.#}%";
            _healthLabel.TextColor = summary.Malformed == 0 ? Color.FromArgb("#42C97A") : Color.FromArgb("#F0B44C");
            _healthLabel.FontSize = 21;
            _healthLabel.FontAttributes = FontAttributes.Bold;
            _countsLabel.Text =
                $"Products: {summary.Products}   Healthy: {summary.Healthy}   Malformed: {summary.Malformed}\n" +
                $"Duplicate AssetIds: {summary.DuplicateAssetIds}   Missing Assets: {summary.MissingAssets}   Missing Asset Types: {summary.MissingAssetTypes}";
            _countsLabel.TextColor = Color.FromArgb("#D0D0D0");
            _rows.Children.Clear();
            _emptyLabel.IsVisible = _report.Items.Count == 0;
            foreach (var item in _report.Items)
                _rows.Children.Add(CreateRow(item));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Inventory Audit", ex.Message, "OK");
        }
    }

    private View CreateRow(InventoryAuditItem item)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.2, GridUnitType.Star))
            },
            ColumnSpacing = 5
        };
        AddCell(grid, item.ProductName, 0, Colors.White);
        AddCell(grid, item.ProductId, 1, Color.FromArgb("#BDBDBD"));
        AddCell(grid, item.AssetId, 2, Color.FromArgb("#BDBDBD"));
        AddCell(grid, item.CurrentAssetType, 3, Color.FromArgb("#BDBDBD"));
        AddCell(grid, item.StatusText, 4, item.IsHealthy ? Color.FromArgb("#42C97A") : Color.FromArgb("#F06A6A"));
        var row = Panel(grid);
        if (!item.IsHealthy)
        {
            row.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => OpenEditor(item))
            });
        }
        return row;
    }

    private void OpenEditor(InventoryAuditItem item)
    {
        _editingItem = item;
        _selectedAsset = null;
        _editorTitle.Text = item.ProductName;
        _selectedAssetLabel.Text = $"Current AssetId: {item.AssetId}";
        _assetTypePicker.SelectCanonicalId(StoreProductAssetTypeCatalog.TryResolve(item.CurrentAssetType, out var currentType)
            ? currentType.ToString()
            : null);
        _ownerScopePicker.SelectCanonicalId(Enum.TryParse<StoreProductOwnerScope>(item.OwnerScope, false, out var scope)
            ? scope.ToString()
            : null);
        _catalogSearch.Text = string.Empty;
        FillCatalog();
        _editorOverlay.IsVisible = true;
    }

    private void FillCatalog()
    {
        _catalogRows.Children.Clear();
        if (_report == null) return;
        var query = _catalogSearch.Text?.Trim();
        var selectedType = _assetTypePicker.SelectedCanonicalId();
        var assets = _report.Catalog.Where(asset =>
            (string.IsNullOrWhiteSpace(selectedType) || asset.AssetType.ToString() == selectedType) &&
            (string.IsNullOrWhiteSpace(query) ||
             asset.AssetId.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             asset.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)));
        foreach (var asset in assets.Take(100))
        {
            var button = new Button
            {
                Text = $"{asset.DisplayName}  •  {asset.AssetId}",
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Fill
            };
            button.Clicked += (_, _) => SelectAsset(asset);
            _catalogRows.Children.Add(button);
        }
    }

    private void SelectAsset(RegisteredStoreAsset asset)
    {
        _selectedAsset = asset;
        _selectedAssetLabel.Text = $"Selected AssetId: {asset.AssetId}";
        _assetTypePicker.SelectCanonicalId(asset.AssetType.ToString());
        _ownerScopePicker.SelectCanonicalId(asset.OwnerScope.ToString());
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_editingItem == null || _selectedAsset == null)
        {
            await DisplayAlertAsync("Inventory Audit", "Select an AssetId from the registered catalog.", "OK");
            return;
        }
        if (!Enum.TryParse<StoreProductAssetType>(_assetTypePicker.SelectedCanonicalId(), false, out var type) ||
            !Enum.TryParse<StoreProductOwnerScope>(_ownerScopePicker.SelectedCanonicalId(), false, out var owner))
        {
            await DisplayAlertAsync("Inventory Audit", "Choose Asset Type and OwnerScope.", "OK");
            return;
        }
        try
        {
            await InventoryAuditService.SaveAsync(_editingItem, type, _selectedAsset.AssetId, owner);
            CloseEditor();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Inventory Audit", ex.Message, "OK");
        }
    }

    private async void OnRepairAllClicked(object? sender, EventArgs e)
    {
        var count = await InventoryAuditService.RepairAllSafeItemsAsync();
        await RefreshAsync();
        await DisplayAlertAsync("Inventory Audit", $"Repaired {count} safe item(s). Ambiguous items were left unchanged.", "OK");
    }

    private void CloseEditor()
    {
        _editingItem = null;
        _selectedAsset = null;
        _editorOverlay.IsVisible = false;
    }

    private static Border Panel(View content) => new()
    {
        Padding = 10,
        Background = Color.FromArgb("#141414"),
        Stroke = Color.FromArgb("#363636"),
        StrokeShape = new RoundRectangle { CornerRadius = 14 },
        Content = content
    };

    private static void AddHeader(Grid grid, string text, int column) =>
        AddCell(grid, text, column, Color.FromArgb("#D8B75B"), FontAttributes.Bold);

    private static void AddCell(Grid grid, string? text, int column, Color color, FontAttributes attributes = FontAttributes.None)
    {
        grid.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(text) ? "—" : text,
            TextColor = color,
            FontSize = 10,
            FontAttributes = attributes,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center
        }, column, 0);
    }
}

using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class InventoryNameTypographyPreviewView : ContentView
{
    public static readonly BindableProperty AssetIdProperty =
        BindableProperty.Create(nameof(AssetId), typeof(string), typeof(InventoryNameTypographyPreviewView), string.Empty, propertyChanged: OnChanged);

    public static readonly BindableProperty AssetTypeProperty =
        BindableProperty.Create(nameof(AssetType), typeof(string), typeof(InventoryNameTypographyPreviewView), string.Empty, propertyChanged: OnChanged);

    public static readonly BindableProperty PreviewTextProperty =
        BindableProperty.Create(nameof(PreviewText), typeof(string), typeof(InventoryNameTypographyPreviewView), "اسم اللاعب", propertyChanged: OnChanged);

    private readonly IdentityPlateView _plate = new()
    {
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Center,
        HeightRequest = 42
    };

    private bool _refreshing;

    public InventoryNameTypographyPreviewView()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        Content = _plate;
        Loaded += async (_, _) => await RefreshAsync();
    }

    public string AssetId
    {
        get => (string)GetValue(AssetIdProperty);
        set => SetValue(AssetIdProperty, value);
    }

    public string AssetType
    {
        get => (string)GetValue(AssetTypeProperty);
        set => SetValue(AssetTypeProperty, value);
    }

    public string PreviewText
    {
        get => (string)GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    private static async void OnChanged(BindableObject bindable, object oldValue, object newValue) =>
        await ((InventoryNameTypographyPreviewView)bindable).RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        try
        {
            _refreshing = true;
            var type = StoreAssetCatalogService.CanonicalTypeId(AssetType);
            if (!IsNameTypographyType(type) || string.IsNullOrWhiteSpace(AssetId))
            {
                _plate.Bind(PreviewText, null);
                return;
            }

            var asset = await StoreAssetCatalogService.ResolveAsync(AssetId, type);
            _plate.Bind(PreviewText, asset?.TypographyPreset);
        }
        catch
        {
            _plate.Bind(PreviewText, null);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static bool IsNameTypographyType(string? type) =>
        string.Equals(type, StoreProductAssetType.PlayerNameEffect.ToString(), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, StoreProductAssetType.PlayerNameFrame.ToString(), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, StoreProductAssetType.TeamNameEffect.ToString(), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, StoreProductAssetType.TeamNameFrame.ToString(), StringComparison.OrdinalIgnoreCase);
}

using System.ComponentModel;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.Pages;

public sealed class OwnedCosmeticChoiceItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public OwnedCosmeticChoiceItem(
        CatalogAssetDisplay asset,
        string ownerPlayerId)
    {
        Asset = asset;
        OwnerPlayerId = ownerPlayerId;
    }

    public CatalogAssetDisplay Asset { get; }
    public string AssetId => Asset.AssetId;
    public string AssetType => Asset.AssetType.ToString();
    public string OwnerPlayerId { get; }
    public string AssetDisplayName => string.IsNullOrWhiteSpace(Asset.ArabicDisplayName)
        ? Asset.DisplayName
        : Asset.ArabicDisplayName;
    public string DisplayName => IsTypographyFrame || IsTypographyEffect ? "Aa | أب" : AssetDisplayName;
    public bool HasImage => !string.IsNullOrWhiteSpace(Asset.PreviewImage) &&
                            !RemovedStoreAssetPolicy.IsRemoved(Asset.PreviewImage);
    public bool IsImageFrame => Asset.AssetType == StoreProductAssetType.Frame && HasImage;
    public bool IsTypographyFrame => Asset.AssetType is StoreProductAssetType.PlayerNameFrame or
        StoreProductAssetType.TeamNameFrame;
    public bool IsTypographyEffect => Asset.AssetType is StoreProductAssetType.PlayerNameEffect or
        StoreProductAssetType.TeamNameEffect;
    public TypographyIdentityPreset TypographyPreset => Asset.TypographyPreset;
    public ImageSource? PreviewImageSource => HasImage
        ? InventoryDisplayResolver.ResolveOptionalImageSource(Asset.PreviewImage)
        : null;
    public Color AccentColor => IsSelected
        ? Color.FromArgb("#FFD15F")
        : Color.FromArgb("#6E4A18");

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
            PropertyChanged?.Invoke(this, new(nameof(AccentColor)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class CreateTeamPage
{
    private readonly SemaphoreSlim _ownedCosmeticsGate = new(1, 1);
    private List<OwnedCosmeticChoiceItem> _ownedFrameChoices = new();
    private List<OwnedCosmeticChoiceItem> _ownedTypographyChoices = new();
    private OwnedCosmeticChoiceItem? _selectedTeamNameEffect;
    private OwnedCosmeticChoiceItem? _selectedTeamNameFrame;

    private async Task SyncOwnedCosmeticChoicesAsync()
    {
        if (!await _ownedCosmeticsGate.WaitAsync(0))
            return;

        try
        {
            var ownerIds = await ResolveCurrentTeamEffectOwnerIdsAsync();
            var catalog = await StoreAssetCatalogService.LoadAsync();
            var choices = new List<OwnedCosmeticChoiceItem>();

            foreach (var ownerId in ownerIds)
            {
                var inventory = await PlayerInventoryService.LoadOwnedAsync(ownerId);
                foreach (var owned in inventory.Where(item => item.IsOwned && !item.IsExpired))
                {
                    if (RemovedStoreAssetPolicy.IsRemoved(owned.AssetId))
                        continue;

                    var typeId = StoreAssetCatalogService.CanonicalTypeId(owned.StoreTypeId);
                    if (typeId is not ("Frame" or "PlayerNameFrame" or "TeamNameFrame" or
                        "PlayerNameEffect" or "TeamNameEffect"))
                    {
                        continue;
                    }

                    var asset = StoreAssetCatalogService.Resolve(catalog, owned.AssetId, typeId);
                    if (asset == null || RemovedStoreAssetPolicy.IsRemoved(asset.AssetId, asset.PreviewImage))
                        continue;

                    choices.Add(new OwnedCosmeticChoiceItem(asset, ownerId));
                }
            }

            // Preserve purchases made before team-name assets became PlayerId-owned.
            if (!string.IsNullOrWhiteSpace(CurrentTeam?.TeamId) && ownerIds.Count > 0)
            {
                var legacyTeamInventory = await TeamAssetInventoryService.GetInventoryForTeamAsync(CurrentTeam.TeamId);
                foreach (var owned in legacyTeamInventory.Where(item => item.IsOwned))
                {
                    var typeId = StoreAssetCatalogService.CanonicalTypeId(owned.TeamAssetTypeId);
                    if (typeId is not ("TeamNameEffect" or "TeamNameFrame") ||
                        RemovedStoreAssetPolicy.IsRemoved(owned.TeamAssetId))
                    {
                        continue;
                    }

                    var asset = StoreAssetCatalogService.Resolve(catalog, owned.TeamAssetId, typeId);
                    if (asset != null && !RemovedStoreAssetPolicy.IsRemoved(asset.AssetId, asset.PreviewImage))
                        choices.Add(new OwnedCosmeticChoiceItem(asset, ownerIds[0]));
                }
            }

            var frames = choices
                .Where(item => item.Asset.AssetType is StoreProductAssetType.Frame or
                    StoreProductAssetType.PlayerNameFrame or StoreProductAssetType.TeamNameFrame)
                .Where(item => item.Asset.AssetType != StoreProductAssetType.Frame || item.HasImage)
                .GroupBy(item => $"{item.AssetType}|{item.AssetId}|{item.OwnerPlayerId}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            var typography = choices
                .Where(item => item.Asset.AssetType is StoreProductAssetType.PlayerNameEffect or
                    StoreProductAssetType.TeamNameEffect)
                .GroupBy(item => $"{item.AssetType}|{item.AssetId}|{item.OwnerPlayerId}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _ownedFrameChoices = frames;
                _ownedTypographyChoices = typography;
                OwnedFramesCarousel.ItemsSource = frames;
                OwnedTypographyCarousel.ItemsSource = typography;
                FramesEmptyLabel.IsVisible = frames.Count == 0;
                TypographyEmptyLabel.IsVisible = typography.Count == 0;
            });
        }
        finally
        {
            _ownedCosmeticsGate.Release();
        }
    }

    private async void OnOwnedFrameSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers || e.CurrentSelection.FirstOrDefault() is not OwnedCosmeticChoiceItem selected)
            return;

        SelectOwnedChoice(_ownedFrameChoices, selected);
        if (selected.Asset.AssetType == StoreProductAssetType.TeamNameFrame)
        {
            _selectedTeamNameFrame = selected;
            await ApplySelectedTeamTypographyPreviewAsync();
            return;
        }

        await StoreEquipService.EquipAsync(selected.OwnerPlayerId, selected.AssetId);
        await UpdatePreviewAvatarsAsync();
        await ApplyPreviewNameTypographyAsync();
    }

    private async void OnOwnedTypographySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers || e.CurrentSelection.FirstOrDefault() is not OwnedCosmeticChoiceItem selected)
            return;

        SelectOwnedChoice(_ownedTypographyChoices, selected);
        if (selected.Asset.AssetType == StoreProductAssetType.TeamNameEffect)
        {
            _selectedTeamNameEffect = selected;
            await ApplySelectedTeamTypographyPreviewAsync();
            return;
        }

        await StoreEquipService.EquipAsync(selected.OwnerPlayerId, selected.AssetId);
        await ApplyPreviewNameTypographyAsync();
    }

    private static void SelectOwnedChoice(
        IEnumerable<OwnedCosmeticChoiceItem> choices,
        OwnedCosmeticChoiceItem selected)
    {
        foreach (var choice in choices)
            choice.IsSelected = ReferenceEquals(choice, selected);
    }

    private Task ApplySelectedTeamTypographyPreviewAsync()
    {
        var identity = new NameTypographyIdentity(
            CurrentTeam?.TeamId ?? string.Empty,
            _selectedTeamNameEffect?.Asset,
            _selectedTeamNameFrame?.Asset);
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            PreviewTeamNamePlate.Bind(PreviewTeamName.Text, identity.ResolvePreset());
            PreviewTeamNamePlate.IsVisible = identity.HasVisual;
            PreviewTeamName.IsVisible = !identity.HasVisual;
        });
    }

    private async Task PersistSelectedTeamTypographyAsync(string teamId)
    {
        await PersistAsync(_selectedTeamNameEffect);
        await PersistAsync(_selectedTeamNameFrame);

        async Task PersistAsync(OwnedCosmeticChoiceItem? selected)
        {
            if (selected == null)
                return;

            await TeamAssetInventoryService.AddOwnedAssetAsync(
                teamId,
                selected.AssetId,
                selected.AssetType,
                $"Player:{selected.OwnerPlayerId}");
            await TeamAssetInventoryService.EquipAsync(
                teamId,
                selected.AssetId,
                selected.AssetType);
        }
    }

    private void ShowCosmeticSection(string section)
    {
        OwnedFramesSection.IsVisible = section == "Frames";
        OwnedTypographySection.IsVisible = section == "Typography";
        EmblemBackgroundSection.IsVisible = section == "Backgrounds";
        TeamEffectSection.IsVisible = section == "Effects" && teamEffectItems.Count > 1;

        FramesTab.TextColor = section == "Frames" ? Color.FromArgb("#FFF1B8") : Color.FromArgb("#B89551");
        TypographyTab.TextColor = section == "Typography" ? Color.FromArgb("#FFF1B8") : Color.FromArgb("#B89551");
        BackgroundsTab.TextColor = section == "Backgrounds" ? Color.FromArgb("#FFF1B8") : Color.FromArgb("#B89551");
        EffectsTab.TextColor = section == "Effects" ? Color.FromArgb("#FFF1B8") : Color.FromArgb("#B89551");
        StyleTab(FramesTab, section == "Frames");
        StyleTab(TypographyTab, section == "Typography");
        StyleTab(BackgroundsTab, section == "Backgrounds");
        StyleTab(EffectsTab, section == "Effects");

        static void StyleTab(Button tab, bool selected)
        {
            tab.BackgroundColor = Color.FromArgb(selected ? "#3A290D" : "#101214");
            tab.BorderColor = Color.FromArgb(selected ? "#D6A642" : "#49391F");
        }
    }

    private void OnFramesTabClicked(object sender, EventArgs e) => ShowCosmeticSection("Frames");
    private void OnTypographyTabClicked(object sender, EventArgs e) => ShowCosmeticSection("Typography");
    private void OnBackgroundsTabClicked(object sender, EventArgs e) => ShowCosmeticSection("Backgrounds");
    private void OnEffectsTabClicked(object sender, EventArgs e) => ShowCosmeticSection("Effects");
}

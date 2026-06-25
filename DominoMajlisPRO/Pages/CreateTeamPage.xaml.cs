using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using System.ComponentModel;
using System.Threading;

namespace DominoMajlisPRO.Pages;

public abstract class SelectableCarouselItem : INotifyPropertyChanged
{
    private bool _isSelected;

    protected SelectableCarouselItem(string imagePath) => ImagePath = imagePath;

    public string ImagePath { get; }
    public ImageSource ImageSource =>
        InventoryDisplayResolver.ResolveImageSource(ImagePath);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class EmblemCarouselItem(string assetId, string imagePath, string displayName) : SelectableCarouselItem(imagePath)
{
    public string AssetId { get; } = assetId;
    public string DisplayName { get; } = displayName;
}

public sealed class TeamColorCarouselItem(string assetId, string colorHex, string imagePath, string displayName) : SelectableCarouselItem(imagePath)
{
    public string AssetId { get; } = assetId;
    public string ColorHex { get; } = colorHex;
    public string DisplayName { get; } = displayName;
}

public sealed record EmblemBackgroundPickerItem(string AssetId, string Background, string DisplayName);

public sealed class TeamEffectCarouselItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public TeamEffectCarouselItem(
        string assetId,
        string displayName,
        CatalogAssetDisplay? effect,
        string ownerPlayerId = "")
    {
        AssetId = assetId;
        DisplayName = displayName;
        Effect = effect;
        OwnerPlayerId = ownerPlayerId?.Trim() ?? string.Empty;
    }

    public string AssetId { get; }
    public string DisplayName { get; }
    public CatalogAssetDisplay? Effect { get; }
    public string OwnerPlayerId { get; }
    public bool IsNone => Effect == null;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class CreateTeamPage : ContentPage
{
    private List<EmblemCarouselItem> emblemItems = new();
    private List<TeamColorCarouselItem> colorItems = new();
    private List<EmblemBackgroundPickerItem> backgroundItems = new();
    private List<TeamEffectCarouselItem> teamEffectItems = new();

    List<PlayerProfileModel> allPlayers = new();
    private bool IsEditMode = false;

    string selectedEmblem = "shield_3d.png";
    string selectedEmblemAssetId = "default_emblem_shield";

    bool isTeamMode = true;

    string selectedColor = "#FFD700";
    string selectedColorAssetId = "default_color_gold";

    string selectedEmblemBackground = "Transparent";
    string selectedEmblemBackgroundAssetId = "default_background_transparent";
    string selectedTeamEffectAssetId = "";
    string selectedTeamEffectOwnerPlayerId = "";

    private TeamProfileModel? CurrentTeam = null;
    private bool _suppressSelectionHandlers = false;
    private bool _suppressTeamPlayersChanged = false;
    private readonly SemaphoreSlim _ownedAssetsReloadGate = new(1, 1);
    private bool _ownedAssetsReloadRequested = false;
    private bool _isReloadingOwnedAssets = false;
    private List<TeamProfileModel> LoadedTeams = new();

    public CreateTeamPage()
    {
        InitializeComponent();

        EmblemCarousel.ItemsSource = emblemItems;
        ColorCarousel.ItemsSource = colorItems;
        EmblemBackgroundPicker.ItemsSource = backgroundItems;
        TeamEffectCarousel.ItemsSource = teamEffectItems;

        OnTeamClicked(this, EventArgs.Empty);

        TeamNameEntry.TextChanged += TeamNameChanged;
        Player1Entry.TextChanged += TeamPlayersChanged;
        Player2Entry.TextChanged += TeamPlayersChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;

        await LoadOwnedTeamAssetsAsync();
    }

    protected override void OnDisappearing()
    {
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        base.OnDisappearing();
    }

    async void OnTeamAssetsChanged(string teamId)
    {
        _ = teamId;
        await LoadOwnedTeamAssetsAsync();
    }

    async Task LoadOwnedTeamAssetsAsync()
    {
        if (!await _ownedAssetsReloadGate.WaitAsync(0))
        {
            _ownedAssetsReloadRequested = true;
            return;
        }

        try
        {
            do
            {
                _ownedAssetsReloadRequested = false;
                _isReloadingOwnedAssets = true;

                var ownerTeam = CurrentTeam;

                // Prefer PlayerId-first resolution: try to interpret the entry text as a PlayerId,
                // then fall back to name-based lookup for legacy data.
                string p1Text = Player1Entry.Text?.Trim() ?? string.Empty;
                PlayerProfileModel? player1 = null;
                if (!string.IsNullOrWhiteSpace(p1Text))
                {
                    player1 = await PlayerProfileService.GetPlayerByIdAsync(p1Text);
                    if (player1 == null)
                        player1 = await PlayerProfileService.GetPlayerByNameAsync(p1Text);
                }

                PlayerProfileModel? player2 = null;
                if (isTeamMode)
                {
                    string p2Text = Player2Entry.Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(p2Text))
                    {
                        player2 = await PlayerProfileService.GetPlayerByIdAsync(p2Text);
                        if (player2 == null)
                            player2 = await PlayerProfileService.GetPlayerByNameAsync(p2Text);
                    }
                }

                var inventory = (await TeamEligibleAssetService.GetEligibleAsync(
                        ownerTeam?.TeamId,
                        player1?.PlayerId ?? ownerTeam?.Player1Id,
                        player2?.PlayerId ??
                        (isTeamMode ? ownerTeam?.Player2Id : null)))
                    .Where(item => item.IsOwned || item.Source == "Default")
                    .ToList();

                emblemItems.Clear();
                colorItems.Clear();
                backgroundItems.Clear();

                var catalog = await StoreAssetCatalogService.LoadAsync();
                var storeOwner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
                var ownedTeamEffects = new List<TeamEffectCarouselItem>();

                var teamEffectOwnerIds = new[]
                    {
                        player1?.PlayerId ?? ownerTeam?.Player1Id,
                        player2?.PlayerId ?? (isTeamMode ? ownerTeam?.Player2Id : null),
                        storeOwner.IsGhost ? null : storeOwner.PlayerId
                    }
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var ownerPlayerId in teamEffectOwnerIds)
                {
                    var playerInventory = await PlayerInventoryService.LoadOwnedAsync(ownerPlayerId);
                    foreach (var owned in playerInventory.Where(item =>
                                 string.Equals(StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId),
                                     StoreProductAssetType.TeamEffect.ToString(),
                                     StringComparison.OrdinalIgnoreCase)))
                    {
                        var effect = StoreAssetCatalogService.Resolve(catalog, owned.AssetId,
                            StoreProductAssetType.TeamEffect.ToString());

                        if (effect == null)
                            continue;

                        ownedTeamEffects.Add(new TeamEffectCarouselItem(
                            owned.AssetId,
                            string.IsNullOrWhiteSpace(effect.DisplayName) ? owned.AssetId : effect.DisplayName,
                            effect,
                            ownerPlayerId));
                    }
                }

                foreach (var item in inventory)
                {
                    var payload =
                        TeamAssetPayloadCatalog.Resolve(item.TeamAssetId, item.TeamAssetTypeId)
                        ?? TeamAssetPayloadCatalog.Resolve(item.TeamAssetId);

                    var effectiveTypeId = StoreAssetCatalogService.CanonicalTypeId(
                        payload?.TeamAssetTypeId ?? item.TeamAssetTypeId);

                    var catalogAsset = StoreAssetCatalogService.Resolve(
                        catalog, item.TeamAssetId, effectiveTypeId);

                    var displayName = catalogAsset?.DisplayName ?? (payload == null
                        ? StoreAssetCatalogService.IncompleteDisplayName
                        : !string.IsNullOrWhiteSpace(payload.ArabicDisplayName)
                            ? payload.ArabicDisplayName
                            : payload.EnglishDisplayName ?? StoreAssetCatalogService.IncompleteDisplayName);

                    if (string.Equals(effectiveTypeId, StoreProductAssetType.Emblem.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        AddEmblemIfMissing(
                            item.TeamAssetId,
                            catalogAsset?.PreviewImage ?? payload?.ImagePath ?? "ss.png",
                            displayName);
                    }
                    else if (string.Equals(effectiveTypeId, StoreProductAssetType.TeamColor.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        AddColorIfMissing(
                            item.TeamAssetId,
                            catalogAsset?.ColorHex ?? payload?.ColorHex ?? "#181818",
                            catalogAsset?.PreviewImage ?? payload?.ImagePath ?? "ss.png",
                            displayName);
                    }
                    else if (string.Equals(effectiveTypeId, StoreProductAssetType.EmblemBackground.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        AddBackgroundIfMissing(
                            item.TeamAssetId,
                            catalogAsset?.PreviewImage
                            ?? catalogAsset?.ColorHex
                            ?? payload?.BackgroundImagePath
                            ?? payload?.BackgroundColorHex
                            ?? "Transparent",
                            displayName);
                    }
                }

                AddDefaultVisualAssets();
                // Saved team identity must not inject unowned assets.
                // Owned assets already arrive through TeamEligibleAssetService.

                // Build new lists and assign atomically on the UI thread to avoid RecyclerView mutation during layout.
                var newEmblems = emblemItems.ToList();
                var newColors = colorItems.ToList();
                var newBackgrounds = backgroundItems.ToList();
                var newTeamEffects = ownedTeamEffects.Count == 0
                    ? new List<TeamEffectCarouselItem>()
                    : new[] { new TeamEffectCarouselItem("", "مؤثرات الفريق", null) }
                        .Concat(ownedTeamEffects
                            .GroupBy(item => $"{CanonicalAssetIdentityService.NormalizeForComparison(item.AssetId)}|{item.OwnerPlayerId}", StringComparer.OrdinalIgnoreCase)
                            .Select(group => group.First()))
                        .ToList();

                // Suppress selection handlers while we replace ItemsSource.
                bool prevSuppress = _suppressSelectionHandlers;
                _suppressSelectionHandlers = true;
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        EmblemCarousel.ItemsSource = newEmblems;
                        ColorCarousel.ItemsSource = newColors;
                        EmblemBackgroundPicker.ItemsSource = newBackgrounds;
                        emblemItems = newEmblems;
                        colorItems = newColors;
                        backgroundItems = newBackgrounds;
                        teamEffectItems = newTeamEffects;
                        TeamEffectCarousel.ItemsSource = newTeamEffects;
                        TeamEffectSection.IsVisible = newTeamEffects.Count > 1;
                    });

                    // Restore selection after a short dispatch to avoid immediate selection events while RecyclerView updates.
                    await Task.Delay(75);

                    if (!SelectEmblemByAssetId(selectedEmblemAssetId, animate: false))
                        SelectEmblemByImagePath(selectedEmblem, animate: false);

                    if (!SelectColorByAssetId(selectedColorAssetId, animate: false))
                        SelectColorByHex(selectedColor, animate: false);

                    if (!SelectEmblemBackground(selectedEmblemBackgroundAssetId))
                        SelectEmblemBackground("default_background_transparent");
                    SelectTeamEffectByAssetId(selectedTeamEffectAssetId, animate: false);
                }
                finally
                {
                    _suppressSelectionHandlers = prevSuppress;
                }
            }
            while (_ownedAssetsReloadRequested);
        }
        finally
        {
            _isReloadingOwnedAssets = false;
            _ownedAssetsReloadGate.Release();
        }
    }

    void AddDefaultVisualAssets()
    {
        AddEmblemIfMissing("default_emblem_dragon", "dragon_3d.png", "Dragon");
        AddEmblemIfMissing("default_emblem_crown", "crown_3d.png", "Crown");
        AddEmblemIfMissing("default_emblem_wolf", "wolf_3d.png", "Wolf");
        AddEmblemIfMissing("default_emblem_shield", "shield_3d.png", "Shield");
        AddEmblemIfMissing("default_emblem_lion", "lion_3d.png", "Lion");
        AddEmblemIfMissing("default_emblem_eagle", "eagle_3d.png", "Eagle");

        AddColorIfMissing("default_color_black", "#111111", "black_color.png", "Black");
        AddColorIfMissing("default_color_purple", "#7B1FFF", "purple_color.png", "Purple");
        AddColorIfMissing("default_color_red", "#E50000", "red_color.png", "Red");
        AddColorIfMissing("default_color_green", "#00A000", "green_color.png", "Green");
        AddColorIfMissing("default_color_blue", "#006DFF", "blue_color.png", "Blue");
        AddColorIfMissing("default_color_gold", "#FFD700", "gold_color.png", "Gold");

        AddBackgroundIfMissing("default_background_transparent", "Transparent", "ط¨ط¯ظˆظ† ط®ظ„ظپظٹط©");
    }

    void AddSavedIdentityIfMissing(TeamProfileModel team)
    {
        if (!string.IsNullOrWhiteSpace(team.EmblemAssetId))
            AddEmblemIfMissing(
                team.EmblemAssetId,
                team.Emblem,
                "Current saved emblem");
        if (!string.IsNullOrWhiteSpace(team.TeamColorAssetId))
            AddColorIfMissing(
                team.TeamColorAssetId,
                team.ColorHex,
                "ss.png",
                "Current saved color");
        if (!string.IsNullOrWhiteSpace(team.EmblemBackgroundAssetId))
            AddBackgroundIfMissing(
                team.EmblemBackgroundAssetId,
                team.EmblemBackground,
                "Current saved background");
    }

    void AddEmblemIfMissing(string assetId, string imagePath, string displayName)
    {
        bool exists =
            emblemItems.Any(item =>
                CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId)
                || string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            emblemItems.Add(new EmblemCarouselItem(assetId, imagePath, displayName));
    }

    void AddColorIfMissing(string assetId, string colorHex, string imagePath, string displayName)
    {
        bool exists =
            colorItems.Any(item =>
                CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId)
                || string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ColorHex, colorHex, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            colorItems.Add(new TeamColorCarouselItem(assetId, colorHex, imagePath, displayName));
    }

    void AddBackgroundIfMissing(string assetId, string background, string displayName)
    {
        bool exists =
            backgroundItems.Any(item =>
                CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId)
                || string.Equals(item.Background, background, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            backgroundItems.Add(new EmblemBackgroundPickerItem(assetId, background, displayName));
    }

    void OnSingleClicked(object sender, EventArgs e)
    {
        PreviewMode.Text = "ظپط±ط¯ظٹ";
        isTeamMode = false;

        Player2Layout.IsVisible = false;
        PreviewPlayer2.IsVisible = false;

        string player1 = string.IsNullOrWhiteSpace(Player1Entry.Text)
            ? "ط§ظ„ظ„ط§ط¹ط¨ ط§ظ„ط£ظˆظ„"
            : Player1Entry.Text;

        PreviewPlayer1.Text = player1;
        PreviewPlayer2.Text = "";

        SingleCard.Stroke = Color.FromArgb("#FFD700");
        SingleCard.BackgroundColor = Color.FromArgb("#1A1A00");

        TeamCard.Stroke = Color.FromArgb("#404040");
        TeamCard.BackgroundColor = Color.FromArgb("#151515");

        SingleCard.ScaleTo(1.05, 150);
        TeamCard.ScaleTo(1.00, 150);
    }

    void OnTeamClicked(object sender, EventArgs e)
    {
        PreviewMode.Text = "فريق";
        isTeamMode = true;

        Player2Layout.IsVisible = true;
        if (!_suppressTeamPlayersChanged)
            TeamPlayersChanged(this, null);
        PreviewPlayer2.IsVisible = true;

        TeamCard.Stroke = Color.FromArgb("#FFD700");
        TeamCard.BackgroundColor = Color.FromArgb("#1A1A00");

        SingleCard.Stroke = Color.FromArgb("#404040");
        SingleCard.BackgroundColor = Color.FromArgb("#151515");

        TeamCard.ScaleTo(1.05, 150);
        SingleCard.ScaleTo(1.00, 150);
    }

    void OnEmblemSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers)
            return;

        if (e.CurrentSelection.FirstOrDefault() is EmblemCarouselItem selected)
            SelectEmblemByAssetId(selected.AssetId, animate: true);
    }

    bool SelectEmblemByAssetId(string assetId, bool animate)
    {
        var selected = emblemItems.FirstOrDefault(item =>
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));

        if (selected == null)
            return false;

        ApplySelectedEmblem(selected, animate);
        return true;
    }

    bool SelectEmblemByImagePath(string imagePath, bool animate)
    {
        var selected = emblemItems.FirstOrDefault(item =>
            string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));

        if (selected == null)
            return false;

        ApplySelectedEmblem(selected, animate);
        return true;
    }

    void ApplySelectedEmblem(EmblemCarouselItem selected, bool animate)
    {
        foreach (var item in emblemItems)
            item.IsSelected = ReferenceEquals(item, selected);

        selectedEmblemAssetId = selected.AssetId;
        selectedEmblem = selected.ImagePath;

        PreviewEmblem.Source =
            InventoryDisplayResolver.ResolveImageSource(
                selectedEmblem,
                "shield_3d.png");

        if (!ReferenceEquals(EmblemCarousel.SelectedItem, selected))
            EmblemCarousel.SelectedItem = selected;

        if (!_isReloadingOwnedAssets)
            Dispatcher.Dispatch(() =>
                EmblemCarousel.ScrollTo(selected, position: ScrollToPosition.Center, animate: animate));
    }

    void OnColorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers)
            return;

        if (e.CurrentSelection.FirstOrDefault() is TeamColorCarouselItem selected)
            SelectColorByAssetId(selected.AssetId, animate: true);
    }

    bool SelectColorByAssetId(string assetId, bool animate)
    {
        var selected = colorItems.FirstOrDefault(item =>
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));

        if (selected == null)
            return false;

        ApplySelectedColor(selected, animate);
        return true;
    }

    bool SelectColorByHex(string colorHex, bool animate)
    {
        var selected = colorItems.FirstOrDefault(item =>
            string.Equals(item.ColorHex, colorHex, StringComparison.OrdinalIgnoreCase));

        if (selected == null)
            return false;

        ApplySelectedColor(selected, animate);
        return true;
    }

    void ApplySelectedColor(TeamColorCarouselItem selected, bool animate)
    {
        foreach (var item in colorItems)
            item.IsSelected = ReferenceEquals(item, selected);

        selectedColorAssetId = selected.AssetId;
        selectedColor = selected.ColorHex;

        PreviewColorDot.BackgroundColor = Color.FromArgb(selectedColor);

        if (!ReferenceEquals(ColorCarousel.SelectedItem, selected))
            ColorCarousel.SelectedItem = selected;

        if (!_isReloadingOwnedAssets)
            Dispatcher.Dispatch(() =>
                ColorCarousel.ScrollTo(selected, position: ScrollToPosition.Center, animate: animate));
    }

    void OnEmblemBackgroundSelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressSelectionHandlers)
            return;

        if (EmblemBackgroundPicker.SelectedItem is EmblemBackgroundPickerItem selected)
            SelectEmblemBackground(selected.AssetId);
    }

    bool SelectEmblemBackground(string assetId)
    {
        var selected = backgroundItems.FirstOrDefault(item =>
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId));

        if (selected == null)
            return false;

        selectedEmblemBackgroundAssetId = selected.AssetId;
        selectedEmblemBackground = selected.Background;

        EmblemBackgroundPicker.SelectedItem = selected;
        PreviewEmblemBackground.BackgroundColor = SafeColor(selected.Background);

        return true;
    }

    void OnTeamEffectSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers)
            return;
        if (e.CurrentSelection.FirstOrDefault() is TeamEffectCarouselItem selected)
            SelectTeamEffectByAssetId(selected.AssetId, animate: true);
    }

    bool SelectTeamEffectByAssetId(string? assetId, bool animate)
    {
        var selected = teamEffectItems.FirstOrDefault(item =>
            CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId))
            ?? teamEffectItems.FirstOrDefault(item => item.IsNone);
        if (selected == null)
        {
            selectedTeamEffectAssetId = string.Empty;
            selectedTeamEffectOwnerPlayerId = string.Empty;
            IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);
            IdentityEffectRenderer.ApplyAround(PreviewEmblem, null);
            return false;
        }

        foreach (var item in teamEffectItems)
            item.IsSelected = ReferenceEquals(item, selected);
        selectedTeamEffectAssetId = selected.AssetId;
        if (selected.Effect == null)
        {
            selectedTeamEffectOwnerPlayerId = string.Empty;
            IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);
            IdentityEffectRenderer.ApplyAround(PreviewEmblem, null);
        }
        else
        {
            selectedTeamEffectOwnerPlayerId = selected.OwnerPlayerId;
            IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);
            IdentityEffectRenderer.ApplyAround(PreviewEmblem, selected.Effect, 1.18, lightweight: true);
        }
        if (!ReferenceEquals(TeamEffectCarousel.SelectedItem, selected))
            TeamEffectCarousel.SelectedItem = selected;
        MainThread.BeginInvokeOnMainThread(() =>
            TeamEffectCarousel.ScrollTo(selected, position: ScrollToPosition.Center, animate: animate));
        return true;
    }

    async Task CaptureCurrentEffectOwnerAsync()
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        selectedTeamEffectOwnerPlayerId = owner.PlayerId?.Trim() ?? string.Empty;
    }

    void OnTeamEffectItemLoaded(object? sender, EventArgs e)
    {
        if (sender is not Grid grid ||
            grid.BindingContext is not TeamEffectCarouselItem { Effect: not null } item ||
            grid.Children.OfType<IdentityEffectView>().Any())
            return;
        var effectView = IdentityEffectRenderer.Create(item.Effect, 1.16, lightweight: true);
        effectView.ZIndex = 2;
        grid.Children.Add(effectView);
    }

    static Color SafeColor(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (!value.StartsWith('#') && !string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase))
            ? Colors.Transparent
            : Color.FromArgb(value);

    private string BuildTeamSignature(string player1Id, string player2Id)
    {
        var players = new List<string>();

        if (!string.IsNullOrWhiteSpace(player1Id))
            players.Add(player1Id);

        if (!string.IsNullOrWhiteSpace(player2Id))
            players.Add(player2Id);

        players.Sort();

        return string.Join("|", players);
    }

    private bool TeamCompositionExists(List<TeamProfileModel> teams, string player1Id, string player2Id)
    {
        string newSignature = BuildTeamSignature(player1Id, player2Id);

        foreach (var team in teams)
        {
            if (CurrentTeam != null && team.TeamId == CurrentTeam.TeamId)
                continue;

            string existingSignature = BuildTeamSignature(team.Player1Id, team.Player2Id);

            if (existingSignature == newSignature)
                return true;
        }

        return false;
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TeamNameEntry.Text))
        {
            await DisplayAlert("تنبيه", "أدخل اسم الفريق", "حسناً");
            return;
        }

        var teams = await TeamProfileService.LoadTeamsAsync();

        string player1 = Player1Entry.Text?.Trim() ?? "";
        string player2 = Player2Entry.Text?.Trim() ?? "";

        if (!isTeamMode)
        {
            player2 = "";
            Player2Entry.Text = "";
        }

        string normalizedPlayer1 = PlayerIdentityService.NormalizePlayerName(player1);
        string normalizedPlayer2 = PlayerIdentityService.NormalizePlayerName(player2);

        if (!string.IsNullOrWhiteSpace(player2) && normalizedPlayer1 == normalizedPlayer2)
        {
            await DisplayAlert("لا يمكن إنشاء الفريق", "لا يمكن إضافة نفس اللاعب مرتين داخل الفريق", "حسناً");
            return;
        }

        var duplicateName =
            teams.Any(x =>
                x.TeamName.Trim().Equals(TeamNameEntry.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                &&
                (CurrentTeam == null || x.TeamId != CurrentTeam.TeamId));

        if (duplicateName)
        {
            await DisplayAlert("تنبيه", "اسم الفريق مستخدم مسبقاً", "حسناً");
            return;
        }

        string player1Id = await GetOrCreatePlayerIdAsync(player1);
        string player2Id = await GetOrCreatePlayerIdAsync(player2);

        if (TeamCompositionExists(teams, player1Id, player2Id))
        {
            await DisplayAlert("تنبيه", "هذه التشكيلة تمتلك فريقاً مسبقاً", "حسناً");
            return;
        }

        if (!IsEditMode)
        {
            string nextTeamId = GenerateNextTeamId(teams);

            TeamProfileModel team = new()
            {
                TeamId = nextTeamId,
                TeamName = TeamNameEntry.Text,

                Player1 = player1,
                Player2 = player2,

                Player1Id = player1Id,
                Player2Id = player2Id,

                IsSinglePlayer = !isTeamMode,

                Emblem = selectedEmblem,
                EmblemAssetId = selectedEmblemAssetId,

                ColorHex = selectedColor,
                TeamColorAssetId = selectedColorAssetId,

                EmblemBackground = selectedEmblemBackground,
                EmblemBackgroundAssetId = selectedEmblemBackgroundAssetId,
                EquippedTeamEffectAssetId =
                    selectedTeamEffectOwnerPlayerId == player1Id ||
                    selectedTeamEffectOwnerPlayerId == player2Id
                        ? selectedTeamEffectAssetId
                        : string.Empty,
                EquippedTeamEffectOwnerPlayerId =
                    selectedTeamEffectOwnerPlayerId == player1Id ||
                    selectedTeamEffectOwnerPlayerId == player2Id
                        ? selectedTeamEffectOwnerPlayerId
                        : string.Empty
            };

            teams.Add(team);

            await TeamProfileService.SaveTeamsAsync(teams);
            
            // Publish to VisualEventBus for Living Visual Identity Engine
            var newTeamEmblemPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, team.TeamId },
                { VisualIdentityPayloadKeys.EmblemAssetId, team.EmblemAssetId },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            // No previous emblem for new team
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEmblemChanged, newTeamEmblemPayload, isSticky: true, stickyExpirationMs: 0);
            
            // Publish TeamColorChanged for new team
            var newTeamColorPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, team.TeamId },
                { VisualIdentityPayloadKeys.PrimaryColorHex, team.ColorHex },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamColorChanged, newTeamColorPayload, isSticky: true, stickyExpirationMs: 0);
            
            // Publish TeamEffectChanged for new team if effect equipped
            if (!string.IsNullOrWhiteSpace(team.EquippedTeamEffectAssetId))
            {
                var newTeamEffectPayload = new Dictionary<string, object>
                {
                    { VisualIdentityPayloadKeys.TeamId, team.TeamId },
                    { VisualIdentityPayloadKeys.EffectAssetId, team.EquippedTeamEffectAssetId },
                    { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
                };
                VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEffectChanged, newTeamEffectPayload, isSticky: true, stickyExpirationMs: 0);
            }
            
            // Publish TeamEmblemBackgroundChanged for new team
            var newTeamEmblemBackgroundPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, team.TeamId },
                { VisualIdentityPayloadKeys.BackgroundAssetId, team.EmblemBackgroundAssetId },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEmblemBackgroundChanged, newTeamEmblemBackgroundPayload, isSticky: true, stickyExpirationMs: 0);

            AppEvents.RaiseDataChanged();
            AppEvents.RaiseTeamAssetsChanged(team.TeamId);

            await DisplayAlert("تم", "تم إنشاء الفريق", "ممتاز");

            ResetForm();

            return;
        }

        var existing = teams.FirstOrDefault(x => x.TeamId == CurrentTeam?.TeamId);

        if (existing == null)
            return;

        // Capture previous identity values before changing
        var previousEmblemAssetId = existing.EmblemAssetId;
        var previousColorHex = existing.ColorHex;
        var previousTeamColorAssetId = existing.TeamColorAssetId;
        var previousEmblemBackground = existing.EmblemBackground;
        var previousEmblemBackgroundAssetId = existing.EmblemBackgroundAssetId;
        var previousTeamEffectAssetId = existing.EquippedTeamEffectAssetId;

        existing.TeamName = TeamNameEntry.Text;

        existing.Player1 = Player1Entry.Text ?? "";
        existing.Player2 = isTeamMode ? Player2Entry.Text ?? "" : "";

        existing.Player1Id = player1Id;
        existing.Player2Id = player2Id;

        existing.IsSinglePlayer = !isTeamMode;

        existing.Emblem = selectedEmblem;
        existing.EmblemAssetId = selectedEmblemAssetId;

        existing.ColorHex = selectedColor;
        existing.TeamColorAssetId = selectedColorAssetId;

        existing.EmblemBackground = selectedEmblemBackground;
        existing.EmblemBackgroundAssetId = selectedEmblemBackgroundAssetId;
        if (selectedTeamEffectOwnerPlayerId == player1Id ||
            selectedTeamEffectOwnerPlayerId == player2Id ||
            string.IsNullOrWhiteSpace(selectedTeamEffectAssetId))
        {
            existing.EquippedTeamEffectAssetId = selectedTeamEffectAssetId;
            existing.EquippedTeamEffectOwnerPlayerId =
                string.IsNullOrWhiteSpace(selectedTeamEffectAssetId)
                    ? string.Empty
                    : selectedTeamEffectOwnerPlayerId;
        }

        var rankings = await RankingService.LoadTeamsAsync();

        var rankingTeam = rankings.FirstOrDefault(x => x.TeamId == existing.TeamId);

        if (rankingTeam != null)
        {
            rankingTeam.TeamName = existing.TeamName;

            rankingTeam.Player1 = existing.Player1;
            rankingTeam.Player2 = existing.Player2;

            rankingTeam.Player1Id = existing.Player1Id;
            rankingTeam.Player2Id = existing.Player2Id;

            rankingTeam.IsSinglePlayer = existing.IsSinglePlayer;

            rankingTeam.Emblem = existing.Emblem;
            rankingTeam.EmblemAssetId = existing.EmblemAssetId;

            rankingTeam.ColorHex = existing.ColorHex;
            rankingTeam.TeamColorAssetId = existing.TeamColorAssetId;

            rankingTeam.EmblemBackground = existing.EmblemBackground;
            rankingTeam.EmblemBackgroundAssetId = existing.EmblemBackgroundAssetId;
            rankingTeam.EquippedTeamEffectAssetId = existing.EquippedTeamEffectAssetId;
            rankingTeam.EquippedTeamEffectOwnerPlayerId = existing.EquippedTeamEffectOwnerPlayerId;

            string json =
                System.Text.Json.JsonSerializer.Serialize(
                    rankings,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            string rankingFile = Path.Combine(FileSystem.AppDataDirectory, "rankings.json");

            await File.WriteAllTextAsync(rankingFile, json);
        }

        await TeamProfileService.SaveTeamsAsync(teams);
        
        // Publish to VisualEventBus for Living Visual Identity Engine
        if (!string.Equals(
            previousEmblemAssetId,
            existing.EmblemAssetId,
            StringComparison.OrdinalIgnoreCase))
        {
            var existingTeamEmblemPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, existing.TeamId },
                { VisualIdentityPayloadKeys.EmblemAssetId, existing.EmblemAssetId },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            if (!string.IsNullOrWhiteSpace(previousEmblemAssetId))
            {
                existingTeamEmblemPayload[
                    VisualIdentityPayloadKeys.PreviousEmblemAssetId] =
                    previousEmblemAssetId;
            }
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEmblemChanged, existingTeamEmblemPayload, isSticky: true, stickyExpirationMs: 0);
        }
        
        // Publish TeamColorChanged if color changed
        if (!string.Equals(previousColorHex, existing.ColorHex, StringComparison.OrdinalIgnoreCase))
        {
            var teamColorPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, existing.TeamId },
                { VisualIdentityPayloadKeys.PrimaryColorHex, existing.ColorHex },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            if (!string.IsNullOrWhiteSpace(previousColorHex))
                teamColorPayload[VisualIdentityPayloadKeys.PreviousPrimaryColorHex] = previousColorHex;
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamColorChanged, teamColorPayload, isSticky: true, stickyExpirationMs: 0);
        }
        
        // Publish TeamEffectChanged if effect changed
        if (!CanonicalAssetIdentityService.SameAssetId(previousTeamEffectAssetId, existing.EquippedTeamEffectAssetId))
        {
            var teamEffectPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, existing.TeamId },
                { VisualIdentityPayloadKeys.EffectAssetId, existing.EquippedTeamEffectAssetId },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            if (!string.IsNullOrWhiteSpace(previousTeamEffectAssetId))
                teamEffectPayload[VisualIdentityPayloadKeys.PreviousEffectAssetId] = previousTeamEffectAssetId;
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEffectChanged, teamEffectPayload, isSticky: true, stickyExpirationMs: 0);
        }
        
        // Publish TeamEmblemBackgroundChanged if background changed
        if (!CanonicalAssetIdentityService.SameAssetId(previousEmblemBackgroundAssetId, existing.EmblemBackgroundAssetId))
        {
            var emblemBackgroundPayload = new Dictionary<string, object>
            {
                { VisualIdentityPayloadKeys.TeamId, existing.TeamId },
                { VisualIdentityPayloadKeys.BackgroundAssetId, existing.EmblemBackgroundAssetId },
                { VisualIdentityPayloadKeys.TimestampUtc, DateTimeOffset.UtcNow }
            };
            if (!string.IsNullOrWhiteSpace(previousEmblemBackgroundAssetId))
                emblemBackgroundPayload[VisualIdentityPayloadKeys.PreviousBackgroundAssetId] = previousEmblemBackgroundAssetId;
            VisualEventBus.Publish(EventCategory.Team, VisualIdentityEventNames.TeamEmblemBackgroundChanged, emblemBackgroundPayload, isSticky: true, stickyExpirationMs: 0);
        }

        AppEvents.RaiseDataChanged();
        AppEvents.RaiseTeamAssetsChanged(existing.TeamId);

        await DisplayAlert("تم", "تم تحديث الفريق", "ممتاز");

        ResetForm();

        LoadedTeams = await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;
    }

    async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    void TeamNameChanged(object sender, TextChangedEventArgs e)
    {
        PreviewTeamName.Text =
            string.IsNullOrWhiteSpace(TeamNameEntry.Text)
                ? "اسم الفريق"
                : TeamNameEntry.Text;
    }

    void TeamPlayersChanged(object sender, TextChangedEventArgs? e)
    {
        if (_suppressTeamPlayersChanged)
            return;

        string player1 =
            string.IsNullOrWhiteSpace(Player1Entry.Text)
                ? "اللاعب الأول"
                : Player1Entry.Text;

        string player2 =
            string.IsNullOrWhiteSpace(Player2Entry.Text)
                ? "اللاعب الثاني"
                : Player2Entry.Text;

        PreviewPlayer1.Text = player1;
        PreviewPlayer2.Text = isTeamMode ? player2 : "";
        _ = LoadOwnedTeamAssetsAsync();
    }

    async void OnDeleteTeamClicked(object sender, TappedEventArgs e)
    {
        var image = sender as Image;
        var team = image?.BindingContext as TeamProfileModel;

        if (team == null)
            return;

        bool confirm =
            await DisplayAlert(
                "حذف الفريق",
                $"هل تريد حذف {team.TeamName} ؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        var teams = await TeamProfileService.LoadTeamsAsync();

        teams.RemoveAll(x => x.TeamId == team.TeamId);

        await TeamProfileService.SaveTeamsAsync(teams);

        AppEvents.RaiseDataChanged();

        var rankings = await RankingService.LoadTeamsAsync();

        rankings.RemoveAll(x => x.TeamId == team.TeamId);

        string rankingsJson =
            System.Text.Json.JsonSerializer.Serialize(
                rankings,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string rankingsFile = Path.Combine(FileSystem.AppDataDirectory, "rankings.json");

        await File.WriteAllTextAsync(rankingsFile, rankingsJson);

        AppEvents.RaiseDataChanged();

        LoadedTeams = await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;
    }

    async void OnDeleteAllTeamsClicked(object sender, EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً، هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(new List<TeamProfileModel>());

        AppEvents.RaiseDataChanged();

        string rankingsFile = Path.Combine(FileSystem.AppDataDirectory, "rankings.json");

        if (File.Exists(rankingsFile))
            File.Delete(rankingsFile);

        AppEvents.RaiseDataChanged();

        LoadedTeams.Clear();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;

        CurrentTeam = null;
        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        await DisplayAlert("تم", "تم حذف جميع الفرق", "ممتاز");
    }

    async void OnSelectTeamClicked(object sender, EventArgs e)
    {
        LoadedTeams = await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;

        TeamsOverlay.IsVisible = true;

        await Task.Delay(50);

        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await TeamsOverlay.FadeTo(1, 180);
            });
    }

    async Task OnDeleteAllTeamsDirect()
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً، هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(new List<TeamProfileModel>());

        AppEvents.RaiseDataChanged();

        CurrentTeam = null;
        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        await DisplayAlert("تم", "تم حذف جميع الفرق", "ممتاز");
    }

    void LoadTeam(TeamProfileModel team)
    {
        SaveButtonIcon.Source = "edit_card.png";

        CurrentTeam = team;
        IsEditMode = true;

        bool previousSuppressTeamPlayersChanged = _suppressTeamPlayersChanged;
        _suppressTeamPlayersChanged = true;
        try
        {
            if (team.IsSinglePlayer)
                OnSingleClicked(this, EventArgs.Empty);
            else
                OnTeamClicked(this, EventArgs.Empty);

            TeamNameEntry.Text = team.TeamName;
            Player1Entry.Text = team.Player1;
            Player2Entry.Text = team.Player2;
        }
        finally
        {
            _suppressTeamPlayersChanged = previousSuppressTeamPlayersChanged;
        }

        selectedEmblem = string.IsNullOrWhiteSpace(team.Emblem)
            ? "shield_3d.png"
            : team.Emblem;

        selectedEmblemAssetId = !string.IsNullOrWhiteSpace(team.EmblemAssetId)
            ? team.EmblemAssetId
            : emblemItems.FirstOrDefault(item =>
                string.Equals(item.ImagePath, selectedEmblem, StringComparison.OrdinalIgnoreCase))?.AssetId
              ?? "default_emblem_shield";

        selectedColor = string.IsNullOrWhiteSpace(team.ColorHex)
            ? "#FFD700"
            : team.ColorHex;

        selectedColorAssetId = !string.IsNullOrWhiteSpace(team.TeamColorAssetId)
            ? team.TeamColorAssetId
            : colorItems.FirstOrDefault(item =>
                string.Equals(item.ColorHex, selectedColor, StringComparison.OrdinalIgnoreCase))?.AssetId
              ?? "default_color_gold";

        selectedEmblemBackground = string.IsNullOrWhiteSpace(team.EmblemBackground)
            ? "Transparent"
            : team.EmblemBackground;

        selectedEmblemBackgroundAssetId = string.IsNullOrWhiteSpace(team.EmblemBackgroundAssetId)
            ? "default_background_transparent"
            : team.EmblemBackgroundAssetId;
        selectedTeamEffectAssetId = team.EquippedTeamEffectAssetId ?? string.Empty;
        selectedTeamEffectOwnerPlayerId = team.EquippedTeamEffectOwnerPlayerId ?? string.Empty;

        PreviewEmblem.Source =
            InventoryDisplayResolver.ResolveImageSource(
                selectedEmblem,
                "shield_3d.png");
        PreviewColorDot.BackgroundColor = Color.FromArgb(selectedColor);
        PreviewEmblemBackground.BackgroundColor = SafeColor(selectedEmblemBackground);

        SaveButtonText.Text = "تحديث الفريق‚";

        ApplyLoadedEmblem();
        ApplyLoadedColor();
        _ = LoadOwnedTeamAssetsAsync();
    }

    void ApplyLoadedEmblem()
    {
        if (!SelectEmblemByAssetId(selectedEmblemAssetId, animate: true))
            SelectEmblemByImagePath(selectedEmblem, animate: true);
    }

    void ApplyLoadedColor()
    {
        if (!SelectColorByAssetId(selectedColorAssetId, animate: true))
            SelectColorByHex(selectedColor, animate: true);

        if (!SelectEmblemBackground(selectedEmblemBackgroundAssetId))
            SelectEmblemBackground("default_background_transparent");
    }

    void ResetForm()
    {
        SaveButtonIcon.Source = "save_card.png";

        CurrentTeam = null;
        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        selectedEmblem = "shield_3d.png";
        selectedEmblemAssetId = "default_emblem_shield";

        selectedColor = "#FFD700";
        selectedColorAssetId = "default_color_gold";

        selectedEmblemBackground = "Transparent";
        selectedEmblemBackgroundAssetId = "default_background_transparent";
        selectedTeamEffectAssetId = string.Empty;
        selectedTeamEffectOwnerPlayerId = string.Empty;
        IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);

        PreviewEmblem.Source =
            InventoryDisplayResolver.ResolveImageSource(
                selectedEmblem,
                "shield_3d.png");
        PreviewColorDot.BackgroundColor = Color.FromArgb(selectedColor);
        PreviewEmblemBackground.BackgroundColor = Colors.Transparent;

        SaveButtonText.Text = "إنشاء الفريق";

        _ = LoadOwnedTeamAssetsAsync();
    }

    void OnTeamSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        var team = e.CurrentSelection[0] as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);

        TeamSearchEntry.Text = "";
        NoResultsLabel.IsVisible = false;
        TeamsOverlay.IsVisible = false;
    }

    async void OnEditTeamClicked(object sender, TappedEventArgs e)
    {
        var image = sender as Image;
        var team = image?.BindingContext as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);

        TeamsOverlay.IsVisible = false;

        await Task.Delay(100);
        await MainScroll.ScrollToAsync(0, 0, true);

        TeamNameEntry.Focus();
    }

    void OnTeamSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (LoadedTeams == null)
            return;

        string searchText = e.NewTextValue?.Trim().ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            TeamsCollection.ItemsSource = LoadedTeams;
            NoResultsLabel.IsVisible = false;
            return;
        }

        var filtered =
            LoadedTeams
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.TeamName) && x.TeamName.ToLower().Contains(searchText))
                    ||
                    (!string.IsNullOrWhiteSpace(x.Player1) && x.Player1.ToLower().Contains(searchText))
                    ||
                    (!string.IsNullOrWhiteSpace(x.Player2) && x.Player2.ToLower().Contains(searchText)))
                .ToList();

        TeamsCollection.ItemsSource = filtered;
        NoResultsLabel.IsVisible = filtered.Count == 0;
    }

    void OnCloseTeamsOverlay(object sender, EventArgs e)
    {
        TeamSearchEntry.Text = "";

        TeamsCollection.ItemsSource = LoadedTeams;

        NoResultsLabel.IsVisible = false;
        TeamsOverlay.IsVisible = false;
    }

    async Task<string> GetOrCreatePlayerIdAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return "";

        var players = await PlayerProfileService.LoadPlayersAsync();

        // If input looks like a PlayerId, prefer it first (ID-first policy)
        string trimmed = playerName.Trim();
        if (trimmed.StartsWith("P", StringComparison.OrdinalIgnoreCase) && trimmed.Length <= 8)
        {
            var byId = players.FirstOrDefault(p => string.Equals(p.PlayerId, trimmed, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
                return byId.PlayerId;
        }

        string normalized = PlayerIdentityService.NormalizePlayerName(playerName);

        var existing =
            players.FirstOrDefault(x =>
                PlayerIdentityService.NormalizePlayerName(x.PlayerName) == normalized);

        if (existing != null)
            return existing.PlayerId;

        var similarPlayer =
            players.FirstOrDefault(x =>
                IsVerySimilarName(x.PlayerName, playerName));

        if (similarPlayer != null)
        {
            bool useExisting =
      await DisplayAlert(
                    "لاعب مشابه",
                    $"تم العثور على لاعب مشابه:\n\n{similarPlayer.PlayerName}\n({similarPlayer.PlayerId})\n\nهل تقصد هذا اللاعب؟",
                    "نعم",
                    "لا");
            if (useExisting)
                return similarPlayer.PlayerId;
        }

        string nextId = $"P{(players.Count + 1):0000}";

        players.Add(
            new PlayerProfileModel
            {
                PlayerId = nextId,
                PlayerName = playerName.Trim(),
                CreatedAt = DateTime.Now
            });

        await PlayerProfileService.SavePlayersAsync(players);

        return nextId;
    }

    string GenerateNextTeamId(List<TeamProfileModel> teams)
    {
        if (teams.Count == 0)
            return "T0001";

        int maxId =
            teams
                .Where(x => !string.IsNullOrWhiteSpace(x.TeamId))
                .Select(x =>
                {
                    string numericPart = x.TeamId.Replace("T", "");

                    return int.TryParse(numericPart, out int id)
                        ? id
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

        return $"T{(maxId + 1):0000}";
    }

    bool IsVerySimilarName(string name1, string name2)
    {
        name1 = PlayerIdentityService.NormalizePlayerName(name1);
        name2 = PlayerIdentityService.NormalizePlayerName(name2);

        if (name1 == name2)
            return true;

        if (Math.Abs(name1.Length - name2.Length) > 1)
            return false;

        int differences = 0;

        int minLength = Math.Min(name1.Length, name2.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (name1[i] != name2[i])
                differences++;
        }

        differences += Math.Abs(name1.Length - name2.Length);

        return differences <= 1;
    }

    async Task RegisterPlayerAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        var players = await PlayerProfileService.LoadPlayersAsync();

        string normalized = PlayerIdentityService.NormalizePlayerName(playerName);

        bool exists =
            players.Any(x =>
                PlayerIdentityService.NormalizePlayerName(x.PlayerName) == normalized);

        if (exists)
            return;

        string nextId = $"P{(players.Count + 1):0000}";

        players.Add(
            new PlayerProfileModel
            {
                PlayerId = nextId,
                PlayerName = playerName.Trim(),
                CreatedAt = DateTime.Now
            });

        await PlayerProfileService.SavePlayersAsync(players);
    }
}


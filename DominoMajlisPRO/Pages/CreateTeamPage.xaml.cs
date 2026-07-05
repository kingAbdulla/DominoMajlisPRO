using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.Maui.Controls.Shapes;

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

public sealed class EmblemBackgroundCarouselItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public EmblemBackgroundCarouselItem(string assetId, string background, string displayName)
    {
        AssetId = assetId;
        Background = background;
        DisplayName = displayName;
    }

    public string AssetId { get; }
    public string Background { get; }
    public string DisplayName { get; }
    public ImageSource ImageSource => Background.StartsWith('#') ||
                                      string.Equals(Background, "Transparent", StringComparison.OrdinalIgnoreCase)
        ? InventoryDisplayResolver.ResolveImageSource("ss.png")
        : InventoryDisplayResolver.ResolveImageSource(Background, "ss.png");

    public Color SwatchColor => Background.StartsWith('#')
        ? Color.FromArgb(Background)
        : Color.FromArgb("#16110A");

    public bool IsImage => !Background.StartsWith('#') &&
                           !string.Equals(Background, "Transparent", StringComparison.OrdinalIgnoreCase);

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

public sealed class PlayerSelectionItem(PlayerProfileModel player) : INotifyPropertyChanged
{
    private bool _isSelected;

    public PlayerProfileModel Player { get; } = player;
    public string PlayerId => Player.PlayerId;
    public string DisplayName => string.IsNullOrWhiteSpace(Player.PlayerName) ? Player.PlayerId : Player.PlayerName;
    public ImageSource AvatarSource => PlayerProfileService.GetPlayerImageSource(Player);
    public Color AccentColor => IsSelected ? Color.FromArgb("#FFD15F") : Color.FromArgb("#6E4A18");

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

public sealed class TeamSelectionItem(TeamProfileModel team) : INotifyPropertyChanged
{
    private bool _isSelected;

    public TeamProfileModel Team { get; } = team;
    public string TeamName => string.IsNullOrWhiteSpace(Team.TeamName) ? Team.TeamId : Team.TeamName;
    public string PlayersText => Team.IsSinglePlayer || string.IsNullOrWhiteSpace(Team.Player2)
        ? Team.Player1
        : $"{Team.Player1} + {Team.Player2}";
    public ImageSource EmblemSource => Team.EmblemSource;
    public Color BackgroundColor => ResolveBackgroundColor(Team);
    public Color AccentColor => IsSelected
        ? Color.FromArgb("#FFD15F")
        : SafeTeamColor(Team.ColorHex);

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
            PropertyChanged?.Invoke(this, new(nameof(BackgroundColor)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static Color SafeTeamColor(string? value)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? Color.FromArgb("#6E4A18") : Color.FromArgb(value);
        }
        catch
        {
            return Color.FromArgb("#6E4A18");
        }
    }

    private static Color ResolveBackgroundColor(TeamProfileModel team)
    {
        if (!string.IsNullOrWhiteSpace(team.EmblemBackground) &&
            team.EmblemBackground.StartsWith('#'))
            return SafeTeamColor(team.EmblemBackground).WithAlpha(0.35f);

        return SafeTeamColor(team.ColorHex).WithAlpha(0.22f);
    }
}

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
    public bool HasPreviewImage => Effect != null && !string.IsNullOrWhiteSpace(Effect.PreviewImage);
    public ImageSource? PreviewImageSource =>
        HasPreviewImage ? InventoryDisplayResolver.ResolveImageSource(Effect!.PreviewImage) : null;

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
    private List<EmblemBackgroundCarouselItem> backgroundCarouselItems = new();
    private List<TeamEffectCarouselItem> teamEffectItems = new();
    private List<PlayerSelectionItem> playerSelectionItems = new();
    private List<TeamSelectionItem> teamSelectionItems = new();

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
    private PlayerProfileModel? _selectedPlayer1;
    private PlayerProfileModel? _selectedPlayer2;
    private int _activePlayerSlot = 1;
    private bool _suppressSelectionHandlers = false;
    private bool _suppressTeamPlayersChanged = false;
    private readonly SemaphoreSlim _ownedAssetsReloadGate = new(1, 1);
    private bool _ownedAssetsReloadRequested = false;
    private bool _isReloadingOwnedAssets = false;
    private List<TeamProfileModel> LoadedTeams = new();
    private TaskCompletionSource<bool>? _dialogCompletionSource;

    public CreateTeamPage()
    {
        InitializeComponent();

        EmblemBackgroundPicker.IsVisible = false;
        EmblemCarousel.ItemsSource = emblemItems;
        ColorCarousel.ItemsSource = colorItems;
        EmblemBackgroundPicker.ItemsSource = backgroundItems;
        EmblemBackgroundCarousel.ItemsSource = backgroundCarouselItems;
        TeamEffectCarousel.ItemsSource = teamEffectItems;
        PlayersCollection.ItemsSource = playerSelectionItems;
        TeamsCarousel.ItemsSource = teamSelectionItems;
        TuneCompactActionButtons();

        OnTeamClicked(this, EventArgs.Empty);

        TeamNameEntry.TextChanged += TeamNameChanged;
        Player1Entry.TextChanged += TeamPlayersChanged;
        Player2Entry.TextChanged += TeamPlayersChanged;
        Player1Entry.Focused += (_, _) => _activePlayerSlot = 1;
        Player2Entry.Focused += (_, _) => _activePlayerSlot = 2;
    }

    void TuneCompactActionButtons()
    {
        var actionBorders = ActionButtonsGrid.Children.OfType<Border>().ToList();
        for (int i = 0; i < actionBorders.Count; i++)
        {
            actionBorders[i].StrokeThickness = 1.25;
            actionBorders[i].Shadow = new Shadow
            {
                Brush = new SolidColorBrush(i == 0 ? Color.FromArgb("#D6A642") : Color.FromArgb("#FF5A4D")),
                Radius = 10,
                Opacity = i == 0 ? 0.42f : 0.28f,
                Offset = new Point(0, 2)
            };
        }

        foreach (var label in EnumerateVisualChildren(ActionButtonsGrid).OfType<Label>())
        {
            label.FontSize = ReferenceEquals(label, SaveButtonText) ? 12 : 11;
            label.LineBreakMode = LineBreakMode.TailTruncation;
            label.MaxLines = 1;
            label.HorizontalTextAlignment = TextAlignment.Center;
        }

        foreach (var image in EnumerateVisualChildren(ActionButtonsGrid).OfType<Image>())
        {
            image.WidthRequest = Math.Min(image.WidthRequest > 0 ? image.WidthRequest : 18, 18);
            image.HeightRequest = Math.Min(image.HeightRequest > 0 ? image.HeightRequest : 18, 18);
        }
    }

    async Task<bool> ShowProDialogAsync(
        string title,
        string message,
        string okText = "حسناً",
        string? cancelText = null)
    {
        _dialogCompletionSource?.TrySetResult(false);
        _dialogCompletionSource = new TaskCompletionSource<bool>();

        DialogTitleLabel.Text = title;
        DialogMessageLabel.Text = message;
        DialogOkLabel.Text = okText;
        DialogCancelLabel.Text = cancelText ?? string.Empty;
        DialogCancelButton.IsVisible = !string.IsNullOrWhiteSpace(cancelText);

        DialogOverlay.IsVisible = true;
        DialogOverlay.Opacity = 0;
        DialogSheet.Scale = 0.92;
        DialogSheet.TranslationY = 18;

        await Task.WhenAll(
            DialogOverlay.FadeTo(1, 160, Easing.CubicOut),
            DialogSheet.ScaleTo(1, 210, Easing.CubicOut),
            DialogSheet.TranslateTo(0, 0, 210, Easing.CubicOut));

        return await _dialogCompletionSource.Task;
    }

    Task ShowProAlertAsync(string title, string message, string okText = "حسناً") =>
        ShowProDialogAsync(title, message, okText);

    Task<bool> ShowProConfirmAsync(string title, string message, string okText = "نعم", string cancelText = "إلغاء") =>
        ShowProDialogAsync(title, message, okText, cancelText);

    async void OnDialogOkTapped(object sender, TappedEventArgs e) =>
        await CloseProDialogAsync(true);

    async void OnDialogCancelTapped(object sender, TappedEventArgs e) =>
        await CloseProDialogAsync(false);

    async Task CloseProDialogAsync(bool result)
    {
        var completion = _dialogCompletionSource;
        if (completion == null)
            return;

        await Task.WhenAll(
            DialogOverlay.FadeTo(0, 120, Easing.CubicIn),
            DialogSheet.ScaleTo(0.94, 120, Easing.CubicIn));

        DialogOverlay.IsVisible = false;
        completion.TrySetResult(result);
        if (ReferenceEquals(_dialogCompletionSource, completion))
            _dialogCompletionSource = null;
    }

    static IEnumerable<Element> EnumerateVisualChildren(Element root)
    {
        yield return root;

        switch (root)
        {
            case Border border when border.Content is Element borderContent:
                foreach (var child in EnumerateVisualChildren(borderContent))
                    yield return child;
                break;

            case Layout layout:
                foreach (var child in layout.Children.OfType<Element>())
                {
                    foreach (var nested in EnumerateVisualChildren(child))
                        yield return nested;
                }
                break;

            case ContentView contentView when contentView.Content is Element content:
                foreach (var child in EnumerateVisualChildren(content))
                    yield return child;
                break;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
        window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#050607"));
        window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#050607"));
#endif

        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;

        await LoadOwnedTeamAssetsAsync();
        await LoadPlayersForSelectionAsync();
        await LoadTeamsForSelectionAsync();
        RefreshValidationPanel();
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
                backgroundCarouselItems.Clear();

                var catalog = await StoreAssetCatalogService.LoadAsync();
                var ownedTeamEffects = new List<TeamEffectCarouselItem>();

                var teamEffectOwnerIds = new[]
                    {
                        player1?.PlayerId ?? ownerTeam?.Player1Id,
                        player2?.PlayerId ?? (isTeamMode ? ownerTeam?.Player2Id : null)
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
                var newBackgroundCarouselItems = backgroundCarouselItems.ToList();
                var newTeamEffects = ownedTeamEffects
                    .GroupBy(item => $"{item.AssetId}|{item.OwnerPlayerId}", StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
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
                        EmblemBackgroundCarousel.ItemsSource = newBackgroundCarouselItems;
                        emblemItems = newEmblems;
                        colorItems = newColors;
                        backgroundItems = newBackgrounds;
                        backgroundCarouselItems = newBackgroundCarouselItems;
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

        AddBackgroundIfMissing("default_background_transparent", "Transparent", "بدون خلفية");
    }

    async Task LoadPlayersForSelectionAsync(string? query = null)
    {
        allPlayers = await PlayerProfileService.LoadPlayersAsync();

        var normalizedQuery = NormalizeArabicSearch(query);
        var filtered = string.IsNullOrWhiteSpace(normalizedQuery)
            ? allPlayers
            : allPlayers
                .Where(player => NormalizeArabicSearch(player.PlayerName).Contains(normalizedQuery) ||
                                 NormalizeArabicSearch(player.PlayerId).Contains(normalizedQuery))
                .ToList();

        playerSelectionItems = filtered
            .Select(player =>
            {
                var item = new PlayerSelectionItem(player);
                item.IsSelected =
                    string.Equals(player.PlayerId, _selectedPlayer1?.PlayerId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(player.PlayerId, _selectedPlayer2?.PlayerId, StringComparison.OrdinalIgnoreCase);
                return item;
            })
            .ToList();

        PlayersCollection.ItemsSource = playerSelectionItems;
    }

    async Task LoadTeamsForSelectionAsync(string? query = null)
    {
        LoadedTeams = await TeamProfileService.LoadTeamsAsync();

        var normalizedQuery = NormalizeArabicSearch(query);
        var filtered = string.IsNullOrWhiteSpace(normalizedQuery)
            ? LoadedTeams
            : LoadedTeams
                .Where(team =>
                    NormalizeArabicSearch(team.TeamName).Contains(normalizedQuery) ||
                    NormalizeArabicSearch(team.Player1).Contains(normalizedQuery) ||
                    NormalizeArabicSearch(team.Player2).Contains(normalizedQuery))
                .ToList();

        teamSelectionItems = filtered
            .Select(team =>
            {
                var item = new TeamSelectionItem(team);
                item.IsSelected = CurrentTeam != null &&
                    string.Equals(team.TeamId, CurrentTeam.TeamId, StringComparison.OrdinalIgnoreCase);
                return item;
            })
            .ToList();

        TeamsCarousel.ItemsSource = teamSelectionItems;
    }

    async void OnPlayerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadPlayersForSelectionAsync(e.NewTextValue);
    }

    async void OnInlineTeamSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadTeamsForSelectionAsync(e.NewTextValue);
    }

    void OnPlayerCardTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not PlayerSelectionItem item)
            return;

        SelectPlayerFromCard(item.Player);
    }

    async void OnTeamCardTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not TeamSelectionItem item)
            return;

        LoadTeam(item.Team);
        await LoadTeamsForSelectionAsync(TeamInlineSearchEntry.Text);
        await MainScroll.ScrollToAsync(0, 0, true);
    }

    void SelectPlayerFromCard(PlayerProfileModel player)
    {
        if (player == null)
            return;

        bool fillFirst = string.IsNullOrWhiteSpace(Player1Entry.Text) || _activePlayerSlot == 1;
        bool fillSecond = isTeamMode &&
            (_activePlayerSlot == 2 || (!fillFirst && string.IsNullOrWhiteSpace(Player2Entry.Text)));

        if (fillSecond)
        {
            _selectedPlayer2 = player;
            Player2Entry.Text = player.PlayerName;
            _activePlayerSlot = 1;
        }
        else
        {
            _selectedPlayer1 = player;
            Player1Entry.Text = player.PlayerName;
            _activePlayerSlot = isTeamMode ? 2 : 1;
        }

        _ = LoadPlayersForSelectionAsync(PlayerSearchEntry.Text);
        _ = LoadOwnedTeamAssetsAsync();
        _ = UpdatePreviewAvatarsAsync();
        RefreshValidationPanel();
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
                string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            emblemItems.Add(new EmblemCarouselItem(assetId, imagePath, displayName));
    }

    void AddColorIfMissing(string assetId, string colorHex, string imagePath, string displayName)
    {
        bool exists =
            colorItems.Any(item =>
                string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ColorHex, colorHex, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            colorItems.Add(new TeamColorCarouselItem(assetId, colorHex, imagePath, displayName));
    }

    void AddBackgroundIfMissing(string assetId, string background, string displayName)
    {
        bool exists =
            backgroundItems.Any(item =>
                string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Background, background, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            backgroundItems.Add(new EmblemBackgroundPickerItem(assetId, background, displayName));
            backgroundCarouselItems.Add(new EmblemBackgroundCarouselItem(assetId, background, displayName));
        }
    }

    void OnSingleClicked(object sender, EventArgs e)
    {
        PreviewMode.Text = "فردي";
        isTeamMode = false;

        Player2Layout.IsVisible = false;
        PreviewPlayer2.IsVisible = false;
        PreviewPlayer2Host.IsVisible = false;

        string player1 = string.IsNullOrWhiteSpace(Player1Entry.Text)
            ? "اللاعب الأول"
            : Player1Entry.Text;

        PreviewPlayer1.Text = player1;
        PreviewPlayer2.Text = "";
        _selectedPlayer2 = null;

        SingleCard.Stroke = Color.FromArgb("#FFD700");
        SingleCard.BackgroundColor = Color.FromArgb("#1A1A00");

        TeamCard.Stroke = Color.FromArgb("#404040");
        TeamCard.BackgroundColor = Color.FromArgb("#151515");

        SingleCard.ScaleTo(1.05, 150);
        TeamCard.ScaleTo(1.00, 150);
        RefreshValidationPanel();
    }

    void OnTeamClicked(object sender, EventArgs e)
    {
        PreviewMode.Text = "فريق";
        isTeamMode = true;

        Player2Layout.IsVisible = true;
        if (!_suppressTeamPlayersChanged)
            TeamPlayersChanged(this, null);
        PreviewPlayer2.IsVisible = true;
        PreviewPlayer2Host.IsVisible = true;

        TeamCard.Stroke = Color.FromArgb("#FFD700");
        TeamCard.BackgroundColor = Color.FromArgb("#1A1A00");

        SingleCard.Stroke = Color.FromArgb("#404040");
        SingleCard.BackgroundColor = Color.FromArgb("#151515");

        TeamCard.ScaleTo(1.05, 150);
        SingleCard.ScaleTo(1.00, 150);
        RefreshValidationPanel();
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
            string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));

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
            string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));

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
        PreviewCard.Stroke = Color.FromArgb(selectedColor);

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

    void OnEmblemBackgroundCarouselSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandlers)
            return;

        if (e.CurrentSelection.FirstOrDefault() is EmblemBackgroundCarouselItem selected)
            SelectEmblemBackground(selected.AssetId);
    }

    bool SelectEmblemBackground(string assetId)
    {
        var selected = backgroundItems.FirstOrDefault(item =>
            string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));

        if (selected == null)
            return false;

        selectedEmblemBackgroundAssetId = selected.AssetId;
        selectedEmblemBackground = selected.Background;

        EmblemBackgroundPicker.SelectedItem = selected;
        foreach (var item in backgroundCarouselItems)
            item.IsSelected = string.Equals(item.AssetId, selected.AssetId, StringComparison.OrdinalIgnoreCase);

        var selectedVisual = backgroundCarouselItems.FirstOrDefault(item => item.IsSelected);
        if (selectedVisual != null)
            EmblemBackgroundCarousel.SelectedItem = selectedVisual;

        PreviewEmblemBackground.BackgroundColor = SafeColor(selected.Background);
        ApplyPreviewBackground(selected.Background);

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
            string.Equals(item.AssetId, assetId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
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

    void ApplyPreviewBackground(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            PreviewBackgroundImage.IsVisible = false;
            PreviewEmblemBackground.Background = new SolidColorBrush(Color.FromArgb("#070707"));
            return;
        }

        if (value.StartsWith('#'))
        {
            PreviewBackgroundImage.IsVisible = false;
            PreviewEmblemBackground.Background = new SolidColorBrush(SafeColor(value));
            return;
        }

        PreviewEmblemBackground.Background = new SolidColorBrush(Color.FromArgb("#070707"));
        PreviewBackgroundImage.Source = InventoryDisplayResolver.ResolveImageSource(value, "ss.png");
        PreviewBackgroundImage.IsVisible = true;
    }

    async Task UpdatePreviewAvatarsAsync()
    {
        var player1 = _selectedPlayer1 ?? await ResolvePlayerFromEntryAsync(Player1Entry.Text);
        var player2 = isTeamMode
            ? _selectedPlayer2 ?? await ResolvePlayerFromEntryAsync(Player2Entry.Text)
            : null;

        PreviewPlayer1Avatar.Source = player1 == null
            ? InventoryDisplayResolver.ResolveImageSource("player_card.png")
            : PlayerProfileService.GetPlayerImageSource(player1);

        PreviewPlayer2Avatar.Source = player2 == null
            ? InventoryDisplayResolver.ResolveImageSource("player_card.png")
            : PlayerProfileService.GetPlayerImageSource(player2);
    }

    async Task<PlayerProfileModel?> ResolvePlayerFromEntryAsync(string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return await PlayerProfileService.GetPlayerByIdAsync(value) ??
               await PlayerProfileService.GetPlayerByNameAsync(value);
    }

    void RefreshValidationPanel()
    {
        ValidationPanel.Children.Clear();

        string teamName = TeamNameEntry.Text?.Trim() ?? string.Empty;
        string player1 = Player1Entry.Text?.Trim() ?? string.Empty;
        string player2 = isTeamMode ? Player2Entry.Text?.Trim() ?? string.Empty : string.Empty;
        var readiness = TeamReadinessEngine.Evaluate(
            teamName,
            player1,
            player2,
            isTeamMode,
            LoadedTeams,
            CurrentTeam?.TeamId,
            value => NormalizeArabicSearch(PlayerIdentityService.NormalizePlayerName(value ?? string.Empty)),
            IsVerySimilarName);

        ReadinessStatusLabel.Text = $"{readiness.Summary} • {readiness.ReadinessScore}%";
        ReadinessStatusLabel.TextColor = readiness.CanSave
            ? Color.FromArgb("#8FE3A7")
            : readiness.ReadinessScore >= 70
                ? Color.FromArgb("#F2C46D")
                : Color.FromArgb("#FF9D8F");

        AddValidationChip("اسم الفريق", readiness.HasTeamName);
        AddValidationChip("اللاعب الأول", readiness.HasPlayerOne);
        AddValidationChip("اللاعب الثاني", readiness.HasPlayerTwo);
        AddValidationChip("تكرار اللاعبين", !readiness.HasDuplicatePlayers);
        AddValidationChip("تكرار الفريق", !readiness.HasDuplicateTeam);
        AddValidationChip("ملكية الأصول", readiness.HasRequiredAssets);
    }

    void AddValidationChip(string text, bool valid)
    {
        var border = new Border
        {
            Stroke = valid ? Color.FromArgb("#2F8F4E") : Color.FromArgb("#B64B3E"),
            StrokeThickness = 1,
            BackgroundColor = valid ? Color.FromArgb("#202F8F4E") : Color.FromArgb("#24B64B3E"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 5),
            Margin = new Thickness(4)
        };

        border.Content = new Label
        {
            Text = $"{(valid ? "✓" : "!")} {text}",
            TextColor = valid ? Color.FromArgb("#BDF2C9") : Color.FromArgb("#FFB2A8"),
            FontSize = 12
        };

        ValidationPanel.Children.Add(border);
    }

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

    static string NormalizeArabicSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(ch switch
            {
                '\u0640' => '\0',
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ئ' => 'ي',
                'ؤ' => 'و',
                'ة' => 'ه',
                _ => ch
            });
        }

        return builder
            .ToString()
            .Replace("\0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    void RaiseTeamMutationEvents(string? teamId = null)
    {
        AppEvents.RaiseTeamsChanged();
        if (!string.IsNullOrWhiteSpace(teamId))
            AppEvents.RaiseTeamAssetsChanged(teamId);
        AppEvents.RaiseRankingsChanged();
        AppEvents.RaiseDataChanged();
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
        RefreshValidationPanel();

        if (string.IsNullOrWhiteSpace(TeamNameEntry.Text))
        {
            await ShowProAlertAsync("تنبيه", "أدخل اسم الفريق");
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

        if (string.IsNullOrWhiteSpace(player1) || (isTeamMode && string.IsNullOrWhiteSpace(player2)))
        {
            await ShowProAlertAsync("تنبيه", "أدخل أسماء اللاعبين المطلوبة");
            return;
        }

        string normalizedPlayer1 = NormalizeArabicSearch(PlayerIdentityService.NormalizePlayerName(player1));
        string normalizedPlayer2 = NormalizeArabicSearch(PlayerIdentityService.NormalizePlayerName(player2));

        if (!string.IsNullOrWhiteSpace(player2) &&
            (normalizedPlayer1 == normalizedPlayer2 || IsVerySimilarName(player1, player2)))
        {
            await ShowProAlertAsync(
                "لا يمكن اختيار نفس اللاعب مرتين",
                $"لا يمكن اختيار نفس اللاعب مرتين داخل نفس الفريق\n\n{player1}\n{player2}");
            return;
        }

        var duplicateName =
            teams.Any(x =>
                x.TeamName.Trim().Equals(TeamNameEntry.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                &&
                (CurrentTeam == null || x.TeamId != CurrentTeam.TeamId));

        if (duplicateName)
        {
            await ShowProAlertAsync("تنبيه", "اسم الفريق مستخدم مسبقاً");
            return;
        }

        string player1Id = await GetOrCreatePlayerIdAsync(player1);
        string player2Id = await GetOrCreatePlayerIdAsync(player2);

        if (TeamCompositionExists(teams, player1Id, player2Id))
        {
            await ShowProAlertAsync("تنبيه", "هذه التشكيلة تمتلك فريقاً مسبقاً");
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

            RaiseTeamMutationEvents(team.TeamId);

            await ShowProAlertAsync("تم", "تم إنشاء الفريق", "ممتاز");

            ResetForm();

            return;
        }

        var existing = teams.FirstOrDefault(x => x.TeamId == CurrentTeam?.TeamId);

        if (existing == null)
            return;

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

            await RankingService.SaveTeamsAsync(rankings);
        }

        await TeamProfileService.SaveTeamsAsync(teams);

        RaiseTeamMutationEvents(existing.TeamId);

        await ShowProAlertAsync("تم", "تم تعديل الفريق", "ممتاز");

        ResetForm();

        await LoadTeamsForSelectionAsync(TeamInlineSearchEntry.Text);
    }

    async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    async void OnHelpClicked(object sender, EventArgs e)
    {
        await ShowProAlertAsync(
            "دليل الفريق",
            "اكتب اسم الفريق ثم اختر فردي أو فريق. اختر اللاعبين من الاقتراحات أو اكتب اسماً جديداً. عند اختيار لاعب موجود سيتم استخدام هويته الأصلية. لا يمكن وضع نفس اللاعب مرتين داخل الفريق نفسه. أصول الشعار واللون والخلفية والتأثيرات تظهر فقط عندما يمتلكها أحد لاعبي الفريق.");
    }

    async void OnClearFieldsClicked(object sender, TappedEventArgs e)
    {
        ResetForm();
        await ShowProAlertAsync("تم", "تم تنظيف الحقول وإعادة الخيارات الافتراضية", "ممتاز");
    }

    void TeamNameChanged(object sender, TextChangedEventArgs e)
    {
        PreviewTeamName.Text =
            string.IsNullOrWhiteSpace(TeamNameEntry.Text)
                ? "اسم الفريق"
                : TeamNameEntry.Text;
        RefreshValidationPanel();
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
        PreviewPlayer2Host.IsVisible = isTeamMode;
        _ = UpdatePreviewAvatarsAsync();
        RefreshValidationPanel();
        string activeSearch = ReferenceEquals(sender, Player2Entry) ? Player2Entry.Text ?? string.Empty : Player1Entry.Text ?? string.Empty;
        _ = LoadPlayersForSelectionAsync(activeSearch);
        _ = LoadOwnedTeamAssetsAsync();
    }

    async void OnDeleteCurrentTeamClicked(object sender, TappedEventArgs e)
    {
        if (CurrentTeam == null)
        {
            await ShowProAlertAsync("تنبيه", "اختر فريقاً أولاً قبل الحذف");
            return;
        }

        await DeleteTeamAsync(CurrentTeam);
    }

    async void OnDeleteTeamClicked(object sender, TappedEventArgs e)
    {
        var team = (sender as BindableObject)?.BindingContext as TeamProfileModel;

        if (team == null)
            return;

        await DeleteTeamAsync(team);
    }

    async Task DeleteTeamAsync(TeamProfileModel team)
    {
        if (team == null)
            return;

        bool confirm =
            await ShowProConfirmAsync(
                "حذف الفريق",
                $"هل تريد حذف {team.TeamName}؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        var teams = await TeamProfileService.LoadTeamsAsync();

        teams.RemoveAll(x => x.TeamId == team.TeamId);

        await TeamProfileService.SaveTeamsAsync(teams);

        var rankings = await RankingService.LoadTeamsAsync();

        rankings.RemoveAll(x => x.TeamId == team.TeamId);

        await RankingService.SaveTeamsAsync(rankings);

        RaiseTeamMutationEvents(team.TeamId);

        if (CurrentTeam?.TeamId == team.TeamId)
            ResetForm();

        await LoadTeamsForSelectionAsync(TeamInlineSearchEntry.Text);
    }

    async void OnDeleteAllTeamsClicked(object sender, EventArgs e)
    {
        bool confirm =
            await ShowProConfirmAsync(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً. لن يتم حذف اللاعبين. هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(new List<TeamProfileModel>());
        await RankingService.SaveTeamsAsync(new List<TeamProfileModel>());
        RaiseTeamMutationEvents();

        LoadedTeams.Clear();

        await LoadTeamsForSelectionAsync(TeamInlineSearchEntry.Text);

        CurrentTeam = null;
        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        await ShowProAlertAsync("تم", "تم حذف جميع الفرق", "ممتاز");
    }

    async void OnSelectTeamClicked(object sender, EventArgs e)
    {
        await LoadTeamsForSelectionAsync(TeamInlineSearchEntry.Text);
    }

    async void OnFooterHomeTapped(object sender, TappedEventArgs e)
    {
        await NavigateWithPolishAsync(new DominoMajlisPRO.MainPage());
    }

    async void OnFooterCreateTapped(object sender, TappedEventArgs e)
    {
        await MainScroll.ScrollToAsync(0, 0, true);
    }

    async void OnFooterPlayTapped(object sender, TappedEventArgs e)
    {
        await NavigateWithPolishAsync(new DominoMajlisPRO.MainPage(focusPlayArea: true));
    }

    async void OnFooterStoreTapped(object sender, TappedEventArgs e)
    {
        await NavigateWithPolishAsync(new DominoMajlisPRO.GalleryEngine.Pages.GalleryPage());
    }

    async Task NavigateWithPolishAsync(Page page)
    {
        await this.FadeTo(0.88, 90, Easing.CubicOut);
        await Navigation.PushAsync(page, true);
        Opacity = 1;
    }

    async Task OnDeleteAllTeamsDirect()
    {
        bool confirm =
            await ShowProConfirmAsync(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً. لن يتم حذف اللاعبين. هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(new List<TeamProfileModel>());
        await RankingService.SaveTeamsAsync(new List<TeamProfileModel>());
        RaiseTeamMutationEvents();

        CurrentTeam = null;
        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        await ShowProAlertAsync("تم", "تم حذف جميع الفرق", "ممتاز");
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
        ApplyPreviewBackground(selectedEmblemBackground);

        SaveButtonText.Text = "تعديل الفريق";

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
        ApplyPreviewBackground(selectedEmblemBackground);

        SaveButtonText.Text = "إنشاء الفريق";

        _ = LoadOwnedTeamAssetsAsync();
        RefreshValidationPanel();
    }

    void OnTeamSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        var team = e.CurrentSelection[0] as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);

        TeamInlineSearchEntry.Text = "";
        _ = LoadTeamsForSelectionAsync();
    }

    async void OnEditTeamClicked(object sender, TappedEventArgs e)
    {
        var team = (sender as BindableObject)?.BindingContext as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);

        await Task.Delay(100);
        await MainScroll.ScrollToAsync(0, 0, true);

        TeamNameEntry.Focus();
    }

    void OnTeamSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _ = LoadTeamsForSelectionAsync(e.NewTextValue);
    }

    void OnCloseTeamsOverlay(object sender, EventArgs e)
    {
        TeamInlineSearchEntry.Text = "";
        _ = LoadTeamsForSelectionAsync();
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
                await ShowProConfirmAsync(
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
        name1 = NormalizeArabicSearch(PlayerIdentityService.NormalizePlayerName(name1));
        name2 = NormalizeArabicSearch(PlayerIdentityService.NormalizePlayerName(name2));

        if (name1 == name2)
            return true;

        int minLength = Math.Min(name1.Length, name2.Length);

        // One-letter and two-letter handles must be exact. A and B are clearly different players.
        if (minLength <= 2)
            return false;

        if (name1[0] != name2[0])
            return false;

        if (Math.Abs(name1.Length - name2.Length) > 2)
            return false;

        int maxDistance = minLength >= 6 ? 2 : 1;

        return LevenshteinDistance(name1, name2) <= maxDistance;
    }

    static int LevenshteinDistance(string left, string right)
    {
        if (left.Length == 0)
            return right.Length;
        if (right.Length == 0)
            return left.Length;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (int j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (int i = 1; i <= left.Length; i++)
        {
            current[0] = i;

            for (int j = 1; j <= right.Length; j++)
            {
                int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
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


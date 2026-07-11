using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

internal enum StoreProductPreviewKind
{
    Generic,
    Avatar,
    Background,
    Frame,
    Badge,
    Effect,
    Season
}

internal sealed class StoreProductActionSheet : Grid
{
    private static readonly Color PremiumBlack = Color.FromArgb("#080808");
    private static readonly Color PremiumBlackSoft = Color.FromArgb("#14110B");
    private static readonly Color PremiumGold = Color.FromArgb("#FFD76A");
    private static readonly Color PremiumGoldDark = Color.FromArgb("#8A642E");
    private static readonly Color PrimaryText = Color.FromArgb("#FFF4D2");
    private static readonly Color SecondaryText = Color.FromArgb("#C8B58A");
    private static readonly Color MutedText = Color.FromArgb("#8F7A55");

    private readonly Border _sheet;
    private readonly BoxView _backdrop;
    private readonly Border _rarityBadge;
    private readonly Border _previewSurface;
    private readonly BoxView _accentBar;
    private readonly Image _image;
    private readonly Label _name;
    private readonly Label _rarity;
    private readonly Label _state;
    private readonly Label _description;
    private readonly Label _price;
    private readonly Label _wallet;
    private readonly Label _previewMessage;
    private readonly Button _close;
    private readonly Button _preview;
    private readonly Button _action;
    private readonly Button _cancel;
    private readonly StoreProductPreviewOverlay _previewOverlay;

    private Func<Task>? _previewAction;
    private Func<Task>? _primaryAction;
    private bool _isExecuting;
    private bool _isClosing;
    private bool _isPreviewActive;
    private int _animationVersion;
    private StoreProductPreviewKind _previewKind;
    private string _actionText = string.Empty;
    private string _imagePath = string.Empty;
    private string? _inventoryPlayerId;
    private string? _inventoryAssetId;
    private string? _inventoryStoreTypeId;
    private bool? _inventoryIsFree;
    private string? _inventorySeasonId;
    private string? _inventoryCollectionId;
    private string? _inventoryProductId;
    private int? _inventoryPrice;
    private string? _inventoryCurrencyMetadata;
    private DateTime? _inventoryAvailableFrom;
    private DateTime? _inventoryAvailableUntil;
    private InventoryProductContext? _inventoryContext;
    private TeamAssetPayloadModel? _teamAssetPayload;
    private CatalogAssetDisplay? _effectAsset;

    public StoreProductActionSheet()
    {
        IsVisible = false;
        InputTransparent = false;
        CascadeInputTransparent = false;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        FlowDirection = FlowDirection.RightToLeft;
        RowDefinitions.Add(new RowDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        _backdrop = new BoxView
        {
            Color = Color.FromArgb("#E6000000"),
            InputTransparent = false,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        var blocker = new TapGestureRecognizer();
        blocker.Tapped += (_, _) => { };
        _backdrop.GestureRecognizers.Add(blocker);
        Children.Add(_backdrop);

        _image = new Image
        {
            HeightRequest = 168,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill
        };
        _previewSurface = new Border
        {
            HeightRequest = 188,
            Padding = 10,
            Background = new SolidColorBrush(Color.FromArgb("#10000000")),
            Stroke = PremiumGoldDark,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = _image
        };

        _name = CreateLabel(22, PrimaryText, true);
        _name.HorizontalTextAlignment = TextAlignment.Center;

        _rarity = CreateLabel(12, PremiumBlack, true);
        _rarity.HorizontalTextAlignment = TextAlignment.Center;
        _rarityBadge = new Border
        {
            Padding = new Thickness(12, 5),
            HorizontalOptions = LayoutOptions.Center,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = _rarity
        };

        _state = CreateLabel(14, PremiumGold, true);
        _state.HorizontalTextAlignment = TextAlignment.Center;

        _description = CreateLabel(12, SecondaryText);
        _description.MaxLines = 3;
        _description.LineBreakMode = LineBreakMode.TailTruncation;
        _description.HorizontalTextAlignment = TextAlignment.Center;

        _price = CreateLabel(17, PremiumGold, true);
        _price.HorizontalTextAlignment = TextAlignment.Center;

        _wallet = CreateLabel(12, PrimaryText, true);
        _wallet.HorizontalTextAlignment = TextAlignment.Center;
        _wallet.IsVisible = false;

        _previewMessage = CreateLabel(11, MutedText);
        _previewMessage.Text = "معاينة المنتج";
        _previewMessage.HorizontalTextAlignment = TextAlignment.Center;
        _previewMessage.IsVisible = false;

        _close = CreateButton("✕", 18, Color.FromArgb("#221A0C"), PrimaryText);
        _close.WidthRequest = 42;
        _close.HeightRequest = 42;
        _close.Padding = 0;
        _close.CornerRadius = 21;
        _close.Clicked += (_, _) => Hide();

        _preview = CreateButton("👁 معاينة", 14, Color.FromArgb("#211B10"), PrimaryText);
        _preview.Clicked += OnPreviewClicked;

        _action = CreateButton(string.Empty, 15, PremiumGold, PremiumBlack);
        _action.FontAttributes = FontAttributes.Bold;
        _action.Clicked += OnPrimaryClicked;

        _cancel = CreateButton("✖ إلغاء", 14, Color.FromArgb("#171717"), SecondaryText);
        _cancel.Clicked += (_, _) => Hide();

        var titleRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        titleRow.Add(_close, 0, 0);
        var premiumTitle = CreateLabel(11, PremiumGold, true);
        premiumTitle.Text = "PREMIUM COLLECTION";
        premiumTitle.CharacterSpacing = 2;
        premiumTitle.HorizontalTextAlignment = TextAlignment.Center;
        premiumTitle.VerticalTextAlignment = TextAlignment.Center;
        titleRow.Add(premiumTitle, 1, 0);
        var spacer = new BoxView { WidthRequest = 42, Opacity = 0 };
        titleRow.Add(spacer, 2, 0);

        _accentBar = new BoxView
        {
            HeightRequest = 3,
            HorizontalOptions = LayoutOptions.Fill,
            Color = PremiumGold
        };

        var content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                titleRow,
                _accentBar,
                _previewSurface,
                _name,
                _rarityBadge,
                _description,
                _price,
                _wallet,
                _state,
                _previewMessage,
                _preview,
                _action,
                _cancel
            }
        };

        _sheet = new Border
        {
            InputTransparent = false,
            Padding = new Thickness(20, 14, 20, 20),
            Margin = new Thickness(12, 48, 12, 0),
            MaximumWidthRequest = 620,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(PremiumBlackSoft, 0),
                    new GradientStop(PremiumBlack, 0.58f),
                    new GradientStop(Color.FromArgb("#1B1205"), 1)
                }
            },
            Stroke = PremiumGoldDark,
            StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#99000000")),
                Offset = new Point(0, -8),
                Radius = 24,
                Opacity = 0.9f
            },
            Content = new ScrollView
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Content = content
            }
        };
        Children.Add(_sheet);

        _previewOverlay = new StoreProductPreviewOverlay
        {
            ZIndex = int.MaxValue
        };
        Children.Add(_previewOverlay);
    }

    public void Show(
        View owner,
        string imagePath,
        string name,
        string rarity,
        string description,
        string price,
        string state,
        string actionText,
        bool actionEnabled,
        Func<Task> previewAction,
        Func<Task> primaryAction,
        string? walletBefore = null,
        string? walletAfter = null,
        StoreProductPreviewKind previewKind = StoreProductPreviewKind.Generic,
        string? inventoryPlayerId = null,
        string? inventoryAssetId = null,
        string? inventoryStoreTypeId = null,
        bool? inventoryIsFree = null,
        string? inventorySeasonId = null,
        string? inventoryCollectionId = null,
        string? inventoryProductId = null,
        int? inventoryPrice = null,
        string? inventoryCurrencyMetadata = null,
        DateTime? inventoryAvailableFrom = null,
        DateTime? inventoryAvailableUntil = null)
    {
        AttachToPage(owner);
        ResetPreviewVisuals();

        _image.Source =
            InventoryDisplayResolver.ResolveImageSource(imagePath);
        _imagePath = imagePath;
        _name.Text = name;
        _rarity.Text = string.IsNullOrWhiteSpace(rarity) ? "COMMON" : rarity.ToUpperInvariant();
        _description.Text = description;
        _price.Text = price;
        _state.Text = state;
        _actionText = BuildActionText(actionText);
        _action.Text = _actionText;
        _action.IsEnabled = actionEnabled;
        _previewAction = previewAction;
        _primaryAction = primaryAction;
        _previewKind = previewKind;
        _inventoryPlayerId = inventoryPlayerId;
        _inventoryAssetId = inventoryAssetId;
        _inventoryStoreTypeId = inventoryStoreTypeId;
        _inventoryIsFree = ResolveFreeState(inventoryIsFree, price);
        _inventorySeasonId = inventorySeasonId;
        _inventoryCollectionId = inventoryCollectionId;
        _inventoryProductId = inventoryProductId;
        _inventoryPrice = inventoryPrice;
        _inventoryCurrencyMetadata = inventoryCurrencyMetadata;
        _inventoryAvailableFrom = inventoryAvailableFrom;
        _inventoryAvailableUntil = inventoryAvailableUntil;
        _inventoryContext = CreateInventoryContext(price);
        _teamAssetPayload = TeamAssetPayloadCatalog.Resolve(inventoryAssetId);
        _effectAsset = null;
        if (_previewKind == StoreProductPreviewKind.Effect &&
            !string.IsNullOrWhiteSpace(inventoryAssetId))
            _ = LoadEffectPreviewAsync(inventoryAssetId);
        _previewMessage.IsVisible = false;
        _isExecuting = false;
        _isClosing = false;

        var accent = ResolveRarityAccent(rarity);
        _accentBar.Color = accent;
        _rarityBadge.Background = new SolidColorBrush(accent);
        _rarityBadge.Stroke = string.Equals(rarity, "Immortal", StringComparison.OrdinalIgnoreCase)
            ? PremiumGold
            : accent;
        _sheet.Stroke = accent;

        if (!string.IsNullOrWhiteSpace(walletBefore) || !string.IsNullOrWhiteSpace(walletAfter))
        {
            _wallet.Text = $"الرصيد قبل الاقتناء: {walletBefore ?? "—"}\nالرصيد بعد الاقتناء: {walletAfter ?? "—"}";
            _wallet.IsVisible = true;
        }
        else
        {
            _wallet.Text = string.Empty;
            _wallet.IsVisible = false;
        }

        SetControlsEnabled(true);
        _action.IsEnabled = _teamAssetPayload != null || actionEnabled;
        _animationVersion++;
        var version = _animationVersion;
        _backdrop.CancelAnimations();
        _sheet.CancelAnimations();
        _backdrop.Opacity = 0;
        _sheet.TranslationY = 120;
        IsVisible = true;
        _ = AnimateOpenAsync(version);
        _ = RefreshInventoryStateAsync(version, inventoryPlayerId, inventoryAssetId);
    }

    private async Task RefreshInventoryStateAsync(int version, string? playerId, string? assetId)
    {
        if (_inventoryContext != null)
        {
            var inventoryState =
                await InventoryRouter.GetStateAsync(_inventoryContext);
            if (version != _animationVersion || !IsVisible || _isClosing)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version == _animationVersion && IsVisible && !_isClosing)
                    ApplyInventoryState(inventoryState);
            });
            return;
        }

        if (_teamAssetPayload != null)
        {
            await RefreshTeamAssetStateAsync(version);
            return;
        }

        if (string.IsNullOrWhiteSpace(assetId))
            return;

        var owner =
            await ApplicationUserService.GetCurrentStoreOwnerAsync();
        playerId = owner.PlayerId;

        if (version != _animationVersion || !IsVisible || _isClosing)
            return;

        if (owner.IsGhost || string.IsNullOrWhiteSpace(playerId))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version != _animationVersion ||
                    !IsVisible ||
                    _isClosing)
                {
                    return;
                }

                SetResolvedActionState(
                    "أنشئ هوية لاعب للاقتناء",
                    _inventoryIsFree == true
                        ? "إنشاء هوية لاعب"
                        : "شراء",
                    true);
            });
            return;
        }

        _inventoryPlayerId = playerId;
        var inventory = await PlayerInventoryService.GetInventoryForPlayerAsync(playerId);
        var owned = inventory.FirstOrDefault(item =>
            item.IsOwned &&
            !item.IsExpired &&
            string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));
        if (version != _animationVersion || !IsVisible || _isClosing)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (version != _animationVersion || !IsVisible || _isClosing)
                return;

            var equipCapable = StoreEquipService.IsEquipCapable(_inventoryStoreTypeId);
            if (owned?.IsEquipped == true)
                SetResolvedActionState("مجهز", "مجهز ✓", false);
            else if (owned != null)
                SetResolvedActionState("مملوك", equipCapable ? "تجهيز" : "مملوك", equipCapable);
            else if (_inventoryIsFree == true)
                SetResolvedActionState("غير مملوك", "اقتناء", true);
            else
                SetResolvedActionState("غير مملوك", "شراء", true);
        });
    }

    private async Task RefreshTeamAssetStateAsync(int version)
    {
        var payload = _teamAssetPayload;
        if (payload == null)
            return;

        var owner =
            await ApplicationUserService.GetCurrentStoreOwnerAsync();
        PlayerOwnedStoreItem? owned = null;

        if (!owner.IsGhost &&
            owner.HasPlayerProfile &&
            !string.IsNullOrWhiteSpace(owner.PlayerId))
        {
            var inventory =
                await PlayerInventoryService.GetInventoryForPlayerAsync(owner.PlayerId);
            owned = inventory.FirstOrDefault(item =>
                item.IsOwned &&
                !item.IsExpired &&
                SameId(item.AssetId, payload.TeamAssetId) &&
                SameId(item.StoreTypeId, payload.TeamAssetTypeId));
        }

        if (version != _animationVersion || !IsVisible || _isClosing)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (version != _animationVersion || !IsVisible || _isClosing)
                return;

            if (owner.IsGhost)
                SetResolvedActionState(
                    "أنشئ هوية لاعب للاقتناء",
                    _inventoryIsFree == true
                        ? "إنشاء هوية لاعب"
                        : "شراء",
                    true);
            else if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
                SetResolvedActionState(
                    "يرجى اختيار أو إنشاء فريق أولاً",
                    _inventoryIsFree == true ? "اقتناء" : "شراء",
                    true);
            else if (owned != null)
                SetResolvedActionState("\u0645\u0645\u0644\u0648\u0643", "\u0645\u0645\u0644\u0648\u0643 \u2713", false);
            else if (_inventoryIsFree == true)
                SetResolvedActionState("غير مملوك", "اقتناء", true);
            else
                SetResolvedActionState(
                    "غير مملوك",
                    "شراء",
                    true);
        });
    }

    private void SetResolvedActionState(string state, string actionText, bool enabled)
    {
        _state.Text = state;
        _actionText = actionText;
        _action.Text = actionText;
        _action.IsEnabled = enabled;
    }

    private void ApplyInventoryState(InventoryState state)
    {
        if (!state.IsAvailable && !state.IsOwned)
        {
            SetResolvedActionState(
                state.AvailabilityMessage ?? "هذا العرض غير متاح حالياً",
                "غير متاح",
                false);
            return;
        }

        if (state.RequiresIdentity)
        {
            SetResolvedActionState(
                "أنشئ هوية لاعب للاقتناء",
                "إنشاء هوية لاعب",
                true);
            return;
        }

        if (state.RequiresPlayer)
        {
            SetResolvedActionState(
                "تعذر ربط ملف اللاعب",
                "فتح حسابي",
                true);
            return;
        }

        if (state.RequiresTeam)
        {
            SetResolvedActionState(
                "يرجى اختيار أو إنشاء فريق أولاً",
                "اقتناء",
                true);
            return;
        }

        if (state.Route.OwnerScope == InventoryOwnerScope.Unsupported)
        {
            SetResolvedActionState(
                "نوع العنصر غير مدعوم",
                "غير متاح",
                false);
            return;
        }

        if (state.IsEquipped)
            SetResolvedActionState("✅ مجهز", "✅ مجهز", false);
        else if (state.IsOwned)
            SetResolvedActionState(
                "✅ مملوك",
                state.Route.Equipable ? "تجهيز" : "✅ مملوك",
                state.Route.Equipable);
        else if (state.IsFree)
            SetResolvedActionState("غير مملوك", "اقتناء", true);
        else
            SetResolvedActionState(
                "غير مملوك",
                "شراء",
                true);
    }

    public void Hide()
    {
        if (!IsVisible || _isClosing)
            return;

        _previewOverlay.HideImmediately();
        _ = AnimateCloseAsync();
    }

    private async Task AnimateOpenAsync(int version)
    {
        await Task.WhenAll(
            _backdrop.FadeToAsync(1, 180, Easing.CubicOut),
            _sheet.TranslateToAsync(0, 0, 240, Easing.CubicOut));

        if (version != _animationVersion || _isClosing)
            return;

        _backdrop.Opacity = 1;
        _sheet.TranslationY = 0;
    }

    private async Task AnimateCloseAsync()
    {
        _isClosing = true;
        _animationVersion++;
        var version = _animationVersion;
        SetControlsEnabled(false);
        _backdrop.CancelAnimations();
        _sheet.CancelAnimations();

        await Task.WhenAll(
            _backdrop.FadeToAsync(0, 160, Easing.CubicIn),
            _sheet.TranslateToAsync(0, 120, 200, Easing.CubicIn));

        if (version != _animationVersion)
            return;

        IsVisible = false;
        _previewOverlay.HideImmediately();
        ResetPreviewVisuals();
        _previewMessage.IsVisible = false;
        _previewAction = null;
        _primaryAction = null;
        _isClosing = false;

        if (Parent is Grid parent)
            parent.Children.Remove(this);
    }

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        if (_isExecuting || _previewAction is null)
            return;

        _preview.IsEnabled = false;
        try
        {
            _previewOverlay.Show(new StoreProductPreviewRequest(
                _imagePath,
                _name.Text ?? string.Empty,
                _description.Text ?? string.Empty,
                _rarity.Text ?? string.Empty,
                _price.Text ?? string.Empty,
                _state.Text ?? string.Empty,
                _previewKind,
                ResolveRarityAccent(_rarity.Text ?? string.Empty),
                _effectAsset));
            await _previewAction();
        }
        finally
        {
            if (IsVisible && !_isExecuting && !_isClosing)
                _preview.IsEnabled = true;
        }
    }

    private async Task ShowPreviewAsync()
    {
        _isPreviewActive = true;
        _image.CancelAnimations();
        _previewSurface.CancelAnimations();

        switch (_previewKind)
        {
            case StoreProductPreviewKind.Avatar:
                _previewSurface.HeightRequest = 280;
                _previewSurface.Background = new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.45),
                    Radius = 0.75,
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb("#3A2A12"), 0),
                        new GradientStop(PremiumBlack, 1)
                    }
                };
                _previewSurface.StrokeShape = new RoundRectangle { CornerRadius = 28 };
                _image.Aspect = Aspect.AspectFit;
                break;

            case StoreProductPreviewKind.Background:
                _previewSurface.HeightRequest = 240;
                _previewSurface.Padding = 0;
                _previewSurface.Background = new SolidColorBrush(PremiumBlack);
                _previewSurface.StrokeShape = new RoundRectangle { CornerRadius = 22 };
                _image.Aspect = Aspect.AspectFill;
                break;

            default:
                _previewSurface.HeightRequest = 230;
                _previewSurface.Background = new SolidColorBrush(Color.FromArgb("#241B0C"));
                _previewSurface.StrokeShape = new RoundRectangle { CornerRadius = 22 };
                _image.Aspect = Aspect.AspectFit;
                break;
        }

        _previewSurface.Scale = 0.96;
        await _previewSurface.ScaleToAsync(1, 180, Easing.CubicOut);
    }

    private async Task LoadEffectPreviewAsync(string assetId)
    {
        var asset = await StoreAssetCatalogService.ResolveAsync(assetId, null);
        if (asset == null || !IsVisible || _previewKind != StoreProductPreviewKind.Effect)
            return;
        _effectAsset = asset;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!IsVisible || _previewKind != StoreProductPreviewKind.Effect)
                return;
            var emblem = new Image
            {
                Source = InventoryDisplayResolver.ResolveImageSource(
                    string.IsNullOrWhiteSpace(asset.PreviewImage) ? "shield_3d.png" : asset.PreviewImage,
                    "shield_3d.png"),
                WidthRequest = 126,
                HeightRequest = 126,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            var layers = new Grid();
            layers.Children.Add(IdentityEffectRenderer.Create(asset, 1.24));
            layers.Children.Add(emblem);
            _previewSurface.Content = layers;
        });
    }

    private void ResetPreviewVisuals()
    {
        if (!_isPreviewActive && _previewSurface.HeightRequest == 188)
            return;

        _image.CancelAnimations();
        _previewSurface.CancelAnimations();
        _isPreviewActive = false;
        _previewSurface.Scale = 1;
        _previewSurface.HeightRequest = 188;
        _previewSurface.Padding = 10;
        _previewSurface.Background = new SolidColorBrush(Color.FromArgb("#10000000"));
        _previewSurface.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        _previewSurface.Content = _image;
        _image.Aspect = Aspect.AspectFit;
    }

    private async void OnPrimaryClicked(object? sender, EventArgs e)
    {
        if (_isExecuting || _primaryAction is null || !_action.IsEnabled)
            return;

        _isExecuting = true;
        SetControlsEnabled(false);
        _action.Text = "جارٍ التنفيذ…";

        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            if (_previewOverlay.IsOpen)
                await _previewOverlay.HideAsync();

            try
            {
                if (HasInventoryContext())
                {
                    var task = ExecuteInventoryRouterActionAsync();
                    var completed = await Task.WhenAny(task, Task.Delay(-1, cts.Token));
                    if (completed != task)
                        throw new OperationCanceledException("Operation timed out.");
                    await task; // propagate exceptions
                }
                else
                {
                    var task = _primaryAction();
                    var completed = await Task.WhenAny(task, Task.Delay(-1, cts.Token));
                    if (completed != task)
                        throw new OperationCanceledException("Operation timed out.");
                    await task;
                }
            }
            catch (OperationCanceledException)
            {
                await ShowMessageAsync("تم إلغاء العملية أو انتهى الوقت المسموح.");
            }
            catch (Exception ex)
            {
                try { await ShowMessageAsync($"حدث خطأ: {ex.Message}"); } catch { }
            }
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
            _isExecuting = false;
            if (IsVisible && !_isClosing)
            {
                _action.Text = _actionText;
                SetControlsEnabled(true);
                try
                {
                    if (HasInventoryContext())
                        await RefreshInventoryStateAsync(_animationVersion, _inventoryPlayerId, _inventoryAssetId);
                }
                catch { }
            }
        }
    }

    private bool HasInventoryContext() => _inventoryContext != null;

    private InventoryProductContext? CreateInventoryContext(string displayedPrice)
    {
        if (string.IsNullOrWhiteSpace(_inventoryAssetId) ||
            string.IsNullOrWhiteSpace(_inventoryStoreTypeId))
        {
            return null;
        }

        return new InventoryProductContext(
            string.IsNullOrWhiteSpace(_inventoryProductId)
                ? _inventoryAssetId
                : _inventoryProductId,
            _inventoryAssetId,
            _inventoryStoreTypeId,
            _inventoryPrice,
            _inventoryIsFree,
            _inventoryCurrencyMetadata,
            displayedPrice,
            _inventorySeasonId,
            _inventoryCollectionId,
            _inventoryAvailableFrom,
            _inventoryAvailableUntil);
    }

    private async Task ExecuteInventoryRouterActionAsync()
    {
        var result =
            await InventoryRouter.AcquireOrEquipAsync(_inventoryContext!);
        if (!result.State.IsAvailable && !result.State.IsOwned)
        {
            await ShowMessageAsync(
                result.State.AvailabilityMessage ??
                "هذا العرض غير متاح حالياً.");
            return;
        }

        if (result.State.RequiresIdentity)
        {
            await ShowCreateIdentityMessageAsync();
            return;
        }

        if (result.State.RequiresPlayer)
        {
            await ShowMessageAsync(
                "تعذر ربط ملف اللاعب بالحساب الحالي.");
            return;
        }

        if (result.State.RequiresTeam)
        {
            await ShowMessageAsync(
                "يرجى اختيار أو إنشاء فريق أولاً");
            return;
        }

        if (result.PaidActionRequired)
        {
            var checkout =
                await StoreCheckoutService.PurchaseAsync(
                    _inventoryContext!);
            if (!checkout.Success)
            {
                await ShowMessageAsync(checkout.Message);
                return;
            }

            _state.Text = checkout.WasEquipped
                ? "✅ مجهز"
                : "✅ مملوك";
        }
    }

    private bool HasTeamAssetContext() =>
        _teamAssetPayload != null &&
        !string.IsNullOrWhiteSpace(_teamAssetPayload.TeamAssetId) &&
        !string.IsNullOrWhiteSpace(_teamAssetPayload.TeamAssetTypeId) &&
        _inventoryIsFree.HasValue;

    private async Task ExecuteTeamAssetActionAsync()
    {
        var payload = _teamAssetPayload!;
        var owner =
            await ApplicationUserService.GetCurrentStoreOwnerAsync();

        if (owner.IsGhost)
        {
            await ShowCreateIdentityMessageAsync();
            return;
        }

        if (!owner.HasPlayerProfile)
        {
            await ShowMessageAsync(
                "تعذر ربط ملف اللاعب بالحساب الحالي.");
            return;
        }

        var owned = await PlayerInventoryService.IsOwnedAsync(owner.PlayerId, payload.TeamAssetId);
        if (owned)
        {
            AppEvents.RaiseStoreEconomyChanged(owner.PlayerId);
            return;
        }

        if (_inventoryIsFree != true)
        {
            var checkout = await StoreCheckoutService.PurchaseAsync(
                new InventoryProductContext(
                    string.IsNullOrWhiteSpace(_inventoryProductId)
                        ? payload.TeamAssetId
                        : _inventoryProductId,
                    payload.TeamAssetId,
                    payload.TeamAssetTypeId,
                    _inventoryPrice,
                    false,
                    _inventoryCurrencyMetadata,
                    _price.Text,
                    _inventorySeasonId,
                    _inventoryCollectionId));
            if (!checkout.Success)
            {
                await ShowMessageAsync(checkout.Message);
            }
            return;
        }

        var added = await PlayerInventoryService.AddOwnedItemWithoutNotificationAsync(
            owner.PlayerId,
            payload.TeamAssetId,
            payload.TeamAssetTypeId,
            "FreeAcquire",
            seasonId: _inventorySeasonId,
            collectionId: _inventoryCollectionId);

        bool acquired = added || await PlayerInventoryService.IsOwnedAsync(owner.PlayerId, payload.TeamAssetId);
        if (acquired)
        {
            // Ensure UI and other systems refresh after persistence completes.
            AppEvents.RaiseStoreEconomyChanged(owner.PlayerId);
        }
    }

    private static async Task<string?> ResolveActiveTeamIdAsync(
        string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        var player = await PlayerProfileService.GetPlayerByIdAsync(playerId);
        var currentTeamIds = (player?.CurrentTeamIds ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        if (currentTeamIds.Count == 1)
            return currentTeamIds[0];

        if (currentTeamIds.Count > 1)
            return null;

        var team =
            await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
        return string.IsNullOrWhiteSpace(team?.TeamId)
            ? null
            : team.TeamId.Trim();
    }

    private static async Task ShowCreateIdentityMessageAsync()
    {
        var page =
            Shell.Current?.CurrentPage ??
            Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
            return;

        bool openAccountHub = await page.DisplayAlertAsync(
            "حساب لاعب مطلوب",
            "أنشئ حساب لاعب أو سجّل الدخول أولاً، ثم أعد محاولة الحصول على العنصر.",
            "فتح حسابي",
            "إلغاء");

        if (openAccountHub)
            await page.Navigation.PushAsync(new PlayerProfilesPage());
    }

    private void AttachToPage(View owner)
    {
        if (Parent is Grid currentParent)
            currentParent.Children.Remove(this);

        Element? element = owner;
        while (element is not Page && element is not null)
            element = element.Parent;

        if (element is not ContentPage page || page.Content is not Grid root)
            return;

        root.Children.Add(this);
        Grid.SetRow(this, 0);
        Grid.SetColumn(this, 0);
        Grid.SetRowSpan(this, Math.Max(1, root.RowDefinitions.Count));
        Grid.SetColumnSpan(this, Math.Max(1, root.ColumnDefinitions.Count));
        ZIndex = int.MaxValue;
    }

    private static Task ShowMessageAsync(string message)
    {
        var page =
            Shell.Current?.CurrentPage ??
            Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlert("متجر الفريق", message, "حسناً") ??
               Task.CompletedTask;
    }

    private static bool SameId(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);

    private static bool ResolveFreeState(
        bool? declaredFree,
        string? displayedPrice)
    {
        if (declaredFree == true)
            return true;

        string price = displayedPrice?.Trim() ?? "";
        if (price.Length == 0)
            return declaredFree ?? false;

        if (price.Contains("مجاني", StringComparison.OrdinalIgnoreCase) ||
            price.Contains("free", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string digits = new(price
            .Where(char.IsDigit)
            .ToArray());
        return digits.Length > 0 &&
               digits.All(digit => digit == '0');
    }

    private void SetControlsEnabled(bool enabled)
    {
        _close.IsEnabled = enabled;
        _preview.IsEnabled = enabled;
        _action.IsEnabled = enabled;
        _cancel.IsEnabled = enabled;
    }

    private static string BuildActionText(string actionText)
    {
        if (actionText.Contains("استخدام", StringComparison.Ordinal))
            return "✅ استخدام";

        if (actionText.Contains("الحصول", StringComparison.Ordinal))
            return "🛒 الحصول";

        if (actionText.Contains("اقتناء", StringComparison.Ordinal) ||
            actionText.Contains("شراء", StringComparison.Ordinal))
            return "🛒 اقتناء";

        return actionText;
    }

    private static Color ResolveRarityAccent(string rarity)
    {
        return rarity.Trim().ToLowerInvariant() switch
        {
            "rare" => Color.FromArgb("#4DA3FF"),
            "epic" => Color.FromArgb("#B56CFF"),
            "legendary" => PremiumGold,
            "mythic" => Color.FromArgb("#FF4F7B"),
            "immortal" => Color.FromArgb("#FFFDF1"),
            _ => Color.FromArgb("#A7A7A7")
        };
    }

    private static Label CreateLabel(double size, Color color, bool bold = false)
    {
        return new Label
        {
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = color
        };
    }

    private static Button CreateButton(string text, double size, Color background, Color foreground)
    {
        return new Button
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            HeightRequest = 48,
            Padding = new Thickness(12, 0),
            CornerRadius = 14,
            BackgroundColor = background,
            TextColor = foreground,
            BorderColor = PremiumGoldDark,
            BorderWidth = 1
        };
    }
}

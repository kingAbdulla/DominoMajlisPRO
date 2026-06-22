using DominoMajlisPRO.GalleryEngine.Admin.Models;
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
    static readonly Color PremiumBlack = Color.FromArgb("#080808");
    static readonly Color PremiumBlackSoft = Color.FromArgb("#14110B");
    static readonly Color PremiumGold = Color.FromArgb("#FFD76A");
    static readonly Color PremiumGoldDark = Color.FromArgb("#8A642E");
    static readonly Color PrimaryText = Color.FromArgb("#FFF4D2");
    static readonly Color SecondaryText = Color.FromArgb("#C8B58A");
    static readonly Color MutedText = Color.FromArgb("#8F7A55");

    readonly Border _sheet;
    readonly BoxView _backdrop;
    readonly Border _rarityBadge;
    readonly Border _previewSurface;
    readonly BoxView _accentBar;
    readonly Image _image;
    readonly EffectPreviewHostView _effectPreview;
    readonly Label _name;
    readonly Label _rarity;
    readonly Label _state;
    readonly Label _description;
    readonly Label _price;
    readonly Label _wallet;
    readonly Label _previewMessage;
    readonly Button _close;
    readonly Button _preview;
    readonly Button _action;
    readonly Button _cancel;
    readonly StoreProductPreviewOverlay _previewOverlay;

    Func<Task>? _previewAction;
    Func<Task>? _primaryAction;
    bool _isExecuting;
    bool _isClosing;
    int _animationVersion;
    StoreProductPreviewKind _previewKind;
    string _actionText = string.Empty;
    string _imagePath = string.Empty;
    string? _inventoryPlayerId;
    string? _inventoryAssetId;
    string? _inventoryStoreTypeId;
    bool? _inventoryIsFree;
    string? _inventorySeasonId;
    string? _inventoryCollectionId;
    string? _inventoryProductId;
    int? _inventoryPrice;
    string? _inventoryCurrencyMetadata;
    InventoryProductContext? _inventoryContext;

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
            WidthRequest = 168,
            HeightRequest = 168,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _effectPreview = new EffectPreviewHostView(168)
        {
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var previewGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        previewGrid.Children.Add(_image);
        previewGrid.Children.Add(_effectPreview);

        _previewSurface = new Border
        {
            HeightRequest = 188,
            Padding = 10,
            Background = new SolidColorBrush(Color.FromArgb("#10000000")),
            Stroke = PremiumGoldDark,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = previewGrid
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
        _previewMessage.Text = "تجربة مؤقتة";
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
        titleRow.Add(new BoxView { WidthRequest = 42, Opacity = 0 }, 2, 0);

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

        _previewOverlay = new StoreProductPreviewOverlay { ZIndex = int.MaxValue };
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
        string? inventoryCurrencyMetadata = null)
    {
        AttachToPage(owner);
        ResetPreviewVisuals(force: true);

        var isEffectPreview = IsEffectPreviewCandidate(
            previewKind,
            inventoryStoreTypeId,
            inventoryAssetId,
            name,
            description,
            imagePath);

        _previewKind = isEffectPreview ? StoreProductPreviewKind.Effect : previewKind;
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
        _inventoryPlayerId = inventoryPlayerId;
        _inventoryAssetId = inventoryAssetId;
        _inventoryStoreTypeId = inventoryStoreTypeId;
        _inventoryIsFree = ResolveFreeState(inventoryIsFree, price);
        _inventorySeasonId = inventorySeasonId;
        _inventoryCollectionId = inventoryCollectionId;
        _inventoryProductId = inventoryProductId;
        _inventoryPrice = inventoryPrice;
        _inventoryCurrencyMetadata = inventoryCurrencyMetadata;
        _inventoryContext = CreateInventoryContext(price);
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
        _action.IsEnabled = actionEnabled;
        _animationVersion++;
        var version = _animationVersion;
        _backdrop.CancelAnimations();
        _sheet.CancelAnimations();
        _backdrop.Opacity = 0;
        _sheet.TranslationY = 120;
        IsVisible = true;

        if (isEffectPreview)
            RenderEffectPreview();
        else
            RenderImagePreview(imagePath);

        _ = AnimateOpenAsync(version);
        _ = RefreshInventoryStateAsync(version, inventoryPlayerId, inventoryAssetId);
    }

    void RenderImagePreview(string imagePath)
    {
        _effectPreview.Clear();
        _effectPreview.IsVisible = false;
        _image.Source = InventoryDisplayResolver.ResolveImageSource(imagePath);
        _image.IsVisible = true;
        _image.WidthRequest = 168;
        _image.HeightRequest = 168;
        _image.HorizontalOptions = LayoutOptions.Center;
        _image.VerticalOptions = LayoutOptions.Center;
    }

    void RenderEffectPreview()
    {
        _image.Source = null;
        _image.IsVisible = false;
        _effectPreview.SetHostSize(168);
        _effectPreview.IsVisible = true;
        _effectPreview.Apply(BuildEffectDisplay(), 1.0);
    }

    CatalogAssetDisplay BuildEffectDisplay()
    {
        return new CatalogAssetDisplay(
            string.IsNullOrWhiteSpace(_inventoryAssetId) ? (_name.Text ?? "Effect") : _inventoryAssetId,
            StoreProductAssetType.Effect,
            StoreProductOwnerScope.Player,
            _name.Text ?? "Effect",
            _name.Text ?? "Effect",
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            "Glow",
            "Breathing",
            0,
            "PlayerAvatar",
            "Gold",
            "Gold",
            string.Empty,
            string.Empty,
            new[] { "Glow", "Aura", "Pulse", "Particle" },
            0.95,
            1.0,
            1.0,
            1.0);
    }

    static bool IsEffectPreviewCandidate(
        StoreProductPreviewKind previewKind,
        string? storeTypeId,
        string? assetId,
        string? name,
        string? description,
        string? imagePath = null)
    {
        if (previewKind == StoreProductPreviewKind.Effect)
            return true;

        var canonical = StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        if (string.Equals(canonical, "Effect", StringComparison.OrdinalIgnoreCase))
            return true;

        var key = $"{storeTypeId} {assetId} {name} {description} {imagePath}".ToLowerInvariant();
        return key.Contains("effect") ||
               key.Contains("effects") ||
               key.Contains("effact") ||
               key.Contains("تأثير") ||
               key.Contains("تاثير") ||
               key.Contains("glow") ||
               key.Contains("aura") ||
               key.Contains("pulse") ||
               key.Contains("ring") ||
               key.Contains("lightning") ||
               key.Contains("spark") ||
               key.Contains("برق") ||
               key.Contains("هالة") ||
               key.Contains("توهج");
    }

    async Task RefreshInventoryStateAsync(int version, string? playerId, string? assetId)
    {
        if (_inventoryContext != null)
        {
            var inventoryState = await InventoryRouter.GetStateAsync(_inventoryContext);
            if (version != _animationVersion || !IsVisible || _isClosing)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version == _animationVersion && IsVisible && !_isClosing)
                    ApplyInventoryState(inventoryState);
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(assetId))
            return;

        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        playerId = owner.PlayerId;
        if (version != _animationVersion || !IsVisible || _isClosing)
            return;

        if (owner.IsGhost || string.IsNullOrWhiteSpace(playerId))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version == _animationVersion && IsVisible && !_isClosing)
                    SetResolvedActionState("أنشئ هوية لاعب للاقتناء", _inventoryIsFree == true ? "إنشاء هوية لاعب" : "شراء", true);
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
                SetResolvedActionState("✅ مجهز", "✅ مجهز", false);
            else if (owned != null)
                SetResolvedActionState("✅ مملوك", equipCapable ? "تجهيز" : "✅ مملوك", equipCapable);
            else if (_inventoryIsFree == true)
                SetResolvedActionState("غير مملوك", "اقتناء", true);
            else
                SetResolvedActionState("غير مملوك", "شراء", true);
        });
    }

    void ApplyInventoryState(dynamic state)
    {
        if (state.RequiresIdentity)
        {
            SetResolvedActionState("أنشئ هوية لاعب للاقتناء", "إنشاء هوية لاعب", true);
            return;
        }

        if (state.RequiresPlayer)
        {
            SetResolvedActionState("تعذر ربط ملف اللاعب", "فتح حسابي", true);
            return;
        }

        if (state.RequiresTeam)
        {
            SetResolvedActionState("يرجى اختيار أو إنشاء فريق أولاً", "اقتناء", true);
            return;
        }

        if (state.Route.OwnerScope == InventoryOwnerScope.Unsupported)
        {
            SetResolvedActionState("نوع العنصر غير مدعوم", "غير متاح", false);
            return;
        }

        if (state.IsEquipped)
            SetResolvedActionState("✅ مجهز", "✅ مجهز", false);
        else if (state.IsOwned)
            SetResolvedActionState("✅ مملوك", state.Route.Equipable ? "تجهيز" : "✅ مملوك", state.Route.Equipable);
        else if (state.IsFree)
            SetResolvedActionState("غير مملوك", "اقتناء", true);
        else
            SetResolvedActionState("غير مملوك", "شراء", true);
    }

    void SetResolvedActionState(string state, string actionText, bool enabled)
    {
        _state.Text = state;
        _actionText = actionText;
        _action.Text = actionText;
        _action.IsEnabled = enabled;
    }

    public void Hide()
    {
        if (!IsVisible || _isClosing)
            return;

        _previewOverlay.HideImmediately();
        _ = AnimateCloseAsync();
    }

    async Task AnimateOpenAsync(int version)
    {
        await Task.WhenAll(
            _backdrop.FadeToAsync(1, 180, Easing.CubicOut),
            _sheet.TranslateToAsync(0, 0, 240, Easing.CubicOut));

        if (version != _animationVersion || _isClosing)
            return;

        _backdrop.Opacity = 1;
        _sheet.TranslationY = 0;
    }

    async Task AnimateCloseAsync()
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
        ResetPreviewVisuals(force: true);
        _previewMessage.IsVisible = false;
        _previewAction = null;
        _primaryAction = null;
        _isClosing = false;

        if (Parent is Grid parent)
            parent.Children.Remove(this);
    }

    async void OnPreviewClicked(object? sender, EventArgs e)
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
                ResolveRarityAccent(_rarity.Text ?? string.Empty)));
            await _previewAction();
        }
        finally
        {
            if (IsVisible && !_isExecuting && !_isClosing)
                _preview.IsEnabled = true;
        }
    }

    void ResetPreviewVisuals(bool force = false)
    {
        _image.CancelAnimations();
        PlayerEffectEngine.Apply(_image, null);
        _effectPreview.Clear();
        _effectPreview.IsVisible = false;
        _previewSurface.CancelAnimations();
        _previewSurface.Scale = 1;
        _previewSurface.HeightRequest = 188;
        _previewSurface.Padding = 10;
        _previewSurface.Background = new SolidColorBrush(Color.FromArgb("#10000000"));
        _previewSurface.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        _image.Aspect = Aspect.AspectFit;
        _image.Source = null;
        _image.IsVisible = true;
    }

    async void OnPrimaryClicked(object? sender, EventArgs e)
    {
        if (_isExecuting || _primaryAction is null || !_action.IsEnabled)
            return;

        _isExecuting = true;
        SetControlsEnabled(false);
        _action.Text = "جارٍ التنفيذ…";

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
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
                    await task;
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

    bool HasInventoryContext() => _inventoryContext != null;

    InventoryProductContext? CreateInventoryContext(string displayedPrice)
    {
        if (string.IsNullOrWhiteSpace(_inventoryAssetId) ||
            string.IsNullOrWhiteSpace(_inventoryStoreTypeId))
            return null;

        return new InventoryProductContext(
            string.IsNullOrWhiteSpace(_inventoryProductId) ? _inventoryAssetId : _inventoryProductId,
            _inventoryAssetId,
            _inventoryStoreTypeId,
            _inventoryPrice,
            _inventoryIsFree,
            _inventoryCurrencyMetadata,
            displayedPrice,
            _inventorySeasonId,
            _inventoryCollectionId);
    }

    async Task ExecuteInventoryRouterActionAsync()
    {
        var result = await InventoryRouter.AcquireOrEquipAsync(_inventoryContext!);
        if (result.State.RequiresIdentity)
        {
            await ShowCreateIdentityMessageAsync();
            return;
        }

        if (result.State.RequiresPlayer)
        {
            await ShowMessageAsync("تعذر ربط ملف اللاعب بالحساب الحالي.");
            return;
        }

        if (result.State.RequiresTeam)
        {
            await ShowMessageAsync("يرجى اختيار أو إنشاء فريق أولاً");
            return;
        }

        if (result.PaidActionRequired)
        {
            var checkout = await StoreCheckoutService.PurchaseAsync(_inventoryContext!);
            if (!checkout.Success)
            {
                await ShowMessageAsync(checkout.Message);
                return;
            }

            _state.Text = checkout.WasEquipped ? "✅ مجهز" : "✅ مملوك";
        }
    }

    static async Task ShowCreateIdentityMessageAsync()
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
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

    void AttachToPage(View owner)
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

    static Task ShowMessageAsync(string message)
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlert("متجر الفريق", message, "حسناً") ?? Task.CompletedTask;
    }

    static bool ResolveFreeState(bool? declaredFree, string? displayedPrice)
    {
        if (declaredFree == true)
            return true;

        string price = displayedPrice?.Trim() ?? string.Empty;
        if (price.Length == 0)
            return declaredFree ?? false;

        if (price.Contains("مجاني", StringComparison.OrdinalIgnoreCase) ||
            price.Contains("free", StringComparison.OrdinalIgnoreCase))
            return true;

        string digits = new(price.Where(char.IsDigit).ToArray());
        return digits.Length > 0 && digits.All(digit => digit == '0');
    }

    void SetControlsEnabled(bool enabled)
    {
        _close.IsEnabled = enabled;
        _preview.IsEnabled = enabled;
        _action.IsEnabled = enabled;
        _cancel.IsEnabled = enabled;
    }

    static string BuildActionText(string actionText)
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

    static Color ResolveRarityAccent(string rarity)
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

    static Label CreateLabel(double size, Color color, bool bold = false)
    {
        return new Label
        {
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = color
        };
    }

    static Button CreateButton(string text, double size, Color background, Color foreground)
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

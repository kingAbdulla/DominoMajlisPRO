using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class BackgroundsSectionView : StoreProductsSectionBase
{
    private IReadOnlyList<BackgroundRecord> _published = Array.Empty<BackgroundRecord>();
    private readonly StoreProductActionSheet _actionSheet;
    private int _visibleItemCount = StoreNavigationState.PageSize;

    public event EventHandler<int>? AvailableItemCountChanged;

    public BackgroundsSectionView()
        : base("الخلفيات", "BACKGROUNDS", "عرض الكل")
    {
        _actionSheet = new StoreProductActionSheet();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public new void Bind(List<GalleryItem> items) { _ = items; _ = RefreshAsync(); }
    public void SetVisibleItemCount(int count) { _visibleItemCount = Math.Max(0, count); _ = RefreshAsync(); }
    public Task<StorePurchaseResult> PurchaseAsync(string playerId, string itemId) => PlayerStoreIdentityService.PurchaseAsync(playerId, itemId, StoreItemType.Background);
    public Task<bool> EquipAsync(string playerId, string itemId) => PlayerStoreIdentityService.EquipAsync(playerId, itemId, StoreItemType.Background);

    private void OnLoaded(object? sender, EventArgs e)
    {
        BackgroundsAdminService.PublishedChanged -= OnPublishedChanged;
        BackgroundsAdminService.PublishedChanged += OnPublishedChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
        AppEvents.StoreEconomyChanged += OnStoreEconomyChanged;
        _ = RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        BackgroundsAdminService.PublishedChanged -= OnPublishedChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
    }

    private void OnPublishedChanged() => _ = RefreshAsync();
    private void OnStoreEconomyChanged(string playerId) => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        _published = await StoreAssetQueryService.LoadBackgroundsAsync();
        MainThread.BeginInvokeOnMainThread(() => AvailableItemCountChanged?.Invoke(this, _published.Count));
        if (_published.Count == 0)
        {
            MainThread.BeginInvokeOnMainThread(() => base.Bind(new List<GalleryItem>()));
            return;
        }

        var playerId = await PlayerStoreIdentityService.GetDeviceIdentityPlayerIdAsync();
        var inventory = playerId == null ? Array.Empty<PlayerOwnedStoreItem>() : (await PlayerStoreIdentityService.GetInventoryAsync(playerId)).ToArray();
        var wallet = playerId == null ? null : await PlayerStoreIdentityService.GetWalletAsync(playerId);
        MainThread.BeginInvokeOnMainThread(() => BuildPurchaseGrid(playerId, wallet, inventory));
    }

    private void BuildPurchaseGrid(string? playerId, PlayerWalletModel? wallet, IReadOnlyList<PlayerOwnedStoreItem> inventory)
    {
        if (Content is not VerticalStackLayout section || section.Children.Count < 2 || section.Children[1] is not Grid grid)
            return;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        for (var column = 0; column < 3; column++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        var visible = _published.Take(_visibleItemCount).ToList();
        for (var row = 0; row < Math.Ceiling(visible.Count / 3d); row++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var index = 0; index < visible.Count; index++)
        {
            var record = visible[index];
            var owned = inventory.FirstOrDefault(item => CanonicalAssetIdentityService.SameAssetId(item.AssetId, record.Id));
            grid.Add(CreatePurchaseCard(record, playerId, wallet, owned), index % 3, index / 3);
        }
    }

    private View CreatePurchaseCard(BackgroundRecord record, string? playerId, PlayerWalletModel? wallet, PlayerOwnedStoreItem? owned)
    {
        var theme = GalleryThemeEngine.Current;
        var card = new PremiumGalleryCard();
        card.Bind(ToGalleryItem(record));
        AttachCardTap(card, () => OpenActionSheet(record, playerId, wallet, owned));
        var status = new Label { FontSize = 10, HorizontalTextAlignment = TextAlignment.Center, TextColor = theme.TextMuted, MaxLines = 1 };
        var action = new Button { FontSize = 11, HeightRequest = 34, Padding = new Thickness(4, 0) };
        var ribbons = CreateRibbons(record.Tag, record.IsLimited, record.IsFeatured);
        if (owned?.IsEquipped == true) { status.Text = "مستخدم ✓"; action.Text = "مستخدم ✓"; action.IsEnabled = false; }
        else if (owned != null) { status.Text = "مملوك"; action.Text = "استخدام"; action.Clicked += (_, _) => OpenActionSheet(record, playerId, wallet, owned); }
        else
        {
            var isFree = record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free;
            var insufficient = !isFree && wallet != null && (record.CurrencyType == BackgroundCurrencyType.Coins ? wallet.Coins : wallet.Gems) < record.Price;
            status.Text = isFree ? "مجاني" : record.CurrencyType == BackgroundCurrencyType.Coins ? $"🪙 {record.Price}" : $"💎 {record.Price}";
            action.Text = insufficient ? "الرصيد غير كافٍ" : isFree ? "الحصول" : "شراء";
            action.IsEnabled = !insufficient;
            action.Clicked += (_, _) => OpenActionSheet(record, playerId, wallet, owned);
        }
        var container = new VerticalStackLayout { Spacing = 4, Children = { ribbons, card, status, action } };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OpenActionSheet(record, playerId, wallet, owned);
        container.GestureRecognizers.Add(tap);
        return container;
    }

    private static void AttachCardTap(PremiumGalleryCard card, Action action)
    {
        var cardTap = new TapGestureRecognizer();
        cardTap.Tapped += (_, _) => action();
        card.GestureRecognizers.Add(cardTap);

        if (card.Content is View visibleRoot)
        {
            var rootTap = new TapGestureRecognizer();
            rootTap.Tapped += (_, _) => action();
            visibleRoot.GestureRecognizers.Add(rootTap);
        }
    }

    private void OpenActionSheet(BackgroundRecord record, string? playerId, PlayerWalletModel? wallet, PlayerOwnedStoreItem? owned)
    {
        var isFree = record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free;
        var state = owned?.IsEquipped == true ? "مجهز" : owned != null ? "مملوك" : "غير مملوك";
        var price = isFree ? "مجاني" : record.CurrencyType == BackgroundCurrencyType.Coins ? $"🪙 {record.Price}" : $"💎 {record.Price}";
        var actionText = owned?.IsEquipped == true ? "مجهز ✓" : owned != null ? "تجهيز" : isFree ? "اقتناء" : "شراء";
        _actionSheet.Show(
            this,
            string.IsNullOrWhiteSpace(record.ThumbnailPath) ? record.ImagePath : record.ThumbnailPath,
            string.IsNullOrWhiteSpace(record.NameAr) ? record.NameEn : record.NameAr,
            record.Rarity.ToString(),
            record.Description,
            price,
            state,
            actionText,
            owned?.IsEquipped != true && !string.IsNullOrWhiteSpace(playerId),
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            null,
            null,
            StoreProductPreviewKind.Background,
            playerId,
            record.Id,
            StoreProductAssetType.ProfileBackground.ToString(),
            isFree,
            record.SeasonId,
            record.CollectionId,
            inventoryProductId: record.Id,
            inventoryPrice: isFree ? 0 : record.Price,
            inventoryCurrencyMetadata: record.CurrencyType.ToString());
    }

    private async Task ConfirmPurchaseAsync(string? playerId, BackgroundRecord record, PlayerWalletModel? wallet, bool isFree)
    {
        if (string.IsNullOrWhiteSpace(playerId)) { await ShowMessageAsync("يرجى اختيار لاعب أولاً"); return; }
        var confirmed = isFree
            ? await ConfirmAsync("هل تريد إضافة هذا العنصر إلى مقتنياتك؟")
            : await ConfirmAsync(BuildConfirmation(record.Price, record.CurrencyType == BackgroundCurrencyType.Coins, wallet));
        if (!confirmed) return;

        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var purchaseTask = PurchaseAsync(playerId, record.Id);
            var completed = await Task.WhenAny(purchaseTask, Task.Delay(-1, cts.Token));
            if (completed != purchaseTask)
                throw new OperationCanceledException("Operation timed out.");

            var result = await purchaseTask; // propagate exceptions
            await ShowMessageAsync(result.IsSuccess ? "تم الاقتناء بنجاح" : FriendlyFailure(result.Message));
            if (result.IsSuccess)
            {
                if (await ConfirmAsync("هل تريد استخدام هذا العنصر الآن؟"))
                {
                    var equipTask = EquipAsync(playerId, record.Id);
                    var eqCompleted = await Task.WhenAny(equipTask, Task.Delay(-1, cts.Token));
                    if (eqCompleted != equipTask)
                        throw new OperationCanceledException("Equip timed out.");
                    await equipTask;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await ShowMessageAsync("تم إلغاء العملية أو انتهى الوقت المسموح.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"خطأ أثناء عملية الشراء: {ex.Message}");
        }
        finally
        {
            try { _actionSheet.Hide(); } catch { }
            _ = RefreshAsync();
        }
    }

    private async Task EquipItemAsync(string? playerId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) { await ShowMessageAsync("يرجى اختيار لاعب أولاً"); return; }
        if (!await EquipAsync(playerId, itemId)) { await ShowMessageAsync("يجب امتلاك العنصر قبل استخدامه"); return; }
        _ = RefreshAsync();
    }

    private static string BuildConfirmation(int price, bool coins, PlayerWalletModel? wallet)
    {
        var symbol = coins ? "🪙" : "💎";
        var balance = coins ? wallet?.Coins ?? 0 : wallet?.Gems ?? 0;
        return $"هل تريد اقتناء هذا العنصر مقابل {symbol}{price}؟\nرصيدك بعد الاقتناء: {symbol}{Math.Max(0, balance - price)}";
    }

    private static Task<bool> ConfirmAsync(string message)
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlert("تأكيد الاقتناء", message, "تأكيد", "إلغاء") ?? Task.FromResult(false);
    }

    private static string FriendlyFailure(string message) => message switch
    {
        "Insufficient wallet balance." => "الرصيد غير كافٍ",
        "Item is already owned." => "العنصر مملوك بالفعل",
        "Item is not published or does not exist." => "العنصر غير متاح للشراء",
        _ => message
    };

    private static View CreateRibbons(string tag, bool limited, bool featured)
    {
        var row = new HorizontalStackLayout { Spacing = 3, HorizontalOptions = LayoutOptions.Center, HeightRequest = 20 };
        if (!string.IsNullOrWhiteSpace(tag))
            row.Children.Add(CreateRibbon(tag));
        if (limited)
            row.Children.Add(CreateRibbon("محدود"));
        if (featured)
            row.Children.Add(CreateRibbon("مميز"));
        return row;
    }

    private static Border CreateRibbon(string text) => new()
    {
        Padding = new Thickness(5, 1),
        StrokeThickness = 0.6,
        Stroke = GalleryThemeEngine.Current.Stroke,
        Background = GalleryThemeEngine.Current.ActionBackground,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 7 },
        Content = new Label { Text = text, FontSize = 8, TextColor = GalleryThemeEngine.Current.Gold, MaxLines = 1 }
    };

    private static Task ShowMessageAsync(string message)
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlert("المتجر", message, "حسناً") ?? Task.CompletedTask;
    }

    private static GalleryItem ToGalleryItem(BackgroundRecord record) => new()
    {
        Id = record.Id,
        Name = string.IsNullOrWhiteSpace(record.NameAr) ? record.NameEn : record.NameAr,
        Subtitle = record.NameEn,
        Description = record.Description,
        Category = record.CategoryId,
        Rarity = record.Rarity.ToString(),
        Image = string.IsNullOrWhiteSpace(record.ThumbnailPath) ? record.ImagePath : record.ThumbnailPath,
        Price = record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free ? 0 : record.Price,
        Currency = record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free ? "Free" : record.CurrencyType.ToString(),
        IsLimited = record.IsLimited
    };
}

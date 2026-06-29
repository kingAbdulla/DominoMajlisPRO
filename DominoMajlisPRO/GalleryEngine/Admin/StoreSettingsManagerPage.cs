using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class StoreSettingsManagerPage : ContentPage
{
    private readonly Switch _storeEnabled = new();
    private readonly Switch _newArrivals = new();
    private readonly Switch _limitedOffers = new();
    private readonly Switch _categories = new();
    private readonly Entry _pageSize = new() { IsReadOnly = true };
    private readonly Label _status = new() { HorizontalTextAlignment = TextAlignment.End, LineBreakMode = LineBreakMode.WordWrap };
    private StoreRuntimeConfiguration _configuration = new();

    public StoreSettingsManagerPage()
    {
        Title = "إعدادات المتجر";
        FlowDirection = FlowDirection.RightToLeft;
        NavigationPage.SetHasNavigationBar(this, false);
        BuildPage();
        ApplyTheme();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _configuration = await StoreRuntimeConfigurationService.LoadAsync();
        _storeEnabled.IsToggled = _configuration.IsStoreEnabled;
        _newArrivals.IsToggled = _configuration.ShowNewArrivals;
        _limitedOffers.IsToggled = _configuration.ShowLimitedOffers;
        _categories.IsToggled = _configuration.ShowBrowseCategories;
        await RefreshPublishedCountAsync();
    }

    private void BuildPage()
    {
        var theme = GalleryThemeEngine.Current;
        var back = PremiumButton("‹", theme.CardBackground, theme.Gold, 42);
        back.Clicked += async (_, _) => await Navigation.PopAsync();
        var save = PremiumButton("حفظ ونشر الإعدادات", theme.Gold, Colors.Black);
        save.Clicked += async (_, _) => await SaveAsync();

        var hideAll = DangerButton("أرشفة جميع المنشورات", "إخفاء كل منشورات المتجر الحقيقية مع إبقائها قابلة للاسترجاع");
        hideAll.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RunBulkMaintenanceAsync(BulkStoreMaintenanceAction.HideAll)) });
        var deleteAll = DangerButton("حذف جميع المنشورات", "حذف كل المنشورات والمخفية من كل أقسام المتجر نهائياً");
        deleteAll.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RunBulkMaintenanceAsync(BulkStoreMaintenanceAction.DeleteAll)) });
        var hideSection = PremiumMaintenanceCard("أرشفة قسم محدد", "اختر نوع أصل واحد لإخفائه من المتجر فقط", "◐", theme);
        hideSection.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RunBulkMaintenanceAsync(BulkStoreMaintenanceAction.HideSection)) });
        var deleteSection = PremiumMaintenanceCard("حذف قسم محدد", "اختر نوع أصل واحد لحذف منشوراته نهائياً", "✕", theme);
        deleteSection.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RunBulkMaintenanceAsync(BulkStoreMaintenanceAction.DeleteSection)) });
        var deleteDrafts = PremiumMaintenanceCard("تنظيف المسودات", "حذف مسودات كل الأقسام أو قسم محدد", "⌫", theme);
        deleteDrafts.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RunBulkMaintenanceAsync(BulkStoreMaintenanceAction.DeleteDrafts)) });

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    back,
                    new Label { Text = "إعدادات المتجر", FontSize = 25, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextPrimary },
                    new Label { Text = "إعدادات النشر والظهور العامة", FontSize = 12, HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextSecondary },
                    SettingsPanel(theme),
                    _status,
                    save,
                    new Label { Text = "صيانة المنشورات", FontSize = 21, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextPrimary, Margin = new Thickness(0, 14, 0, 0) },
                    new Label { Text = "أدوات مطور لحذف أو أرشفة المنشورات ومزامنة صفحة المتجر فوراً", FontSize = 12, HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextMuted },
                    hideSection,
                    deleteSection,
                    deleteDrafts,
                    hideAll,
                    deleteAll
                }
            }
        };
    }

    private View SettingsPanel(GalleryTheme theme) => new Border
    {
        Padding = 14,
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Background = theme.CardBackground,
        Stroke = theme.Stroke,
        Content = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Row("تشغيل المتجر", _storeEnabled),
                Row("إظهار وصل حديثاً", _newArrivals),
                Row("إظهار العروض المحدودة", _limitedOffers),
                Row("إظهار بطاقات الفئات", _categories),
                new Label { Text = "عدد العناصر المنشورة حالياً", HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextSecondary },
                _pageSize
            }
        }
    };

    private async Task SaveAsync()
    {
        _configuration.IsStoreEnabled = _storeEnabled.IsToggled;
        _configuration.ShowNewArrivals = _newArrivals.IsToggled;
        _configuration.ShowLimitedOffers = _limitedOffers.IsToggled;
        _configuration.ShowBrowseCategories = _categories.IsToggled;
        await StoreRuntimeConfigurationService.SaveAsync(_configuration);
        NotifyAllStoreSections();
        await RefreshPublishedCountAsync();
        _status.Text = "تم حفظ الإعدادات ونشرها ومزامنة المتجر فوراً";
    }

    private async Task RunBulkMaintenanceAsync(BulkStoreMaintenanceAction action)
    {
        var typeId = action is BulkStoreMaintenanceAction.HideSection or BulkStoreMaintenanceAction.DeleteSection
            ? await PickStoreTypeAsync()
            : action == BulkStoreMaintenanceAction.DeleteDrafts
                ? await PickOptionalStoreTypeAsync()
                : null;
        if (typeId == CancelToken)
            return;

        var label = string.IsNullOrWhiteSpace(typeId) ? "جميع الأقسام" : DisplayType(typeId);
        var operation = action switch
        {
            BulkStoreMaintenanceAction.HideAll or BulkStoreMaintenanceAction.HideSection => "أرشفة",
            BulkStoreMaintenanceAction.DeleteDrafts => "حذف مسودات",
            _ => "حذف نهائي"
        };

        var confirm = await DisplayAlert("تأكيد صيانة المنشورات", $"سيتم تنفيذ: {operation}\nالنطاق: {label}\nهل أنت متأكد؟", "تنفيذ", "إلغاء");
        if (!confirm)
            return;

        var changed = action switch
        {
            BulkStoreMaintenanceAction.HideAll => await HideAllPublishedAsync(),
            BulkStoreMaintenanceAction.HideSection => await HidePublishedByTypeAsync(typeId),
            BulkStoreMaintenanceAction.DeleteSection => await DeletePublishedByTypeAsync(typeId),
            BulkStoreMaintenanceAction.DeleteDrafts => await DeleteDraftsByTypeAsync(typeId),
            _ => await DeleteAllPublishedAsync()
        };

        NotifyAllStoreSections();
        await RefreshPublishedCountAsync();
        _status.Text = changed == 0
            ? $"لا توجد عناصر مطابقة في {label}"
            : $"تمت العملية بنجاح: {changed} عنصر/عناصر — تمت مزامنة صفحة المتجر";
    }

    private async Task<int> CountPublishedAsync()
    {
        var arrivals = await NewArrivalsAdminService.LoadPublishedAsync();
        var offers = await LimitedOffersAdminService.LoadPublishedAsync();
        var categories = await StoreCategoriesAdminService.LoadPublishedAsync();
        var avatars = await AvatarsAdminService.LoadPublishedAsync();
        var backgrounds = await BackgroundsAdminService.LoadPublishedAsync();
        return arrivals.Count + offers.Count + categories.Count + avatars.Count + backgrounds.Count;
    }

    private async Task RefreshPublishedCountAsync() => _pageSize.Text = (await CountPublishedAsync()).ToString();

    private async Task<int> HideAllPublishedAsync()
    {
        var changed = 0;
        foreach (var item in await NewArrivalsAdminService.LoadPublishedAsync()) { await NewArrivalsAdminService.HidePublishedAsync(NewArrivalsAdminService.GetAssetId(item)); changed++; }
        foreach (var item in await LimitedOffersAdminService.LoadPublishedAsync()) { await LimitedOffersAdminService.HidePublishedAsync(LimitedOffersAdminService.GetAssetId(item)); changed++; }
        foreach (var item in await StoreCategoriesAdminService.LoadPublishedAsync()) { await StoreCategoriesAdminService.HidePublishedAsync(item.Id); changed++; }
        foreach (var item in await AvatarsAdminService.LoadPublishedAsync()) { await AvatarsAdminService.HidePublishedAsync(item.Id); changed++; }
        foreach (var item in await BackgroundsAdminService.LoadPublishedAsync()) { await BackgroundsAdminService.HidePublishedAsync(item.Id); changed++; }
        return changed;
    }

    private async Task<int> DeleteAllPublishedAsync()
    {
        var changed = 0;
        foreach (var item in await NewArrivalsAdminService.LoadManagedAsync()) { await NewArrivalsAdminService.DeletePublishedAsync(NewArrivalsAdminService.GetAssetId(item)); changed++; }
        foreach (var item in await LimitedOffersAdminService.LoadManagedOffersAsync()) { await LimitedOffersAdminService.DeletePublishedAsync(LimitedOffersAdminService.GetAssetId(item)); changed++; }
        foreach (var item in await StoreCategoriesAdminService.LoadManagedAsync()) { await StoreCategoriesAdminService.DeletePublishedAsync(item.Id); changed++; }
        foreach (var item in await AvatarsAdminService.LoadManagedAsync()) { await AvatarsAdminService.DeletePublishedAsync(item.Id); changed++; }
        foreach (var item in await BackgroundsAdminService.LoadManagedAsync()) { await BackgroundsAdminService.DeletePublishedAsync(item.Id); changed++; }
        return changed;
    }

    private async Task<int> HidePublishedByTypeAsync(string? typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            return await HideAllPublishedAsync();
        var changed = await NewArrivalsAdminService.HidePublishedByTypeAsync(typeId);
        if (IsType(typeId, StoreProductAssetType.Avatar))
            foreach (var item in await AvatarsAdminService.LoadPublishedAsync()) { await AvatarsAdminService.HidePublishedAsync(item.Id); changed++; }
        if (IsType(typeId, StoreProductAssetType.ProfileBackground))
            foreach (var item in await BackgroundsAdminService.LoadPublishedAsync()) { await BackgroundsAdminService.HidePublishedAsync(item.Id); changed++; }
        foreach (var item in (await StoreCategoriesAdminService.LoadPublishedAsync()).Where(item => CategoryMatchesType(item, typeId))) { await StoreCategoriesAdminService.HidePublishedAsync(item.Id); changed++; }
        return changed;
    }

    private async Task<int> DeletePublishedByTypeAsync(string? typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            return await DeleteAllPublishedAsync();
        var changed = await NewArrivalsAdminService.DeletePublishedByTypeAsync(typeId);
        if (IsType(typeId, StoreProductAssetType.Avatar))
            foreach (var item in await AvatarsAdminService.LoadManagedAsync()) { await AvatarsAdminService.DeletePublishedAsync(item.Id); changed++; }
        if (IsType(typeId, StoreProductAssetType.ProfileBackground))
            foreach (var item in await BackgroundsAdminService.LoadManagedAsync()) { await BackgroundsAdminService.DeletePublishedAsync(item.Id); changed++; }
        foreach (var item in (await StoreCategoriesAdminService.LoadManagedAsync()).Where(item => CategoryMatchesType(item, typeId))) { await StoreCategoriesAdminService.DeletePublishedAsync(item.Id); changed++; }
        return changed;
    }

    private async Task<int> DeleteDraftsByTypeAsync(string? typeId)
    {
        var changed = await NewArrivalsAdminService.DeleteDraftsByTypeAsync(typeId);
        if (string.IsNullOrWhiteSpace(typeId) || IsType(typeId, StoreProductAssetType.Avatar))
            foreach (var item in await AvatarsAdminService.LoadAllDraftsAsync()) { await AvatarsAdminService.DeleteDraftAsync(item.Id); changed++; }
        if (string.IsNullOrWhiteSpace(typeId) || IsType(typeId, StoreProductAssetType.ProfileBackground))
            foreach (var item in await BackgroundsAdminService.LoadAllDraftsAsync()) { await BackgroundsAdminService.DeleteDraftAsync(item.Id); changed++; }
        if (string.IsNullOrWhiteSpace(typeId))
        {
            foreach (var item in await LimitedOffersAdminService.LoadAllDraftsAsync()) { await LimitedOffersAdminService.DeleteDraftAsync(LimitedOffersAdminService.GetAssetId(item)); changed++; }
            foreach (var item in await StoreCategoriesAdminService.LoadAllDraftsAsync()) { await StoreCategoriesAdminService.DeleteDraftAsync(item.Id); changed++; }
        }
        return changed;
    }

    private static bool CategoryMatchesType(StoreCategoryRecord record, string typeId) =>
        StoreTypeRegistry.Resolve(record.Category, record.NameEn, record.NameAr)?.TypeId.Equals(typeId, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsType(string typeId, StoreProductAssetType type) =>
        string.Equals(typeId, type.ToString(), StringComparison.OrdinalIgnoreCase);

    private static void NotifyAllStoreSections()
    {
        NewArrivalsAdminService.NotifyPublishedChanged();
        LimitedOffersAdminService.NotifyPublishedChanged();
    }

    private async Task<string?> PickStoreTypeAsync()
    {
        var map = StoreTypeOptions();
        var selected = await DisplayActionSheet("اختر القسم", "إلغاء", null, map.Select(item => item.Label).ToArray());
        return selected == null || selected == "إلغاء" ? CancelToken : map.First(item => item.Label == selected).TypeId;
    }

    private async Task<string?> PickOptionalStoreTypeAsync()
    {
        var map = new List<StoreTypeChoice> { new("كل المسودات", null) };
        map.AddRange(StoreTypeOptions());
        var selected = await DisplayActionSheet("اختر نطاق المسودات", "إلغاء", null, map.Select(item => item.Label).ToArray());
        return selected == null || selected == "إلغاء" ? CancelToken : map.First(item => item.Label == selected).TypeId;
    }

    private static IReadOnlyList<StoreTypeChoice> StoreTypeOptions() => Enum.GetValues<StoreProductAssetType>()
        .Select(type => new StoreTypeChoice(DisplayType(type.ToString()), type.ToString()))
        .OrderBy(item => item.Label, StringComparer.CurrentCulture)
        .ToList();

    private static string DisplayType(string? typeId) => typeId switch
    {
        nameof(StoreProductAssetType.Avatar) => "الأفاتارات",
        nameof(StoreProductAssetType.ProfileBackground) => "خلفيات الحساب",
        nameof(StoreProductAssetType.Frame) => "الإطارات",
        nameof(StoreProductAssetType.Effect) => "تأثيرات الأفاتار",
        nameof(StoreProductAssetType.Title) => "الألقاب",
        nameof(StoreProductAssetType.Badge) => "الشارات",
        nameof(StoreProductAssetType.Emblem) => "شعارات الفرق",
        nameof(StoreProductAssetType.TeamLivingEmblem) => "الشعارات الحية",
        nameof(StoreProductAssetType.EmblemBackground) => "خلفيات الشعارات",
        nameof(StoreProductAssetType.TeamColor) => "ألوان الفرق",
        nameof(StoreProductAssetType.TeamEffect) => "تأثيرات الفرق",
        nameof(StoreProductAssetType.Bundle) => "الحزم",
        _ => typeId ?? "جميع الأقسام"
    };

    private static View Row(string text, Switch toggle)
    {
        var theme = GalleryThemeEngine.Current;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new Label { Text = text, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End, TextColor = theme.TextPrimary }, 0);
        grid.Add(toggle, 1);
        return grid;
    }

    private static Border PremiumMaintenanceCard(string title, string subtitle, string icon, GalleryTheme theme)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
        grid.Add(new Label { Text = icon, FontSize = 22, TextColor = theme.Gold, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center }, 0, 0);
        grid.Add(new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = theme.TextPrimary, HorizontalTextAlignment = TextAlignment.End }, new Label { Text = subtitle, FontSize = 11, TextColor = theme.TextMuted, HorizontalTextAlignment = TextAlignment.End, LineBreakMode = LineBreakMode.WordWrap } } }, 1, 0);
        return new Border { Padding = new Thickness(14, 12), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Background = theme.ActionBackground, Stroke = theme.Stroke, Content = grid };
    }

    private static Border DangerButton(string title, string subtitle)
    {
        var theme = GalleryThemeEngine.Current;
        return new Border
        {
            Padding = new Thickness(14, 12),
            StrokeThickness = 1.2,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1), GradientStops = { new GradientStop(Color.FromArgb("#2A1111"), 0f), new GradientStop(Color.FromArgb("#3B1717"), 1f) } },
            Stroke = Color.FromArgb("#B64A4A"),
            Content = new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFD7D7"), HorizontalTextAlignment = TextAlignment.End }, new Label { Text = subtitle, FontSize = 11, TextColor = theme.TextMuted, HorizontalTextAlignment = TextAlignment.End, LineBreakMode = LineBreakMode.WordWrap } } }
        };
    }

    private static Button PremiumButton(string text, Brush background, Color textColor, double? width = null) => new()
    {
        Text = text,
        Background = background,
        TextColor = textColor,
        WidthRequest = width ?? -1,
        HeightRequest = 44,
        CornerRadius = 14,
        FontAttributes = FontAttributes.Bold
    };

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        Background = theme.Background;
        _pageSize.TextColor = theme.TextPrimary;
        _pageSize.BackgroundColor = Color.FromArgb("#141414");
        _status.TextColor = theme.Gold;
    }

    private const string CancelToken = "__cancelled__";
    private enum BulkStoreMaintenanceAction { HideAll, DeleteAll, HideSection, DeleteSection, DeleteDrafts }
    private sealed record StoreTypeChoice(string Label, string? TypeId);
}

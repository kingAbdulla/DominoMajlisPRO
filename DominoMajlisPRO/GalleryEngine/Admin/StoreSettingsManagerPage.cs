using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class StoreSettingsManagerPage : ContentPage
{
    private readonly Switch _storeEnabled = new();
    private readonly Switch _newArrivals = new();
    private readonly Switch _limitedOffers = new();
    private readonly Switch _categories = new();
    private readonly Entry _pageSize = new()
    {
        Keyboard = Keyboard.Numeric,
        HorizontalTextAlignment = TextAlignment.End,
        FontFamily = "Tajawal-Regular",
        IsReadOnly = true
    };
    private readonly Label _status = new()
    {
        HorizontalTextAlignment = TextAlignment.End,
        FontFamily = "Tajawal-Regular",
        MaxLines = 10,
        LineBreakMode = LineBreakMode.WordWrap
    };
    private readonly List<Label> _labels = new();
    private readonly List<Border> _cards = new();
    private Button _backButton = null!;
    private Button _saveButton = null!;
    private Button _deleteAllButton = null!;
    private Button _archiveButton = null!;
    private Button _archivedItemsButton = null!;
    private Button _deleteArchivedButton = null!;
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
        NewArrivalsAdminService.PublishedChanged -= OnPublishedContentChanged;
        LimitedOffersAdminService.PublishedChanged -= OnPublishedContentChanged;
        AvatarsAdminService.PublishedChanged -= OnPublishedContentChanged;
        BackgroundsAdminService.PublishedChanged -= OnPublishedContentChanged;
        StoreCategoriesAdminService.PublishedChanged -= OnPublishedContentChanged;
        NewArrivalsAdminService.PublishedChanged += OnPublishedContentChanged;
        LimitedOffersAdminService.PublishedChanged += OnPublishedContentChanged;
        AvatarsAdminService.PublishedChanged += OnPublishedContentChanged;
        BackgroundsAdminService.PublishedChanged += OnPublishedContentChanged;
        StoreCategoriesAdminService.PublishedChanged += OnPublishedContentChanged;
        _configuration = await StoreRuntimeConfigurationService.LoadAsync();
        _storeEnabled.IsToggled = _configuration.IsStoreEnabled;
        _newArrivals.IsToggled = _configuration.ShowNewArrivals;
        _limitedOffers.IsToggled = _configuration.ShowLimitedOffers;
        _categories.IsToggled = _configuration.ShowBrowseCategories;
        await RefreshPublishedCountAsync();
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        NewArrivalsAdminService.PublishedChanged -= OnPublishedContentChanged;
        LimitedOffersAdminService.PublishedChanged -= OnPublishedContentChanged;
        AvatarsAdminService.PublishedChanged -= OnPublishedContentChanged;
        BackgroundsAdminService.PublishedChanged -= OnPublishedContentChanged;
        StoreCategoriesAdminService.PublishedChanged -= OnPublishedContentChanged;
        base.OnDisappearing();
    }

    private void OnPublishedContentChanged() =>
        MainThread.BeginInvokeOnMainThread(async () => await RefreshPublishedCountAsync());

    private void BuildPage()
    {
        _backButton = new Button
        {
            Text = "‹",
            WidthRequest = 42,
            HeightRequest = 42,
            CornerRadius = 14,
            FontSize = 24
        };
        _backButton.Clicked += async (_, _) => await Navigation.PopAsync();

        _saveButton = ActionButton("حفظ ونشر الإعدادات");
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        _deleteAllButton = ActionButton("حذف جميع منشورات المتجر");
        _deleteAllButton.Clicked += async (_, _) => await DeleteAllPublishedContentAsync();

        _archiveButton = ActionButton("أرشفة جميع المنشورات الحالية");
        _archiveButton.Clicked += async (_, _) => await ArchivePublishedContentAsync();
        _archivedItemsButton = ActionButton("عرض المنشورات المؤرشفة");
        _archivedItemsButton.Clicked += async (_, _) => await Navigation.PushAsync(new ArchivedStoreItemsPage());
        _deleteArchivedButton = ActionButton("حذف جميع المنشورات المؤرشفة نهائياً");
        _deleteArchivedButton.Clicked += async (_, _) => await DeleteArchivedContentAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 32),
                Spacing = 14,
                Children =
                {
                    _backButton,
                    TextLabel("إعدادات المتجر", 25, true),
                    TextLabel("تحكم في ظهور أقسام المتجر والمنشورات المتاحة للمستخدمين.", 12, false),
                    Row("تشغيل المتجر", _storeEnabled),
                    Row("إظهار وصل حديثًا", _newArrivals),
                    Row("إظهار العروض المحدودة", _limitedOffers),
                    Row("إظهار بطاقات الفئات", _categories),
                    TextLabel("عدد العناصر في الصفحة", 13, false),
                    _pageSize,
                    _status,
                    _saveButton,
                    _archiveButton,
                    _archivedItemsButton,
                    _deleteArchivedButton,
                    DangerPanel()
                }
            }
        };
    }

    private Button ActionButton(string text) => new()
    {
        Text = text,
        MinimumHeightRequest = 48,
        Padding = new Thickness(14, 8),
        CornerRadius = 12,
        FontFamily = "Tajawal-Regular",
        FontAttributes = FontAttributes.Bold,
        LineBreakMode = LineBreakMode.WordWrap
    };

    private View DangerPanel()
    {
        var border = new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(12),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    TextLabel("منطقة حساسة", 17, true),
                    TextLabel("ينشئ هذا الإجراء نسخة طوارئ ثم يحذف كل منشورات المتجر ومسوداته وعروضه ومحتوى الموسم. لا يحذف ملفات اللاعبين أو الفرق أو المباريات أو سجلات الملكية.", 12, false),
                    _deleteAllButton
                }
            }
        };
        _cards.Add(border);
        return border;
    }

    private async Task SaveAsync()
    {
        _configuration.IsStoreEnabled = _storeEnabled.IsToggled;
        _configuration.ShowNewArrivals = _newArrivals.IsToggled;
        _configuration.ShowLimitedOffers = _limitedOffers.IsToggled;
        _configuration.ShowBrowseCategories = _categories.IsToggled;
        await StoreRuntimeConfigurationService.SaveAsync(_configuration);
        _status.Text = "تم حفظ الإعدادات ونشرها فورًا.";
    }

    private async Task RefreshPublishedCountAsync()
    {
        var catalog = await StoreAssetCatalogService.LoadAsync();
        _pageSize.Text = catalog
            .SelectMany(item => item.ProductIds)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();
    }

    private async Task ArchivePublishedContentAsync()
    {
        var confirmed = await DisplayAlertAsync(
            "أرشفة المنشورات",
            "ستختفي المنشورات الحالية من المتجر ويمكن استعادتها لاحقاً. هل تريد المتابعة؟",
            "أرشفة",
            "إلغاء");
        if (!confirmed)
            return;

        var arrivals = await NewArrivalsAdminService.ArchiveAllPublishedAsync();
        var offers = await LimitedOffersAdminService.ArchiveAllPublishedAsync();
        await RefreshPublishedCountAsync();
        _status.Text = $"تمت أرشفة {arrivals + offers} منشوراً بنجاح.";
    }

    private async Task DeleteArchivedContentAsync()
    {
        var confirmed = await DisplayAlertAsync(
            "حذف المؤرشف نهائياً",
            "سيتم حذف جميع المنشورات المؤرشفة نهائياً ولا يمكن التراجع. هل تريد المتابعة؟",
            "حذف نهائي",
            "إلغاء");
        if (!confirmed)
            return;

        var arrivals = await NewArrivalsAdminService.DeleteAllArchivedAsync();
        var offers = await LimitedOffersAdminService.DeleteAllArchivedAsync();
        await RefreshPublishedCountAsync();
        _status.Text = $"تم حذف {arrivals + offers} منشوراً مؤرشفاً نهائياً.";
    }

    private async Task DeleteAllPublishedContentAsync()
    {
        var confirm = await DisplayAlertAsync(
            "تحذير نهائي",
            "سيتم إنشاء نسخة طوارئ ثم حذف جميع منشورات المتجر ومسوداته وعروضه وفئاته ومحتوى الموسم. لن تُحذف ملفات اللاعبين أو الفرق أو المباريات أو سجلات الملكية. هل تريد المتابعة؟",
            "متابعة",
            "إلغاء");
        if (!confirm)
            return;

        var password = await DisplayPromptAsync(
            "تأكيد المطور",
            "أدخل كلمة مرور المطور للمتابعة.",
            "متابعة",
            "إلغاء",
            "كلمة مرور المطور",
            maxLength: 128,
            keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(password))
            return;

        if (!await DeveloperLockService.VerifyPasswordAsync(password.Trim()))
        {
            _status.Text = "كلمة مرور المطور غير صحيحة. لم يُحذف أي منشور.";
            return;
        }

        var typedConfirmation = await DisplayPromptAsync(
            "تأكيد الحذف النهائي",
            "اكتب DELETE STORE حرفيًا لإكمال الحذف.",
            "حذف",
            "إلغاء",
            "DELETE STORE",
            maxLength: 32,
            keyboard: Keyboard.Text);
        if (!string.Equals(typedConfirmation, "DELETE STORE", StringComparison.Ordinal))
        {
            _status.Text = "أُلغي الحذف لأن عبارة التأكيد غير مطابقة. لم يُحذف أي منشور.";
            return;
        }

        _deleteAllButton.IsEnabled = false;
        try
        {
            var report = await StoreResetService.ResetDeveloperStoreAsync();
            await RefreshPublishedCountAsync();
            _status.Text =
                $"اكتمل حذف منشورات المتجر. المنشور: {report.PublishedCount}، المسودات: {report.DraftCount}، العروض: {report.LimitedOfferCount}، الفئات: {report.CategoryCount}، المراجع اليتيمة المرصودة: {report.OrphanReferenceCount}. النسخة الاحتياطية: {report.BackupPath}. الوقت: {report.CompletedAtUtc:O}";
        }
        catch (Exception ex)
        {
            _status.Text = $"تعذر إكمال الحذف بأمان. لم تُحذف سجلات الملكية. التفاصيل: {ex.Message}";
        }
        finally
        {
            _deleteAllButton.IsEnabled = true;
        }
    }

    private View Row(string text, Switch toggle)
    {
        var grid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(TextLabel(text, 13, false), 0);
        grid.Add(toggle, 1);

        var card = new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(12, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = grid
        };
        _cards.Add(card);
        return card;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        Background = theme.Background;
        foreach (var label in _labels)
            label.TextColor = theme.TextPrimary;
        foreach (var card in _cards)
        {
            card.Background = theme.CardBackground;
            card.Stroke = theme.Stroke;
        }

        _backButton.BackgroundColor = Colors.Transparent;
        _backButton.BorderColor = theme.Stroke;
        _backButton.TextColor = theme.Gold;
        _saveButton.BackgroundColor = theme.Accent;
        _saveButton.TextColor = Colors.Black;
        _deleteAllButton.BackgroundColor = Color.FromArgb("#6E1717");
        _deleteAllButton.TextColor = Color.FromArgb("#FFE0D2");
        _archiveButton.BackgroundColor = Color.FromArgb("#4A3512");
        _archiveButton.TextColor = Color.FromArgb("#FFE7A8");
        _archivedItemsButton.BackgroundColor = Color.FromArgb("#161A1F");
        _archivedItemsButton.TextColor = theme.Gold;
        _deleteArchivedButton.BackgroundColor = Color.FromArgb("#491414");
        _deleteArchivedButton.TextColor = Color.FromArgb("#FFB8AE");
        _pageSize.BackgroundColor = Colors.Transparent;
        _pageSize.TextColor = theme.TextPrimary;
        _pageSize.PlaceholderColor = theme.TextMuted;
        _status.TextColor = theme.Gold;
    }

    private Label TextLabel(string text, double size, bool bold)
    {
        var label = new Label
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 4,
            LineBreakMode = LineBreakMode.WordWrap
        };
        _labels.Add(label);
        return label;
    }
}

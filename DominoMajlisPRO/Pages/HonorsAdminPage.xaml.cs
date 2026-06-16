using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class HonorsAdminPage : ContentPage
{
    string developerIdentityText = "";

    public HonorsAdminPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ApplyRoleBasedVisibilityAsync();
        await RefreshDeveloperIdentityAsync();
    }

    async Task ApplyRoleBasedVisibilityAsync()
    {
        HonorRoleType currentRole =
            await HonorIdentityService.GetCurrentRoleAsync();

        bool isDeveloper =
            currentRole == HonorRoleType.Developer;

        DeveloperIdentitySection.IsVisible = isDeveloper;
        DeveloperToolsSection.IsVisible = isDeveloper;
        FounderToolsSection.IsVisible = isDeveloper;
        HonorToolsSection.IsVisible = isDeveloper;
        DeveloperRecoveryCodesSection.IsVisible = isDeveloper;
        DeveloperVaultSection.IsVisible = isDeveloper;
        DeveloperFullResetSection.IsVisible = isDeveloper;
    }

    async Task RefreshDeveloperIdentityAsync()
    {
        try
        {
            var developer =
                await DeveloperLockService.LoadAsync();

            var honorIdentity =
                await HonorIdentityService.LoadAsync();

            developerIdentityText =
                $"Developer ID: {developer.DeveloperId}\n" +
                $"Username: {developer.Username}\n" +
                $"Role: {honorIdentity.Role}\n" +
                $"Device: {developer.DeviceFingerprint}\n" +
                $"Created At: {developer.CreatedAt:yyyy/MM/dd HH:mm}\n" +
                $"Last Login: {developer.LastLoginAt:yyyy/MM/dd HH:mm}\n" +
                $"Last Password Change: {developer.LastPasswordChange:yyyy/MM/dd HH:mm}";

            DeveloperIdentityLabel.Text =
                developerIdentityText;
        }
        catch
        {
            developerIdentityText =
                "تعذر تحميل هوية المطور.";

            DeveloperIdentityLabel.Text =
                developerIdentityText;
        }
    }

    async void OnRefreshDeveloperIdentityClicked(object sender, EventArgs e)
    {
        await RefreshDeveloperIdentityAsync();
    }

    async void OnCopyDeveloperIdentityClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(developerIdentityText))
            await RefreshDeveloperIdentityAsync();

        await Clipboard.Default.SetTextAsync(
            developerIdentityText);

        await DisplayAlert(
            "تم",
            "تم نسخ هوية المطور.",
            "حسناً");
    }

    async void OnCreateDeveloperKeyClicked(object sender, EventArgs e)
    {
        string key =
            await SpecialHonorsService.CreateDeveloperActivationKeyAsync();

        GeneratedKeyLabel.Text =
            $"Developer Key:\n{key}";
    }

    async void OnCreateFounderKeyClicked(object sender, EventArgs e)
    {
        string key =
            await SpecialHonorsService.CreateFounderActivationKeyAsync();

        GeneratedKeyLabel.Text =
            $"Founder Key:\n{key}";
    }

    async void OnActivateDeveloperClicked(object sender, EventArgs e)
    {
        var result =
            await SpecialHonorsService.ActivateDeveloperAsync(
                PlayerIdEntry.Text?.Trim() ?? "",
                ActivationKeyEntry.Text?.Trim() ?? "");

        ActivationResultLabel.Text =
            result.Success
            ? $"تم التفعيل بنجاح\n\nRecovery Key:\n{result.RecoveryKey}\n\nMaster Recovery Key:\n{result.MasterKey}\n\nاحفظ هذه المفاتيح الآن. لن تظهر مرة أخرى."
            : result.Message;
    }

    async void OnActivateFounderClicked(object sender, EventArgs e)
    {
        var result =
            await SpecialHonorsService.ActivateFounderAsync(
                PlayerIdEntry.Text?.Trim() ?? "",
                ActivationKeyEntry.Text?.Trim() ?? "");

        ActivationResultLabel.Text =
            result.Success
            ? $"تم التفعيل بنجاح\n\nRecovery Key:\n{result.RecoveryKey}\n\nMaster Recovery Key:\n{result.MasterKey}\n\nاحفظ هذه المفاتيح الآن. لن تظهر مرة أخرى."
            : result.Message;
    }

    async void OnRecoverHonorClicked(object sender, EventArgs e)
    {
        bool success =
            await SpecialHonorsService.RecoverHonorAsync(
                RecoveryPlayerIdEntry.Text?.Trim() ?? "",
                RecoveryKeyEntry.Text?.Trim() ?? "");

        RecoveryResultLabel.Text =
            success
            ? "تمت استعادة الشرف بنجاح"
            : "فشلت عملية الاستعادة";
    }

    async void OnGenerateDeveloperRecoveryCodesClicked(object sender, EventArgs e)
    {
        string username =
            RecoveryCodesUsernameEntry.Text?.Trim() ?? "";

        string password =
            RecoveryCodesPasswordEntry.Text?.Trim() ?? "";

        try
        {
            List<string> codes =
                await DeveloperLockService.RegenerateRecoveryCodesAsync(
                    username,
                    password);

            string filePath =
                await CreateDeveloperRecoveryCodesFileAsync(
                    username,
                    codes);

            DeveloperRecoveryCodesResultLabel.Text =
                "تم توليد 5 أكواد استرداد جديدة.\nتم إلغاء الأكواد القديمة.\nتم إنشاء ملف TXT للمشاركة.";

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Developer Recovery Codes",
                    File = new ShareFile(filePath)
                });
        }
        catch (Exception ex)
        {
            DeveloperRecoveryCodesResultLabel.Text =
                ex.Message;

            await DisplayAlert(
                "خطأ",
                ex.Message,
                "حسناً");
        }
    }

    async Task<string> CreateDeveloperRecoveryCodesFileAsync(
        string username,
        List<string> codes)
    {
        string fileName =
            $"DominoMajlisPRO_Developer_RecoveryCodes_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt";

        string filePath =
            Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

        string content =
            "Domino Majlis PRO - Developer Recovery Codes\n" +
            "============================================\n\n" +
            $"Developer Username: {username}\n" +
            $"Generated At: {DateTime.Now:yyyy/MM/dd HH:mm:ss}\n\n" +
            "IMPORTANT:\n" +
            "- These codes reset the Developer password only.\n" +
            "- Each code can be used one time only.\n" +
            "- Generating new codes invalidates all previous codes.\n" +
            "- If all app files are deleted, use Developer Vault instead.\n\n" +
            "Recovery Codes:\n\n" +
            string.Join(Environment.NewLine, codes);

        await File.WriteAllTextAsync(
            filePath,
            content);

        return filePath;
    }

    async void OnExportDeveloperVaultClicked(object sender, EventArgs e)
    {
        string password =
            DeveloperVaultPasswordEntry.Text?.Trim() ?? "";

        try
        {
            var lockData =
                await DeveloperLockService.LoadAsync();

            bool valid =
                await DeveloperLockService.VerifyLoginAsync(
                    lockData.Username,
                    password);

            if (!valid)
            {
                DeveloperVaultResultLabel.Text =
                    "كلمة مرور المطور غير صحيحة.";

                await DisplayAlert(
                    "مرفوض",
                    "كلمة مرور المطور غير صحيحة.",
                    "حسناً");

                return;
            }

            string vaultPath =
                await DeveloperVaultService.ExportVaultAsync(password);

            DeveloperVaultResultLabel.Text =
                $"تم إنشاء خزنة المطور:\n{Path.GetFileName(vaultPath)}";

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Developer Vault - Domino Majlis PRO",
                    File = new ShareFile(vaultPath)
                });
        }
        catch (Exception ex)
        {
            DeveloperVaultResultLabel.Text =
                ex.Message;

            await DisplayAlert(
                "خطأ",
                ex.Message,
                "OK");
        }
    }

    async void OnImportDeveloperVaultClicked(object sender, EventArgs e)
    {
        string password =
            DeveloperVaultPasswordEntry.Text?.Trim() ?? "";

        try
        {
            FileResult? file =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "اختر ملف Developer Vault"
                    });

            if (file == null)
                return;

            await DeveloperVaultService.ImportVaultAsync(
                file,
                password);

            DeveloperVaultResultLabel.Text =
                "تم استيراد خزنة المطور بنجاح.";

            await DisplayAlert(
                "تم",
                "تم استيراد خزنة المطور بنجاح. أغلق التطبيق وافتحه من جديد.",
                "OK");
        }
        catch (Exception ex)
        {
            DeveloperVaultResultLabel.Text =
                ex.Message;

            await DisplayAlert(
                "خطأ",
                ex.Message,
                "OK");
        }
    }

    async void OnDeveloperFullResetClicked(object sender, EventArgs e)
    {
        HonorRoleType currentRole =
            await HonorIdentityService.GetCurrentRoleAsync();

        if (currentRole != HonorRoleType.Developer)
        {
            await DisplayAlert(
                "مرفوض",
                "هذه الأداة متاحة للمطور فقط.",
                "OK");

            return;
        }

        bool confirm =
            await DisplayAlert(
                "تحذير خطير",
                "سيتم إنشاء نسخة احتياطية قابلة للمشاركة ثم حذف جميع ملفات بيانات التطبيق.\n\nهل تريد المتابعة؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        string typed =
            await DisplayPromptAsync(
                "تأكيد نهائي",
                "اكتب RESET للمتابعة:",
                "تنفيذ",
                "إلغاء");

        if (typed != "RESET")
        {
            await DisplayAlert(
                "تم الإلغاء",
                "لم يتم حذف أي بيانات.",
                "OK");

            return;
        }

        var result =
            await DataMaintenanceService.FullResetAllAppDataAsync();

        DeveloperFullResetResultLabel.Text =
            result.Message;

        await DisplayAlert(
            "تم التصفير",
            result.Message,
            "OK");

        await Share.Default.RequestAsync(
            new ShareFileRequest
            {
                Title = "نسخة احتياطية قبل تصفير Domino Majlis PRO",
                File = new ShareFile(result.BackupPath)
            });
    }

    async void OnBackImageTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}
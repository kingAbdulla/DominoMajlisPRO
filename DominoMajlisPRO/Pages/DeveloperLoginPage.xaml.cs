using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class DeveloperLoginPage : ContentPage
{
    bool isSetupMode = false;
    bool isRecoveryMode = false;

    string lastRecoveryCodesText = "";
    string lastUsername = "";
    string lastPassword = "";

    public DeveloperLoginPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool hasAccount =
            await DeveloperLockService.HasDeveloperAccountAsync();

        if (hasAccount)
            ShowLoginMode();
        else
            ShowSetupMode();
    }

    void ShowSetupMode()
    {
        isSetupMode = true;
        isRecoveryMode = false;

        PageModeLabel.Text = "First Developer Setup";
        TitleLabel.Text = "إنشاء حساب المطور الأول";
        DescriptionLabel.Text =
            "أنشئ اسم مستخدم وكلمة مرور للمطور. سيتم توليد 5 أكواد استرداد ومشاركتها كملف TXT.";

        UsernameEntry.Text = "";
        PasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
        RecoveryCodeEntry.Text = "";

        PasswordEntry.Placeholder = "Password";
        ConfirmPasswordEntry.Placeholder = "Confirm Password";

        UsernameEntry.IsVisible = true;
        PasswordEntry.IsVisible = true;
        ConfirmPasswordEntry.IsVisible = true;
        RecoveryCodeEntry.IsVisible = false;

        PrimaryActionButton.Text = "إنشاء حساب المطور";
        RecoveryButton.IsVisible = false;
        BackToLoginButton.IsVisible = false;

        AuthenticatedToolsSection.IsVisible = false;
        RecoveryCodesSection.IsVisible = false;
        ResultLabel.Text = "";
    }

    void ShowLoginMode()
    {
        isSetupMode = false;
        isRecoveryMode = false;

        PageModeLabel.Text = "Developer Login";
        TitleLabel.Text = "تسجيل دخول المطور";
        DescriptionLabel.Text =
            "أدخل اسم المستخدم وكلمة مرور المطور للوصول إلى أدوات الإدارة.";

        UsernameEntry.Text = "";
        PasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
        RecoveryCodeEntry.Text = "";

        PasswordEntry.Placeholder = "Password";
        ConfirmPasswordEntry.Placeholder = "Confirm Password";

        UsernameEntry.IsVisible = true;
        PasswordEntry.IsVisible = true;
        ConfirmPasswordEntry.IsVisible = false;
        RecoveryCodeEntry.IsVisible = false;

        PrimaryActionButton.Text = "دخول";
        RecoveryButton.IsVisible = true;
        BackToLoginButton.IsVisible = false;

        AuthenticatedToolsSection.IsVisible = false;
        RecoveryCodesSection.IsVisible = false;
        ResultLabel.Text = "";
    }

    void ShowRecoveryMode()
    {
        isSetupMode = false;
        isRecoveryMode = true;

        PageModeLabel.Text = "Developer Recovery";
        TitleLabel.Text = "استعادة كلمة مرور المطور";
        DescriptionLabel.Text =
            "أدخل اسم المستخدم، كود استرداد غير مستخدم، ثم كلمة مرور جديدة.";

        UsernameEntry.Text = "";
        PasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
        RecoveryCodeEntry.Text = "";

        UsernameEntry.IsVisible = true;
        RecoveryCodeEntry.IsVisible = true;
        PasswordEntry.IsVisible = true;
        ConfirmPasswordEntry.IsVisible = true;

        PasswordEntry.Placeholder = "New Password";
        ConfirmPasswordEntry.Placeholder = "Confirm New Password";

        PrimaryActionButton.Text = "إعادة تعيين كلمة المرور";
        RecoveryButton.IsVisible = false;
        BackToLoginButton.IsVisible = true;

        AuthenticatedToolsSection.IsVisible = false;
        RecoveryCodesSection.IsVisible = false;
        ResultLabel.Text = "";
    }

    async void OnPrimaryActionClicked(
        object sender,
        EventArgs e)
    {
        if (isSetupMode)
        {
            await SetupFirstDeveloperAsync();
            return;
        }

        if (isRecoveryMode)
        {
            await ResetPasswordWithRecoveryCodeAsync();
            return;
        }

        await LoginDeveloperAsync();
    }

    async Task SetupFirstDeveloperAsync()
    {
        lastUsername =
            UsernameEntry.Text?.Trim() ?? "";

        lastPassword =
            PasswordEntry.Text?.Trim() ?? "";

        var result =
            await DeveloperLockService.SetupFirstDeveloperAsync(
                lastUsername,
                lastPassword,
                ConfirmPasswordEntry.Text?.Trim() ?? "");

        if (!result.Success)
        {
            ResultLabel.Text = result.Message;

            await DisplayAlert(
                "تنبيه",
                result.Message,
                "حسناً");

            return;
        }

        await ShowAndShareRecoveryCodesAsync(
            result.RecoveryCodes,
            "Developer_First_Setup");

        AuthenticatedToolsSection.IsVisible = true;

        ResultLabel.Text =
            result.Message;

        await DisplayAlert(
            "تم",
            result.Message,
            "حسناً");
    }

    async Task LoginDeveloperAsync()
    {
        lastUsername =
            UsernameEntry.Text?.Trim() ?? "";

        lastPassword =
            PasswordEntry.Text?.Trim() ?? "";

        bool success =
            await DeveloperLockService.VerifyLoginAsync(
                lastUsername,
                lastPassword);

        if (!success)
        {
            ResultLabel.Text =
                "اسم المستخدم أو كلمة المرور غير صحيحة.";

            await DisplayAlert(
                "مرفوض",
                "اسم المستخدم أو كلمة المرور غير صحيحة.",
                "حسناً");

            return;
        }

        AuthenticatedToolsSection.IsVisible = true;

        await Navigation.PushAsync(
            new HonorsAdminPage());
    }

    async Task ResetPasswordWithRecoveryCodeAsync()
    {
        lastUsername =
            UsernameEntry.Text?.Trim() ?? "";

        lastPassword =
            PasswordEntry.Text?.Trim() ?? "";

        var result =
            await DeveloperLockService.ResetPasswordWithRecoveryCodeAsync(
                lastUsername,
                RecoveryCodeEntry.Text?.Trim() ?? "",
                lastPassword,
                ConfirmPasswordEntry.Text?.Trim() ?? "");

        ResultLabel.Text =
            result.Message;

        await DisplayAlert(
            result.Success ? "تم" : "مرفوض",
            result.Message,
            "حسناً");

        if (result.Success)
        {
            AuthenticatedToolsSection.IsVisible = true;
            ShowLoginMode();
        }
    }

    async void OnGenerateRecoveryCodesClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lastUsername) ||
                string.IsNullOrWhiteSpace(lastPassword))
            {
                await DisplayAlert(
                    "تنبيه",
                    "سجل الدخول أولاً لتأكيد هوية المطور.",
                    "حسناً");

                return;
            }

            List<string> codes =
                await DeveloperLockService.RegenerateRecoveryCodesAsync(
                    lastUsername,
                    lastPassword);

            await ShowAndShareRecoveryCodesAsync(
                codes,
                "Developer_Recovery_Codes");

            await DisplayAlert(
                "تم",
                "تم توليد 5 أكواد استرداد جديدة. الأكواد القديمة لم تعد صالحة.",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                ex.Message,
                "حسناً");
        }
    }

    async Task ShowAndShareRecoveryCodesAsync(
        List<string> codes,
        string filePrefix)
    {
        lastRecoveryCodesText =
            string.Join(
                Environment.NewLine,
                codes);

        RecoveryCodesLabel.Text =
            lastRecoveryCodesText;

        RecoveryCodesSection.IsVisible =
            true;

        string filePath =
            await CreateRecoveryCodesTextFileAsync(
                codes,
                filePrefix);

        await Share.Default.RequestAsync(
            new ShareFileRequest
            {
                Title = "Developer Recovery Codes",
                File = new ShareFile(filePath)
            });
    }

    async Task<string> CreateRecoveryCodesTextFileAsync(
        List<string> codes,
        string filePrefix)
    {
        string fileName =
            $"{filePrefix}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt";

        string filePath =
            Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

        string content =
            "Domino Majlis PRO - Developer Recovery Codes\n" +
            "============================================\n\n" +
            $"Generated At: {DateTime.Now:yyyy/MM/dd HH:mm:ss}\n\n" +
            "IMPORTANT:\n" +
            "- Each code can be used one time only.\n" +
            "- Save this file outside the app.\n" +
            "- If app data is fully deleted, use Developer Vault instead.\n\n" +
            string.Join(Environment.NewLine, codes);

        await File.WriteAllTextAsync(
            filePath,
            content);

        return filePath;
    }

    async void OnShareRecoveryCodesFileClicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(lastRecoveryCodesText))
        {
            await DisplayAlert(
                "تنبيه",
                "لا توجد أكواد استرداد حالياً.",
                "حسناً");

            return;
        }

        string[] codes =
            lastRecoveryCodesText
                .Split(
                    Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries);

        string filePath =
            await CreateRecoveryCodesTextFileAsync(
                codes.ToList(),
                "Developer_Recovery_Codes");

        await Share.Default.RequestAsync(
            new ShareFileRequest
            {
                Title = "Developer Recovery Codes",
                File = new ShareFile(filePath)
            });
    }

    async void OnImportVaultClicked(
        object sender,
        EventArgs e)
    {
        string password =
            VaultPasswordEntry.Text?.Trim() ?? "";

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

            await DisplayAlert(
                "تم",
                "تم استيراد Developer Vault بنجاح. أغلق التطبيق وافتحه من جديد.",
                "حسناً");

            ShowLoginMode();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                ex.Message,
                "حسناً");
        }
    }

    void OnRecoveryModeClicked(
        object sender,
        EventArgs e)
    {
        ShowRecoveryMode();
    }

    void OnBackToLoginClicked(
        object sender,
        EventArgs e)
    {
        ShowLoginMode();
    }

    async void OnContinueToAdminClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new HonorsAdminPage());
    }

    async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
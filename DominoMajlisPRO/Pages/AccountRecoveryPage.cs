using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public sealed class AccountRecoveryPage : ContentPage
{
    readonly VerticalStackLayout contentHost;
    readonly SupabaseRecoveryOtpService otpService = new();

    Entry? usernameEntry;
    Entry? emailEntry;
    Entry? otpEntry;
    Entry? recoveryCodeEntry;
    Entry? newPasswordEntry;
    Entry? confirmPasswordEntry;
    Label? errorLabel;

    public AccountRecoveryPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Colors.Black;

        contentHost = new VerticalStackLayout
        {
            Spacing = 18,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(18, 30)
        };

        Content = new Grid
        {
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#020202"), 0),
                    new(Color.FromArgb("#151006"), 0.55f),
                    new(Color.FromArgb("#050505"), 1)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Children =
            {
                new BoxView
                {
                    Color = Color.FromArgb("#22D4AF37"),
                    CornerRadius = 220,
                    WidthRequest = 260,
                    HeightRequest = 260,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    Opacity = 0.35
                },
                contentHost
            }
        };

        ShowOptions();
    }

    void ShowOptions()
    {
        contentHost.Children.Clear();
        contentHost.Children.Add(Title("استعادة الحساب"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Info("اختر طريقة الاستعادة المناسبة لحسابك."),
                PrimaryButton("الاستعادة عبر رمز البريد الإلكتروني", ShowEmailOtpRequest),
                SecondaryButton("الاستعادة عبر Recovery Code", ShowRecoveryCodeReset),
                SecondaryButton("الاستعادة عبر أسئلة الأمان", ShowSecurityQuestionsPlaceholder),
                GhostButton("الرجوع", async () => await Navigation.PopAsync())
            }
        }));
    }

    void ShowEmailOtpRequest()
    {
        contentHost.Children.Clear();
        usernameEntry = EntryField("Username اسم المستخدم");
        emailEntry = EntryField("البريد الإلكتروني المسجل", keyboard: Keyboard.Email);
        errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("رمز البريد الإلكتروني"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Info("سيتم إرسال رمز تحقق مكوّن من 6 أرقام إلى البريد المرتبط بنفس اسم المستخدم."),
                usernameEntry,
                emailEntry,
                errorLabel,
                PrimaryButton("إرسال الرمز", async () => await RequestEmailOtpAsync()),
                GhostButton("الرجوع", ShowOptions)
            }
        }));
    }

    async Task RequestEmailOtpAsync()
    {
        if (errorLabel == null || usernameEntry == null || emailEntry == null)
            return;

        SetInlineError(errorLabel, "");

        string username = usernameEntry.Text?.Trim() ?? "";
        string email = emailEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
        {
            SetInlineError(errorLabel, "أدخل اسم المستخدم والبريد الإلكتروني.");
            return;
        }

        var result = await otpService.RequestEmailOtpAsync(username, email);
        if (!result.Success)
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        ShowEmailOtpReset(username, email, result.Message);
    }

    void ShowEmailOtpReset(string username, string email, string serverMessage)
    {
        contentHost.Children.Clear();
        otpEntry = EntryField("رمز التحقق 6 أرقام", keyboard: Keyboard.Numeric);
        newPasswordEntry = EntryField("كلمة المرور الجديدة", isPassword: true);
        confirmPasswordEntry = EntryField("تأكيد كلمة المرور الجديدة", isPassword: true);
        errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("تعيين كلمة مرور جديدة"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Info(serverMessage),
                otpEntry,
                newPasswordEntry,
                confirmPasswordEntry,
                Info("بعد نجاح التحقق سيتم حفظ كلمة المرور الجديدة على الخادم لهذا الحساب فقط."),
                errorLabel,
                PrimaryButton("حفظ كلمة المرور الجديدة", async () => await VerifyOtpAndResetAsync(username, email)),
                GhostButton("الرجوع", ShowEmailOtpRequest)
            }
        }));
    }

    async Task VerifyOtpAndResetAsync(string username, string email)
    {
        if (errorLabel == null || otpEntry == null || newPasswordEntry == null || confirmPasswordEntry == null)
            return;

        SetInlineError(errorLabel, "");

        string otp = otpEntry.Text?.Trim() ?? "";
        string password = newPasswordEntry.Text ?? "";
        string confirm = confirmPasswordEntry.Text ?? "";

        if (!ValidateNewPassword(password, confirm, errorLabel))
            return;

        var result = await otpService.VerifyEmailOtpAndResetPasswordAsync(username, email, otp, password);
        if (!result.Success)
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        ShowMessage("تم تحديث كلمة المرور", "يمكنك الآن تسجيل الدخول باسم المستخدم وكلمة المرور الجديدة.");
    }

    void ShowRecoveryCodeReset()
    {
        contentHost.Children.Clear();
        usernameEntry = EntryField("Username اسم المستخدم");
        recoveryCodeEntry = EntryField("Recovery Code", isPassword: true);
        newPasswordEntry = EntryField("كلمة المرور الجديدة", isPassword: true);
        confirmPasswordEntry = EntryField("تأكيد كلمة المرور الجديدة", isPassword: true);
        errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("Recovery Code"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Info("أدخل اسم المستخدم ورمز الاسترداد المحفوظ. بعد النجاح سيتم توليد Recovery Code جديد وإلغاء القديم."),
                usernameEntry,
                recoveryCodeEntry,
                newPasswordEntry,
                confirmPasswordEntry,
                errorLabel,
                PrimaryButton("إعادة تعيين كلمة المرور", async () => await ResetWithRecoveryCodeAsync()),
                GhostButton("الرجوع", ShowOptions)
            }
        }));
    }

    async Task ResetWithRecoveryCodeAsync()
    {
        if (errorLabel == null || usernameEntry == null || recoveryCodeEntry == null || newPasswordEntry == null || confirmPasswordEntry == null)
            return;

        SetInlineError(errorLabel, "");

        string username = usernameEntry.Text?.Trim() ?? "";
        string recoveryCode = recoveryCodeEntry.Text?.Trim() ?? "";
        string password = newPasswordEntry.Text ?? "";
        string confirm = confirmPasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(recoveryCode))
        {
            SetInlineError(errorLabel, "أدخل اسم المستخدم ورمز الاسترداد.");
            return;
        }

        if (!ValidateNewPassword(password, confirm, errorLabel))
            return;

        try
        {
            var result = await PremiumAccountAuthService.ResetPasswordWithRecoveryKeyAsync(
                username,
                recoveryCode,
                password,
                confirm);

            ShowMessage(
                "تم تحديث كلمة المرور",
                "تم تحديث كلمة المرور بنجاح. احفظ Recovery Code الجديد الآن:\n\n" + result.NewRecoveryCode);
        }
        catch (Exception ex)
        {
            SetInlineError(errorLabel, ex.Message);
        }
    }

    void ShowSecurityQuestionsPlaceholder()
    {
        ShowMessage(
            "أسئلة الأمان",
            "تم تحويل أسئلة الأمان في إنشاء الحساب إلى اختيارات ثابتة. تفعيل الاستعادة بها يتطلب حفظ الإجابات في Backend، وهي المرحلة التالية بعد اختبار OTP.");
    }

    void ShowMessage(string title, string message)
    {
        contentHost.Children.Clear();
        contentHost.Children.Add(Title(title));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Info(message),
                PrimaryButton("العودة لتسجيل الدخول", async () => await Navigation.PopAsync()),
                GhostButton("طرق استعادة أخرى", ShowOptions)
            }
        }));
    }

    static bool ValidateNewPassword(string password, string confirm, Label label)
    {
        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            SetInlineError(label, "كلمتا المرور غير متطابقتين.");
            return false;
        }

        if (!IsStrongPassword(password))
        {
            SetInlineError(label, "كلمة السر يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف كبير، حرف صغير، رقم، ورمز.");
            return false;
        }

        return true;
    }

    static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    static Label Title(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#D4AF37"),
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center
    };

    static Label Info(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#CFCFCF"),
        FontSize = 13,
        HorizontalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.WordWrap
    };

    static Border CreatePanel(View content) => new()
    {
        Stroke = Color.FromArgb("#7A5B18"),
        StrokeThickness = 1.3,
        BackgroundColor = Color.FromArgb("#E60B0B0B"),
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 26 },
        Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#806B4E12")),
            Radius = 20,
            Opacity = 0.45f,
            Offset = new Point(0, 8)
        },
        Content = content
    };

    static Entry EntryField(string placeholder, bool isPassword = false, Keyboard? keyboard = null) => new()
    {
        Placeholder = placeholder,
        PlaceholderColor = Color.FromArgb("#AFAFAF"),
        TextColor = Colors.White,
        IsPassword = isPassword,
        Keyboard = keyboard ?? Keyboard.Text,
        BackgroundColor = Color.FromArgb("#111111"),
        HorizontalTextAlignment = TextAlignment.End
    };

    static Button PrimaryButton(string text, Action action) => new()
    {
        Text = text,
        TextColor = Colors.Black,
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = Color.FromArgb("#D4AF37"),
        CornerRadius = 18,
        HeightRequest = 48,
        Command = new Command(action)
    };

    static Button SecondaryButton(string text, Action action) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#D4AF37"),
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = Color.FromArgb("#181818"),
        BorderColor = Color.FromArgb("#5A4415"),
        BorderWidth = 1,
        CornerRadius = 18,
        HeightRequest = 46,
        Command = new Command(action)
    };

    static Button GhostButton(string text, Action action) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#CFCFCF"),
        BackgroundColor = Colors.Transparent,
        BorderColor = Color.FromArgb("#333333"),
        BorderWidth = 1,
        CornerRadius = 18,
        HeightRequest = 44,
        Command = new Command(action)
    };

    static Label ErrorLabel() => new()
    {
        TextColor = Color.FromArgb("#FF5A5A"),
        FontSize = 12,
        IsVisible = false,
        HorizontalTextAlignment = TextAlignment.Center
    };

    static void SetInlineError(Label label, string message)
    {
        label.Text = message;
        label.IsVisible = !string.IsNullOrWhiteSpace(message);
    }
}

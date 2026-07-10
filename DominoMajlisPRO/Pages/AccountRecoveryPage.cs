using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public sealed class AccountRecoveryPage : ContentPage
{
    readonly VerticalStackLayout contentHost;
    readonly SupabaseRecoveryOtpService recoveryService = new();

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
                SecondaryButton("الاستعادة عبر أسئلة الأمان", ShowSecurityIdentityStep),
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
                Info("سيتم إرسال رمز تحقق مكوّن من 6 أرقام إلى البريد المرتبط بالحساب."),
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
        if (!ValidateIdentity(username, email, errorLabel)) return;

        var result = await recoveryService.RequestEmailOtpAsync(username, email);
        if (!result.Success)
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        ShowEmailOtpReset(username, email, result.Message);
    }

    void ShowEmailOtpReset(string username, string email, string message)
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
                Info(message),
                otpEntry,
                newPasswordEntry,
                confirmPasswordEntry,
                Info(PremiumAccountAuthService.PasswordPolicyText),
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
        string password = newPasswordEntry.Text ?? "";
        if (!ValidateNewPassword(password, confirmPasswordEntry.Text ?? "", errorLabel)) return;

        var result = await recoveryService.VerifyEmailOtpAndResetPasswordAsync(
            username,
            email,
            otpEntry.Text?.Trim() ?? "",
            password);

        if (!result.Success)
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        await AuditRecoveryAsync("Email OTP", username, email);
        ShowSuccess();
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
                Info("أدخل اسم المستخدم ورمز الاسترداد المحفوظ."),
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

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(recoveryCode))
        {
            SetInlineError(errorLabel, "أدخل اسم المستخدم ورمز الاسترداد.");
            return;
        }

        if (!ValidateNewPassword(password, confirmPasswordEntry.Text ?? "", errorLabel)) return;

        try
        {
            var result = await PremiumAccountAuthService.ResetPasswordWithRecoveryKeyAsync(
                username,
                recoveryCode,
                password,
                confirmPasswordEntry.Text ?? "");

            await AuditRecoveryAsync("Recovery Code", username, "Local credential");
            ShowMessage(
                "تم تحديث كلمة المرور",
                "احفظ Recovery Code الجديد الآن:\n\n" + result.NewRecoveryCode);
        }
        catch (Exception ex)
        {
            SetInlineError(errorLabel, ex.Message);
        }
    }

    void ShowSecurityIdentityStep()
    {
        contentHost.Children.Clear();
        usernameEntry = EntryField("Username اسم المستخدم");
        emailEntry = EntryField("البريد الإلكتروني المسجل", keyboard: Keyboard.Email);
        errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("التحقق بأسئلة الأمان"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                StepBadge("المرحلة 1 من 3", "تأكيد هوية الحساب"),
                Info("أدخل اسم المستخدم والبريد المسجل لتحميل أسئلة الأمان الأصلية من الخادم."),
                usernameEntry,
                emailEntry,
                errorLabel,
                PrimaryButton("التالي", async () => await BeginSecurityRecoveryAsync()),
                GhostButton("الرجوع", ShowOptions)
            }
        }));
    }

    async Task BeginSecurityRecoveryAsync()
    {
        if (usernameEntry == null || emailEntry == null || errorLabel == null) return;

        SetInlineError(errorLabel, "");
        string username = usernameEntry.Text?.Trim() ?? "";
        string email = emailEntry.Text?.Trim() ?? "";
        if (!ValidateIdentity(username, email, errorLabel)) return;

        var result = await recoveryService.BeginSecurityQuestionsRecoveryAsync(username, email);
        if (!result.Success || result.Questions.Count != 3 || string.IsNullOrWhiteSpace(result.ChallengeToken))
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        ShowSecurityAnswersStep(username, email, result.ChallengeToken, result.Questions);
    }

    void ShowSecurityAnswersStep(
        string username,
        string email,
        string challengeToken,
        IReadOnlyList<SupabaseRecoveryOtpService.SecurityQuestionItem> questions)
    {
        contentHost.Children.Clear();
        errorLabel = ErrorLabel();

        var answerEntries = questions.Select((_, index) =>
            EntryField($"إجابة السؤال {index + 1}", isPassword: true)).ToArray();

        var stack = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                StepBadge("المرحلة 2 من 3", "الإجابة عن الأسئلة المسجلة"),
                Info("الأسئلة معروضة كما سجلتها سابقًا ولا يمكن تغييرها.")
            }
        };

        for (int i = 0; i < questions.Count; i++)
        {
            stack.Children.Add(QuestionCard(i + 1, questions[i].Question));
            stack.Children.Add(answerEntries[i]);
        }

        stack.Children.Add(errorLabel);
        stack.Children.Add(PrimaryButton("تحقق من الإجابات", async () =>
            await VerifySecurityAnswersAsync(username, email, challengeToken, questions, answerEntries)));
        stack.Children.Add(GhostButton("البدء من جديد", ShowSecurityIdentityStep));

        contentHost.Children.Add(Title("أسئلة الأمان"));
        contentHost.Children.Add(CreatePanel(new ScrollView
        {
            MaximumHeightRequest = 620,
            Content = stack
        }));
    }

    async Task VerifySecurityAnswersAsync(
        string username,
        string email,
        string challengeToken,
        IReadOnlyList<SupabaseRecoveryOtpService.SecurityQuestionItem> questions,
        IReadOnlyList<Entry> entries)
    {
        if (errorLabel == null) return;
        SetInlineError(errorLabel, "");

        var answers = questions.Select((question, index) =>
            (question.Id, entries[index].Text?.Trim() ?? "")).ToArray();

        if (answers.Any(item => item.Item2.Length < 3))
        {
            SetInlineError(errorLabel, "أدخل إجابة واضحة لكل سؤال، لا تقل عن 3 أحرف.");
            return;
        }

        var result = await recoveryService.VerifySecurityAnswersAsync(username, email, challengeToken, answers);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ResetToken))
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        ShowSecurityPasswordStep(username, email, result.ResetToken);
    }

    void ShowSecurityPasswordStep(string username, string email, string resetToken)
    {
        contentHost.Children.Clear();
        newPasswordEntry = EntryField("كلمة المرور الجديدة", isPassword: true);
        confirmPasswordEntry = EntryField("تأكيد كلمة المرور الجديدة", isPassword: true);
        errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("إعادة تعيين كلمة المرور"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                StepBadge("المرحلة 3 من 3", "حماية الحساب بكلمة مرور جديدة"),
                Info("تم التحقق من إجاباتك. أنشئ كلمة مرور قوية جديدة."),
                newPasswordEntry,
                confirmPasswordEntry,
                Info(PremiumAccountAuthService.PasswordPolicyText),
                errorLabel,
                PrimaryButton("حفظ كلمة المرور الجديدة", async () =>
                    await ResetWithSecurityTokenAsync(username, email, resetToken)),
                GhostButton("إلغاء", ShowOptions)
            }
        }));
    }

    async Task ResetWithSecurityTokenAsync(string username, string email, string resetToken)
    {
        if (newPasswordEntry == null || confirmPasswordEntry == null || errorLabel == null) return;

        SetInlineError(errorLabel, "");
        string password = newPasswordEntry.Text ?? "";
        if (!ValidateNewPassword(password, confirmPasswordEntry.Text ?? "", errorLabel)) return;

        var result = await recoveryService.ResetPasswordWithSecurityTokenAsync(
            username,
            email,
            resetToken,
            password);

        if (!result.Success)
        {
            SetInlineError(errorLabel, result.Message);
            return;
        }

        await AuditRecoveryAsync("Security Questions", username, email);
        ShowSuccess();
    }

    void ShowSuccess() => ShowMessage(
        "تم تأمين الحساب",
        "تم تحديث كلمة المرور وإبطال رموز الاسترداد السابقة. سجّل الدخول باسم المستخدم وكلمة المرور الجديدة.");

    async Task AuditRecoveryAsync(string method, string username, string identity)
    {
        await SecurityLogService.AddAsync(
            "Account Recovery",
            "Password Reset Success",
            $"Method: {method}\nUsername: {username}\nIdentity: {identity}\nTimestamp UTC: {DateTime.UtcNow:O}",
            "Warning",
            true);
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

    static bool ValidateIdentity(string username, string email, Label label)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
        {
            SetInlineError(label, "أدخل اسم المستخدم والبريد الإلكتروني.");
            return false;
        }

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            SetInlineError(label, "البريد الإلكتروني غير صالح.");
            return false;
        }

        return true;
    }

    static bool ValidateNewPassword(string password, string confirm, Label label)
    {
        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            SetInlineError(label, "كلمتا المرور غير متطابقتين.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            SetInlineError(label, PremiumAccountAuthService.PasswordPolicyText);
            return false;
        }

        return true;
    }

    static Border StepBadge(string step, string title) => new()
    {
        Stroke = Color.FromArgb("#8A6A1D"),
        StrokeThickness = 1,
        BackgroundColor = Color.FromArgb("#241B08"),
        Padding = new Thickness(12, 9),
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Content = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = step, TextColor = Color.FromArgb("#D4AF37"), FontSize = 11, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = title, TextColor = Colors.White, FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }
            }
        }
    };

    static Border QuestionCard(int number, string question) => new()
    {
        Stroke = Color.FromArgb("#4F3C13"),
        BackgroundColor = Color.FromArgb("#151515"),
        Padding = 12,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Content = new Label
        {
            Text = $"{number}. {question}",
            TextColor = Colors.White,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.End,
            LineBreakMode = LineBreakMode.WordWrap
        }
    };

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

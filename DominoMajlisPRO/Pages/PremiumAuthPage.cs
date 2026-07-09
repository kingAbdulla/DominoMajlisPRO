using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Backend.Profiles;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public sealed class PremiumAuthPage : ContentPage
{
    static readonly string[] SecurityQuestions =
    {
        "ما اسم أول مقهى لعبت فيه الدومينو؟",
        "ما اسم أقرب صديق دومينو لديك؟",
        "ما المدينة التي بدأت فيها لعب الدومينو؟",
        "ما اسم أول فريق دومينو لعبت معه؟",
        "ما الكلمة السرية التي تختارها للتذكير؟"
    };

    readonly Grid root;
    readonly VerticalStackLayout contentHost;
    readonly SupabaseAuthenticationService supabaseAuth = new();
    readonly SupabaseRecoveryOtpService recoveryService = new();
    bool checkedActiveSession;

    Entry? loginUsernameEntry;
    Entry? loginPasswordEntry;
    Label? loginErrorLabel;

    Entry? registerUsernameEntry;
    Entry? registerNicknameEntry;
    Entry? registerEmailEntry;
    Entry? registerPasswordEntry;
    Entry? registerConfirmPasswordEntry;
    Entry? registerAgeEntry;
    Picker? registerGenderPicker;
    Picker? securityQuestion1Picker;
    Picker? securityQuestion2Picker;
    Picker? securityQuestion3Picker;
    Entry? securityAnswer1Entry;
    Entry? securityAnswer2Entry;
    Entry? securityAnswer3Entry;
    CheckBox? ageCheckBox;
    CheckBox? privacyCheckBox;
    CheckBox? termsCheckBox;
    CheckBox? credentialsCheckBox;
    Button? saveAccountButton;
    Label? registerErrorLabel;

    public PremiumAuthPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Colors.Black;

        root = new Grid
        {
            Padding = new Thickness(18, 30),
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#020202"), 0),
                    new(Color.FromArgb("#151006"), 0.55f),
                    new(Color.FromArgb("#050505"), 1)
                },
                new Point(0, 0),
                new Point(1, 1))
        };

        contentHost = new VerticalStackLayout
        {
            Spacing = 18,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill
        };

        root.Children.Add(new BoxView
        {
            Color = Color.FromArgb("#22D4AF37"),
            CornerRadius = 220,
            WidthRequest = 260,
            HeightRequest = 260,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Opacity = 0.35
        });

        root.Children.Add(contentHost);
        Content = root;
        ShowWelcome();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (checkedActiveSession)
            return;

        checkedActiveSession = true;

        if (!await ApplicationUserService.HasActiveSessionAsync())
            return;

        var currentUser = await ApplicationUserService.GetCurrentUserAsync();
        if (currentUser.Role != ApplicationUserRole.Ghost)
            OpenMainPage();
    }

    void ShowWelcome()
    {
        contentHost.Children.Clear();
        contentHost.Children.Add(Title("DOMINO MAJLIS PRO"));
        contentHost.Children.Add(Subtitle("بوابة الهوية الآمنة"));
        contentHost.Children.Add(Info("تسجيل الدخول يتم باسم المستخدم. البريد الإلكتروني مخصص للتأكيد والاسترداد فقط."));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                PrimaryButton("تسجيل الدخول", ShowLogin),
                SecondaryButton("إنشاء حساب", ShowRegister),
                GhostButton("الدخول كضيف", async () => await ContinueAsGuestAsync())
            }
        }));
        contentHost.Children.Add(Info("+18 فقط • التطبيق لتنظيم وتوثيق نتائج الدومينو وليس للمراهنة أو القمار", 11));
    }

    void ShowLogin()
    {
        contentHost.Children.Clear();
        loginUsernameEntry = EntryField("Username اسم المستخدم");
        loginPasswordEntry = EntryField("كلمة السر", isPassword: true);
        loginErrorLabel = ErrorLabel();

        contentHost.Children.Add(Title("تسجيل الدخول"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                loginUsernameEntry,
                Info("مثال: KingEsmat", 11),
                loginPasswordEntry,
                loginErrorLabel,
                PrimaryButton("دخول", async () => await LoginAsync()),
                SecondaryButton("نسيت كلمة المرور؟", async () => await Navigation.PushAsync(new AccountRecoveryPage())),
                GhostButton("الرجوع", ShowWelcome)
            }
        }));
    }

    void ShowRegister()
    {
        contentHost.Children.Clear();

        registerUsernameEntry = EntryField("Username اسم المستخدم");
        registerNicknameEntry = EntryField("User Nickname الاسم الظاهر");
        registerEmailEntry = EntryField("البريد الإلكتروني للاسترداد والتأكيد", keyboard: Keyboard.Email);
        registerPasswordEntry = EntryField("كلمة السر القوية", isPassword: true);
        registerConfirmPasswordEntry = EntryField("تأكيد كلمة السر", isPassword: true);
        registerAgeEntry = EntryField("العمر", keyboard: Keyboard.Numeric);
        registerGenderPicker = PickerField("الجنس", "ذكر", "أنثى", "أفضل عدم التحديد");
        securityQuestion1Picker = PickerField("سؤال الأمان الأول", SecurityQuestions);
        securityQuestion2Picker = PickerField("سؤال الأمان الثاني", SecurityQuestions);
        securityQuestion3Picker = PickerField("سؤال الأمان الثالث", SecurityQuestions);
        securityAnswer1Entry = EntryField("إجابة السؤال الأول", isPassword: true);
        securityAnswer2Entry = EntryField("إجابة السؤال الثاني", isPassword: true);
        securityAnswer3Entry = EntryField("إجابة السؤال الثالث", isPassword: true);

        ageCheckBox = ConsentBox();
        privacyCheckBox = ConsentBox();
        termsCheckBox = ConsentBox();
        credentialsCheckBox = ConsentBox();
        registerErrorLabel = ErrorLabel();
        saveAccountButton = PrimaryButton("إنشاء الحساب", async () => await RegisterAsync());
        saveAccountButton.IsEnabled = false;
        saveAccountButton.Opacity = 0.45;

        foreach (var box in new[] { ageCheckBox, privacyCheckBox, termsCheckBox, credentialsCheckBox })
            box.CheckedChanged += (_, _) => UpdateSaveButtonState();

        contentHost.Children.Add(Title("إنشاء حساب آمن"));
        contentHost.Children.Add(CreatePanel(new ScrollView
        {
            MaximumHeightRequest = 620,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    registerUsernameEntry,
                    registerNicknameEntry,
                    registerEmailEntry,
                    registerPasswordEntry,
                    Info(PremiumAccountAuthService.PasswordPolicyText, 11),
                    registerConfirmPasswordEntry,
                    registerAgeEntry,
                    registerGenderPicker,
                    Info("اختر 3 أسئلة أمان مختلفة. ستستخدم هذه الأسئلة لاحقاً لاستعادة الحساب وإعادة تعيين كلمة المرور.", 11),
                    securityQuestion1Picker,
                    securityAnswer1Entry,
                    securityQuestion2Picker,
                    securityAnswer2Entry,
                    securityQuestion3Picker,
                    securityAnswer3Entry,
                    LegalOpenButton(),
                    ConsentRow(ageCheckBox, "أؤكد أن عمري 18 سنة أو أكثر."),
                    ConsentRow(privacyCheckBox, "قرأت سياسة الخصوصية وأوافق عليها."),
                    ConsentRow(termsCheckBox, "قرأت شروط الاستخدام وأوافق عليها."),
                    ConsentRow(credentialsCheckBox, "أفهم أنني مسؤول عن المحافظة على بيانات الدخول."),
                    registerErrorLabel,
                    saveAccountButton,
                    SecondaryButton("تراجع", ShowWelcome)
                }
            }
        }));
    }

    async Task LoginAsync()
    {
        try
        {
            SetLoginError("");
            string username = loginUsernameEntry?.Text?.Trim() ?? "";
            string password = loginPasswordEntry?.Text ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                SetLoginError("أدخل اسم المستخدم.");
                return;
            }

            string? email = await SupabaseAccountLinkService.ResolveEmailByUsernameAsync(username);
            if (string.IsNullOrWhiteSpace(email))
            {
                SetLoginError("اسم المستخدم غير موجود على هذا الجهاز. استخدم مركز الاستعادة عبر البريد الإلكتروني.");
                return;
            }

            var result = await supabaseAuth.SignInAsync(email, password);
            if (!result.IsSuccess || result.Session == null)
            {
                SetLoginError(result.Message);
                return;
            }

            await SupabaseAccountLinkService.EnsureLinkedApplicationUserAsync(result.Session, result.Session.Nickname);
            OpenMainPage();
        }
        catch (Exception ex)
        {
            SetLoginError(ex.Message);
        }
    }

    async Task RegisterAsync()
    {
        try
        {
            SetRegisterError("");
            ValidateRegistrationInput();

            string username = registerUsernameEntry?.Text?.Trim() ?? "";
            string email = registerEmailEntry?.Text?.Trim() ?? "";
            string nickname = registerNicknameEntry?.Text?.Trim() ?? "";
            var securityAnswers = BuildSecurityQuestionAnswers();

            await SupabaseAccountLinkService.RegisterPendingLinkAsync(username, email, nickname);

            var result = await supabaseAuth.SignUpAsync(
                email,
                registerPasswordEntry?.Text ?? "",
                username,
                nickname,
                securityAnswers.Select(item => item.Question));

            if (!result.IsSuccess)
            {
                SetRegisterError(result.Message);
                return;
            }

            var securityResult = await recoveryService.RegisterSecurityQuestionsAsync(username, email, securityAnswers);
            if (!securityResult.Success)
            {
                SetRegisterError(securityResult.Message);
                return;
            }

            ShowMessagePanel(
                "تم إنشاء الحساب",
                "تم إرسال رسالة تأكيد إلى بريدك الإلكتروني، وتم حفظ أسئلة الأمان للاسترداد. بعد تأكيد البريد يمكنك تسجيل الدخول باسم المستخدم وليس بالبريد.",
                "العودة لتسجيل الدخول",
                ShowLogin);
        }
        catch (Exception ex)
        {
            SetRegisterError(ex.Message);
        }
    }

    void ValidateRegistrationInput()
    {
        string username = registerUsernameEntry?.Text?.Trim() ?? "";
        string nickname = registerNicknameEntry?.Text?.Trim() ?? "";
        string email = registerEmailEntry?.Text?.Trim() ?? "";
        string password = registerPasswordEntry?.Text ?? "";
        string confirm = registerConfirmPasswordEntry?.Text ?? "";
        int.TryParse(registerAgeEntry?.Text?.Trim(), out int age);

        if (ageCheckBox?.IsChecked != true || privacyCheckBox?.IsChecked != true || termsCheckBox?.IsChecked != true || credentialsCheckBox?.IsChecked != true)
            throw new InvalidOperationException("يجب الموافقة على جميع بنود الحماية والاستخدام قبل إنشاء الحساب.");

        if (age < 18)
            throw new InvalidOperationException("التطبيق مخصص لمن هم بعمر 18 سنة أو أكثر فقط.");

        ValidateUsername(username);

        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 40)
            throw new InvalidOperationException("الاسم الظاهر مطلوب ويجب ألا يتجاوز 40 حرفاً.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 120)
            throw new InvalidOperationException("البريد الإلكتروني مطلوب وغير صالح.");

        if (registerGenderPicker?.SelectedItem == null)
            throw new InvalidOperationException("اختر الجنس لإكمال إنشاء الحساب.");

        if (BuildSecurityQuestionAnswers().Count != 3)
            throw new InvalidOperationException("اختر 3 أسئلة أمان مختلفة وأدخل إجابة لكل سؤال.");

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
            throw new InvalidOperationException("كلمتا السر غير متطابقتين.");

        if (!IsStrongPassword(password))
            throw new InvalidOperationException(PremiumAccountAuthService.PasswordPolicyText);
    }

    List<(string Question, string Answer)> BuildSecurityQuestionAnswers()
    {
        var items = new List<(string Question, string Answer)>
        {
            (securityQuestion1Picker?.SelectedItem?.ToString() ?? "", securityAnswer1Entry?.Text ?? ""),
            (securityQuestion2Picker?.SelectedItem?.ToString() ?? "", securityAnswer2Entry?.Text ?? ""),
            (securityQuestion3Picker?.SelectedItem?.ToString() ?? "", securityAnswer3Entry?.Text ?? "")
        };

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Question) && !string.IsNullOrWhiteSpace(item.Answer))
            .GroupBy(item => item.Question, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    static void ValidateUsername(string username)
    {
        if (username.Length < 3 || username.Length > 32)
            throw new InvalidOperationException("اسم المستخدم يجب أن يكون بين 3 و32 حرفاً.");

        bool valid = username.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.');
        if (!valid)
            throw new InvalidOperationException("اسم المستخدم يسمح بالحروف والأرقام و . _ - فقط.");

        if (username.Contains('@', StringComparison.Ordinal))
            throw new InvalidOperationException("اسم المستخدم لا يكون بريداً إلكترونياً.");
    }

    static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    void ShowMessagePanel(string title, string message, string buttonText, Action action)
    {
        contentHost.Children.Clear();
        contentHost.Children.Add(Title(title));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Info(message, 15),
                PrimaryButton(buttonText, action)
            }
        }));
    }

    async Task ContinueAsGuestAsync()
    {
        await ApplicationUserService.EnsureGhostUserAsync();
        OpenMainPage();
    }

    void OpenMainPage()
    {
        Application.Current!.MainPage = new NavigationPage(new MainPage());
    }

    Button LegalOpenButton() => GhostButton("قراءة سياسة الحماية والاستخدام", async () =>
    {
        await DisplayAlert(
            "سياسة الحماية والاستخدام",
            "أقر بأن عمري ثمانية عشر (18) عاماً أو أكثر، وأتحمل مسؤولية صحة هذه المعلومة.\n\n" +
            "المستخدم مسؤول بالكامل عن سرية بيانات تسجيل الدخول، ويعد أي نشاط يتم من خلال حسابه صادراً عنه ما لم يثبت وجود خلل تقني في التطبيق.\n\n" +
            "لا يتحمل المطور مسؤولية فقدان الحساب الناتج عن مشاركة كلمة السر أو الإهمال أو استخدام أجهزة غير آمنة.\n\n" +
            "يحظر استخدام التطبيق في نشاط مخالف للقانون أو الغش أو انتحال الشخصية، ويحتفظ المطور بحق تعليق الحسابات المخالفة.\n\n" +
            "التطبيق مخصص لإدارة وتنظيم وتوثيق نتائج مباريات الدومينو فقط، ولا يقدم خدمات مراهنة أو قمار أو جوائز مالية.",
            "فهمت");
    });

    void UpdateSaveButtonState()
    {
        if (saveAccountButton == null)
            return;

        bool enabled = ageCheckBox?.IsChecked == true && privacyCheckBox?.IsChecked == true && termsCheckBox?.IsChecked == true && credentialsCheckBox?.IsChecked == true;
        saveAccountButton.IsEnabled = enabled;
        saveAccountButton.Opacity = enabled ? 1 : 0.45;
    }

    static Picker PickerField(string title, params string[] items)
    {
        var picker = new Picker
        {
            Title = title,
            TextColor = Colors.White,
            TitleColor = Color.FromArgb("#AFAFAF"),
            BackgroundColor = Color.FromArgb("#111111"),
            HorizontalTextAlignment = TextAlignment.End
        };

        foreach (var item in items)
            picker.Items.Add(item);

        return picker;
    }

    static Label Title(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#D4AF37"),
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center
    };

    static Label Subtitle(string text) => new()
    {
        Text = text,
        TextColor = Colors.White,
        FontSize = 18,
        HorizontalTextAlignment = TextAlignment.Center
    };

    static Label Info(string text, double size = 13) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#CFCFCF"),
        FontSize = size,
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

    static HorizontalStackLayout ConsentRow(CheckBox box, string text) => new()
    {
        Spacing = 8,
        HorizontalOptions = LayoutOptions.End,
        Children =
        {
            new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontSize = 12,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.End
            },
            box
        }
    };

    static CheckBox ConsentBox() => new()
    {
        Color = Color.FromArgb("#D4AF37")
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

    void SetLoginError(string message)
    {
        if (loginErrorLabel == null)
            return;

        loginErrorLabel.Text = message;
        loginErrorLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    void SetRegisterError(string message)
    {
        if (registerErrorLabel == null)
            return;

        registerErrorLabel.Text = message;
        registerErrorLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
    }
}

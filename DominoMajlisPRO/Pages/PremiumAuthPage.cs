using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Backend.Profiles;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public sealed class PremiumAuthPage : ContentPage
{
    readonly Grid root;
    readonly VerticalStackLayout contentHost;
    readonly SupabaseAuthenticationService supabaseAuth = new();
    bool checkedActiveSession;

    Entry? loginEmailEntry;
    Entry? loginPasswordEntry;
    Label? loginErrorLabel;

    Entry? registerNicknameEntry;
    Entry? registerPasswordEntry;
    Entry? registerConfirmPasswordEntry;
    Entry? registerAgeEntry;
    Picker? registerGenderPicker;
    Entry? registerEmailEntry;
    Entry? registerSecurityQuestionEntry;
    Entry? registerSecurityAnswerEntry;
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

        contentHost.Children.Add(new Label
        {
            Text = "DOMINO MAJLIS PRO",
            TextColor = Color.FromArgb("#D4AF37"),
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        contentHost.Children.Add(new Label
        {
            Text = "بوابة الهوية الآمنة",
            TextColor = Colors.White,
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center
        });

        contentHost.Children.Add(new Label
        {
            Text = "التسجيل وتسجيل الدخول يتمان الآن عبر Supabase مع تأكيد البريد الإلكتروني.",
            TextColor = Color.FromArgb("#CFCFCF"),
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        });

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

        contentHost.Children.Add(new Label
        {
            Text = "+18 فقط • التطبيق لتنظيم وتوثيق نتائج الدومينو وليس للمراهنة أو القمار",
            TextColor = Color.FromArgb("#AFAFAF"),
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center
        });
    }

    void ShowLogin()
    {
        contentHost.Children.Clear();

        loginEmailEntry = EntryField("البريد الإلكتروني Email", keyboard: Keyboard.Email);
        loginPasswordEntry = EntryField("كلمة السر", isPassword: true);
        loginErrorLabel = ErrorLabel();

        contentHost.Children.Add(Title("تسجيل الدخول"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                loginEmailEntry,
                loginPasswordEntry,
                loginErrorLabel,
                PrimaryButton("دخول", async () => await LoginAsync()),
                SecondaryButton("نسيت كلمة المرور", ShowPasswordRecovery),
                GhostButton("الرجوع", ShowWelcome)
            }
        }));
    }

    void ShowPasswordRecovery()
    {
        contentHost.Children.Clear();

        var emailEntry = EntryField("البريد الإلكتروني Email", keyboard: Keyboard.Email);
        var errorLabel = ErrorLabel();

        contentHost.Children.Add(Title("استعادة كلمة المرور"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                emailEntry,
                new Label
                {
                    Text = "سيتم إرسال رابط استعادة كلمة المرور إلى بريدك الإلكتروني عبر Supabase.",
                    TextColor = Color.FromArgb("#CFCFCF"),
                    FontSize = 11,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                errorLabel,
                PrimaryButton("إرسال رابط الاستعادة", async () =>
                {
                    try
                    {
                        SetInlineError(errorLabel, "");
                        var result = await supabaseAuth.SendPasswordResetAsync(emailEntry.Text ?? "");

                        if (!result.IsSuccess)
                        {
                            SetInlineError(errorLabel, result.Message);
                            return;
                        }

                        ShowMessagePanel(
                            "تم الإرسال",
                            result.Message,
                            "العودة لتسجيل الدخول",
                            ShowLogin);
                    }
                    catch (Exception ex)
                    {
                        SetInlineError(errorLabel, ex.Message);
                    }
                }),
                SecondaryButton("الرجوع", ShowLogin)
            }
        }));
    }

    void ShowRegister()
    {
        contentHost.Children.Clear();

        registerNicknameEntry = EntryField("الاسم الظاهر User Nickname");
        registerEmailEntry = EntryField("البريد الإلكتروني Email", keyboard: Keyboard.Email);
        registerPasswordEntry = EntryField("كلمة السر القوية", isPassword: true);
        registerConfirmPasswordEntry = EntryField("تأكيد كلمة السر", isPassword: true);
        registerAgeEntry = EntryField("العمر", keyboard: Keyboard.Numeric);
        registerGenderPicker = new Picker
        {
            Title = "الجنس",
            TextColor = Colors.White,
            TitleColor = Color.FromArgb("#AFAFAF"),
            BackgroundColor = Color.FromArgb("#111111")
        };
        registerGenderPicker.Items.Add("ذكر");
        registerGenderPicker.Items.Add("أنثى");
        registerGenderPicker.Items.Add("أفضل عدم التحديد");
        registerSecurityQuestionEntry = EntryField("سؤال الأمان المحلي");
        registerSecurityAnswerEntry = EntryField("إجابة سؤال الأمان المحلي", isPassword: true);

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
                    registerNicknameEntry,
                    registerEmailEntry,
                    registerPasswordEntry,
                    new Label
                    {
                        Text = PremiumAccountAuthService.PasswordPolicyText,
                        TextColor = Color.FromArgb("#BEBEBE"),
                        FontSize = 11,
                        HorizontalTextAlignment = TextAlignment.End
                    },
                    registerConfirmPasswordEntry,
                    registerAgeEntry,
                    registerGenderPicker,
                    registerSecurityQuestionEntry,
                    registerSecurityAnswerEntry,
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

            var result = await supabaseAuth.SignInAsync(
                loginEmailEntry?.Text ?? "",
                loginPasswordEntry?.Text ?? "");

            if (!result.IsSuccess || result.Session == null)
            {
                SetLoginError(result.Message);
                return;
            }

            await SupabaseAccountLinkService.EnsureLinkedApplicationUserAsync(
                result.Session,
                result.Session.Nickname);
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

            var result = await supabaseAuth.SignUpAsync(
                registerEmailEntry?.Text ?? "",
                registerPasswordEntry?.Text ?? "",
                registerNicknameEntry?.Text ?? "");

            if (!result.IsSuccess)
            {
                SetRegisterError(result.Message);
                return;
            }

            ShowMessagePanel(
                "تم إنشاء الحساب",
                "تم إرسال رسالة تأكيد إلى بريدك الإلكتروني. لن تتمكن من تسجيل الدخول حتى تؤكد البريد.",
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
        string nickname = registerNicknameEntry?.Text?.Trim() ?? "";
        string email = registerEmailEntry?.Text?.Trim() ?? "";
        string password = registerPasswordEntry?.Text ?? "";
        string confirm = registerConfirmPasswordEntry?.Text ?? "";
        string securityQuestion = registerSecurityQuestionEntry?.Text?.Trim() ?? "";
        string securityAnswer = registerSecurityAnswerEntry?.Text?.Trim() ?? "";
        int.TryParse(registerAgeEntry?.Text?.Trim(), out int age);

        if (ageCheckBox?.IsChecked != true ||
            privacyCheckBox?.IsChecked != true ||
            termsCheckBox?.IsChecked != true ||
            credentialsCheckBox?.IsChecked != true)
            throw new InvalidOperationException("يجب الموافقة على جميع بنود الحماية والاستخدام قبل إنشاء الحساب.");

        if (age < 18)
            throw new InvalidOperationException("التطبيق مخصص لمن هم بعمر 18 سنة أو أكثر فقط.");

        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 40)
            throw new InvalidOperationException("الاسم الظاهر مطلوب ويجب ألا يتجاوز 40 حرفاً.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 120)
            throw new InvalidOperationException("البريد الإلكتروني مطلوب وغير صالح.");

        if (registerGenderPicker?.SelectedItem == null)
            throw new InvalidOperationException("اختر الجنس لإكمال إنشاء الحساب.");

        if (string.IsNullOrWhiteSpace(securityQuestion) || securityQuestion.Length < 6)
            throw new InvalidOperationException("سؤال الأمان المحلي مطلوب ويجب أن يكون واضحاً.");

        if (string.IsNullOrWhiteSpace(securityAnswer) || securityAnswer.Length < 3)
            throw new InvalidOperationException("إجابة سؤال الأمان المحلي مطلوبة ويجب ألا تقل عن 3 أحرف.");

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
            throw new InvalidOperationException("كلمتا السر غير متطابقتين.");

        if (!IsStrongPassword(password))
            throw new InvalidOperationException(PremiumAccountAuthService.PasswordPolicyText);
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

    void ShowMessagePanel(
        string title,
        string message,
        string buttonText,
        Action action)
    {
        contentHost.Children.Clear();
        contentHost.Children.Add(Title(title));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = message,
                    TextColor = Colors.White,
                    FontSize = 15,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                },
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
            "يحتفظ المطور بحق تعديل الشروط والسياسات عند الحاجة، ويعتبر استمرار استخدام التطبيق بعد التعديل موافقة عليه.\n\n" +
            "في حال فقدان كلمة السر يمكن استخدام نظام استعادة الحساب عند توفره.\n\n" +
            "لا يجمع التطبيق بيانات شخصية أكثر من اللازم لتشغيل الخدمات الأساسية.\n\n" +
            "التطبيق مخصص لإدارة وتنظيم وتوثيق نتائج مباريات الدومينو فقط، ولا يقدم خدمات مراهنة أو قمار أو جوائز مالية.\n\n" +
            "يتم توفير التطبيق كما هو، ويبذل المطور أفضل الجهود لضمان الاستقرار دون ضمان خلوه من جميع الأخطاء أو انقطاع الخدمة.",
            "فهمت");
    });

    void UpdateSaveButtonState()
    {
        if (saveAccountButton == null)
            return;

        bool enabled = ageCheckBox?.IsChecked == true &&
                       privacyCheckBox?.IsChecked == true &&
                       termsCheckBox?.IsChecked == true &&
                       credentialsCheckBox?.IsChecked == true;

        saveAccountButton.IsEnabled = enabled;
        saveAccountButton.Opacity = enabled ? 1 : 0.45;
    }

    static Label Title(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#D4AF37"),
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center
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

using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public sealed class PremiumAuthPage : ContentPage
{
    readonly Grid root;
    readonly VerticalStackLayout contentHost;
    bool checkedActiveSession;

    Entry? loginUsernameEntry;
    Entry? loginPasswordEntry;
    Label? loginErrorLabel;

    Entry? registerUsernameEntry;
    Entry? registerNicknameEntry;
    Entry? registerPasswordEntry;
    Entry? registerConfirmPasswordEntry;
    Entry? registerAgeEntry;
    Picker? registerGenderPicker;
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
            Text = "سجّل الدخول باسم الدخول وكلمة السر. الاسم الظاهر ليس وسيلة دخول، ومعرّف اللاعب يبقى داخلياً لحماية حسابك ومقتنياتك.",
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

        loginUsernameEntry = EntryField("اسم الدخول Username");
        loginPasswordEntry = EntryField("كلمة السر", isPassword: true);
        loginErrorLabel = ErrorLabel();

        contentHost.Children.Add(Title("تسجيل الدخول"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                loginUsernameEntry,
                loginPasswordEntry,
                loginErrorLabel,
                PrimaryButton("دخول", async () => await LoginAsync()),
                SecondaryButton("الرجوع", ShowWelcome),
                GhostButton("استعادة الرمز السري - لاحقاً", () =>
                {
                    loginErrorLabel.Text = "سيتم تفعيل نظام الاستعادة بواسطة رمز الاسترداد في إصدار لاحق.";
                    loginErrorLabel.IsVisible = true;
                })
            }
        }));
    }

    void ShowRegister()
    {
        contentHost.Children.Clear();

        registerUsernameEntry = EntryField("اسم الدخول Username");
        registerNicknameEntry = EntryField("الاسم الظاهر User Nickname");
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

        ageCheckBox = ConsentBox();
        privacyCheckBox = ConsentBox();
        termsCheckBox = ConsentBox();
        credentialsCheckBox = ConsentBox();
        registerErrorLabel = ErrorLabel();
        saveAccountButton = PrimaryButton("حفظ الحساب", async () => await RegisterAsync());
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
                    LegalOpenButton(),
                    ConsentRow(ageCheckBox, "أؤكد أن عمري 18 سنة أو أكثر."),
                    ConsentRow(privacyCheckBox, "قرأت سياسة الخصوصية وأوافق عليها."),
                    ConsentRow(termsCheckBox, "قرأت شروط الاستخدام وأوافق عليها."),
                    ConsentRow(credentialsCheckBox, "أفهم أنني مسؤول عن المحافظة على اسم الدخول وكلمة السر."),
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
            await PremiumAccountAuthService.LoginAsync(
                loginUsernameEntry?.Text ?? "",
                loginPasswordEntry?.Text ?? "");

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

            int.TryParse(registerAgeEntry?.Text?.Trim(), out int age);

            var result = await PremiumAccountAuthService.RegisterAsync(
                registerUsernameEntry?.Text ?? "",
                registerNicknameEntry?.Text ?? "",
                registerPasswordEntry?.Text ?? "",
                registerConfirmPasswordEntry?.Text ?? "",
                age,
                registerGenderPicker?.SelectedItem?.ToString() ?? "",
                ageCheckBox?.IsChecked == true,
                privacyCheckBox?.IsChecked == true,
                termsCheckBox?.IsChecked == true,
                credentialsCheckBox?.IsChecked == true);

            ShowRecoveryCode(result.RecoveryCode);
        }
        catch (Exception ex)
        {
            SetRegisterError(ex.Message);
        }
    }

    void ShowRecoveryCode(string recoveryCode)
    {
        contentHost.Children.Clear();

        CheckBox savedCheck = ConsentBox();
        Button continueButton = PrimaryButton("متابعة إلى التطبيق", OpenMainPage);
        continueButton.IsEnabled = false;
        continueButton.Opacity = 0.45;
        savedCheck.CheckedChanged += (_, _) =>
        {
            continueButton.IsEnabled = savedCheck.IsChecked;
            continueButton.Opacity = savedCheck.IsChecked ? 1 : 0.45;
        };

        contentHost.Children.Add(Title("تم حفظ بياناتك"));
        contentHost.Children.Add(CreatePanel(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "تم تأكيد حفظ بياناتك. احفظ كلمة السر ورمز الاسترداد في مكان آمن.",
                    TextColor = Colors.White,
                    FontSize = 15,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                new Border
                {
                    Stroke = Color.FromArgb("#D4AF37"),
                    StrokeThickness = 1.5,
                    BackgroundColor = Color.FromArgb("#0A0A0A"),
                    Padding = 16,
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Content = new Label
                    {
                        Text = recoveryCode,
                        TextColor = Color.FromArgb("#FFD76A"),
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                },
                new Label
                {
                    Text = "هذا هو رمز الاسترداد الوحيد لحسابك. يمكن استخدامه مستقبلاً لاستعادة الوصول عند نسيان كلمة السر.",
                    TextColor = Color.FromArgb("#CFCFCF"),
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                ConsentRow(savedCheck, "لقد قمت بحفظ رمز الاسترداد."),
                continueButton
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
            "في حال فقدان كلمة السر يمكن استخدام نظام استعادة الحساب عند توفره في الإصدارات المستقبلية.\n\n" +
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

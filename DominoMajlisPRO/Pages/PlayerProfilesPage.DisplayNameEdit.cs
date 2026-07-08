using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class PlayerProfilesPage
{
    Button? changeDisplayNameButton;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        EnsureDisplayNameButton();
    }

    void EnsureDisplayNameButton()
    {
        if (changeDisplayNameButton != null || RegisteredAccountControls == null)
            return;

        changeDisplayNameButton = new Button
        {
            Text = "تغيير الاسم",
            Margin = new Thickness(0, 0, 6, 6),
            BackgroundColor = Color.FromArgb("#1D1D1D"),
            TextColor = Color.FromArgb("#D4AF37"),
            FontAttributes = FontAttributes.Bold
        };

        changeDisplayNameButton.Clicked += OnChangeDisplayNameClicked;
        RegisteredAccountControls.Children.Insert(1, changeDisplayNameButton);
    }

    async void OnChangeDisplayNameClicked(object? sender, EventArgs e)
    {
        try
        {
            var user = await ApplicationUserService.GetCurrentUserAsync();

            if (user.Role == ApplicationUserRole.Ghost)
            {
                await DisplayAlert(
                    "تغيير الاسم",
                    "يجب تسجيل الدخول بحساب عضو لتغيير الاسم.",
                    "حسنًا");
                return;
            }

            string currentName = user.DisplayName?.Trim() ?? "";
            string? newName = await DisplayPromptAsync(
                "تغيير الاسم الظاهر",
                "أدخل الاسم الجديد. يجب أن يكون بين 3 و40 حرفاً.",
                "حفظ",
                "إلغاء",
                currentName,
                maxLength: 40,
                keyboard: Keyboard.Text,
                initialValue: currentName);

            if (string.IsNullOrWhiteSpace(newName))
                return;

            await PlayerDisplayNameService.UpdateCurrentDisplayNameAsync(newName);
            await RefreshIdentityAsync();
            await LoadPlayersAsync();

            await DisplayAlert(
                "تم التحديث",
                "تم تحديث الاسم الظاهر بنجاح.",
                "حسنًا");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "تعذر تغيير الاسم",
                ex.Message,
                "حسنًا");
        }
    }
}

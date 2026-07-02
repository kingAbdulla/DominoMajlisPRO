using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    new Task DisplayAlert(string title, string message, string cancel) =>
        ShowPremiumMessageAsync(title, message, cancel);

    new Task<bool> DisplayAlert(string title, string message, string accept, string cancel) =>
        ShowPremiumConfirmAsync(title, message, accept, cancel);

    new Task<string> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons) =>
        ShowPremiumActionSheetAsync(title, cancel, destruction, buttons);

    new Task<string?> DisplayPromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancel",
        string? placeholder = null,
        int maxLength = -1,
        Keyboard? keyboard = null,
        string initialValue = "") =>
        ShowPremiumPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);

    Task ShowPremiumMessageAsync(string title, string message, string buttonText)
    {
        var tcs = new TaskCompletionSource();
        ShowPremiumDialog(title, message, new[]
        {
            CreateDialogButton(buttonText, true, () => tcs.TrySetResult())
        });
        return tcs.Task;
    }

    Task<bool> ShowPremiumConfirmAsync(string title, string message, string accept, string cancel)
    {
        var tcs = new TaskCompletionSource<bool>();
        ShowPremiumDialog(title, message, new[]
        {
            CreateDialogButton(cancel, false, () => tcs.TrySetResult(false)),
            CreateDialogButton(accept, true, () => tcs.TrySetResult(true))
        });
        return tcs.Task;
    }

    Task<string> ShowPremiumActionSheetAsync(string title, string cancel, string? destruction, IReadOnlyList<string> buttons)
    {
        var tcs = new TaskCompletionSource<string>();
        var views = new List<View>();

        foreach (var button in buttons)
        {
            var isDanger = button.Contains("حذف", StringComparison.OrdinalIgnoreCase) ||
                button.Contains("🗑", StringComparison.OrdinalIgnoreCase);
            views.Add(CreateDialogButton(button, !isDanger, () => tcs.TrySetResult(button), isDanger));
        }

        if (!string.IsNullOrWhiteSpace(destruction))
            views.Add(CreateDialogButton(destruction, false, () => tcs.TrySetResult(destruction), true));

        views.Add(CreateDialogButton(cancel, false, () => tcs.TrySetResult(cancel)));
        ShowPremiumDialog(title, string.Empty, views, isActionSheet: true);
        return tcs.Task;
    }

    Task<string?> ShowPremiumPromptAsync(
        string title,
        string message,
        string accept,
        string cancel,
        string? placeholder,
        int maxLength,
        Keyboard? keyboard,
        string initialValue)
    {
        var tcs = new TaskCompletionSource<string?>();
        var entry = new Entry
        {
            Text = initialValue,
            Placeholder = placeholder ?? string.Empty,
            Keyboard = keyboard ?? Keyboard.Default,
            MaxLength = maxLength > 0 ? maxLength : int.MaxValue,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#777777"),
            BackgroundColor = Color.FromArgb("#101010"),
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            FlowDirection = FlowDirection.RightToLeft
        };

        ShowPremiumDialog(title, message, new View[]
        {
            entry,
            CreateDialogButton(cancel, false, () => tcs.TrySetResult(null)),
            CreateDialogButton(accept, true, () => tcs.TrySetResult(entry.Text))
        });
        return tcs.Task;
    }

    void ShowPremiumDialog(string title, string message, IReadOnlyList<View> actionViews, bool isActionSheet = false)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Content is not Grid root)
                return;

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                ZIndex = 2000,
                Padding = new Thickness(18),
                FlowDirection = FlowDirection.RightToLeft
            };

            var panel = new Border
            {
                Padding = new Thickness(16),
                Stroke = Color.FromArgb("#D4AF37"),
                StrokeThickness = 1.2,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb("#101010"), 0f),
                        new GradientStop(Color.FromArgb("#18130A"), 1f)
                    }
                },
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = isActionSheet ? LayoutOptions.End : LayoutOptions.Center,
                MaximumWidthRequest = 430,
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? -1 : 420
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 12
            };

            stack.Children.Add(new Label
            {
                Text = title,
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 18 : 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#D4AF37"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            });

            if (!string.IsNullOrWhiteSpace(message))
            {
                stack.Children.Add(new Label
                {
                    Text = message,
                    FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 13 : 15,
                    TextColor = Color.FromArgb("#E8E8E8"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            foreach (var view in actionViews)
                stack.Children.Add(view);

            panel.Content = stack;
            overlay.Children.Add(panel);
            root.Children.Add(overlay);

            foreach (var button in FindButtons(panel))
            {
                var originalCommand = button.Command;
                var originalClickedHandlers = button.GetType();
                button.Clicked += (_, _) => root.Children.Remove(overlay);
            }
        });
    }

    Button CreateDialogButton(string text, bool primary, Action action, bool danger = false)
    {
        var button = new Button
        {
            Text = text,
            HeightRequest = 46,
            CornerRadius = 14,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            BackgroundColor = primary
                ? Color.FromArgb("#D4AF37")
                : danger
                    ? Color.FromArgb("#2A1111")
                    : Color.FromArgb("#111111"),
            TextColor = primary
                ? Colors.Black
                : danger
                    ? Color.FromArgb("#FFD7D7")
                    : Colors.White,
            BorderColor = danger ? Color.FromArgb("#B64A4A") : Color.FromArgb("#8A5B27"),
            BorderWidth = 1
        };

        button.Clicked += (_, _) => action();
        return button;
    }

    static IEnumerable<Button> FindButtons(IView view)
    {
        if (view is Button button)
        {
            yield return button;
            yield break;
        }

        if (view is Border border && border.Content is IView content)
        {
            foreach (var found in FindButtons(content))
                yield return found;
        }
        else if (view is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                foreach (var found in FindButtons(child))
                    yield return found;
            }
        }
    }
}

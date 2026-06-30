using System.Text.Json;

namespace DominoMajlisPRO.Pages;

public sealed class LivingEmblem3DTestPage : ContentPage
{
    private const string ReadyScheme = "living-emblem://ready";
    private const string ErrorScheme = "living-emblem://error";

    private readonly WebView _webView = new();
    private readonly Grid _fallback;
    private readonly Label _status;
    private bool _rendererReady;

    public LivingEmblem3DTestPage()
    {
        Title = "Living Emblem 3D Proof";
        BackgroundColor = Color.FromArgb("#111111");

        _status = new Label
        {
            Text = "Initializing isolated 3D proof...",
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 14,
            Margin = new Thickness(16, 12)
        };

        _fallback = new Grid
        {
            IsVisible = false,
            BackgroundColor = Color.FromArgb("#181818"),
            Children =
            {
                new Image
                {
                    Source = ImageSource.FromFile("dragon_3d.png"),
                    Aspect = Aspect.AspectFit,
                    WidthRequest = 220,
                    HeightRequest = 220,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "Static PNG fallback",
                    TextColor = Colors.White,
                    FontSize = 13,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    Margin = new Thickness(0, 0, 0, 24)
                }
            }
        };

        _webView.Navigating += OnWebViewNavigating;

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Children =
            {
                _webView,
                _fallback,
                _status
            }
        };

        Grid.SetRowSpan(_webView, 2);
        Grid.SetRowSpan(_fallback, 2);
        Grid.SetRow(_status, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_webView.Source == null)
            await LoadProofAsync();
        else
            await SendLifecycleCommandAsync("livingEmblemResume");
    }

    protected override async void OnDisappearing()
    {
        await SendLifecycleCommandAsync("livingEmblemPause");
        base.OnDisappearing();
    }

    private async Task LoadProofAsync()
    {
        try
        {
            var html = await ReadRawAssetAsync("living_emblem_3d_test.html");
            var gltf = await ReadRawAssetAsync("living_emblem_test.gltf");
            var escapedGltf = JsonSerializer.Serialize(gltf);

            _webView.Source = new HtmlWebViewSource
            {
                Html = html.Replace("__LIVING_EMBLEM_GLTF_JSON__", escapedGltf)
            };

            Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (!_rendererReady)
                    ShowFallback("3D proof did not signal ready; fallback is active.");
                return false;
            });
        }
        catch (Exception ex)
        {
            ShowFallback($"3D proof failed to load: {ex.Message}");
        }
    }

    private async Task SendLifecycleCommandAsync(string command)
    {
        if (!_rendererReady)
            return;

        try
        {
            await _webView.EvaluateJavaScriptAsync($"{command}();");
        }
        catch
        {
            ShowFallback("3D proof lifecycle command failed; fallback is active.");
        }
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith(ReadyScheme, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            _rendererReady = true;
            _fallback.IsVisible = false;
            _webView.IsVisible = true;
            _status.Text = Uri.UnescapeDataString(e.Url[ReadyScheme.Length..].TrimStart('?'));
        }
        else if (e.Url.StartsWith(ErrorScheme, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            ShowFallback(Uri.UnescapeDataString(e.Url[ErrorScheme.Length..].TrimStart('?')));
        }
    }

    private void ShowFallback(string message)
    {
        _rendererReady = false;
        _webView.IsVisible = false;
        _fallback.IsVisible = true;
        _status.Text = message;
    }

    private static async Task<string> ReadRawAssetAsync(string assetName)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class PremiumHeaderView : ContentView
{
    public PremiumHeaderView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        var cart = CreateCart();
        var title = CreateTitle();
        var currencies = CreateCurrencies();

        root.Add(cart, 0, 0);
        root.Add(title, 1, 0);
        root.Add(currencies, 2, 0);

        Content = new Border
        {
            BackgroundColor = Color.FromArgb("#070707"),
            Stroke = Color.FromArgb("#3F2A12"),
            StrokeThickness = 0.9,
            Padding = new Thickness(12, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = root
        };
    }

    private static View CreateCart()
    {
        return new Border
        {
            WidthRequest = 46,
            HeightRequest = 46,
            BackgroundColor = Color.FromArgb("#090909"),
            Stroke = Color.FromArgb("#5B3B18"),
            StrokeThickness = 0.9,
            StrokeShape = new RoundRectangle { CornerRadius = 23 },
            Content = new Label
            {
                Text = "🛒",
                FontSize = 24,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View CreateTitle()
    {
        return new VerticalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "♛ متجر دومينو",
                    FontFamily = "Tajawal-Regular",
                    FontSize = 21,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFE8A3"),
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = "كل ما يميزك في مكان واحد",
                    FontFamily = "Tajawal-Regular",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#C8B58A"),
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };
    }

    private static View CreateCurrencies()
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                CreateCurrencyRow("🪙", "18,500"),
                CreateCurrencyRow("💎", "420")
            }
        };
    }

    private static View CreateCurrencyRow(string icon, string value)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#090909"),
            Stroke = Color.FromArgb("#5B3B18"),
            StrokeThickness = 0.8,
            Padding = new Thickness(8, 4),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new HorizontalStackLayout
            {
                FlowDirection = FlowDirection.LeftToRight,
                Spacing = 5,
                Children =
                {
                    new Label
                    {
                        Text = icon,
                        FontSize = 15,
                        VerticalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = value,
                        FontFamily = "CinzelDecorative-Bold",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#FFE8A3"),
                        VerticalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = "+",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#FFD76A"),
                        VerticalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }
}
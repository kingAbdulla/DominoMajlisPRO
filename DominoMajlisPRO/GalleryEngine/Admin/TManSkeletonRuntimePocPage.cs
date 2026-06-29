using DominoMajlisPRO.LivingVisualPlatform.Controls;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class TManSkeletonRuntimePocPage : ContentPage
{
    private const string PackagePath = "LivingEmblems/t_man/character.glb";
    private const string RuntimeMode = "Living Mind Runtime v1";
    private readonly ContentView _previewHost = new() { HeightRequest = 520 };

    public TManSkeletonRuntimePocPage()
    {
        Title = "T-Man Skeleton Runtime POC";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#030303");
        NavigationPage.SetHasNavigationBar(this, false);
        BuildPage();
        LoadPreview();
    }

    private void BuildPage()
    {
        var back = Button("Back", async () => await Navigation.PopAsync());
        var reload = Button("Reload Preview", () =>
        {
            LoadPreview();
            return Task.CompletedTask;
        });

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        header.Add(back, 0);
        header.Add(new VerticalStackLayout
        {
            Children =
            {
                new Label
                {
                    Text = "T-Man Skeleton Runtime POC",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFD966"),
                    HorizontalTextAlignment = TextAlignment.End
                },
                new Label
                {
                    Text = "Developer R&D Preview",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A88E45"),
                    HorizontalTextAlignment = TextAlignment.End
                }
            }
        }, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    header,
                    Panel(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label
                            {
                                Text = "T-Man Skeleton Runtime POC",
                                FontSize = 16,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#FFD966"),
                                HorizontalTextAlignment = TextAlignment.Center
                            },
                            _previewHost
                        }
                    }),
                    Panel(new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            Diagnostic("Asset Id", StoreCatalogLivingVisualManifestProvider.TManSkeletonRuntimeAssetId),
                            Diagnostic("Package Path", PackagePath),
                            Diagnostic("Runtime Mode", RuntimeMode),
                            Diagnostic("Runtime", "Living Mind Runtime v1"),
                            Diagnostic("Animation Clips", "None"),
                            Diagnostic("Display Location", LivingVisualDisplayLocation.StorePreview.ToString()),
                            Diagnostic("Effects", "Disabled"),
                            Diagnostic("Behavior", "Autonomous procedural mind")
                        }
                    }),
                    reload
                }
            }
        };
    }

    private void LoadPreview()
    {
        _previewHost.Content = new LivingVisualHost
        {
            AssetId = StoreCatalogLivingVisualManifestProvider.TManSkeletonRuntimeAssetId,
            StaticFallbackImage = "shield_3d.png",
            ApplicationUserId = string.Empty,
            PlayerId = string.Empty,
            TeamId = string.Empty,
            DisplayLocation = LivingVisualDisplayLocation.StorePreview,
            IsDeveloperPreview = true,
            IsStorePreview = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
    }

    private static Label Diagnostic(string label, string value) => new()
    {
        Text = $"{label}: {value}",
        FontSize = 12,
        TextColor = Color.FromArgb("#DCCB88"),
        HorizontalTextAlignment = TextAlignment.Start
    };

    private static Border Panel(View content) => new()
    {
        Padding = 12,
        StrokeThickness = 1,
        Stroke = Color.FromArgb("#5A4211"),
        Background = Color.FromArgb("#101010"),
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Content = content
    };

    private static Button Button(string text, Func<Task> action)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await action();
        return button;
    }
}

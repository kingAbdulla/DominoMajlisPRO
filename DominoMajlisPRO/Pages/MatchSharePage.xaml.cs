using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace DominoMajlisPRO.Pages;

public enum MatchShareMode
{
    Image,
    Pdf
}

/// <summary>
/// Off-screen render + capture surface used to produce a shareable match
/// result image or PDF. Mirrors the proven capture approach used by
/// CertificatePrintPage so Arabic text stays perfectly shaped in the output.
/// </summary>
public partial class MatchSharePage : ContentPage
{
    readonly SavedMatch? match;
    readonly MatchShareMode mode;
    bool started;

    const double TargetWidth = 1000;
    const string Gold = "#D4AF37";
    const string Panel = "#14000000";

    public MatchSharePage(SavedMatch? savedMatch, MatchShareMode shareMode)
    {
        InitializeComponent();

        match = savedMatch;
        mode = shareMode;

        BusyLabel.Text = mode == MatchShareMode.Pdf
            ? "جارٍ إنشاء ملف PDF..."
            : "جارٍ إنشاء الصورة...";

        Loaded += async (_, _) =>
        {
            if (started)
                return;

            started = true;
            await RunAsync();
        };
    }

    async Task RunAsync()
    {
        if (match == null)
        {
            await SafePopAsync();
            return;
        }

        try
        {
            await BuildCardAsync();

            // Give the layout time to render images/effects before capture.
            await Task.Delay(900);

            var height = MeasureContainerHeight();
            ShareContainer.HeightRequest = height;

            await Task.Delay(500);

            var screenshot = await ShareContainer.CaptureAsync();
            if (screenshot == null)
            {
                await FailAsync();
                return;
            }

            var pngPath = System.IO.Path.Combine(
                FileSystem.CacheDirectory,
                $"Match_Result_{FileStamp()}.png");

            using (var input = await screenshot.OpenReadAsync())
            using (var output = File.Create(pngPath))
            {
                await input.CopyToAsync(output);
            }

            if (mode == MatchShareMode.Image)
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Domino Majlis PRO",
                    File = new ShareFile(pngPath)
                });
            }
            else
            {
                var pdfPath = BuildPdf(pngPath);
                TryDelete(pngPath);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Domino Majlis PRO",
                    File = new ShareFile(pdfPath)
                });
            }

            await SafePopAsync();
        }
        catch
        {
            await FailAsync();
        }
    }

    double MeasureContainerHeight()
    {
        try
        {
            var size = ((IView)ShareContainer).Measure(
                TargetWidth,
                double.PositiveInfinity);

            var h = size.Height;
            if (double.IsNaN(h) || h < 400)
                h = 1200;

            return h;
        }
        catch
        {
            return 1400;
        }
    }

    string BuildPdf(string pngPath)
    {
        var pdfPath = System.IO.Path.Combine(
            FileSystem.CacheDirectory,
            $"Match_Result_{FileStamp()}.pdf");

        TryDelete(pdfPath);

        var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawRectangle(XBrushes.Black, 0, 0, page.Width, page.Height);

        using var fs = new FileStream(
            pngPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var image = XImage.FromStream(() => fs);

        const double margin = 18;
        double maxW = page.Width - margin * 2;
        double maxH = page.Height - margin * 2;

        double imageRatio = image.PixelWidth / (double)image.PixelHeight;
        double boxRatio = maxW / maxH;

        double drawW, drawH;
        if (imageRatio > boxRatio)
        {
            drawW = maxW;
            drawH = drawW / imageRatio;
        }
        else
        {
            drawH = maxH;
            drawW = drawH * imageRatio;
        }

        double x = (page.Width - drawW) / 2;
        double y = (page.Height - drawH) / 2;

        gfx.DrawImage(image, x, y, drawW, drawH);
        document.Save(pdfPath);

        return pdfPath;
    }

    async Task BuildCardAsync()
    {
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(40, 44, 40, 44),
            Spacing = 18
        };

        // ---- Header ----
        root.Children.Add(new HorizontalStackLayout
        {
            Spacing = 16,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image
                {
                    Source = "domino_gold_icon.png",
                    WidthRequest = 66,
                    HeightRequest = 66,
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "Domino Majlis PRO",
                    TextColor = Color.FromArgb(Gold),
                    FontSize = 40,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        });

        root.Children.Add(new Label
        {
            Text = "نتيجة المباراة",
            TextColor = Colors.White,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        // ---- Teams ----
        var (team1Emblem, team2Emblem) = await ResolveEmblemsAsync();

        var teamsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        teamsGrid.Add(TeamColumn(team1Emblem, match!.Team1Name, match.Team1Players), 0);
        teamsGrid.Add(new Image
        {
            Source = "vs_gold_icon.png",
            WidthRequest = 96,
            HeightRequest = 96,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center
        }, 1);
        teamsGrid.Add(TeamColumn(team2Emblem, match.Team2Name, match.Team2Players), 2);

        root.Children.Add(Panelize(teamsGrid));

        // ---- Score + winner ----
        var winnerName = string.IsNullOrWhiteSpace(match.WinnerTeamName)
            ? match.WinnerTeam
            : match.WinnerTeamName;

        root.Children.Add(Panelize(new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "النتيجة النهائية",
                    TextColor = Color.FromArgb(Gold),
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = $"{match.Team1Score} - {match.Team2Score}",
                    TextColor = Colors.White,
                    FontSize = 74,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = string.IsNullOrWhiteSpace(winnerName)
                        ? "الفائز: —"
                        : $"الفائز: {winnerName}",
                    TextColor = Color.FromArgb("#4CAF50"),
                    FontSize = 34,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 2,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        }));

        // ---- Info grid ----
        root.Children.Add(Panelize(BuildInfoGrid()));

        // ---- Rounds history (PDF only) ----
        if (mode == MatchShareMode.Pdf && match.RoundsHistory.Any())
            root.Children.Add(Panelize(BuildRoundsSummary()));

        // ---- QR + footer ----
        root.Children.Add(BuildQrFooter());

        ShareContainer.Children.Clear();
        ShareContainer.Children.Add(root);
    }

    View BuildInfoGrid()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnSpacing = 12,
            RowSpacing = 14
        };

        var start = match!.MatchDate.ToString("HH:mm");
        var end = match.MatchEndDate > DateTime.MinValue
            ? match.MatchEndDate.ToString("HH:mm")
            : "--:--";

        grid.Add(InfoCell("القوانين", match.IsLocalRules ? "محلي" : "دولي"), 0, 0);
        grid.Add(InfoCell("التاريخ", match.MatchDate.ToString("yyyy/MM/dd")), 1, 0);
        grid.Add(InfoCell("الجولات", match.RoundsHistory.Count.ToString()), 2, 0);
        grid.Add(InfoCell("وقت البداية", start), 0, 1);
        grid.Add(InfoCell("وقت النهاية", end), 1, 1);
        grid.Add(InfoCell("الملص", match.HasMeles ? "ملص" : "لا يوجد"), 2, 1);

        return grid;
    }

    View BuildRoundsSummary()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };

        stack.Children.Add(new Label
        {
            Text = "سجل الجولات",
            TextColor = Color.FromArgb(Gold),
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        // Header row
        stack.Children.Add(RoundRow(
            "الجولة", "الفائز", "النقاط", "النتيجة", "الملص", true));

        foreach (var r in match!.RoundsHistory)
        {
            stack.Children.Add(RoundRow(
                r.RoundNumber.ToString(),
                r.WinnerTeam,
                $"+{r.Points}",
                $"{r.Team1NewScore}-{r.Team2NewScore}",
                r.IsMeles ? "ملص" : "—",
                false));
        }

        return stack;
    }

    View RoundRow(
        string a, string b, string c, string d, string e, bool header)
    {
        var color = header ? Color.FromArgb(Gold) : Colors.White;
        var attr = header ? FontAttributes.Bold : FontAttributes.None;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 6,
            Padding = new Thickness(4, 8)
        };

        grid.Add(RoundCell(a, color, attr), 0);
        grid.Add(RoundCell(b, color, attr), 1);
        grid.Add(RoundCell(c, header ? color : Color.FromArgb("#4CAF50"), attr), 2);
        grid.Add(RoundCell(d, header ? color : Color.FromArgb(Gold), attr), 3);
        grid.Add(RoundCell(e, color, attr), 4);

        if (!header)
        {
            return new Border
            {
                BackgroundColor = Color.FromArgb("#10FFFFFF"),
                Stroke = Color.FromArgb("#33D4AF37"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = grid
            };
        }

        return grid;
    }

    static Label RoundCell(string text, Color color, FontAttributes attr) =>
        new()
        {
            Text = text,
            TextColor = color,
            FontSize = 20,
            FontAttributes = attr,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

    View BuildQrFooter()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 16
        };

        grid.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = "Domino Majlis PRO",
                    TextColor = Color.FromArgb(Gold),
                    FontSize = 30,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = $"معرّف الشهادة: {CertificateQrService.BuildCertificateId(match)}",
                    TextColor = Colors.White,
                    FontSize = 18,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        }, 0);

        grid.Add(new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb(Gold),
                    StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = 8,
                    Content = new Image
                    {
                        Source = CertificateQrService.GenerateQrImageSource(match, 14),
                        WidthRequest = 130,
                        HeightRequest = 130,
                        Aspect = Aspect.AspectFit
                    }
                },
                new Label
                {
                    Text = "تحقق من الشهادة",
                    TextColor = Color.FromArgb(Gold),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        }, 1);

        return Panelize(grid);
    }

    static View TeamColumn(ImageSource emblem, string name, string players)
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image
                {
                    Source = emblem,
                    WidthRequest = 150,
                    HeightRequest = 150,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = string.IsNullOrWhiteSpace(name) ? "—" : name,
                    TextColor = Colors.White,
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text = players ?? "",
                    TextColor = Color.FromArgb("#D8D0C2"),
                    FontSize = 18,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 2,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        };
    }

    static View InfoCell(string title, string value)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = title,
                    TextColor = Color.FromArgb(Gold),
                    FontSize = 18,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                    TextColor = Colors.White,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        };
    }

    static Border Panelize(View content) =>
        new()
        {
            BackgroundColor = Color.FromArgb(Panel),
            Stroke = Color.FromArgb(Gold),
            StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = new Thickness(18, 16),
            Content = content
        };

    async Task<(ImageSource, ImageSource)> ResolveEmblemsAsync()
    {
        ImageSource e1 = "shield_3d.png";
        ImageSource e2 = "shield_3d.png";

        try
        {
            var team1 = await TeamProfileService.GetTeamByIdAsync(match!.Team1Id);
            var team2 = await TeamProfileService.GetTeamByIdAsync(match.Team2Id);

            e1 = InventoryDisplayResolver.ResolveImageSource(
                team1?.Emblem ?? match.Team1Emblem, "shield_3d.png");
            e2 = InventoryDisplayResolver.ResolveImageSource(
                team2?.Emblem ?? match.Team2Emblem, "shield_3d.png");
        }
        catch
        {
            // Keep safe fallbacks.
        }

        return (e1, e2);
    }

    string FileStamp()
    {
        var id = CertificateQrService.BuildCertificateId(match);
        return $"{id}_{DateTime.Now:HHmmss}";
    }

    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }

    async Task FailAsync()
    {
        try
        {
            await DisplayAlert(
                "تعذر المشاركة",
                "تعذر إنشاء ملف المشاركة حالياً. حاول مرة أخرى.",
                "حسناً");
        }
        catch
        {
            // Ignore.
        }

        await SafePopAsync();
    }

    async Task SafePopAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopAsync();
            });
        }
        catch
        {
            // Ignore navigation errors.
        }
    }
}

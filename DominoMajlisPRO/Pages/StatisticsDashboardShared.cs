using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public enum ChartKind
{
    Line,
    Area,
    Donut
}

public sealed class TrendChartDrawable : IDrawable
{
    public IReadOnlyList<double> Values { get; set; } = Array.Empty<double>();
    public ChartKind Kind { get; set; } = ChartKind.Line;
    public Color PrimaryColor { get; set; } = Color.FromArgb("#D4AE62");
    public Color SecondaryColor { get; set; } = Color.FromArgb("#69D84F");
    public string CenterText { get; set; } = "";

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;
        canvas.FillColor = Color.FromArgb("#101010");
        canvas.FillRoundedRectangle(dirtyRect, 12);

        var plot = new RectF(dirtyRect.X + 24, dirtyRect.Y + 20, dirtyRect.Width - 38, dirtyRect.Height - 34);
        DrawGrid(canvas, plot);

        if (Kind == ChartKind.Donut)
        {
            DrawDonut(canvas, dirtyRect);
            DrawCenterText(canvas, dirtyRect);
            canvas.RestoreState();
            return;
        }

        var values = Values.Count == 0 ? new[] { 0d, 0d } : Values;
        double max = Math.Max(1, values.Max());
        double min = Math.Min(0, values.Min());
        double span = Math.Max(1, max - min);
        var points = values.Select((value, index) =>
        {
            float x = plot.Left + (float)(index / (double)Math.Max(1, values.Count - 1) * plot.Width);
            float y = plot.Bottom - (float)((value - min) / span * plot.Height);
            return new PointF(x, y);
        }).ToList();

        if (points.Count > 1)
        {
            var path = new PathF();
            path.MoveTo(points[0]);
            foreach (var point in points.Skip(1))
                path.LineTo(point);

            if (Kind == ChartKind.Area)
            {
                var area = new PathF(path);
                area.LineTo(points[^1].X, plot.Bottom);
                area.LineTo(points[0].X, plot.Bottom);
                area.Close();
                canvas.FillColor = PrimaryColor.WithAlpha(0.28f);
                canvas.FillPath(area);
            }

            canvas.StrokeColor = PrimaryColor;
            canvas.StrokeSize = 3;
            canvas.DrawPath(path);

            canvas.FillColor = PrimaryColor;
            foreach (var point in points)
                canvas.FillCircle(point, 3);
        }

        DrawCenterText(canvas, dirtyRect);

        canvas.RestoreState();
    }

    void DrawGrid(ICanvas canvas, RectF plot)
    {
        canvas.StrokeColor = Color.FromArgb("#2A2A2A");
        canvas.StrokeSize = 1;
        for (int i = 0; i <= 4; i++)
        {
            float y = plot.Top + plot.Height * i / 4;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
        }
    }

    void DrawDonut(ICanvas canvas, RectF rect)
    {
        if (Values.Count <= 2 && !string.IsNullOrWhiteSpace(CenterText))
        {
            DrawSingleValueRing(canvas, rect);
            return;
        }

        double win = Values.ElementAtOrDefault(0);
        double loss = Values.ElementAtOrDefault(1);
        double draw = Values.ElementAtOrDefault(2);
        double total = Math.Max(1, win + loss + draw);
        float size = Math.Min(rect.Width, rect.Height) * 0.58f;
        var center = rect.Center;
        var donut = new RectF(center.X - size / 2, center.Y - size / 2, size, size);
        float start = -90;
        DrawArc(canvas, donut, start, (float)(win / total * 360), Color.FromArgb("#69D84F"));
        start += (float)(win / total * 360);
        DrawArc(canvas, donut, start, (float)(loss / total * 360), Color.FromArgb("#FF3B30"));
        start += (float)(loss / total * 360);
        DrawArc(canvas, donut, start, (float)(draw / total * 360), Color.FromArgb("#BFC3C7"));

        canvas.FillColor = Color.FromArgb("#101010");
        canvas.FillCircle(center, size * 0.28f);
    }

    void DrawSingleValueRing(ICanvas canvas, RectF rect)
    {
        double progress = Math.Clamp(Values.ElementAtOrDefault(0), 0, 1);
        float size = Math.Min(rect.Width, rect.Height) * 0.58f;
        var center = rect.Center;
        var donut = new RectF(center.X - size / 2, center.Y - size / 2, size, size);

        canvas.StrokeColor = Color.FromArgb("#2A2A2A");
        canvas.StrokeSize = Math.Max(12, donut.Width * 0.16f);
        canvas.DrawArc(donut, -90, 360, false, false);

        DrawArc(canvas, donut, -90, (float)(progress * 360), PrimaryColor);

        canvas.FillColor = Color.FromArgb("#101010");
        canvas.FillCircle(center, size * 0.28f);
    }

    static void DrawArc(ICanvas canvas, RectF rect, float start, float sweep, Color color)
    {
        if (sweep <= 0)
            return;

        canvas.StrokeColor = color;
        canvas.StrokeSize = Math.Max(12, rect.Width * 0.16f);
        canvas.DrawArc(rect, start, sweep, false, false);
    }

    void DrawCenterText(ICanvas canvas, RectF rect)
    {
        if (string.IsNullOrWhiteSpace(CenterText))
            return;

        canvas.FontColor = Colors.White;
        canvas.FontSize = Kind == ChartKind.Donut ? 22 : 16;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.DrawString(
            CenterText,
            rect.X,
            rect.Center.Y - 14,
            rect.Width,
            28,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
    }
}

public static class StatisticsDashboardUi
{
    public const string PageBackground = "#030303";
    public const string Panel = "#070707";
    public const string Card = "#101010";
    public const string Gold = "#D4AE62";
    public const string Bronze = "#6F461A";
    public const string Muted = "#C8B58A";

    public static Border Frame(View content, double radius = 22, string stroke = Bronze, string background = Panel, double padding = 10)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb(background),
            Stroke = Color.FromArgb(stroke),
            StrokeThickness = 0.85,
            Padding = padding,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Content = content
        };
    }

    public static Label Label(string text, double size, Color color, bool bold = false, TextAlignment align = TextAlignment.Center, int maxLines = 1)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            FontFamily = "timesbi",
            TextColor = color,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = align,
            MaxLines = maxLines,
            LineBreakMode = maxLines == 1 ? LineBreakMode.TailTruncation : LineBreakMode.WordWrap
        };
    }

    public static Button CommandButton(string text)
    {
        return new Button
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 : 14,
            TextColor = Color.FromArgb(Gold),
            BackgroundColor = Color.FromArgb("#15100A"),
            BorderColor = Color.FromArgb("#8A5B27"),
            BorderWidth = 0.8,
            CornerRadius = 14,
            HeightRequest = 38,
            Padding = new Thickness(10, 0)
        };
    }

    public static View Metric(string icon, string value, string title)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 38 },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8,
            FlowDirection = FlowDirection.RightToLeft
        };

        row.Add(new Image { Source = icon, WidthRequest = 32, HeightRequest = 32, Aspect = Aspect.AspectFit });
        var text = new VerticalStackLayout { Spacing = 0 };
        text.Children.Add(Label(value, DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : 20, Colors.White, true, TextAlignment.End));
        text.Children.Add(Label(title, 11, Color.FromArgb(Muted), false, TextAlignment.End));
        Grid.SetColumn(text, 1);
        row.Add(text);
        return Frame(row, 12, "#5B3B18", Card, 8);
    }

    public static Grid MetricGrid(IEnumerable<View> cards)
    {
        int columns = DeviceInfo.Idiom == DeviceIdiom.Phone ? 2 : 5;
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        for (int i = 0; i < columns; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        int index = 0;
        foreach (var card in cards)
        {
            int row = index / columns;
            while (grid.RowDefinitions.Count <= row)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(card, index % columns);
            Grid.SetRow(card, row);
            grid.Add(card);
            index++;
        }

        return grid;
    }

    public static View ChartCard(string title, IReadOnlyList<double> values, ChartKind kind, Color color, string centerText = "")
    {
        var chart = new GraphicsView
        {
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 150 : 190,
            Drawable = new TrendChartDrawable { Values = values, Kind = kind, PrimaryColor = color, CenterText = centerText }
        };
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(Label(title, 14, Color.FromArgb(Gold), true));
        if (!string.IsNullOrWhiteSpace(centerText) && kind != ChartKind.Donut)
            stack.Children.Add(Label(centerText, 13, Colors.White, true));
        stack.Children.Add(chart);
        return Frame(stack, 14, "#5B3B18", Card, 8);
    }

    public static View StatusBadge(SafeStatusResult status)
    {
        var dot = new Ellipse
        {
            WidthRequest = 12,
            HeightRequest = 12,
            Fill = new SolidColorBrush(Color.FromArgb(status.ColorHex)),
            VerticalOptions = LayoutOptions.Center
        };
        var stack = new VerticalStackLayout { Spacing = 1 };
        stack.Children.Add(Label(status.Title, 12, Color.FromArgb(Gold), true, TextAlignment.End));
        stack.Children.Add(Label(status.Subtitle, 10, Color.FromArgb(Muted), false, TextAlignment.End, 2));
        var row = new HorizontalStackLayout { FlowDirection = FlowDirection.RightToLeft, Spacing = 8, Children = { dot, stack } };
        return Frame(row, 14, "#8A5B27", "#15100A", 8);
    }
}

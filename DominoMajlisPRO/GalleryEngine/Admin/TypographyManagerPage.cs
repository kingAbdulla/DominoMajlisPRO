namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class TypographyManagerPage : ContentPage
{
    public TypographyManagerPage()
    {
        Title = "Typography Manager";
        FlowDirection = FlowDirection.RightToLeft;
        Content = new Label
        {
            Text = "Typography Manager",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
    }
}

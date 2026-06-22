$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$xamlPath = Join-Path $projectRoot 'MainPage.xaml'

if (-not (Test-Path $xamlPath)) {
    throw "MainPage.xaml was not found at $xamlPath"
}

$text = Get-Content -LiteralPath $xamlPath -Raw -Encoding UTF8

$new = @'
                                <Border
                                    Grid.Column="0"
                                    WidthRequest="{OnIdiom Phone=76, Tablet=96}"
                                    HeightRequest="{OnIdiom Phone=76, Tablet=96}"
                                    HorizontalOptions="Center"
                                    VerticalOptions="Center"
                                    BackgroundColor="#151515"
                                    Stroke="#D4AF37"
                                    StrokeThickness="2.4"
                                    StrokeShape="RoundRectangle 999">
                                    <Border.Shadow>
                                        <Shadow Brush="#D4AF37" Radius="18" Opacity="0.45"/>
                                    </Border.Shadow>

                                    <Grid>
                                        <Image
                                            x:Name="HeaderPlayerAvatar"
                                            Source="normal_avatar_1.png"
                                            Aspect="AspectFill"/>

                                        <Image
                                            x:Name="HeaderAvatarFrameOverlay"
                                            Aspect="AspectFit"
                                            InputTransparent="True"
                                            IsVisible="False"/>

                                        <Image
                                            x:Name="HeaderAvatarEffectOverlay"
                                            Aspect="AspectFit"
                                            InputTransparent="True"
                                            IsVisible="False"/>

                                        <Border
                                            x:Name="ProfileStatusBadge"
                                            WidthRequest="{OnIdiom Phone=10, Tablet=12}"
                                            HeightRequest="{OnIdiom Phone=10, Tablet=12}"
                                            BackgroundColor="Red"
                                            Stroke="#111111"
                                            StrokeThickness="1"
                                            HorizontalOptions="End"
                                            VerticalOptions="End">
                                            <Border.StrokeShape>
                                                <RoundRectangle CornerRadius="999"/>
                                            </Border.StrokeShape>
                                        </Border>
                                    </Grid>
                                </Border>
'@

if ($text.Contains('WidthRequest="{OnIdiom Phone=76, Tablet=96}"') -and
    $text.Contains('x:Name="HeaderPlayerAvatar"') -and
    $text.Contains('StrokeShape="RoundRectangle 999"')) {
    $text = $text.Replace(
        '<Image' + "`r`n" + '                                            x:Name="HeaderAvatarEffectOverlay"' + "`r`n" + '                                            Aspect="AspectFit"' + "`r`n" + '                                            InputTransparent="True"' + "`r`n" + '                                            IsVisible="False"/>' + "`r`n`r`n" + '                                        <Image' + "`r`n" + '                                            x:Name="HeaderPlayerAvatar"',
        '<Image' + "`r`n" + '                                            x:Name="HeaderPlayerAvatar"')
    exit 0
}

$pattern = '(?s)\s*<Grid\s+Grid\.Column="0"\s+WidthRequest="\{OnIdiom Phone=40, Tablet=48\}"\s+HeightRequest="\{OnIdiom Phone=40, Tablet=48\}"\s+VerticalOptions="Center">.*?<Image\s+x:Name="HeaderPlayerAvatar".*?</Grid>\s*(?=<VerticalStackLayout\s+Grid\.Column="1")'

$match = [regex]::Match($text, $pattern)
if (-not $match.Success) {
    throw 'MainPage header avatar XAML block was not found. Patch not applied.'
}

$text = [regex]::Replace($text, $pattern, "`r`n$new", 1)
Set-Content -LiteralPath $xamlPath -Value $text -Encoding UTF8 -NoNewline

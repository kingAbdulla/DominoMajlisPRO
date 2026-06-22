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

$oldGridPattern = '(?s)\s*<Grid\s+Grid\.Column="0"\s+WidthRequest="\{OnIdiom Phone=40, Tablet=48\}"\s+HeightRequest="\{OnIdiom Phone=40, Tablet=48\}"\s+VerticalOptions="Center">.*?x:Name="ProfileStatusBadge".*?</Border>\s*</Grid>\s*(?=<VerticalStackLayout\s+Grid\.Column="1")'
$patchedBorderPattern = '(?s)\s*<Border\s+Grid\.Column="0"\s+WidthRequest="\{OnIdiom Phone=76, Tablet=96\}"\s+HeightRequest="\{OnIdiom Phone=76, Tablet=96\}".*?x:Name="ProfileStatusBadge".*?</Border>\s*</Grid>\s*</Border>\s*(?=<VerticalStackLayout\s+Grid\.Column="1")'

if ([regex]::IsMatch($text, $oldGridPattern)) {
    $text = [regex]::Replace($text, $oldGridPattern, "`r`n$new", 1)
    Set-Content -LiteralPath $xamlPath -Value $text -Encoding UTF8 -NoNewline
    exit 0
}

if ([regex]::IsMatch($text, $patchedBorderPattern)) {
    $text = [regex]::Replace($text, $patchedBorderPattern, "`r`n$new", 1)
    Set-Content -LiteralPath $xamlPath -Value $text -Encoding UTF8 -NoNewline
    exit 0
}

throw 'MainPage header avatar XAML block was not found. Patch not applied.'

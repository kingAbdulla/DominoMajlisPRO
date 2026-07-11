namespace DominoMajlisPRO.Cloud;

public sealed class CloudApiOptions
{
    public const string DefaultBaseUrl = "https://dominomajlispro2.pages.dev/";

    public string BaseUrl { get; init; } = DefaultBaseUrl;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

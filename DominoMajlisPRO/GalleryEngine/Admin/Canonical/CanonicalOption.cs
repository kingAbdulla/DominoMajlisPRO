namespace DominoMajlisPRO.GalleryEngine.Admin.Canonical;

public sealed record CanonicalOption(
    string CanonicalId,
    string DisplayName)
{
    public override string ToString() => DisplayName;
}
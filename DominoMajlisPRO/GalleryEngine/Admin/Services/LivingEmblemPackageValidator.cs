using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class LivingEmblemPackageValidator
{
    private static readonly string[] RequiredFiles =
    {
        "manifest.json",
        "living_emblem.glb",
        "thumbnail.png",
        "fallback.png",
        "behavior.json",
        "metadata.json"
    };

    public static void ValidateProductionImport(LivingEmblemPackageImportResult result)
    {
        var package = result.Package;
        if (package == null)
            return;

        var manifest = package.Manifest;
        var behavior = package.Behavior;
        var metadata = package.Metadata;

        RequireValue(manifest.SchemaVersion, "ManifestSchemaVersionMissing", "Manifest schema version is required.", result);
        if (!string.Equals(manifest.SchemaVersion, "1.0", StringComparison.OrdinalIgnoreCase))
        {
            Error(result, "ManifestSchemaUnsupported", $"Manifest schema '{manifest.SchemaVersion}' is not supported.");
        }

        foreach (var fileName in RequiredFiles)
        {
            if (!ManifestReferencesFile(manifest, fileName))
                Error(result, "RequiredFileNotReferenced", $"Manifest must reference required file '{fileName}'.");
        }

        if (!string.Equals(manifest.Backend, LivingRendererBackend.Filament.ToString(), StringComparison.OrdinalIgnoreCase))
            Error(result, "BackendCompatibilityFailed", "Production Living Emblems must target the Filament backend.");

        if (!manifest.SupportedPlatforms.Any(platform => string.Equals(platform, "Android", StringComparison.OrdinalIgnoreCase)))
            Error(result, "PlatformCompatibilityFailed", "Production Living Emblems must declare Android platform support.");

        if (!string.Equals(manifest.GLB, "living_emblem.glb", StringComparison.OrdinalIgnoreCase))
            Error(result, "RendererCompatibilityFailed", "Renderer compatibility requires manifest GLB path 'living_emblem.glb'.");

        if (!package.ResolvedGlbPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            Error(result, "GlbExtensionInvalid", "Living Emblem model must be a GLB asset.");

        if (!package.ResolvedThumbnailPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            Error(result, "ThumbnailExtensionInvalid", "Living Emblem thumbnail must be a PNG asset.");

        if (!package.ResolvedFallbackPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            Error(result, "FallbackExtensionInvalid", "Living Emblem fallback must be a PNG asset.");

        RequireValue(behavior.ProfileId, "BehaviorProfileMissing", "Behavior schema requires profileId.", result);
        RequireValue(metadata.AuthoringTool, "MetadataAuthoringToolMissing", "Metadata schema requires authoringTool.", result);
        RequireValue(metadata.Exporter, "MetadataExporterMissing", "Metadata schema requires exporter.", result);
        RequireValue(metadata.SourceAsset, "MetadataSourceAssetMissing", "Metadata schema requires sourceAsset.", result);
        RequireValue(metadata.ArtStatus, "MetadataArtStatusMissing", "Metadata schema requires artStatus.", result);

        if (metadata.IsTemporaryArt)
            Error(result, "TemporaryArtRejected", "Temporary placeholder art cannot be approved or published as a production Living Emblem.");

        foreach (var extension in manifest.FutureExtensions)
        {
            if (LooksLikeRequiredAsset(extension.Value) &&
                !ReferencesKnownPackageAsset(extension.Value, manifest))
            {
                Error(result, "FutureExtensionAssetUnresolved", $"Future extension '{extension.Key}' references '{extension.Value}', but it is not a package asset.");
            }
        }
    }

    private static bool ManifestReferencesFile(LivingEmblemPackageManifest manifest, string fileName) =>
        string.Equals("manifest.json", fileName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(manifest.GLB, fileName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(manifest.Thumbnail, fileName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(manifest.Fallback, fileName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(manifest.Behavior, fileName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(manifest.Metadata, fileName, StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeRequiredAsset(string value) =>
        value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".ktx", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".ktx2", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);

    private static bool ReferencesKnownPackageAsset(string value, LivingEmblemPackageManifest manifest) =>
        string.Equals(value, manifest.GLB, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, manifest.Thumbnail, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, manifest.Fallback, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, manifest.Behavior, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, manifest.Metadata, StringComparison.OrdinalIgnoreCase);

    private static void RequireValue(
        string? value,
        string code,
        string message,
        LivingEmblemPackageImportResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
            Error(result, code, message);
    }

    private static void Error(
        LivingEmblemPackageImportResult result,
        string code,
        string message) =>
        result.Diagnostics.Add(new LivingEmblemPackageDiagnostic(
            LivingEmblemPackageDiagnosticSeverity.Error,
            code,
            message));
}

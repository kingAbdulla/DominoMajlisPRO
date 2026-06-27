using System.Text.Json;
using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public static class LivingEmblemPackagePaths
{
    public const string DefaultProductionPackagePath = "LivingEmblems/production_default";
    public const string ManifestFileName = "manifest.json";

    public static string Combine(string root, string relativePath)
    {
        var cleanRoot = Normalize(root);
        var cleanRelative = Normalize(relativePath);
        return string.IsNullOrWhiteSpace(cleanRoot)
            ? cleanRelative
            : $"{cleanRoot}/{cleanRelative}";
    }

    public static string Normalize(string? path) =>
        (path ?? string.Empty)
            .Trim()
            .Replace('\\', '/')
            .Trim('/');
}

public sealed class LivingEmblemPackageLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<LivingEmblemPackageImportResult> LoadAsync(
        string packageRootPath,
        CancellationToken cancellationToken = default)
    {
        var result = new LivingEmblemPackageImportResult();
        var root = LivingEmblemPackagePaths.Normalize(packageRootPath);

        if (string.IsNullOrWhiteSpace(root))
        {
            result.Diagnostics.Add(Error("PackagePathMissing", "Package path is required."));
            return result;
        }

        if (root.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
            root.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
        {
            result.Diagnostics.Add(Error(
                "PackageManifestMissing",
                "Direct GLB paths are not production packages. Provide a folder containing manifest.json."));
            return result;
        }

        var manifestPath = LivingEmblemPackagePaths.Combine(root, LivingEmblemPackagePaths.ManifestFileName);
        var manifest = await ReadJsonAsync<LivingEmblemPackageManifest>(manifestPath, result, "Manifest", cancellationToken);
        if (manifest == null)
            return result;

        ValidateManifest(manifest, result);

        var glbPath = LivingEmblemPackagePaths.Combine(root, manifest.GLB);
        var thumbnailPath = LivingEmblemPackagePaths.Combine(root, manifest.Thumbnail);
        var fallbackPath = LivingEmblemPackagePaths.Combine(root, manifest.Fallback);
        var behaviorPath = LivingEmblemPackagePaths.Combine(root, manifest.Behavior);
        var metadataPath = LivingEmblemPackagePaths.Combine(root, manifest.Metadata);

        await RequireAssetAsync(glbPath, "GLB", result, cancellationToken);
        await RequireAssetAsync(thumbnailPath, "Thumbnail", result, cancellationToken);
        await RequireAssetAsync(fallbackPath, "Fallback", result, cancellationToken);

        var behavior = await ReadJsonAsync<LivingEmblemBehaviorProfile>(behaviorPath, result, "Behavior", cancellationToken)
            ?? new LivingEmblemBehaviorProfile();
        var metadata = await ReadJsonAsync<LivingEmblemMetadata>(metadataPath, result, "Metadata", cancellationToken)
            ?? new LivingEmblemMetadata();

        if (result.IsValid)
        {
            result.Package = new LivingEmblemPackage
            {
                PackageRootPath = root,
                ManifestPath = manifestPath,
                Manifest = manifest,
                Metadata = metadata,
                Behavior = behavior,
                ResolvedGlbPath = glbPath,
                ResolvedThumbnailPath = thumbnailPath,
                ResolvedFallbackPath = fallbackPath,
                ResolvedBehaviorPath = behaviorPath,
                ResolvedMetadataPath = metadataPath
            };

            result.Diagnostics.Add(Info("PackageValid", $"Package '{manifest.PackageId}' is valid."));
        }

        return result;
    }

    private static async Task<T?> ReadJsonAsync<T>(
        string assetPath,
        LivingEmblemPackageImportResult result,
        string label,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            cancellationToken.ThrowIfCancellationRequested();
            var model = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
            if (model == null)
                result.Diagnostics.Add(Error($"{label}Empty", $"{label} file '{assetPath}' is empty."));
            return model;
        }
        catch (FileNotFoundException)
        {
            result.Diagnostics.Add(Error($"{label}Missing", $"{label} file '{assetPath}' was not found."));
            return default;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            result.Diagnostics.Add(Error($"{label}Invalid", $"{label} file '{assetPath}' is invalid: {ex.Message}"));
            return default;
        }
    }

    private static async Task RequireAssetAsync(
        string assetPath,
        string label,
        LivingEmblemPackageImportResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = new byte[1];
            var read = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
            if (read <= 0)
                result.Diagnostics.Add(Error($"{label}Empty", $"{label} file '{assetPath}' is empty."));
        }
        catch (FileNotFoundException)
        {
            result.Diagnostics.Add(Error($"{label}Missing", $"{label} file '{assetPath}' was not found."));
        }
        catch (IOException ex)
        {
            result.Diagnostics.Add(Error($"{label}Unreadable", $"{label} file '{assetPath}' cannot be read: {ex.Message}"));
        }
    }

    private static void ValidateManifest(
        LivingEmblemPackageManifest manifest,
        LivingEmblemPackageImportResult result)
    {
        RequireText(manifest.SchemaVersion, "SchemaVersion", result);
        RequireText(manifest.PackageId, "PackageId", result);
        RequireText(manifest.AssetId, "AssetId", result);
        RequireText(manifest.DisplayName, "DisplayName", result);
        RequireText(manifest.Version, "Version", result);
        RequireText(manifest.Backend, "Backend", result);
        RequireText(manifest.GLB, "GLB", result);
        RequireText(manifest.Thumbnail, "Thumbnail", result);
        RequireText(manifest.Fallback, "Fallback", result);
        RequireText(manifest.Behavior, "Behavior", result);
        RequireText(manifest.Metadata, "Metadata", result);

        if (!string.Equals(manifest.SchemaVersion, "1.0", StringComparison.OrdinalIgnoreCase))
        {
            result.Diagnostics.Add(Error(
                "ManifestSchemaUnsupported",
                $"Manifest schema '{manifest.SchemaVersion}' is not supported. Expected '1.0'."));
        }

        if (!Enum.TryParse<LivingRendererBackend>(manifest.Backend, ignoreCase: true, out var backend) ||
            backend != LivingRendererBackend.Filament)
        {
            result.Diagnostics.Add(Error(
                "BackendUnsupported",
                $"Backend '{manifest.Backend}' is not supported by this production pipeline."));
        }

        if (!Enum.TryParse<LivingEmblemCameraPreset>(manifest.CameraPreset, ignoreCase: true, out _))
        {
            result.Diagnostics.Add(Error(
                "CameraPresetUnsupported",
                $"Camera preset '{manifest.CameraPreset}' is not supported."));
        }

        if (!Enum.TryParse<LivingEmblemLightingPreset>(manifest.LightingPreset, ignoreCase: true, out _))
        {
            result.Diagnostics.Add(Error(
                "LightingPresetUnsupported",
                $"Lighting preset '{manifest.LightingPreset}' is not supported."));
        }

        if (manifest.BoundingBoxOverride != null &&
            (manifest.BoundingBoxOverride.Center.Length != 3 ||
             manifest.BoundingBoxOverride.HalfExtent.Length != 3))
        {
            result.Diagnostics.Add(Error(
                "BoundingBoxOverrideInvalid",
                "BoundingBoxOverride requires Center and HalfExtent arrays with exactly three values."));
        }
    }

    private static void RequireText(
        string? value,
        string field,
        LivingEmblemPackageImportResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
            result.Diagnostics.Add(Error($"{field}Missing", $"Manifest field '{field}' is required."));
    }

    private static LivingEmblemPackageDiagnostic Info(string code, string message) =>
        new(LivingEmblemPackageDiagnosticSeverity.Info, code, message);

    private static LivingEmblemPackageDiagnostic Error(string code, string message) =>
        new(LivingEmblemPackageDiagnosticSeverity.Error, code, message);
}

public sealed class LivingEmblemImporter
{
    private readonly LivingEmblemPackageLoader _loader = new();

    public Task<LivingEmblemPackageImportResult> ImportAsync(
        string packageRootPath,
        CancellationToken cancellationToken = default) =>
        _loader.LoadAsync(packageRootPath, cancellationToken);
}

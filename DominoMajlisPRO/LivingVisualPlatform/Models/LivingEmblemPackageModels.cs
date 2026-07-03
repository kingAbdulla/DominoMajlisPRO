using System.Text.Json.Serialization;

namespace DominoMajlisPRO.LivingVisualPlatform.Models;

public sealed class LivingEmblemPackage
{
    public string PackageRootPath { get; init; } = string.Empty;
    public string ManifestPath { get; init; } = string.Empty;
    public LivingEmblemPackageManifest Manifest { get; init; } = new();
    public LivingEmblemMetadata Metadata { get; init; } = new();
    public LivingEmblemBehaviorProfile Behavior { get; init; } = new();
    public string ResolvedGlbPath { get; init; } = string.Empty;
    public string ResolvedThumbnailPath { get; init; } = string.Empty;
    public string ResolvedFallbackPath { get; init; } = string.Empty;
    public string ResolvedBehaviorPath { get; init; } = string.Empty;
    public string ResolvedMetadataPath { get; init; } = string.Empty;
}

public sealed class LivingEmblemPackageManifest
{
    public string SchemaVersion { get; set; } = "1.0";
    public string PackageId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Backend { get; set; } = LivingRendererBackend.Filament.ToString();
    public string GLB { get; set; } = "living_emblem.glb";
    public string Thumbnail { get; set; } = "thumbnail.png";
    public string Fallback { get; set; } = "fallback.png";
    public string Behavior { get; set; } = "behavior.json";
    public string Metadata { get; set; } = "metadata.json";
    public LivingEmblemLodManifest LOD { get; set; } = new();
    public List<string> SupportedPlatforms { get; set; } = new() { "Android" };
    public string MinimumDeviceTier { get; set; } = string.Empty;
    public LivingEmblemBoundingBoxOverride? BoundingBoxOverride { get; set; }
    public string CameraPreset { get; set; } = LivingEmblemCameraPreset.AutoBounds.ToString();
    public string LightingPreset { get; set; } = LivingEmblemLightingPreset.DeveloperPreview.ToString();
    public List<string> AnimationSet { get; set; } = new();
    public Dictionary<string, string> FutureExtensions { get; set; } = new();
}

public sealed class LivingEmblemLodManifest
{
    public string Strategy { get; set; } = "SingleAsset";
    public List<string> Levels { get; set; } = new();
}

public sealed class LivingEmblemBoundingBoxOverride
{
    public float[] Center { get; set; } = Array.Empty<float>();
    public float[] HalfExtent { get; set; } = Array.Empty<float>();
}

public sealed class LivingEmblemMetadata
{
    public string AuthoringTool { get; set; } = string.Empty;
    public string Exporter { get; set; } = string.Empty;
    public string SourceAsset { get; set; } = string.Empty;
    public string ArtStatus { get; set; } = string.Empty;
    public bool IsTemporaryArt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public sealed class LivingEmblemBehaviorProfile
{
    public string ProfileId { get; set; } = string.Empty;
    public string Idle { get; set; } = "None";
    public string Breathing { get; set; } = "None";
    public string Blink { get; set; } = "None";
    public string HeadMovement { get; set; } = "None";
    public string MaterialPulse { get; set; } = "None";
    public Dictionary<string, string> FutureAnimations { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter<LivingEmblemCameraPreset>))]
public enum LivingEmblemCameraPreset
{
    Hero,
    Shield,
    Creature,
    Flying,
    Portrait,
    AutoBounds
}

[JsonConverter(typeof(JsonStringEnumConverter<LivingEmblemLightingPreset>))]
public enum LivingEmblemLightingPreset
{
    GoldStudio,
    DarkMajlis,
    Silver,
    Fire,
    Royal,
    DeveloperPreview
}

public enum LivingEmblemPackageDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record LivingEmblemPackageDiagnostic(
    LivingEmblemPackageDiagnosticSeverity Severity,
    string Code,
    string Message);

public sealed class LivingEmblemPackageImportResult
{
    public bool IsValid => Diagnostics.All(item => item.Severity != LivingEmblemPackageDiagnosticSeverity.Error);
    public LivingEmblemPackage? Package { get; set; }
    public List<LivingEmblemPackageDiagnostic> Diagnostics { get; } = new();
}

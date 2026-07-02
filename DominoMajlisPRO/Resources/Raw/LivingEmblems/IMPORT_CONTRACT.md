# Living Emblem Production Package Contract

This folder is the package drop target for production Living Emblems. Runtime rendering is already owned by LivingVisualHost, Filament, and GLTFIO; adding a new Living Emblem must not require renderer changes.

## Package Flow

1. Create a package folder under `Resources/Raw/LivingEmblems/<package-id>`.
2. Include `manifest.json`, `living_emblem.glb`, `thumbnail.png`, `fallback.png`, `behavior.json`, and `metadata.json`.
3. Add optional future folders such as `textures/`, `materials/`, `animations/`, and `lod/` when the manifest references them.
4. Open the Living Emblem Package admin tool.
5. Import Package.
6. Validate.
7. Preview through LivingVisualHost.
8. Approve.
9. Publish.

## Manifest-Driven Fields

No production workflow should infer identity from creature names, image names, or hardcoded package paths. These values come from `manifest.json`:

- `packageId`
- `assetId`
- `displayName`
- `version`
- `backend`
- `glb`
- `thumbnail`
- `fallback`
- `behavior`
- `metadata`
- `supportedPlatforms`
- `minimumDeviceTier`
- `cameraPreset`
- `lightingPreset`
- `animationSet`
- `futureExtensions`

## Validation

The import validation gate checks the package folder and rejects direct `.glb` paths. A package is not publishable until the manifest and all required files are valid.

Validation must cover:

- Manifest schema version.
- Required manifest fields.
- `living_emblem.glb` exists and is readable.
- Thumbnail exists and is readable.
- Fallback exists and is readable.
- `behavior.json` parses into the behavior schema.
- `metadata.json` parses into the metadata schema.
- Backend compatibility.
- Platform compatibility.
- Renderer compatibility.

## Preview

Preview must render the GLB path resolved from the validated manifest:

`LivingVisualHost -> Filament -> GLTFIO -> living_emblem.glb`

Preview must not use `thumbnail.png` or `fallback.png` as the living visual. PNG assets are metadata/fallback surfaces only.

## Publish

Publishing stores the validated package metadata on the catalog record:

- PackageId
- AssetId
- Version
- Manifest path
- Backend
- Package path
- Thumbnail
- Fallback
- Behavior
- Metadata

## Current State

`production_default` is still a temporary import placeholder. It proves the package loader, validation surface, and Filament preview path are ready, but it is not artist-authored production art. The only missing step is replacing the package contents with the approved artist GLB package and publishing it through the admin tool.

# Living Asset Constitution v1.0

This constitution is the official production standard for every Living Emblem asset in Domino Majlis PRO.

It applies to artists, Blender/Maya authors, package builders, validators, developers, and future renderer integrations.

The goal is simple: adding a new Living Emblem must require a valid package, not renderer code changes.

---

## Article 1 - Authority

1. LivingVisualPlatform owns runtime rendering.
2. LivingVisualHost owns display integration.
3. Filament/GLTFIO owns Android 3D rendering.
4. The package manifest owns asset identity.
5. No renderer code may infer identity from names such as dragon, lion, eagle, wolf, shield, crown, phoenix, or future emblem names.
6. New Living Emblems must be data-driven through package files.

---

## Article 2 - Package Constitution

Every production Living Emblem package must live under:

`Resources/Raw/LivingEmblems/<package-id>/`

Required files:

- `manifest.json`
- `living_emblem.glb`
- `thumbnail.png`
- `fallback.png`
- `behavior.json`
- `metadata.json`

Optional folders are allowed only when referenced by the manifest:

- `textures/`
- `materials/`
- `animations/`
- `lod/`

The package folder name must be stable and lowercase kebab/snake style. Runtime identity must still come from `manifest.json`, not the folder name.

---

## Article 3 - Manifest Constitution

The manifest is the source of truth for package identity and runtime routing.

Required manifest fields:

- `schemaVersion`
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

Rules:

1. `backend` must match an approved renderer backend such as `Filament`.
2. `glb` must resolve to the package GLB file.
3. `thumbnail` and `fallback` are never the living render.
4. `version` must change when the package geometry, skeleton, behavior contract, or material contract changes.
5. The manifest must not contain hardcoded page names or UI layout instructions.

---

## Article 4 - Skeleton Constitution

Production creature assets should use stable node or bone names whenever applicable.

Canonical node names:

- `Root`
- `Body`
- `Neck`
- `Head`
- `JawLower`
- `EyeLeft`
- `EyeRight`
- `HornLeft`
- `HornRight`
- `Crown`
- `WingLeft`
- `WingRight`
- `Tail`

Rules:

1. Missing nodes must be declared in `metadata.json`.
2. Extra nodes are allowed, but behavior-critical nodes must map to canonical names.
3. Artist-specific names are not allowed as the only control names.
4. Runtime behavior must not depend on undocumented node names.
5. Non-creature emblems such as shields or crowns may omit creature-specific nodes but must declare their control map.

---

## Article 5 - Morph Constitution

Canonical morph target names:

- `BlinkLeft`
- `BlinkRight`
- `JawOpen`
- `Snarl`
- `BreatheExpand`
- `FirePrep`
- `Pulse`

Rules:

1. Morph targets are optional for the first static GLB import.
2. Autonomous living behavior requires declared morph support.
3. Missing morph targets must not crash runtime.
4. Behavior Brain must fail gracefully or choose a compatible behavior set.

---

## Article 6 - Material Constitution

Canonical material slots:

- `ObsidianStone`
- `GoldTrim`
- `EyeGlow`
- `MouthInner`
- `FireCore`
- `CrownGold`
- `WingMembrane`

Rules:

1. Materials must be compatible with glTF 2.0 PBR where possible.
2. Emissive/glow materials must be declared in metadata.
3. Transparency must be avoided unless explicitly required and tested.
4. Texture references must be package-local or embedded in the GLB.
5. Missing material channels must not cause renderer failure.

---

## Article 7 - Animation Constitution

Canonical animation clip names:

- `Idle`
- `Breathing`
- `Looking`
- `Special`
- `Cooldown`
- `Sleep`

Future optional clips:

- `Blink`
- `JawOpen`
- `Roar`
- `FirePrep`
- `WingFlex`

Rules:

1. Animation names must be stable.
2. Behavior Brain selects clips by manifest/behavior mapping.
3. Renderer adapters must not hardcode creature-specific animation names.
4. Packages with no animations are allowed only if metadata declares `animations=0` or equivalent.

---

## Article 8 - Camera Constitution

Approved camera presets:

- `AutoBounds`
- `Creature`
- `Flying`
- `Shield`
- `Hero`
- `Portrait`

Rules:

1. The manifest selects the camera preset.
2. The renderer may use bounding-box framing as the baseline.
3. No asset may require page layout changes to fit.
4. No asset may intentionally clip outside approved emblem slots.
5. Camera overrides must be declared, not hardcoded.

---

## Article 9 - Lighting Constitution

Approved lighting presets:

- `GoldStudio`
- `Royal`
- `DarkMajlis`
- `Fire`
- `Silver`
- `DeveloperPreview`

Rules:

1. The manifest selects the lighting preset.
2. Lighting must preserve Domino Majlis PRO black/gold premium identity unless the package explicitly declares a compatible visual theme.
3. Lighting must be safe for Android performance.
4. Renderer adapters may translate presets into backend-specific lights.

---

## Article 10 - Performance Constitution

Production assets must be mobile-first.

Initial recommended limits:

- Use optimized geometry suitable for small preview cards.
- Avoid excessive material counts.
- Avoid unnecessary 4K textures.
- Prefer embedded or package-local textures.
- LOD should be added for complex assets.
- Only visible/eligible living emblems should render live.
- Offscreen emblems must pause.

Rules:

1. Low-end devices may fall back to static fallback.
2. Performance failure must never block page rendering.
3. Package validation should warn about excessive file size and complexity.

---

## Article 11 - Validation Constitution

A package must be rejected if:

- Required files are missing.
- Manifest schema version is unsupported.
- GLB is missing or unreadable.
- Backend is unsupported.
- Platform is unsupported.
- `metadata.isTemporaryArt` is true for production publishing.
- Thumbnail/fallback paths are invalid.
- Behavior or metadata files fail to parse.

A package may be accepted with warnings if:

- Optional morph targets are missing.
- Optional animations are missing.
- Optional LOD files are missing.

---

## Article 12 - Behavior Constitution

Behavior Brain is renderer-independent.

It may issue neutral commands such as:

- Set node rotation.
- Set morph weight.
- Play animation clip.
- Set material pulse.
- Set level of detail.
- Pause.

Rules:

1. Behavior must map through package metadata and behavior profiles.
2. No behavior logic may directly import Filament, Unity, Godot, WebGL, or another backend.
3. Future emblems differ by Visual DNA and package metadata, not engine branches.

---

## Article 13 - Publishing Constitution

Publishing stores package metadata, not renderer state.

Published records must preserve:

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

Ownership remains:

`ApplicationUserId + PlayerId + AssetId`

`TeamId` is equip/display context only and must never become ownership.

---

## Article 14 - Preview Constitution

Developer preview is the production approval gate.

Preview must use:

`LivingVisualHost -> RendererFactory -> Filament -> GLTFIO -> package GLB`

Preview must not count any of the following as success:

- PNG fallback.
- Static thumbnail.
- Black rectangle.
- Fake 2D animation.
- Procedural placeholder art.

---

## Article 15 - Future Expansion

Adding a new Living Emblem must require only:

1. Create a valid package.
2. Import the package.
3. Validate.
4. Preview.
5. Approve.
6. Publish.

No renderer code changes are allowed unless:

- A new renderer backend is introduced.
- A new command type is introduced.
- A new manifest schema version is introduced.

---

## Current Ratified Status

The Living Visual engine, Filament integration, GLTFIO path, package loader, validation gate, and Developer preview are considered ready.

The first real artist-authored package is still pending.

This constitution becomes active before importing the first real `dragon_master` package.

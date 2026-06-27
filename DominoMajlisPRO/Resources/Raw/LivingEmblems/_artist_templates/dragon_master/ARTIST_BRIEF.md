# Living Dragon Emblem - Production Artist Package Brief

This folder is not a runtime package. It is the source brief for the first real artist-authored Living Dragon Emblem package.

The current renderer, package loader, validation gate, and Developer preview are already functional. Do not change engine code to make the dragon work. The final deliverable must be a valid package that can replace or be copied into a runtime folder such as:

`Resources/Raw/LivingEmblems/dragon_master/`

## Required Runtime Files

The final package must contain exactly these required files:

- `manifest.json`
- `living_emblem.glb`
- `thumbnail.png`
- `fallback.png`
- `behavior.json`
- `metadata.json`

Optional future folders may be added only when referenced by the manifest:

- `textures/`
- `materials/`
- `animations/`
- `lod/`

## Visual Identity Target

The dragon must match the approved Domino Majlis PRO dragon identity:

- Black stone / obsidian body material.
- Gold metallic accents.
- Crowned premium legendary appearance.
- Aggressive but controlled facial personality.
- Orange/gold living eye or core glow.
- Premium black/gold Majlis visual language.
- Must fit inside a team emblem preview card without clipping.

Do not use a flat PNG, billboard, card, or 2D plane as the living model.

## Required 3D Authoring Rules

Author in Blender, Maya, or equivalent professional DCC tool.

Before export:

- Apply transforms.
- Reset origin logically near model center.
- Use real mesh geometry, not generated placeholder primitives.
- Use clean normals.
- Remove hidden test geometry.
- Remove unused cameras/lights unless intentionally needed and declared.
- Keep model centered for AutoBounds camera framing.
- Keep the visual silhouette readable at small mobile preview size.

## Runtime Format

Export runtime as:

`living_emblem.glb`

glTF 2.0 binary GLB only.

FBX may be used internally by the artist, but the app runtime receives GLB.

## Suggested Skeleton / Node Names

Use stable names so behavior profiles can map to them later:

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

If the first production asset is not rigged yet, include the nodes that exist and declare missing nodes in `metadata.json`.

## Suggested Morph Targets

Preferred morph target names:

- `BlinkLeft`
- `BlinkRight`
- `JawOpen`
- `Snarl`
- `BreatheExpand`
- `FirePrep`

Morph targets are optional for the first import but required for later autonomous living behavior.

## Material Guidelines

Recommended material slots:

- `ObsidianStone`
- `GoldTrim`
- `EyeGlow`
- `MouthInner`
- `FireCore`
- `CrownGold`

Use PBR-compatible materials where possible. Avoid excessive transparency until the renderer policy explicitly supports it.

## Mobile Performance Budget

Initial target budget for Android preview:

- One GLB package per living emblem.
- Keep geometry optimized for a small emblem preview.
- Prefer compressed or reasonably sized textures.
- Avoid unnecessary 4K textures.
- The final package should be tested on emulator and a mid-range Android device.

## Acceptance Criteria

The package is acceptable only when:

1. The package validator passes.
2. `metadata.isTemporaryArt` is false.
3. Developer preview renders through `LivingVisualHost -> Filament -> GLTFIO`.
4. Preview shows the real dragon model, not PNG/fallback.
5. Runtime log shows the GLB is loaded from the package path.
6. No renderer code changes are required.
7. No hardcoded dragon-specific engine branches are added.

## Current Status

No final artist-authored dragon GLB is included here. This folder prepares the asset team and app pipeline for the first real package.

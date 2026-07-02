# Pending Living Asset Tasks

This file tracks approved but unfinished Living Asset work so it is not lost between sessions.

## 1. Living Asset Constitution v1.0

Status: Pending / Not implemented yet.

Purpose:
Create the official cross-team constitution for all future Living Emblem assets. This must become the stable reference for engineers, artists, Blender/Maya authors, validators, package importers, and future behavior logic.

Required chapters:

1. Package Constitution
   - Required folder structure.
   - Required package files.
   - Versioning policy.
   - Backward compatibility policy.

2. Skeleton Constitution
   - Official bone/node names.
   - Required naming rules.
   - Creature, winged, shield, crown, and non-creature variants.

3. Material Constitution
   - Official material slot names.
   - PBR requirements.
   - Glow/emissive policy.
   - Transparency restrictions.

4. Morph Constitution
   - Official morph target names.
   - Blink, jaw, breathing, snarl, pulse, and future expression policy.

5. Animation Constitution
   - Official animation clip names.
   - Idle, Breathing, Looking, Special, Cooldown, Sleep, and future action names.

6. Camera Constitution
   - Creature, Flying, Shield, Hero, Portrait, and AutoBounds presets.
   - Frame safety and no-clipping requirements.

7. Lighting Constitution
   - GoldStudio, Royal, DarkMajlis, Fire, Silver, DeveloperPreview.
   - Preset ownership and manifest control.

8. Performance Constitution
   - Polygon budgets.
   - Texture size budgets.
   - Material count limits.
   - LOD policy.
   - Android memory and FPS gates.

9. Validation Constitution
   - Rejection rules.
   - Temporary art policy.
   - Missing file policy.
   - Unsupported backend/platform policy.

10. Behavior Constitution
   - How Behavior Brain maps to bones, morph targets, animations, and material channels.
   - Renderer-neutral command policy.
   - No hardcoded creature-specific engine branches.

## 2. First Real Artist Package: Living Dragon Emblem

Status: Waiting for artist-authored GLB package.

Required runtime package target:

`Resources/Raw/LivingEmblems/dragon_master/`

Required files:

- `manifest.json`
- `living_emblem.glb`
- `thumbnail.png`
- `fallback.png`
- `behavior.json`
- `metadata.json`

Rules:

- Do not use code-generated placeholder geometry.
- Do not use PNG animation or fake 2D animation.
- Do not modify Filament, GLTFIO, LivingVisualHost, Package Loader, Store, Inventory, Equip, or Ownership to make the asset work.
- Replace/import the package and validate through the Developer Living Emblem preview page.

## 3. After Real Dragon Package Approval

Status: Future.

Tasks:

- Validate package.
- Preview through LivingVisualHost -> Filament -> GLTFIO.
- Publish as Emblem / LivingLegendaryEmblem / TeamEmblem.
- Verify acquisition.
- Verify inventory display.
- Verify CreateTeam emblem carousel display.
- Then extend to approved runtime locations only.

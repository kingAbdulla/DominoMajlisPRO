# Living Visual Deformation Investigation Report

## 1. Most likely cause

The shared visual distortion is most likely a scene/render/material composition issue, with GLB material setup also possible. The strongest evidence is that distortion was reported before the T-Man skeleton runtime and appears with both Dragon Master and T-Man. That makes a pure skeleton or touch-reaction cause unlikely as the common root.

## 2. Evidence from current code/logs

- `FilamentRenderSurfaceView` logs material diagnostics, transparent/blended hints, renderable counts, rig-node counts, and a note that duplicate geometry, inverted normals, double-sided material, transparent depth, and lighting/culling are primary suspects.
- `LivingVisualPulseOverlay` is only added when a manifest has `Fire` or `Smoke`; T-Man is explicitly gated away from this overlay in `FilamentLivingVisualRendererAdapter`.
- T-Man touch reaction now flows only through rotation clamps in `LivingProceduralSkeletonController`; no scale transforms are used.
- The renderer uses a transparent Android `SurfaceView` with MAUI overlay composition. SurfaceView/Z-order and transparent background can create composition artifacts on Android depending on device and driver.
- Both Dragon and T-Man share the Filament scene setup, light setup, Android surface composition, asset loading path, and material diagnostic code.

## 3. Classification

Current classification: scene/render/material issue first, GLB asset issue second, skeleton issue only for localized deformation during touch.

The common Dragon + T-Man distortion should be treated as shared render path or material/asset transparency behavior. T-Man touch deformation is separate and has been mitigated with reduced touch impulse, hard clamps, and per-role calibration.

## 4. Recommended fix plan

1. Capture device logs for material diagnostics: material names, blending mode, double-sided flags, transparent/alpha hints.
2. Test with `LivingPreviewFreezeBonesForArtifactTest=true`. If distortion remains, exclude procedural skeleton motion as the root.
3. Temporarily disable overlays for all assets in a local diagnostic branch. If distortion remains, exclude `LivingVisualPulseOverlay`.
4. Test opaque background and non-transparent SurfaceView settings in a diagnostic branch only.
5. Inspect Dragon and T-Man GLBs for duplicate meshes, transparent materials, double-sided materials, inverted normals, and alpha blend/depth-write settings.
6. If distortion tracks only specific GLB files, fix/export assets. If it appears across clean GLBs, focus on Filament/SurfaceView composition.

## 5. Files likely involved

- `Platforms/Android/FilamentRenderSurfaceView.cs`
- `LivingVisualPlatform/Rendering/FilamentLivingVisualRendererAdapter.cs`
- `LivingVisualPlatform/Rendering/LivingVisualPulseOverlay.cs`
- `LivingVisualPlatform/Rendering/FilamentLivingVisualView.cs`
- `LivingVisualPlatform/Services/StoreCatalogLivingVisualManifestProvider.cs`
- Dragon and T-Man GLB assets under `Resources/Raw/LivingEmblems`

## 6. What not to change

- Do not add animation clips or Animator playback.
- Do not change camera as part of this report mission.
- Do not alter Dragon Master behavior to fix T-Man.
- Do not disable skeleton skinning updates blindly.
- Do not change store/product/inventory flows.
- Do not remove the T-Man safety clamps unless on-device evidence proves they are too restrictive.

## 7. Shared root cause assessment

Dragon and T-Man likely share the same root cause only for the pre-existing visual distortion. Their shared path is Filament loading, material/render diagnostics, Android `SurfaceView` composition, lighting, and MAUI host layering.

T-Man touch deformation is not the same root cause. It is localized to procedural bone rotations and has been addressed separately by softer impulses, conservative hand/foot limits, per-role axis calibration, and hard rotation clamps.

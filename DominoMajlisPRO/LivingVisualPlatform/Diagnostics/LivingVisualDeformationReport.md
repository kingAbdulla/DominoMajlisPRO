# Living Visual Deformation Investigation Report

## 1. Current conclusion

The shared visual distortion is no longer classified as a Living Mind problem. The same class of artifact was reported before T-Man and appears across the Dragon/T-Man living-render path, so the common root is most likely in the Android/Filament render composition path or GLB material behavior.

The T-Man touch deformation is a separate local issue. It is caused by procedural rotations applied to skinned bones with uncertain Mixamo axes. This has been mitigated with lower touch impulse, hard clamps, conservative hand/foot limits, and stronger per-role calibration.

## 2. Evidence from current code/logs

- T-Man skeleton detection works and `Animator.UpdateBoneMatrices()` returns success, so the renderer is accepting skinning matrix updates.
- T-Man does not use animation clips, timelines, FBX/Mixamo animation playback, or baked GLB animation playback.
- `LivingVisualPulseOverlay` is gated by Fire/Smoke capabilities. T-Man does not advertise Fire or Smoke capabilities.
- T-Man reactions are rotation-only. No scale transforms are used by the procedural skeleton controller.
- The renderer uses Android `SurfaceView` composition with transparent/alpha-oriented settings. This remains a strong suspect for shared scene artifacts.
- Dragon and T-Man share Filament loading, scene setup, lighting, MAUI host layering, resource loading, and material rendering path.

## 3. Classification

Current classification:

1. Shared artifact: Android SurfaceView / Filament scene composition / material transparency path.
2. Possible asset-specific contributor: GLB material transparency, double-sided materials, duplicate mesh shells, inverted normals, or alpha depth behavior.
3. T-Man-only touch distortion: procedural bone-axis calibration and unsafe rotation magnitude, already reduced but still requires on-device axis validation.

## 4. Recommended fix plan

1. Add a diagnostic render mode that can run the same GLB with an opaque Android surface and no MAUI overlay layer.
2. Add a diagnostic render mode that disables all overlays for every living visual asset.
3. Add a diagnostic render mode that freezes skeleton motion while keeping Filament render active.
4. If distortion remains in opaque + frozen + no-overlay mode, inspect GLB materials/meshes externally.
5. If distortion disappears in opaque mode, replace or rework the transparent SurfaceView composition strategy.
6. If distortion disappears only when frozen, focus on TransformManager/skinning matrix application and per-bone local axis order.

## 5. Files likely involved

- `Platforms/Android/FilamentRenderSurfaceView.cs`
- `LivingVisualPlatform/Rendering/FilamentLivingVisualRendererAdapter.cs`
- `LivingVisualPlatform/Rendering/LivingVisualPulseOverlay.cs`
- `LivingVisualPlatform/Rendering/FilamentLivingVisualView.cs`
- `LivingVisualPlatform/Services/StoreCatalogLivingVisualManifestProvider.cs`
- Dragon and T-Man GLB assets under the LivingEmblems resource path

## 6. What not to change

- Do not add animation clips or Animator playback.
- Do not change game logic, ranking logic, Hall of Fame logic, store ownership, or inventory.
- Do not remove the Living Creature Runtime to hide the issue.
- Do not increase movement blindly before the render/skinning root cause is isolated.
- Do not treat the artifact as proof that the Living Mind constitution is wrong; the failure is lower in the render/skinning pipeline.

## 7. Shared root cause assessment

Dragon and T-Man likely share the same root cause for the persistent visual distortion: Filament scene composition, transparent SurfaceView behavior, renderable/material settings, or GLB material depth behavior.

T-Man touch deformation is not the same root cause. It is local to procedural bone response and must be solved with axis calibration, safe clamps, and potentially a different bone-matrix application path.

## 8. Required next R&D step

The next technical milestone should not be another behavior layer. It should be a render/skinning isolation pass in `FilamentRenderSurfaceView.cs`:

- mode A: opaque surface, no overlay
- mode B: transparent surface, no overlay
- mode C: opaque surface, frozen skeleton
- mode D: transparent surface, frozen skeleton

Only after this matrix is tested should the final render fix be applied.

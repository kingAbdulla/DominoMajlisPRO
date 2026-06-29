# Living Dragon Package Checklist

Use this checklist before copying the final artist package into `Resources/Raw/LivingEmblems/dragon_master/`.

## Required files

- [ ] `manifest.json`
- [ ] `living_emblem.glb`
- [ ] `thumbnail.png`
- [ ] `fallback.png`
- [ ] `behavior.json`
- [ ] `metadata.json`

## Art validation

- [ ] The GLB is artist-authored, not code-generated placeholder geometry.
- [ ] The model visually reads as the approved Domino Majlis PRO dragon.
- [ ] Materials use black stone, gold metal, and controlled warm glow.
- [ ] The model fits inside the emblem preview card.
- [ ] The model does not clip at the default camera preset.
- [ ] The silhouette is readable at small mobile size.

## Technical validation

- [ ] GLB opens in Blender or a glTF viewer.
- [ ] Transforms are applied.
- [ ] Hidden construction geometry is removed.
- [ ] Normals are correct.
- [ ] No missing textures.
- [ ] No unused temporary cameras/lights unless declared.
- [ ] Package validator passes.
- [ ] Developer preview renders through Filament.
- [ ] Preview is not PNG fallback.
- [ ] Runtime log shows the GLB loaded from the package folder.

## Publish gate

Publish only after the Developer preview card shows the final 3D dragon through Filament.

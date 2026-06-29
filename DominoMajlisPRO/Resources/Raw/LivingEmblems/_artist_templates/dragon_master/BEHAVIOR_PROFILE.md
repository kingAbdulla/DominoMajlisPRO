# Living Dragon Behavior Profile

This file describes the intended behavior profile for the first real Living Dragon Emblem package. The final runtime package should convert this into `behavior.json`.

## Personality

- Legendary
- Heavy and controlled
- Slow breathing
- Rare aggressive eye focus
- Rare jaw preparation movement
- Gold/fire material pulse

## Required states

- Idle
- Breathing
- Looking
- Cooldown
- Paused

## Future states

- Blink
- JawOpen
- FirePrep
- Roar

## Safety limits

- No fast spinning.
- No page layout movement.
- No camera jump.
- No animation that exits the emblem frame.
- No autoplay when the page is offscreen.

## Preferred runtime mapping

- Head rotation maps to `Head` node.
- Jaw motion maps to `JawLower` node or `JawOpen` morph.
- Eye blink maps to `BlinkLeft` and `BlinkRight` morphs.
- Breathing maps to `BreatheExpand` morph or subtle `Body` scale.
- Gold/fire pulse maps to material channels declared in the package metadata.

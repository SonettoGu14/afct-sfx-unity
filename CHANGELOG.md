# Changelog

## 0.2.0

Native Editor window.

- The window is now built with IMGUI and Unity's own controls instead of a custom UI Toolkit theme, so it follows the Pro and Light editor skins like the rest of Unity.
- Toolbar with preset popup, search, transport icons, and the sample-rate selector.
- Preset rail behaves like the Project window: category foldouts, one flat row per preset, a draggable splitter, and the selected row inverted to the editor selection color.
- Parameter column follows the Inspector idiom: collapsible groups with standard sliders, a drawn waveform, a readout row, and a status box.
- Export block with file name, trim, peak target, a clickable folder field, and the same importer settings as before.
- Space plays the current sound; R selects the random preset.

## 0.1.0

First version.

- 28 presets in five groups (UI, Action, Cinematic, Footsteps, Utility).
- Parameter synth ported from the web engine: sfxr core, eight footstep terrains, six-stage FX rack.
- Seeded preset generation so a sound can be reproduced.
- Editor window with live audition and WAV export into `Assets/Audio/SFX/`.
- Golden-fixture parity tests against the web engine.

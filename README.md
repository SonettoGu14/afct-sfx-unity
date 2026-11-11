# AFCT SFX Lab (Unity)

Generate retro and cinematic game sound effects inside the Unity Editor. Pick a preset, shape it
with the parameter synth and the FX rack, audition it, and write a 16-bit WAV straight into your
project with sensible import settings already applied.

This is the Unity counterpart of [AFCT SFX Lab for the web](https://sonettogu14.github.io/afct-sfx-lab/).
Both run the same engine: the sfxr parameter synth, eight footstep terrain models, and a six-stage
FX rack. Golden fixtures generated from the web engine keep the two implementations in sync.

## Install

Package Manager ▸ Add package from git URL:

```
https://github.com/SonettoGu14/afct-sfx-unity.git
```

Or, for local development, add a relative path to `Packages/manifest.json`:

```json
"com.afct.sfxlab": "file:../../GitHubProjects/afct-sfx-unity"
```

Minimum Unity 2022.3. Opens under **Tools ▸ AFCT SFX Lab**.

## What it does

- 28 presets in five groups: UI, Action, Cinematic, Footsteps, Utility.
- Full parameter control: envelope, frequency, modulation, texture, filter, and a six-stage FX rack
  (drive, bitcrush, tone tilt, echo, space, peak ceiling).
- Seeded variation, so a sound can be reproduced exactly.
- Live audition in the Editor without entering Play mode.
- Export to `Assets/Audio/SFX/<category>/` as 16-bit PCM WAV, with the AudioImporter configured for
  game use (mono, preload, PCM for short clips, ADPCM for longer ones).

## Sound engine notes

The sfxr core is the classic parameter synthesizer from DrPetter's sfxr (2007). Footsteps use a
small pure-data-style graph per terrain. The FX rack is additive and bypassable; peak normalization
is a ceiling, not loudness normalization, so the master volume still means something.

## Tests

EditMode tests compare the C# engine against golden fixtures rendered by the web engine:

```
Tools/verify.sh          # dotnet, no Unity required
```

Inside Unity, the same tests run in the Test Runner window under EditMode.

## License

GPL-3.0-only. The engine derives from [game-sfx-generator-web](https://github.com/mowangblog/game-sfx-generator-web)
by mowangblog, which is GPL-3.0, and from DrPetter's sfxr (MIT). A distributed plugin that links
this engine must therefore also be GPL-3.0 unless the licensing is revisited.

Not an official XJTLU product. AFCT stands for the Academy of Film and Creative Technology.

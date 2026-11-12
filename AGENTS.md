# AGENTS.md — AFCT SFX Lab for Unity

Guidance for automation and maintenance work in this repository. This is a **port**, not an
independent engine: the web repository owns the DSP semantics. Read the cross-project section
before changing anything under `Runtime/`.

## What this is

A Unity editor tool for generating retro and cinematic game sound effects: 28 presets, a
parameter synth, an eight-terrain footstep engine and a six-stage FX rack, auditioned in the
Editor and exported as 16-bit WAV with import settings already applied.

It is one half of a two-project family:

| | This repository | Sibling (source of truth) |
| --- | --- | --- |
| Project | AFCT SFX Lab for Unity | AFCT SFX Lab (web) |
| Local path | `~/GitHubProjects/afct-sfx-unity` | `~/GitHubProjects/game-sfx-generator-web` |
| Remote | https://github.com/SonettoGu14/afct-sfx-unity | https://github.com/SonettoGu14/afct-sfx-lab |
| Artifact | UPM package `com.afct.sfxlab`, minimum Unity 2022.3 | https://sonettogu14.github.io/afct-sfx-lab/ |
| Stack | C# (netstandard2.1 runtime) + Unity Editor IMGUI | React + TypeScript + Vite |

The two projects release independently, but they must never drift: the C# port is validated
sample-for-sample against fixtures rendered by the web engine.

## Current branch state

- `main`: v0.1.0, the UI Toolkit window (the version users have).
- `native-inspector-ui`: v0.2.0, the IMGUI rewrite of the window (commit `d457c8d`). Implemented,
  verified, pushed, **not merged**. Merge only after a visual pass in a real Unity Editor.

Check `git status` and `git log --oneline -5` before starting; do not assume `main` is the newest
UI.

## Layout

```
Runtime/Core/        engine, no UnityEngine dependencies
  SfxEnums.cs        SfxEngine, SfxWaveform, SfxFootstepTerrain, SfxPresetId (contract order)
  SfxParams.cs       37 double fields with the exact TypeScript names + enum fields
  SfxClamp.cs        clamping rules, including non-finite values mapping to the field minimum
  SfxJson.cs         serialize / deserialize with the same field order as the web JSON
  SfxPresets.cs      28 recipes, PRESET_FX overlays, random overlay, mutate
  SfxPresetInfo.cs   display name / category metadata
  SfxLabEngine.cs    Render / Clamp / Serialize / Deserialize
  SeededRandom.cs    mulberry32, the same sequence as src/lib/prng.ts
  Mastering.cs       trim silence, peak targeting
  WavEncoder.cs      16-bit RIFF writer
  SfxRenderTypes.cs, SfxMath.cs
Runtime/Synth/
  SfxrRenderer.cs    the sfxr parameter loop
  FootstepRenderer.cs 8 terrains (snow, grass, dirt, gravel, mud, wood, metal, water)
  PdSignals.cs       float32-quantized PD primitives used by the footstep graphs
  FxChain.cs         drive -> bitcrush -> tone -> delay -> reverb -> ceiling -> fade
Editor/
  SfxLabMenu.cs      Tools/AFCT SFX Lab
  AudioPreview.cs    disposable audition host (HideAndDontSave, mono, 2D)
  WavExport.cs       writes Assets/Audio/SFX/<category>/ and configures the AudioImporter
  UI/SfxLabWindow.cs the window (internal)
  UI/SfxLabSkin.cs   cached GUIStyles, skin-switched colours
Tests/EditMode/      NUnit tests plus Golden/ fixtures
Tools/Verify/        dotnet harness: runs the same tests without Unity
Tools/CompileCheck/  compiles Runtime + Editor against real Unity assemblies
Docs/CONTRACT.md     the binding API and numeric contract
```

## Quick start

Install in a project via Package Manager (Add package from git URL):

```
https://github.com/SonettoGu14/afct-sfx-unity.git
```

or, for local development, in `Packages/manifest.json` (paths are relative to the `Packages/`
folder, not the project root):

```json
"com.afct.sfxlab": "file:../../../GitHubProjects/afct-sfx-unity"
```

Open the window with **Tools ▸ AFCT SFX Lab**.

Checks that do not need Unity:

```bash
bash Tools/verify.sh                     # 18/18 must pass
dotnet build Tools/CompileCheck -v q     # 0 warnings, 0 errors, against Unity 2022.3 DLLs
bash Tools/generate-golden.sh            # regenerate fixtures from the web repo (see below)
```

`dotnet` 10 is available at `/opt/homebrew/bin/dotnet` in this workspace. Unity installs present:
2022.3.62f3, 2022.3.62f3c1, 6000.0.74f1, 6000.3.20f1.

## Contract invariants

`Docs/CONTRACT.md` is the binding document. Summary of what must not drift:

- **Public API names are frozen**: `SfxPresets.Create/CreateVariant/Order/DisplayName/Category/All/Info`,
  `SfxLabEngine.Render/Clamp/Serialize/Deserialize`, `WavEncoder.Encode`, `Mastering.*`,
  `SeededRandom.*`. Adding is fine; renaming breaks the contract.
- **28 presets in contract order**, five categories. `SfxPresets.Order` drives the rail, the
  export folder and the fixtures.
- **Numeric rules**: DSP in `double`, samples as `float`, mulberry32 for every random draw, audio
  parity tolerance 1e-4 (currently measured at 0, i.e. bit-exact), preset parity 1e-9.
- **FX chain order and tail maths are fixed**: drive → crush → tone → delay → reverb → ceiling →
  fade; tails derive from the wet mix and are capped at 6 seconds.
- **Peak normalize is a ceiling (0.995)**, never loudness normalization.
- **The runtime must not reference UnityEngine or UnityEditor.** `Runtime/` compiles standalone as
  netstandard2.1 / C# 8 with warnings as errors; keep it that way.
- **Never hand-edit `Tests/EditMode/Golden/**`.** Regenerate it from the web engine.

## Editor UI rules (v0.2 branch)

- IMGUI only: `OnGUI`, `EditorGUILayout`, `EditorStyles`, `GUI.skin`, `EditorGUIUtility` icons.
  Do not reintroduce UI Toolkit (`VisualElement`, UXML, USS) for this window.
- No custom fonts, no hardcoded light/dark colours; anything not covered by `EditorStyles` goes
  through `EditorGUIUtility.isProSkin` branches in `Editor/UI/SfxLabSkin.cs`.
- Do not assign `EditorGUIUtility.labelWidth`; let the pane follow the user's Inspector setting.
- No `EditorApplication.update` polling. Use `OnInspectorUpdate` for preview state and repaint.
- Window class stays `internal`; automation drives it by reflection.
- `AudioPreview` and `WavExport` are UI-agnostic: reuse them instead of re-implementing playback
  or asset import. Export settings are PCM under 1 s, ADPCM above, mono, preload, preserve sample
  rate, decompress on load.
- Keep the tool usable at the minimum window size (760×560 since v0.2); check the Inspector column
  actually stretches, since the v0.1 layout bug was a non-expanding scroll view.

## Testing gates (definition of done)

1. `bash Tools/verify.sh` → 18/18, exit 0 (84 preset fixtures, 12 audio fixtures, behaviour tests).
2. `dotnet build Tools/CompileCheck -v q` → 0 warnings, 0 errors.
3. Unity itself compiles the package with no `error CS` (open the project once, or run the headless
   smoke).
4. Any UI change: headless smoke (`-batchmode -executeMethod SfxLabSmoke.Run` in a throwaway
   project) plus a real GUI look. The smoke script lives in the throwaway project under
   `/tmp/sfxlab-unity-check/Assets/Editor/SfxLabSmoke.cs`, not in this repository; reproduce it
   from the assertions in the commit history if it is gone.
5. Audio changes: an audible listen. Headless runs prove the clip has data and the importer
   settings round-trip, not that it sounds right.

## Regenerating the parity fixtures

```bash
bash Tools/generate-golden.sh              # uses WEB_REPO, defaults to the sibling checkout
WEB_REPO=/path/to/game-sfx-generator-web bash Tools/generate-golden.sh
```

The script runs `npx vitest run tools/generate-golden.test.ts` in the web repository, which renders
84 preset cases (28 presets × seeds 1234, 42, 987654321) and 12 audio cases at seed 1234 and writes
`Tests/EditMode/Golden/PresetFixtures.g.cs`, `Fixtures.g.cs` and `*.f32` here. It needs node and the
web repo's dependencies installed.

## Packaging traps

- **Nothing may be imported from build tooling.** Unity imports every file under a local package,
  and stray `bin/`/`obj/` DLLs (especially copies of `UnityEditor.dll`) inject duplicate assembly
  references into the consumer project, producing `CS0433` errors in unrelated packages such as
  uGUI. All dotnet output is redirected to dot-prefixed folders Unity ignores
  (`Tools/Verify/.build/`, `<repo>/.artifacts/`), and both are git-ignored. Keep it that way.
- `Tools/Verify/AFCT.SfxLab.Verify.asmdef` has an unmet `defineConstraints`
  (`AFCT_SFXLAB_VERIFY_HARNESS`) so Unity never compiles the NUnit shim or console `Main`.
- Every asset needs a `.meta`; do not delete orphan metas for folders that still exist, and do not
  commit `bin.meta`/`obj.meta` for folders that do not.
- Unity allows one instance per project. A GUI editor holding the project lock makes batchmode
  runs fail with "another Unity instance is running with this project open"; copy the project or
  close the editor.

## Known traps

- A local-package install in a real project can be poisoned by earlier bad imports: if uGUI shows
  `CS0433` for `UnityEditor`/`PrefabUtility`, close Unity, delete `Library/ScriptAssemblies` and
  `Library/Bee`, reopen.
- `dotnet` builds need the `Microsoft.NETCore.App.Ref 8.0.33` targeting pack; it is in the local
  NuGet cache. There are no package references, so builds are otherwise offline.
- Screenshots of the editor window require macOS Screen Recording permission for the agent
  process; without it `screencapture` fails with "could not create image from window".
  `Docs/screenshot-native.png` is still missing for this reason.

## Cross-project protocol

The web repository is the reference implementation. Its own handoff notes live at
`~/GitHubProjects/game-sfx-generator-web/AGENTS.md` (and
https://github.com/SonettoGu14/afct-sfx-lab/blob/main/AGENTS.md); read them before touching
engine semantics, preset recipes or fixture generation.

1. Engine or preset changes start in `~/GitHubProjects/game-sfx-generator-web/src/lib/**` with
   tests.
2. Regenerate fixtures here (`Tools/generate-golden.sh`).
3. Port the change to `Runtime/**`, update `Docs/CONTRACT.md` if the public API or numeric rules
   changed, and run the gates above.
4. If the port exposes a bug in the web engine, fix it there and regenerate; never patch C# to
   match a known-wrong reference.
5. Release both sides consistently; the web app version and package version are independent numbers
   (web 1.2.0, package 0.2.0 on the UI branch), but a behaviour change lands in both.

Licensing: GPL-3.0-only (the synth derives from sfxr, MIT). Any distributed derivative, including
a Unity plugin or a paid asset, must stay GPL-3.0.

## Open work

- Merge `native-inspector-ui` after a visual review; capture a screenshot for the README once
  Screen Recording permission (or a manual capture) is available.
- Candidate v0.3 work: seed field in the window, authored preset descriptions, multi-variant
  export, batch export, waveform zoom, optional per-project export defaults.

# AFCT SFX Lab — Unity port contract (v0.1)

The web app in `/Users/gyk/GitHubProjects/game-sfx-generator-web` is the source of truth for
sound. This package re-implements the same engine in C# so the Editor can generate, preview and
export sounds without a browser.

Authoritative sources (read them, do not guess):

| Concern | File |
| --- | --- |
| Params, clamping, presets, sfxr loop, footsteps, WAV | `src/lib/sfx.ts` |
| FX rack (drive, crush, tone, delay, reverb, ceiling, fade) | `src/lib/effects.ts` |
| Trim / peak targeting | `src/lib/mastering.ts` |
| Seeded RNG (mulberry32) | `src/lib/prng.ts` |

## Assembly layout

```
Runtime/            AFCT.SfxLab.Runtime     (namespace AFCT.SfxLab)   no Editor APIs
  Core/             params, enums, rng, wav, mastering, presets
  Synth/            sfxr renderer, footstep renderer, fx chain
Editor/             AFCT.SfxLab.EditorTools (namespace AFCT.SfxLab.EditorTools)  Editor only
Tests/EditMode/     AFCT.SfxLab.Tests       EditMode NUnit tests + Golden fixtures
```

## Public API (must exist with these names)

```csharp
namespace AFCT.SfxLab
{
    public enum SfxEngine { Sfxr, Footsteppr }
    public enum SfxWaveform { Square, Sawtooth, Sine, Noise }
    public enum SfxFootstepTerrain { Snow, Grass, Dirt, Gravel, Mud, Wood, Metal, Water }
    public enum SfxPresetId { Random, Mutate, Click, BlipSelect, HoverTick, NotifyPop,
        SuccessChime, ErrorBuzz, PickupCoin, LaserShoot, Explosion, Powerup, HitHurt, Jump,
        Whoosh, Riser, Impact, Glitch, Synth, Tone, FootstepSnow, FootstepGrass, FootstepDirt,
        FootstepGravel, FootstepMud, FootstepWood, FootstepMetal, FootstepWater }

    public sealed class SfxParams { /* all fields from the TS SfxParams, as double,
        plus engine/waveform/terrain enums; public fields, copy constructor, Clone() */ }

    public sealed class SfxRenderOptions
    {
        public int SampleRate = 44100;
        public int BitDepth = 16;
        /// When set, render-time noise (and only noise) is seeded so the render is reproducible.
        public int? Seed;
    }
    public sealed class SfxRenderStats  { public double DurationSeconds; public long Samples;
        public long ClippedSamples; public double Peak; public long EstimatedByteSize; }
    public sealed class SfxRenderResult { public float[] Samples; public SfxRenderStats Stats; }

    public static class SfxPresets        // SFX_PRESET_ORDER, labels/categories metadata
    {
        public static readonly SfxPresetId[] Order;
        public static SfxParams Create(SfxPresetId preset, SfxParams current = null, int? seed = null);
        public static SfxParams CreateVariant(SfxParams current, int? preset, int seed);
    }

    public static class SfxLabEngine
    {
        public static SfxRenderResult Render(SfxParams p, SfxRenderOptions o);
        public static SfxParams Clamp(SfxParams p);          // == clampSfxParams
        public static string Serialize(SfxParams p);         // JSON of the params
        public static SfxParams Deserialize(string json);    // tolerant, clamped
    }

    public static class WavEncoder
    {
        public static byte[] Encode(float[] samples, int sampleRate, int bitDepth);
    }

    public static class Mastering
    {
        public static double MeasurePeak(float[] s);
        public static double MeasurePeakDb(float[] s);
        public static float[] TrimSilence(float[] s, int sampleRate, double thresholdDb = -60,
            double preRollMs = 5, double tailMs = 20);
        public static float[] ApplyTargetPeak(float[] s, double targetDb);
        public static float[] MasterForExport(float[] s, int sampleRate, bool trim,
            double targetPeakDb /* double.NegativeInfinity = off */);
    }

    public static class SeededRandom      // mulberry32, identical to src/lib/prng.ts
    {
        public static double[] Sequence(uint seed, int count); // for tests
    }
}
```

`SfxLabEngine.Render` must match `renderSfx` exactly in behaviour: clamp input, dispatch on
`engine`, pad the tail (`fxTailSeconds`), cap at 6 s, run the FX chain, count clipped samples
against the pre-clamp signal, clamp to ±1, and report duration/samples/peak/estimatedByteSize.

## Numeric rules

- Do all DSP in `double`. Samples are stored as `float` (matching JS `Float32Array`).
- `Math.Sin`, `Math.Tan`, `Math.Pow` in .NET differ from V8 by ~1e-16 relative; that is fine.
  The fixture test tolerance is **1e-4 absolute** on samples.
- RNG: mulberry32 with 32-bit wrapping arithmetic, exactly as in `src/lib/prng.ts`
  (`Math.imul` → `unchecked(t * 0x6D2B79F5U)` on `uint`, and the same 3-line mixing).
- Preset params must match the web engine to **1e-9 relative** for the same seed.

## Fixtures

Generated from the web engine into `Tests/EditMode/Golden/`:

```
Golden/<caseId>.f32      raw little-endian float32 samples
Golden/Fixtures.g.cs     C# source: static readonly Fixture[] All
```

`Fixture` (defined in Tests, namespace `AFCT.SfxLab.Tests`) exposes `Id`, `SampleRate`,
`BitDepth`, `Seed`, `SfxParams Params`.

**Render-time noise is seeded.** `renderSfx` in the web engine now runs inside `withRandomSource`
when `options.seed` is set, so the noise waveform and the footstep PD noise are reproducible. The
C# renderer must do the same: when `SfxRenderOptions.Seed` has a value, every render-time random
draw comes from `mulberry32(seed)`; when it is null, draw from an unseeded generator.

Two test groups:
1. **Preset parity** — for every preset and seed in `Golden/PresetFixtures.g.cs`, compare every
   numeric param to the expected value (1e-9 relative).
2. **Audio parity** — for every `.f32`, render with the same params and compare lengths and
   per-sample absolute difference ≤ 1e-4.

Regenerate fixtures with `Tools/generate-golden.sh` (drives the web repo). Never hand-edit them.

## Verification (no Unity needed)

```
Tools/verify.sh           # compiles Runtime/ + Tests/ with dotnet, runs the same NUnit tests
```

This is the gate before opening Unity. Inside Unity, run the same tests from Test Runner
(EditMode).

## Unity-side rules

- Editor window lives in `Editor/UI/SfxLabWindow.cs` with `SfxLabWindow.uxml` + `SfxLabWindow.uss`.
- Menu item: `Tools/AFCT SFX Lab`.
- Export writes 16-bit PCM WAV to `Assets/Audio/SFX/<category>/`, then configures the
  `AudioImporter` (forceToMono when mono, loadInBackground false, preloadAudioData true,
  compressionFormat PCM for clips < 1 s else ADPCM).
- No `EditorApplication.update` polling loops; no network access; no `System.Linq` in hot paths.
- No em-dashes in UI copy; English UI, sentence case, no exclamation marks.

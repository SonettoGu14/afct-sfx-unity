using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// The 28 preset recipes, ported literally from <c>createSfxPresetCore</c> and
    /// <c>PRESET_FX</c> in <c>src/lib/sfx.ts</c>.
    /// </summary>
    public static class SfxPresets
    {
        /// <summary>Canonical preset order, matching <c>SFX_PRESET_ORDER</c>.</summary>
        public static readonly SfxPresetId[] Order =
        {
            SfxPresetId.Random,
            SfxPresetId.Mutate,
            SfxPresetId.Click,
            SfxPresetId.BlipSelect,
            SfxPresetId.HoverTick,
            SfxPresetId.NotifyPop,
            SfxPresetId.SuccessChime,
            SfxPresetId.ErrorBuzz,
            SfxPresetId.PickupCoin,
            SfxPresetId.LaserShoot,
            SfxPresetId.Explosion,
            SfxPresetId.Powerup,
            SfxPresetId.HitHurt,
            SfxPresetId.Jump,
            SfxPresetId.Whoosh,
            SfxPresetId.Riser,
            SfxPresetId.Impact,
            SfxPresetId.Glitch,
            SfxPresetId.Synth,
            SfxPresetId.Tone,
            SfxPresetId.FootstepSnow,
            SfxPresetId.FootstepGrass,
            SfxPresetId.FootstepDirt,
            SfxPresetId.FootstepGravel,
            SfxPresetId.FootstepMud,
            SfxPresetId.FootstepWood,
            SfxPresetId.FootstepMetal,
            SfxPresetId.FootstepWater,
        };

        /// <summary>Display metadata for every preset, in <see cref="Order"/>.</summary>
        public static readonly SfxPresetInfo[] All = BuildAll();

        /// <summary>Display metadata for one preset.</summary>
        public static SfxPresetInfo Info(SfxPresetId preset)
        {
            return new SfxPresetInfo(preset, DisplayName(preset), Category(preset));
        }

        private static SfxPresetInfo[] BuildAll()
        {
            var infos = new SfxPresetInfo[Order.Length];
            for (int index = 0; index < Order.Length; index += 1)
            {
                infos[index] = Info(Order[index]);
            }

            return infos;
        }

        /// <summary>Human readable preset name.</summary>
        public static string DisplayName(SfxPresetId preset)
        {
            if (preset == SfxPresetId.Powerup)
            {
                return "Power Up";
            }

            string name = preset.ToString();
            var builder = new System.Text.StringBuilder(name.Length + 4);
            for (int index = 0; index < name.Length; index += 1)
            {
                char current = name[index];
                if (index > 0 && char.IsUpper(current) && char.IsLower(name[index - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        /// <summary>Preset group used by the browser UI: UI, Action, Cinematic, Footsteps or Utility.</summary>
        public static string Category(SfxPresetId preset)
        {
            switch (preset)
            {
                case SfxPresetId.Click:
                case SfxPresetId.BlipSelect:
                case SfxPresetId.HoverTick:
                case SfxPresetId.NotifyPop:
                case SfxPresetId.SuccessChime:
                case SfxPresetId.ErrorBuzz:
                    return "UI";
                case SfxPresetId.PickupCoin:
                case SfxPresetId.LaserShoot:
                case SfxPresetId.Explosion:
                case SfxPresetId.Powerup:
                case SfxPresetId.HitHurt:
                case SfxPresetId.Jump:
                    return "Action";
                case SfxPresetId.Whoosh:
                case SfxPresetId.Riser:
                case SfxPresetId.Impact:
                case SfxPresetId.Glitch:
                case SfxPresetId.Synth:
                case SfxPresetId.Tone:
                    return "Cinematic";
                case SfxPresetId.FootstepSnow:
                case SfxPresetId.FootstepGrass:
                case SfxPresetId.FootstepDirt:
                case SfxPresetId.FootstepGravel:
                case SfxPresetId.FootstepMud:
                case SfxPresetId.FootstepWood:
                case SfxPresetId.FootstepMetal:
                case SfxPresetId.FootstepWater:
                    return "Footsteps";
                default:
                    return "Utility";
            }
        }

        /// <summary>
        /// Builds a fresh take of <paramref name="preset"/>. Passing a seed makes the
        /// result reproducible; <paramref name="current"/> is only used by the mutate preset.
        /// </summary>
        public static SfxParams Create(SfxPresetId preset, SfxParams current = null, int? seed = null)
        {
            SfxParams startingParams = current ?? new SfxParams();
            Mulberry32 random = SfxRandom.ForSeed(seed);
            SfxParams baseParams = CreateCore(preset, startingParams, random);

            if (preset == SfxPresetId.Random)
            {
                return SfxClamp.Apply(ApplyRandomFxOverlay(baseParams, random));
            }

            if (HasPresetFx(preset))
            {
                ApplyPresetFx(preset, baseParams);
                return SfxClamp.Apply(baseParams);
            }

            return baseParams;
        }

        /// <summary>
        /// Re-rolls an existing sound. Uses the preset recipe when the take still knows
        /// its preset, otherwise nudges the current parameters.
        /// </summary>
        public static SfxParams CreateVariant(SfxParams current, int? preset, int seed)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            bool hasRecipe = preset.HasValue && preset.Value != (int)SfxPresetId.Mutate;
            return hasRecipe
                ? Create((SfxPresetId)preset.Value, current, seed)
                : MutateWithActiveRandom(current, SfxRandom.ForSeed(seed));
        }

        private static SfxParams CreateCore(SfxPresetId preset, SfxParams current, Mulberry32 random)
        {
            var p = new SfxParams();

            switch (preset)
            {
                case SfxPresetId.Random:
                    p.waveform = RandomWaveform(random);
                    p.attack = random.NextUnit() * 0.12;
                    p.sustain = random.NextRange(0.05, 0.44);
                    p.sustainPunch = random.NextUnit();
                    p.decay = random.NextRange(0.08, 0.52);
                    p.startFrequency = random.NextRange(0.12, 0.95);
                    p.minFrequency = random.NextUnit() * 0.3;
                    p.slide = random.NextRange(-0.65, 0.42);
                    p.deltaSlide = random.NextRange(-0.22, 0.22);
                    p.vibratoDepth = random.NextUnit() * 0.7;
                    p.vibratoSpeed = random.NextUnit() * 0.8;
                    p.changeAmount = random.NextRange(-0.7, 0.7);
                    p.changeSpeed = random.NextUnit() * 0.9;
                    p.squareDuty = random.NextUnit();
                    p.dutySweep = random.NextRange(-0.4, 0.4);
                    p.repeatSpeed = random.NextUnit() * 0.7;
                    p.phaserOffset = random.NextRange(-0.8, 0.8);
                    p.phaserSweep = random.NextRange(-0.3, 0.3);
                    p.lowPassCutoff = random.NextRange(0.2, 1.0);
                    p.lowPassSweep = random.NextRange(-0.25, 0.18);
                    p.lowPassResonance = random.NextUnit() * 0.7;
                    p.highPassCutoff = random.NextUnit() * 0.25;
                    p.highPassSweep = random.NextRange(-0.12, 0.1);
                    p.masterVolume = random.NextRange(0.38, 0.72);
                    return SfxClamp.Apply(p);

                case SfxPresetId.PickupCoin:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Square : SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.04, 0.12);
                    p.sustainPunch = random.NextRange(0.35, 0.8);
                    p.decay = random.NextRange(0.08, 0.16);
                    p.startFrequency = random.NextRange(0.66, 0.92);
                    p.changeAmount = random.NextRange(0.18, 0.42);
                    p.changeSpeed = random.NextRange(0.18, 0.38);
                    p.squareDuty = random.NextRange(0.25, 0.58);
                    p.lowPassCutoff = 0.96;
                    p.highPassCutoff = random.NextRange(0.02, 0.08);
                    p.masterVolume = 0.54;
                    return SfxClamp.Apply(p);

                case SfxPresetId.LaserShoot:
                    p.waveform = SfxWaveform.Noise;
                    p.sustain = random.NextRange(0.08, 0.22);
                    p.sustainPunch = random.NextRange(0.12, 0.34);
                    p.decay = random.NextRange(0.14, 0.34);
                    p.startFrequency = random.NextRange(0.48, 0.92);
                    p.minFrequency = random.NextRange(0.02, 0.16);
                    p.slide = random.NextRange(-0.72, -0.24);
                    p.deltaSlide = random.NextRange(-0.08, 0.12);
                    p.vibratoDepth = random.NextRange(0.0, 0.24);
                    p.vibratoSpeed = random.NextRange(0.2, 0.62);
                    p.squareDuty = random.NextRange(0.05, 0.4);
                    p.dutySweep = random.NextRange(-0.34, 0.18);
                    p.phaserOffset = random.NextRange(-0.45, 0.45);
                    p.phaserSweep = random.NextRange(-0.08, 0.08);
                    p.highPassCutoff = random.NextRange(0.05, 0.16);
                    p.masterVolume = 0.56;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Explosion:
                    p.waveform = SfxWaveform.Noise;
                    p.sustain = random.NextRange(0.18, 0.44);
                    p.sustainPunch = random.NextRange(0.12, 0.44);
                    p.decay = random.NextRange(0.24, 0.62);
                    p.startFrequency = random.NextRange(0.06, 0.22);
                    p.minFrequency = 0.0;
                    p.slide = random.NextRange(-0.38, -0.08);
                    p.repeatSpeed = random.NextRange(0.02, 0.16);
                    p.phaserOffset = random.NextRange(-0.4, 0.4);
                    p.phaserSweep = random.NextRange(-0.06, 0.06);
                    p.lowPassCutoff = random.NextRange(0.2, 0.52);
                    p.lowPassSweep = random.NextRange(-0.12, -0.02);
                    p.lowPassResonance = random.NextRange(0.2, 0.55);
                    p.highPassCutoff = random.NextRange(0.02, 0.12);
                    p.masterVolume = 0.66;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Powerup:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Square : SfxWaveform.Sawtooth;
                    p.sustain = random.NextRange(0.14, 0.28);
                    p.sustainPunch = random.NextRange(0.18, 0.45);
                    p.decay = random.NextRange(0.16, 0.3);
                    p.startFrequency = random.NextRange(0.24, 0.44);
                    p.slide = random.NextRange(0.18, 0.42);
                    p.changeAmount = random.NextRange(0.08, 0.32);
                    p.changeSpeed = random.NextRange(0.18, 0.42);
                    p.squareDuty = random.NextRange(0.2, 0.6);
                    p.dutySweep = random.NextRange(0.04, 0.2);
                    p.lowPassCutoff = 0.9;
                    p.masterVolume = 0.56;
                    return SfxClamp.Apply(p);

                case SfxPresetId.HitHurt:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Noise : SfxWaveform.Square;
                    p.sustain = random.NextRange(0.02, 0.12);
                    p.sustainPunch = random.NextRange(0.2, 0.55);
                    p.decay = random.NextRange(0.08, 0.2);
                    p.startFrequency = random.NextRange(0.26, 0.68);
                    p.slide = random.NextRange(-0.44, -0.08);
                    p.highPassCutoff = random.NextRange(0.06, 0.18);
                    p.lowPassCutoff = random.NextRange(0.45, 0.9);
                    p.masterVolume = 0.56;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Jump:
                    p.waveform = random.NextUnit() > 0.35 ? SfxWaveform.Square : SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.08, 0.16);
                    p.sustainPunch = random.NextRange(0.12, 0.28);
                    p.decay = random.NextRange(0.12, 0.24);
                    p.startFrequency = random.NextRange(0.38, 0.62);
                    p.slide = random.NextRange(0.08, 0.28);
                    p.squareDuty = random.NextRange(0.2, 0.5);
                    p.highPassCutoff = random.NextRange(0.02, 0.1);
                    p.masterVolume = 0.54;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepSnow:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Snow;
                    p.footstepHeel = random.NextRange(0.28, 0.42);
                    p.footstepRoll = random.NextRange(0.72, 0.9);
                    p.footstepBall = random.NextRange(0.36, 0.56);
                    p.footstepSwiftness = random.NextRange(0.34, 0.5);
                    p.attack = random.NextRange(0.01, 0.05);
                    p.sustain = random.NextRange(0.06, 0.14);
                    p.sustainPunch = random.NextRange(0.14, 0.3);
                    p.decay = random.NextRange(0.08, 0.16);
                    p.startFrequency = random.NextRange(0.12, 0.24);
                    p.minFrequency = random.NextRange(0.02, 0.08);
                    p.slide = random.NextRange(-0.18, 0.0);
                    p.squareDuty = random.NextRange(0.2, 0.45);
                    p.repeatSpeed = random.NextRange(0.02, 0.08);
                    p.lowPassCutoff = random.NextRange(0.26, 0.46);
                    p.lowPassSweep = random.NextRange(-0.06, 0.04);
                    p.lowPassResonance = random.NextRange(0.08, 0.22);
                    p.highPassCutoff = random.NextRange(0.08, 0.16);
                    p.highPassSweep = random.NextRange(-0.02, 0.04);
                    p.masterVolume = 0.48;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepGrass:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Grass;
                    p.footstepHeel = random.NextRange(0.28, 0.4);
                    p.footstepRoll = random.NextRange(0.56, 0.74);
                    p.footstepBall = random.NextRange(0.44, 0.66);
                    p.footstepSwiftness = random.NextRange(0.42, 0.58);
                    p.attack = random.NextRange(0.01, 0.03);
                    p.sustain = random.NextRange(0.05, 0.12);
                    p.sustainPunch = random.NextRange(0.1, 0.24);
                    p.decay = random.NextRange(0.08, 0.14);
                    p.startFrequency = random.NextRange(0.18, 0.32);
                    p.minFrequency = random.NextRange(0.04, 0.12);
                    p.slide = random.NextRange(-0.12, 0.04);
                    p.repeatSpeed = random.NextRange(0.03, 0.1);
                    p.lowPassCutoff = random.NextRange(0.42, 0.64);
                    p.lowPassSweep = random.NextRange(-0.04, 0.05);
                    p.lowPassResonance = random.NextRange(0.06, 0.18);
                    p.highPassCutoff = random.NextRange(0.1, 0.18);
                    p.highPassSweep = random.NextRange(-0.02, 0.04);
                    p.masterVolume = 0.46;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepDirt:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = random.NextUnit() > 0.75 ? SfxWaveform.Square : SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Dirt;
                    p.footstepHeel = random.NextRange(0.48, 0.68);
                    p.footstepRoll = random.NextRange(0.48, 0.68);
                    p.footstepBall = random.NextRange(0.32, 0.5);
                    p.footstepSwiftness = random.NextRange(0.44, 0.62);
                    p.attack = random.NextRange(0.0, 0.02);
                    p.sustain = random.NextRange(0.05, 0.11);
                    p.sustainPunch = random.NextRange(0.16, 0.32);
                    p.decay = random.NextRange(0.09, 0.16);
                    p.startFrequency = random.NextRange(0.1, 0.22);
                    p.minFrequency = random.NextRange(0.02, 0.08);
                    p.slide = random.NextRange(-0.2, -0.04);
                    p.squareDuty = random.NextRange(0.1, 0.28);
                    p.lowPassCutoff = random.NextRange(0.24, 0.44);
                    p.lowPassSweep = random.NextRange(-0.08, 0.02);
                    p.lowPassResonance = random.NextRange(0.1, 0.24);
                    p.highPassCutoff = random.NextRange(0.04, 0.12);
                    p.highPassSweep = random.NextRange(-0.03, 0.02);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepGravel:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Gravel;
                    p.footstepHeel = random.NextRange(0.56, 0.76);
                    p.footstepRoll = random.NextRange(0.34, 0.56);
                    p.footstepBall = random.NextRange(0.4, 0.6);
                    p.footstepSwiftness = random.NextRange(0.5, 0.7);
                    p.attack = random.NextRange(0.0, 0.02);
                    p.sustain = random.NextRange(0.06, 0.16);
                    p.sustainPunch = random.NextRange(0.22, 0.42);
                    p.decay = random.NextRange(0.1, 0.18);
                    p.startFrequency = random.NextRange(0.26, 0.42);
                    p.minFrequency = random.NextRange(0.08, 0.16);
                    p.slide = random.NextRange(-0.08, 0.08);
                    p.deltaSlide = random.NextRange(-0.03, 0.03);
                    p.repeatSpeed = random.NextRange(0.04, 0.14);
                    p.phaserOffset = random.NextRange(-0.08, 0.08);
                    p.phaserSweep = random.NextRange(-0.02, 0.02);
                    p.lowPassCutoff = random.NextRange(0.52, 0.78);
                    p.lowPassResonance = random.NextRange(0.04, 0.16);
                    p.highPassCutoff = random.NextRange(0.16, 0.28);
                    p.highPassSweep = random.NextRange(0.0, 0.05);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepMud:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Mud;
                    p.footstepHeel = random.NextRange(0.4, 0.58);
                    p.footstepRoll = random.NextRange(0.58, 0.76);
                    p.footstepBall = random.NextRange(0.3, 0.48);
                    p.footstepSwiftness = random.NextRange(0.24, 0.38);
                    p.attack = random.NextRange(0.01, 0.04);
                    p.sustain = random.NextRange(0.14, 0.24);
                    p.sustainPunch = random.NextRange(0.18, 0.36);
                    p.decay = random.NextRange(0.18, 0.3);
                    p.startFrequency = random.NextRange(0.08, 0.16);
                    p.minFrequency = random.NextRange(0.01, 0.05);
                    p.slide = random.NextRange(-0.24, -0.06);
                    p.lowPassCutoff = random.NextRange(0.16, 0.3);
                    p.lowPassSweep = random.NextRange(-0.1, 0.02);
                    p.lowPassResonance = random.NextRange(0.2, 0.38);
                    p.highPassCutoff = random.NextRange(0.01, 0.06);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepWood:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = random.NextUnit() > 0.8 ? SfxWaveform.Square : SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Wood;
                    p.footstepHeel = random.NextRange(0.6, 0.8);
                    p.footstepRoll = random.NextRange(0.3, 0.5);
                    p.footstepBall = random.NextRange(0.42, 0.6);
                    p.footstepSwiftness = random.NextRange(0.56, 0.74);
                    p.attack = 0.0;
                    p.sustain = random.NextRange(0.03, 0.09);
                    p.sustainPunch = random.NextRange(0.3, 0.5);
                    p.decay = random.NextRange(0.06, 0.12);
                    p.startFrequency = random.NextRange(0.3, 0.46);
                    p.minFrequency = random.NextRange(0.12, 0.24);
                    p.slide = random.NextRange(-0.14, 0.0);
                    p.lowPassCutoff = random.NextRange(0.6, 0.82);
                    p.lowPassResonance = random.NextRange(0.12, 0.28);
                    p.highPassCutoff = random.NextRange(0.1, 0.2);
                    p.masterVolume = 0.48;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepMetal:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Metal;
                    p.footstepHeel = random.NextRange(0.52, 0.7);
                    p.footstepRoll = random.NextRange(0.4, 0.58);
                    p.footstepBall = random.NextRange(0.5, 0.68);
                    p.footstepSwiftness = random.NextRange(0.52, 0.68);
                    p.attack = 0.0;
                    p.sustain = random.NextRange(0.06, 0.12);
                    p.sustainPunch = random.NextRange(0.26, 0.46);
                    p.decay = random.NextRange(0.1, 0.16);
                    p.startFrequency = random.NextRange(0.56, 0.74);
                    p.minFrequency = random.NextRange(0.2, 0.34);
                    p.slide = random.NextRange(-0.08, 0.06);
                    p.phaserOffset = random.NextRange(-0.14, 0.14);
                    p.lowPassCutoff = random.NextRange(0.78, 0.96);
                    p.lowPassResonance = random.NextRange(0.3, 0.5);
                    p.highPassCutoff = random.NextRange(0.24, 0.38);
                    p.masterVolume = 0.52;
                    return SfxClamp.Apply(p);

                case SfxPresetId.FootstepWater:
                    p.engine = SfxEngine.Footsteppr;
                    p.waveform = SfxWaveform.Noise;
                    p.footstepTerrain = SfxFootstepTerrain.Water;
                    p.footstepHeel = random.NextRange(0.44, 0.62);
                    p.footstepRoll = random.NextRange(0.5, 0.68);
                    p.footstepBall = random.NextRange(0.46, 0.64);
                    p.footstepSwiftness = random.NextRange(0.4, 0.56);
                    p.attack = 0.0;
                    p.sustain = random.NextRange(0.1, 0.2);
                    p.sustainPunch = random.NextRange(0.2, 0.4);
                    p.decay = random.NextRange(0.14, 0.24);
                    p.startFrequency = random.NextRange(0.34, 0.5);
                    p.minFrequency = random.NextRange(0.08, 0.18);
                    p.slide = random.NextRange(-0.1, 0.06);
                    p.lowPassCutoff = random.NextRange(0.5, 0.72);
                    p.lowPassSweep = random.NextRange(-0.06, 0.04);
                    p.lowPassResonance = random.NextRange(0.18, 0.34);
                    p.highPassCutoff = random.NextRange(0.14, 0.26);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Click:
                    p.waveform = SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.01, 0.04);
                    p.sustainPunch = random.NextRange(0.18, 0.48);
                    p.decay = random.NextRange(0.01, 0.06);
                    p.startFrequency = random.NextRange(0.42, 0.78);
                    p.squareDuty = random.NextRange(0.1, 0.34);
                    p.highPassCutoff = random.NextRange(0.08, 0.22);
                    p.masterVolume = 0.44;
                    return SfxClamp.Apply(p);

                case SfxPresetId.BlipSelect:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Square : SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.02, 0.08);
                    p.sustainPunch = random.NextRange(0.08, 0.3);
                    p.decay = random.NextRange(0.04, 0.12);
                    p.startFrequency = random.NextRange(0.44, 0.82);
                    p.squareDuty = random.NextRange(0.25, 0.6);
                    p.highPassCutoff = random.NextRange(0.02, 0.1);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.HoverTick:
                    p.waveform = SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.005, 0.02);
                    p.sustainPunch = random.NextRange(0.06, 0.18);
                    p.decay = random.NextRange(0.015, 0.04);
                    p.startFrequency = random.NextRange(0.72, 0.9);
                    p.squareDuty = random.NextRange(0.2, 0.4);
                    p.highPassCutoff = random.NextRange(0.3, 0.46);
                    p.masterVolume = 0.3;
                    return SfxClamp.Apply(p);

                case SfxPresetId.NotifyPop:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Sine : SfxWaveform.Square;
                    p.sustain = random.NextRange(0.06, 0.14);
                    p.sustainPunch = random.NextRange(0.1, 0.26);
                    p.decay = random.NextRange(0.08, 0.16);
                    p.startFrequency = random.NextRange(0.5, 0.7);
                    p.slide = random.NextRange(0.16, 0.3);
                    p.changeAmount = random.NextRange(0.14, 0.3);
                    p.changeSpeed = random.NextRange(0.2, 0.4);
                    p.squareDuty = random.NextRange(0.25, 0.5);
                    p.lowPassCutoff = random.NextRange(0.8, 0.98);
                    p.masterVolume = 0.44;
                    return SfxClamp.Apply(p);

                case SfxPresetId.SuccessChime:
                    p.waveform = random.NextUnit() > 0.4 ? SfxWaveform.Sine : SfxWaveform.Square;
                    p.attack = random.NextRange(0.005, 0.02);
                    p.sustain = random.NextRange(0.16, 0.3);
                    p.sustainPunch = random.NextRange(0.04, 0.16);
                    p.decay = random.NextRange(0.2, 0.36);
                    p.startFrequency = random.NextRange(0.46, 0.6);
                    p.slide = random.NextRange(0.12, 0.26);
                    p.changeAmount = random.NextRange(0.16, 0.34);
                    p.changeSpeed = random.NextRange(0.24, 0.44);
                    p.squareDuty = random.NextRange(0.3, 0.55);
                    p.lowPassCutoff = random.NextRange(0.86, 1.0);
                    p.lowPassResonance = random.NextRange(0.04, 0.16);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.ErrorBuzz:
                    p.waveform = SfxWaveform.Square;
                    p.sustain = random.NextRange(0.18, 0.32);
                    p.sustainPunch = random.NextRange(0.1, 0.24);
                    p.decay = random.NextRange(0.14, 0.26);
                    p.startFrequency = random.NextRange(0.16, 0.28);
                    p.minFrequency = random.NextRange(0.04, 0.1);
                    p.vibratoDepth = random.NextRange(0.45, 0.75);
                    p.vibratoSpeed = random.NextRange(0.55, 0.85);
                    p.squareDuty = random.NextRange(0.3, 0.5);
                    p.lowPassCutoff = random.NextRange(0.42, 0.62);
                    p.lowPassResonance = random.NextRange(0.1, 0.26);
                    p.masterVolume = 0.5;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Synth:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Sawtooth : SfxWaveform.Sine;
                    p.attack = random.NextRange(0.02, 0.1);
                    p.sustain = random.NextRange(0.22, 0.54);
                    p.sustainPunch = random.NextRange(0.0, 0.2);
                    p.decay = random.NextRange(0.22, 0.54);
                    p.startFrequency = random.NextRange(0.16, 0.68);
                    p.slide = random.NextRange(-0.08, 0.12);
                    p.vibratoDepth = random.NextRange(0.06, 0.36);
                    p.vibratoSpeed = random.NextRange(0.18, 0.62);
                    p.changeAmount = random.NextRange(-0.12, 0.12);
                    p.squareDuty = random.NextRange(0.22, 0.7);
                    p.lowPassCutoff = random.NextRange(0.5, 0.92);
                    p.lowPassResonance = random.NextRange(0.06, 0.38);
                    p.highPassCutoff = random.NextRange(0.01, 0.08);
                    p.masterVolume = 0.58;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Tone:
                    p.waveform = SfxWaveform.Sine;
                    p.attack = random.NextRange(0.01, 0.08);
                    p.sustain = random.NextRange(0.38, 0.62);
                    p.sustainPunch = 0.0;
                    p.decay = random.NextRange(0.18, 0.42);
                    p.startFrequency = random.NextRange(0.18, 0.82);
                    p.lowPassCutoff = 1.0;
                    p.highPassCutoff = 0.0;
                    p.masterVolume = 0.52;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Whoosh:
                    p.waveform = SfxWaveform.Noise;
                    p.attack = random.NextRange(0.18, 0.3);
                    p.sustain = random.NextRange(0.2, 0.34);
                    p.sustainPunch = random.NextRange(0.02, 0.12);
                    p.decay = random.NextRange(0.42, 0.62);
                    p.startFrequency = random.NextRange(0.3, 0.46);
                    p.minFrequency = random.NextRange(0.02, 0.1);
                    p.slide = random.NextRange(-0.5, -0.28);
                    p.vibratoDepth = random.NextRange(0.06, 0.18);
                    p.vibratoSpeed = random.NextRange(0.2, 0.4);
                    p.lowPassCutoff = random.NextRange(0.34, 0.56);
                    p.lowPassSweep = random.NextRange(-0.34, -0.16);
                    p.lowPassResonance = random.NextRange(0.28, 0.5);
                    p.highPassCutoff = random.NextRange(0.02, 0.08);
                    p.masterVolume = 0.48;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Riser:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Sawtooth : SfxWaveform.Square;
                    p.attack = random.NextRange(0.14, 0.26);
                    p.sustain = random.NextRange(0.48, 0.68);
                    p.sustainPunch = random.NextRange(0.02, 0.1);
                    p.decay = random.NextRange(0.3, 0.46);
                    p.startFrequency = random.NextRange(0.14, 0.26);
                    p.slide = random.NextRange(0.52, 0.82);
                    p.deltaSlide = random.NextRange(0.04, 0.14);
                    p.changeAmount = random.NextRange(0.16, 0.34);
                    p.changeSpeed = random.NextRange(0.24, 0.42);
                    p.squareDuty = random.NextRange(0.3, 0.6);
                    p.lowPassCutoff = random.NextRange(0.74, 0.94);
                    p.lowPassResonance = random.NextRange(0.06, 0.2);
                    p.masterVolume = 0.46;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Impact:
                    p.waveform = random.NextUnit() > 0.5 ? SfxWaveform.Sawtooth : SfxWaveform.Sine;
                    p.sustain = random.NextRange(0.08, 0.16);
                    p.sustainPunch = random.NextRange(0.5, 0.8);
                    p.decay = random.NextRange(0.34, 0.52);
                    p.startFrequency = random.NextRange(0.12, 0.2);
                    p.minFrequency = 0.0;
                    p.slide = random.NextRange(-0.6, -0.34);
                    p.deltaSlide = random.NextRange(-0.06, 0.0);
                    p.lowPassCutoff = random.NextRange(0.3, 0.5);
                    p.lowPassSweep = random.NextRange(-0.2, -0.06);
                    p.lowPassResonance = random.NextRange(0.26, 0.46);
                    p.highPassCutoff = random.NextRange(0.02, 0.08);
                    p.masterVolume = 0.72;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Glitch:
                    p.waveform = random.NextUnit() > 0.3 ? SfxWaveform.Square : SfxWaveform.Sawtooth;
                    p.sustain = random.NextRange(0.1, 0.2);
                    p.sustainPunch = random.NextRange(0.2, 0.44);
                    p.decay = random.NextRange(0.12, 0.22);
                    p.startFrequency = random.NextRange(0.38, 0.62);
                    p.changeAmount = random.NextRange(0.5, 0.86) * (random.NextUnit() > 0.5 ? 1.0 : -1.0);
                    p.changeSpeed = random.NextRange(0.55, 0.9);
                    p.squareDuty = random.NextRange(0.08, 0.3);
                    p.dutySweep = random.NextRange(0.3, 0.56) * (random.NextUnit() > 0.5 ? 1.0 : -1.0);
                    p.repeatSpeed = random.NextRange(0.28, 0.5);
                    p.phaserOffset = random.NextRange(0.3, 0.7) * (random.NextUnit() > 0.5 ? 1.0 : -1.0);
                    p.phaserSweep = random.NextRange(0.2, 0.5) * (random.NextUnit() > 0.5 ? 1.0 : -1.0);
                    p.lowPassCutoff = random.NextRange(0.6, 0.9);
                    p.highPassCutoff = random.NextRange(0.06, 0.18);
                    p.masterVolume = 0.44;
                    return SfxClamp.Apply(p);

                case SfxPresetId.Mutate:
                    return MutateWithActiveRandom(current, random);

                default:
                    return SfxClamp.Apply(current);
            }
        }

        private static SfxParams MutateWithActiveRandom(SfxParams current, Mulberry32 random)
        {
            if (current.engine == SfxEngine.Footsteppr)
            {
                var footstep = new SfxParams(current);
                footstep.footstepHeel = current.footstepHeel + random.NextRange(-0.12, 0.12);
                footstep.footstepRoll = current.footstepRoll + random.NextRange(-0.12, 0.12);
                footstep.footstepBall = current.footstepBall + random.NextRange(-0.12, 0.12);
                footstep.footstepSwiftness = current.footstepSwiftness + random.NextRange(-0.1, 0.1);
                footstep.masterVolume = current.masterVolume + random.NextRange(-0.05, 0.05);
                footstep.fxDrive = current.fxDrive + random.NextRange(-0.04, 0.04);
                footstep.fxTone = current.fxTone + random.NextRange(-0.08, 0.08);
                footstep.fxReverbSize = current.fxReverbSize + random.NextRange(-0.1, 0.1);
                footstep.fxReverbMix = current.fxReverbMix + random.NextRange(-0.06, 0.06);
                return SfxClamp.Apply(footstep);
            }

            var mutated = new SfxParams(current);
            mutated.attack = current.attack + random.NextRange(-0.08, 0.08);
            mutated.sustain = current.sustain + random.NextRange(-0.12, 0.12);
            mutated.sustainPunch = current.sustainPunch + random.NextRange(-0.15, 0.15);
            mutated.decay = current.decay + random.NextRange(-0.12, 0.12);
            mutated.startFrequency = current.startFrequency + random.NextRange(-0.18, 0.18);
            mutated.minFrequency = current.minFrequency + random.NextRange(-0.12, 0.12);
            mutated.slide = current.slide + random.NextRange(-0.24, 0.24);
            mutated.deltaSlide = current.deltaSlide + random.NextRange(-0.16, 0.16);
            mutated.vibratoDepth = current.vibratoDepth + random.NextRange(-0.2, 0.2);
            mutated.vibratoSpeed = current.vibratoSpeed + random.NextRange(-0.18, 0.18);
            mutated.changeAmount = current.changeAmount + random.NextRange(-0.24, 0.24);
            mutated.changeSpeed = current.changeSpeed + random.NextRange(-0.24, 0.24);
            mutated.squareDuty = current.squareDuty + random.NextRange(-0.18, 0.18);
            mutated.dutySweep = current.dutySweep + random.NextRange(-0.24, 0.24);
            mutated.repeatSpeed = current.repeatSpeed + random.NextRange(-0.2, 0.2);
            mutated.phaserOffset = current.phaserOffset + random.NextRange(-0.28, 0.28);
            mutated.phaserSweep = current.phaserSweep + random.NextRange(-0.18, 0.18);
            mutated.lowPassCutoff = current.lowPassCutoff + random.NextRange(-0.2, 0.2);
            mutated.lowPassSweep = current.lowPassSweep + random.NextRange(-0.18, 0.18);
            mutated.lowPassResonance = current.lowPassResonance + random.NextRange(-0.14, 0.14);
            mutated.highPassCutoff = current.highPassCutoff + random.NextRange(-0.14, 0.14);
            mutated.highPassSweep = current.highPassSweep + random.NextRange(-0.12, 0.12);
            mutated.masterVolume = current.masterVolume + random.NextRange(-0.08, 0.08);
            mutated.fxDrive = current.fxDrive + random.NextRange(-0.05, 0.05);
            mutated.fxCrushRate = current.fxCrushRate + random.NextRange(-0.05, 0.05);
            mutated.fxCrushBits = current.fxCrushBits + random.NextRange(-0.05, 0.05);
            mutated.fxTone = current.fxTone + random.NextRange(-0.12, 0.12);
            mutated.fxDelayTime = current.fxDelayTime + random.NextRange(-0.08, 0.08);
            mutated.fxDelayFeedback = current.fxDelayFeedback + random.NextRange(-0.08, 0.08);
            mutated.fxDelayMix = current.fxDelayMix + random.NextRange(-0.08, 0.08);
            mutated.fxReverbSize = current.fxReverbSize + random.NextRange(-0.1, 0.1);
            mutated.fxReverbMix = current.fxReverbMix + random.NextRange(-0.1, 0.1);
            return SfxClamp.Apply(mutated);
        }

        private static bool HasPresetFx(SfxPresetId preset)
        {
            switch (preset)
            {
                case SfxPresetId.PickupCoin:
                case SfxPresetId.LaserShoot:
                case SfxPresetId.Explosion:
                case SfxPresetId.Powerup:
                case SfxPresetId.HitHurt:
                case SfxPresetId.SuccessChime:
                case SfxPresetId.ErrorBuzz:
                case SfxPresetId.Whoosh:
                case SfxPresetId.Riser:
                case SfxPresetId.Impact:
                case SfxPresetId.Glitch:
                case SfxPresetId.FootstepMud:
                case SfxPresetId.FootstepWood:
                case SfxPresetId.FootstepMetal:
                case SfxPresetId.FootstepWater:
                case SfxPresetId.Synth:
                case SfxPresetId.Tone:
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyPresetFx(SfxPresetId preset, SfxParams p)
        {
            switch (preset)
            {
                case SfxPresetId.PickupCoin:
                    p.fxDelayMix = 0.1;
                    p.fxDelayFeedback = 0.3;
                    break;
                case SfxPresetId.LaserShoot:
                    p.fxDelayMix = 0.16;
                    p.fxDelayFeedback = 0.34;
                    p.fxTone = 0.1;
                    break;
                case SfxPresetId.Explosion:
                    p.fxDrive = 0.42;
                    p.fxTone = -0.14;
                    p.fxReverbMix = 0.4;
                    p.fxReverbSize = 0.78;
                    break;
                case SfxPresetId.Powerup:
                    p.fxDelayMix = 0.14;
                    p.fxDelayFeedback = 0.32;
                    break;
                case SfxPresetId.HitHurt:
                    p.fxDrive = 0.18;
                    break;
                case SfxPresetId.SuccessChime:
                    p.fxReverbMix = 0.2;
                    p.fxReverbSize = 0.5;
                    break;
                case SfxPresetId.ErrorBuzz:
                    p.fxDrive = 0.14;
                    p.fxCrushBits = 0.2;
                    break;
                case SfxPresetId.Whoosh:
                    p.fxDelayMix = 0.26;
                    p.fxDelayFeedback = 0.44;
                    p.fxReverbMix = 0.3;
                    p.fxReverbSize = 0.7;
                    p.fxTone = 0.1;
                    break;
                case SfxPresetId.Riser:
                    p.fxDelayMix = 0.28;
                    p.fxDelayFeedback = 0.48;
                    p.fxReverbMix = 0.3;
                    p.fxReverbSize = 0.78;
                    break;
                case SfxPresetId.Impact:
                    p.fxDrive = 0.52;
                    p.fxReverbMix = 0.42;
                    p.fxReverbSize = 0.82;
                    break;
                case SfxPresetId.Glitch:
                    p.fxCrushRate = 0.42;
                    p.fxCrushBits = 0.6;
                    p.fxDelayMix = 0.18;
                    p.fxDelayFeedback = 0.4;
                    break;
                case SfxPresetId.FootstepMud:
                    p.fxTone = -0.16;
                    p.fxReverbMix = 0.12;
                    p.fxReverbSize = 0.45;
                    break;
                case SfxPresetId.FootstepWood:
                    p.fxTone = 0.12;
                    p.fxReverbMix = 0.1;
                    p.fxReverbSize = 0.4;
                    break;
                case SfxPresetId.FootstepMetal:
                    p.fxTone = 0.16;
                    p.fxReverbMix = 0.26;
                    p.fxReverbSize = 0.55;
                    break;
                case SfxPresetId.FootstepWater:
                    p.fxReverbMix = 0.3;
                    p.fxReverbSize = 0.62;
                    break;
                case SfxPresetId.Synth:
                    p.fxReverbMix = 0.18;
                    p.fxReverbSize = 0.55;
                    break;
                case SfxPresetId.Tone:
                    p.fxReverbMix = 0.16;
                    p.fxReverbSize = 0.5;
                    break;
            }
        }

        private static SfxParams ApplyRandomFxOverlay(SfxParams baseParams, Mulberry32 random)
        {
            var p = new SfxParams(baseParams);
            p.fxDrive = random.NextRange(0.0, 0.22);
            p.fxCrushRate = random.NextUnit() > 0.7 ? random.NextRange(0.1, 0.4) : 0.0;
            p.fxCrushBits = random.NextUnit() > 0.75 ? random.NextRange(0.1, 0.5) : 0.0;
            p.fxTone = random.NextRange(-0.3, 0.3);
            p.fxDelayMix = random.NextUnit() > 0.5 ? random.NextRange(0.05, 0.22) : 0.0;
            p.fxDelayFeedback = random.NextRange(0.2, 0.45);
            p.fxReverbMix = random.NextUnit() > 0.4 ? random.NextRange(0.08, 0.26) : 0.0;
            p.fxReverbSize = random.NextRange(0.35, 0.8);
            return p;
        }

        private static SfxWaveform RandomWaveform(Mulberry32 random)
        {
            int index = (int)Math.Floor(random.NextUnit() * 4.0);
            switch (index)
            {
                case 0: return SfxWaveform.Square;
                case 1: return SfxWaveform.Sawtooth;
                case 2: return SfxWaveform.Sine;
                default: return SfxWaveform.Noise;
            }
        }
    }
}

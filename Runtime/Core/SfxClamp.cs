using System;

namespace AFCT.SfxLab
{
    /// <summary>Port of <c>clampSfxParams</c>: merge onto defaults, then clamp every field.</summary>
    internal static class SfxClamp
    {
        internal static SfxParams Apply(SfxParams source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var defaults = new SfxParams();

            return new SfxParams
            {
                engine = source.engine == SfxEngine.Footsteppr ? SfxEngine.Footsteppr : SfxEngine.Sfxr,
                waveform = IsWaveform(source.waveform) ? source.waveform : defaults.waveform,
                footstepTerrain = IsTerrain(source.footstepTerrain) ? source.footstepTerrain : defaults.footstepTerrain,
                footstepHeel = SfxMath.Clamp(source.footstepHeel, 0.0, 1.0),
                footstepRoll = SfxMath.Clamp(source.footstepRoll, 0.0, 1.0),
                footstepBall = SfxMath.Clamp(source.footstepBall, 0.0, 1.0),
                footstepSwiftness = SfxMath.Clamp(source.footstepSwiftness, 0.0, 1.0),
                attack = SfxMath.Clamp(source.attack, 0.0, 1.0),
                sustain = SfxMath.Clamp(source.sustain, 0.0, 1.0),
                sustainPunch = SfxMath.Clamp(source.sustainPunch, 0.0, 1.0),
                decay = SfxMath.Clamp(source.decay, 0.0, 1.0),
                startFrequency = SfxMath.Clamp(source.startFrequency, 0.0, 1.0),
                minFrequency = SfxMath.Clamp(source.minFrequency, 0.0, 1.0),
                slide = SfxMath.Clamp(source.slide, -1.0, 1.0),
                deltaSlide = SfxMath.Clamp(source.deltaSlide, -1.0, 1.0),
                vibratoDepth = SfxMath.Clamp(source.vibratoDepth, 0.0, 1.0),
                vibratoSpeed = SfxMath.Clamp(source.vibratoSpeed, 0.0, 1.0),
                changeAmount = SfxMath.Clamp(source.changeAmount, -1.0, 1.0),
                changeSpeed = SfxMath.Clamp(source.changeSpeed, 0.0, 1.0),
                squareDuty = SfxMath.Clamp(source.squareDuty, 0.0, 1.0),
                dutySweep = SfxMath.Clamp(source.dutySweep, -1.0, 1.0),
                repeatSpeed = SfxMath.Clamp(source.repeatSpeed, 0.0, 1.0),
                phaserOffset = SfxMath.Clamp(source.phaserOffset, -1.0, 1.0),
                phaserSweep = SfxMath.Clamp(source.phaserSweep, -1.0, 1.0),
                lowPassCutoff = SfxMath.Clamp(source.lowPassCutoff, 0.0, 1.0),
                lowPassSweep = SfxMath.Clamp(source.lowPassSweep, -1.0, 1.0),
                lowPassResonance = SfxMath.Clamp(source.lowPassResonance, 0.0, 1.0),
                highPassCutoff = SfxMath.Clamp(source.highPassCutoff, 0.0, 1.0),
                highPassSweep = SfxMath.Clamp(source.highPassSweep, -1.0, 1.0),
                masterVolume = SfxMath.Clamp(source.masterVolume, 0.0, 1.0),
                fxDrive = SfxMath.Clamp(source.fxDrive, 0.0, 1.0),
                fxCrushRate = SfxMath.Clamp(source.fxCrushRate, 0.0, 1.0),
                fxCrushBits = SfxMath.Clamp(source.fxCrushBits, 0.0, 1.0),
                fxTone = SfxMath.Clamp(source.fxTone, -1.0, 1.0),
                fxDelayTime = SfxMath.Clamp(source.fxDelayTime, 0.0, 1.0),
                fxDelayFeedback = SfxMath.Clamp(source.fxDelayFeedback, 0.0, 1.0),
                fxDelayMix = SfxMath.Clamp(source.fxDelayMix, 0.0, 1.0),
                fxReverbSize = SfxMath.Clamp(source.fxReverbSize, 0.0, 1.0),
                fxReverbMix = SfxMath.Clamp(source.fxReverbMix, 0.0, 1.0),
                fxNormalize = SfxMath.IsFinite(source.fxNormalize)
                    ? (source.fxNormalize >= 0.5 ? 1.0 : 0.0)
                    : defaults.fxNormalize,
            };
        }

        internal static bool IsWaveform(SfxWaveform waveform)
        {
            return waveform == SfxWaveform.Square
                || waveform == SfxWaveform.Sawtooth
                || waveform == SfxWaveform.Sine
                || waveform == SfxWaveform.Noise;
        }

        internal static bool IsTerrain(SfxFootstepTerrain terrain)
        {
            return terrain == SfxFootstepTerrain.Snow
                || terrain == SfxFootstepTerrain.Grass
                || terrain == SfxFootstepTerrain.Dirt
                || terrain == SfxFootstepTerrain.Gravel
                || terrain == SfxFootstepTerrain.Mud
                || terrain == SfxFootstepTerrain.Wood
                || terrain == SfxFootstepTerrain.Metal
                || terrain == SfxFootstepTerrain.Water;
        }
    }
}

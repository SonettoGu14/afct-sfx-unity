namespace AFCT.SfxLab
{
    /// <summary>
    /// Every parameter of the synth and the FX rack. Field names and defaults match
    /// <c>SfxParams</c> / <c>DEFAULT_SFX_PARAMS</c> in <c>src/lib/sfx.ts</c> exactly.
    /// </summary>
    public sealed class SfxParams
    {
        public SfxEngine engine;
        public SfxWaveform waveform;
        public SfxFootstepTerrain footstepTerrain;

        public double footstepHeel;
        public double footstepRoll;
        public double footstepBall;
        public double footstepSwiftness;

        public double attack;
        public double sustain;
        public double sustainPunch;
        public double decay;
        public double startFrequency;
        public double minFrequency;
        public double slide;
        public double deltaSlide;
        public double vibratoDepth;
        public double vibratoSpeed;
        public double changeAmount;
        public double changeSpeed;
        public double squareDuty;
        public double dutySweep;
        public double repeatSpeed;
        public double phaserOffset;
        public double phaserSweep;
        public double lowPassCutoff;
        public double lowPassSweep;
        public double lowPassResonance;
        public double highPassCutoff;
        public double highPassSweep;
        public double masterVolume;

        public double fxDrive;
        public double fxCrushRate;
        public double fxCrushBits;
        public double fxTone;
        public double fxDelayTime;
        public double fxDelayFeedback;
        public double fxDelayMix;
        public double fxReverbSize;
        public double fxReverbMix;
        public double fxNormalize;

        /// <summary>Creates a parameter set holding the web engine's defaults.</summary>
        public SfxParams()
        {
            engine = SfxEngine.Sfxr;
            waveform = SfxWaveform.Square;
            footstepTerrain = SfxFootstepTerrain.Dirt;
            footstepHeel = 0.5;
            footstepRoll = 0.5;
            footstepBall = 0.5;
            footstepSwiftness = 0.5;
            attack = 0;
            sustain = 0.18;
            sustainPunch = 0.28;
            decay = 0.26;
            startFrequency = 0.42;
            minFrequency = 0;
            slide = 0;
            deltaSlide = 0;
            vibratoDepth = 0;
            vibratoSpeed = 0;
            changeAmount = 0;
            changeSpeed = 0;
            squareDuty = 0.5;
            dutySweep = 0;
            repeatSpeed = 0;
            phaserOffset = 0;
            phaserSweep = 0;
            lowPassCutoff = 1;
            lowPassSweep = 0;
            lowPassResonance = 0;
            highPassCutoff = 0;
            highPassSweep = 0;
            masterVolume = 0.56;
            fxDrive = 0;
            fxCrushRate = 0;
            fxCrushBits = 0;
            fxTone = 0;
            fxDelayTime = 0.25;
            fxDelayFeedback = 0.3;
            fxDelayMix = 0;
            fxReverbSize = 0.5;
            fxReverbMix = 0;
            fxNormalize = 1;
        }

        /// <summary>Copy constructor.</summary>
        public SfxParams(SfxParams other)
        {
            if (other == null)
            {
                throw new System.ArgumentNullException(nameof(other));
            }

            engine = other.engine;
            waveform = other.waveform;
            footstepTerrain = other.footstepTerrain;
            footstepHeel = other.footstepHeel;
            footstepRoll = other.footstepRoll;
            footstepBall = other.footstepBall;
            footstepSwiftness = other.footstepSwiftness;
            attack = other.attack;
            sustain = other.sustain;
            sustainPunch = other.sustainPunch;
            decay = other.decay;
            startFrequency = other.startFrequency;
            minFrequency = other.minFrequency;
            slide = other.slide;
            deltaSlide = other.deltaSlide;
            vibratoDepth = other.vibratoDepth;
            vibratoSpeed = other.vibratoSpeed;
            changeAmount = other.changeAmount;
            changeSpeed = other.changeSpeed;
            squareDuty = other.squareDuty;
            dutySweep = other.dutySweep;
            repeatSpeed = other.repeatSpeed;
            phaserOffset = other.phaserOffset;
            phaserSweep = other.phaserSweep;
            lowPassCutoff = other.lowPassCutoff;
            lowPassSweep = other.lowPassSweep;
            lowPassResonance = other.lowPassResonance;
            highPassCutoff = other.highPassCutoff;
            highPassSweep = other.highPassSweep;
            masterVolume = other.masterVolume;
            fxDrive = other.fxDrive;
            fxCrushRate = other.fxCrushRate;
            fxCrushBits = other.fxCrushBits;
            fxTone = other.fxTone;
            fxDelayTime = other.fxDelayTime;
            fxDelayFeedback = other.fxDelayFeedback;
            fxDelayMix = other.fxDelayMix;
            fxReverbSize = other.fxReverbSize;
            fxReverbMix = other.fxReverbMix;
            fxNormalize = other.fxNormalize;
        }

        /// <summary>Deep copy.</summary>
        public SfxParams Clone()
        {
            return new SfxParams(this);
        }
    }
}

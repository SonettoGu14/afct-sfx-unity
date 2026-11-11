using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// The sfxr engine, ported literally from <c>renderSfxrSamples</c> in <c>src/lib/sfx.ts</c>.
    /// </summary>
    internal static class SfxrRenderer
    {
        private const int PhaserBufferSize = 2048;
        private const double MaxFrequency = 12000.0;
        private const double MinDurationSeconds = 0.18;

        internal static float[] Render(SfxParams p, SfxRenderOptions options, Mulberry32 random)
        {
            double attackSeconds = p.attack * 0.4;
            double sustainSeconds = 0.02 + p.sustain * 1.2;
            double decaySeconds = p.decay * 1.8;
            double durationSeconds = Math.Max(
                MinDurationSeconds,
                attackSeconds + sustainSeconds + decaySeconds);

            double attackSamples = Math.Max(0.0, Math.Floor(attackSeconds * options.SampleRate));
            double sustainSamples = Math.Max(1.0, Math.Floor(sustainSeconds * options.SampleRate));
            double decaySamples = Math.Max(1.0, Math.Floor(decaySeconds * options.SampleRate));
            int totalSamples = (int)Math.Max(
                Math.Floor(durationSeconds * options.SampleRate),
                attackSamples + sustainSamples + decaySamples);

            var samples = new float[totalSamples];
            double startFrequency = NormalizedFrequency(p.startFrequency);
            double minFrequency = Math.Min(startFrequency, NormalizedFrequency(p.minFrequency));
            double initialSlide = SignedCurve(p.slide) * 0.00008;
            double slideDelta = SignedCurve(p.deltaSlide) * 0.00000008;
            double initialDuty = SfxMath.Clamp(0.05 + p.squareDuty * 0.9, 0.05, 0.95);
            double dutyDelta = p.dutySweep * 0.00004;
            double vibratoRate = 0.5 + p.vibratoSpeed * 18.0;
            double vibratoDepth = p.vibratoDepth * 0.35;
            double repeatLimit = p.repeatSpeed > 0.01
                ? Math.Max(128.0, Math.Floor(options.SampleRate * (0.12 + (1.0 - p.repeatSpeed) * 1.48)))
                : 0.0;
            double changeLimit = p.changeSpeed > 0.01
                ? Math.Max(128.0, Math.Floor(options.SampleRate * (0.05 + (1.0 - p.changeSpeed) * 1.3)))
                : 0.0;
            double changeMultiplier = p.changeAmount >= 0.0
                ? 1.0 + p.changeAmount * 1.5
                : 1.0 / (1.0 + Math.Abs(p.changeAmount) * 1.5);

            var phaserBuffer = new float[PhaserBufferSize];
            double phaserOffset = p.phaserOffset * 1024.0;
            double phaserSweep = p.phaserSweep * 0.3;
            int phaserIndex = 0;

            double lowPassCutoff = SfxMath.Clamp(0.02 + p.lowPassCutoff * p.lowPassCutoff * 0.48, 0.02, 1.0);
            double lowPassSweep = p.lowPassSweep * 0.00002;
            double lowPassDamping = SfxMath.Clamp(1.0 - p.lowPassResonance * 0.4, 0.45, 0.995);
            double lowPassState = 0.0;
            double lowPassDelta = 0.0;

            double highPassAlpha = SfxMath.Clamp(0.999 - p.highPassCutoff * p.highPassCutoff * 0.94, 0.02, 0.999);
            double highPassSweep = p.highPassSweep * 0.00002;
            double highPassPrevInput = 0.0;
            double highPassPrevOutput = 0.0;

            double frequency = startFrequency;
            double slide = initialSlide;
            double duty = initialDuty;
            double phase = 0.0;
            int repeatCounter = 0;
            int changeCounter = 0;
            bool changeTriggered = false;
            double vibratoPhase = 0.0;
            double noiseValue = random.NextSigned();

            for (int index = 0; index < totalSamples; index += 1)
            {
                if (repeatLimit > 0.0 && repeatCounter >= repeatLimit)
                {
                    frequency = startFrequency;
                    slide = initialSlide;
                    duty = initialDuty;
                    phase = 0.0;
                    repeatCounter = 0;
                    changeCounter = 0;
                    changeTriggered = false;
                    noiseValue = random.NextSigned();
                }

                repeatCounter += 1;
                changeCounter += 1;

                if (!changeTriggered && changeLimit > 0.0 && changeCounter >= changeLimit)
                {
                    frequency = SfxMath.Clamp(frequency * changeMultiplier, minFrequency, MaxFrequency);
                    changeTriggered = true;
                }

                slide += slideDelta;
                frequency = SfxMath.Clamp(frequency * (1.0 + slide), minFrequency, MaxFrequency);
                duty = SfxMath.Clamp(duty + dutyDelta, 0.02, 0.98);
                vibratoPhase += (Math.PI * 2.0 * vibratoRate) / options.SampleRate;

                double effectiveFrequency = SfxMath.Clamp(
                    frequency * (1.0 + Math.Sin(vibratoPhase) * vibratoDepth),
                    minFrequency,
                    MaxFrequency);

                phase += effectiveFrequency / options.SampleRate;
                while (phase >= 1.0)
                {
                    phase -= 1.0;
                    if (p.waveform == SfxWaveform.Noise)
                    {
                        noiseValue = random.NextSigned();
                    }
                }

                double sample = GetWaveformSample(p.waveform, phase, duty, noiseValue);

                lowPassCutoff = SfxMath.Clamp(lowPassCutoff + lowPassSweep, 0.001, 1.0);
                lowPassDelta += (sample - lowPassState) * lowPassCutoff;
                lowPassDelta *= lowPassDamping;
                lowPassState += lowPassDelta;
                sample = lowPassState;

                highPassAlpha = SfxMath.Clamp(highPassAlpha + highPassSweep, 0.02, 0.999);
                double highPassed = highPassAlpha * (highPassPrevOutput + sample - highPassPrevInput);
                highPassPrevInput = sample;
                highPassPrevOutput = highPassed;
                sample = highPassed;

                phaserOffset = SfxMath.Clamp(
                    phaserOffset + phaserSweep,
                    -(PhaserBufferSize - 1),
                    PhaserBufferSize - 1);
                int phaserDelay = (int)Math.Abs(SfxMath.Round(phaserOffset));
                int delayedIndex = (phaserIndex - phaserDelay + PhaserBufferSize) % PhaserBufferSize;
                double phaserSign = phaserOffset == 0.0 ? 1.0 : SfxMath.Sign(phaserOffset);
                sample += phaserBuffer[delayedIndex] * 0.65 * phaserSign;
                phaserBuffer[phaserIndex] = (float)sample;
                phaserIndex = (phaserIndex + 1) % PhaserBufferSize;

                sample *= GetEnvelopeLevel(
                    index,
                    attackSamples,
                    sustainSamples,
                    decaySamples,
                    p.sustainPunch);
                sample *= p.masterVolume;

                samples[index] = (float)sample;
            }

            return samples;
        }

        private static double GetWaveformSample(SfxWaveform waveform, double phase, double duty, double noiseValue)
        {
            switch (waveform)
            {
                case SfxWaveform.Square:
                    return phase < duty ? 1.0 : -1.0;
                case SfxWaveform.Sawtooth:
                    return 1.0 - phase * 2.0;
                case SfxWaveform.Sine:
                    return Math.Sin(phase * Math.PI * 2.0);
                case SfxWaveform.Noise:
                    return noiseValue;
                default:
                    return 0.0;
            }
        }

        private static double GetEnvelopeLevel(
            int index,
            double attackSamples,
            double sustainSamples,
            double decaySamples,
            double sustainPunch)
        {
            if (attackSamples > 0.0 && index < attackSamples)
            {
                return index / attackSamples;
            }

            double sustainStart = attackSamples;
            double sustainEnd = sustainStart + sustainSamples;
            if (index < sustainEnd)
            {
                double sustainProgress = sustainSamples <= 1.0
                    ? 1.0
                    : (index - sustainStart) / sustainSamples;
                return 1.0 + (1.0 - sustainProgress) * sustainPunch * 0.8;
            }

            double decayStart = sustainEnd;
            double decayEnd = decayStart + decaySamples;
            if (index < decayEnd)
            {
                double decayProgress = decaySamples <= 1.0
                    ? 1.0
                    : (index - decayStart) / decaySamples;
                return Math.Max(0.0, 1.0 - decayProgress);
            }

            return 0.0;
        }

        private static double NormalizedFrequency(double value)
        {
            return 32.0 * Math.Pow(2.0, SfxMath.Clamp(value, 0.0, 1.0) * 6.5);
        }

        private static double SignedCurve(double value)
        {
            double clamped = SfxMath.Clamp(value, -1.0, 1.0);
            return SfxMath.Sign(clamped) * Math.Pow(Math.Abs(clamped), 3.0);
        }
    }
}

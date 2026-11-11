using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// The FX rack, ported literally from <c>src/lib/effects.ts</c>. Every method
    /// mutates its buffer in place and writes float samples, matching <c>Float32Array</c>.
    /// </summary>
    internal static class FxChain
    {
        internal const double PeakCeiling = 0.995;
        internal const double MaxTailSeconds = 3.0;
        internal const double MaxRenderSeconds = 6.0;

        private const double DelayMinSeconds = 0.03;
        private const double DelayMaxSeconds = 0.4;
        private const double DelayMaxFeedback = 0.85;
        private const double DelayDamping = 0.3;

        private const double ToneCutoffHz = 1000.0;
        private const double ToneAmount = 0.9;

        private static readonly double[] ReverbCombDelaysMs = { 29.7, 37.1, 41.1, 43.7 };
        private static readonly double[] ReverbAllpassDelaysMs = { 5.0, 1.7 };
        private const double ReverbAllpassFeedback = 0.5;
        private const double ReverbCombDamping = 0.35;
        private const double ReverbCombSpread = 0.25;

        private const int CrushMaxHoldSamples = 40;
        private const int CrushMinBits = 2;

        internal static double FxDelaySeconds(double fxDelayTime)
        {
            return DelayMinSeconds + SfxMath.ClampUnit(fxDelayTime) * (DelayMaxSeconds - DelayMinSeconds);
        }

        /// <summary>Length of the echo/reverb tail that must follow the dry signal.</summary>
        internal static double FxTailSeconds(SfxParams p)
        {
            double delaySeconds = FxDelaySeconds(p.fxDelayTime);
            double delayTail = p.fxDelayMix > 0.0
                ? delaySeconds
                    * (3.0 + SfxMath.ClampUnit(p.fxDelayFeedback) * 4.0)
                    * Math.Min(1.0, SfxMath.ClampUnit(p.fxDelayMix) * 2.0)
                : 0.0;
            double reverbTail = p.fxReverbMix > 0.0
                ? (0.35 + SfxMath.ClampUnit(p.fxReverbSize) * 1.2)
                    * Math.Min(1.0, SfxMath.ClampUnit(p.fxReverbMix) * 2.0)
                : 0.0;

            return Math.Min(MaxTailSeconds, delayTail + reverbTail);
        }

        internal static void ApplyChain(float[] samples, SfxParams p, int sampleRate)
        {
            ApplyDrive(samples, p.fxDrive);
            ApplyBitcrush(samples, p.fxCrushRate, p.fxCrushBits);
            ApplyTone(samples, p.fxTone, sampleRate);
            ApplyDelay(samples, sampleRate, FxDelaySeconds(p.fxDelayTime), p.fxDelayFeedback, p.fxDelayMix);
            ApplyReverb(samples, sampleRate, p.fxReverbSize, p.fxReverbMix);

            if (p.fxNormalize >= 0.5)
            {
                ApplyPeakCeiling(samples, PeakCeiling);
            }

            ApplyFadeOut(samples, sampleRate);
        }

        /// <summary>Soft saturation with makeup gain, blended against the dry signal.</summary>
        internal static void ApplyDrive(float[] samples, double amount)
        {
            double wet = SfxMath.ClampUnit(amount);
            if (wet <= 0.0)
            {
                return;
            }

            double gain = 1.0 + wet * 9.0;
            double normalizer = Math.Tanh(gain);

            for (int index = 0; index < samples.Length; index += 1)
            {
                double dry = samples[index];
                double shaped = Math.Tanh(dry * gain) / normalizer;
                samples[index] = (float)(dry + (shaped - dry) * wet);
            }
        }

        /// <summary>Sample-and-hold decimation plus bit quantization.</summary>
        internal static void ApplyBitcrush(float[] samples, double rateAmount, double bitsAmount)
        {
            int hold = rateAmount > 0.0
                ? Math.Max(1, (int)SfxMath.Round(1.0 + SfxMath.ClampUnit(rateAmount) * (CrushMaxHoldSamples - 1)))
                : 1;
            int bits = bitsAmount > 0.0
                ? Math.Max(CrushMinBits, (int)SfxMath.Round(16.0 - SfxMath.ClampUnit(bitsAmount) * 14.0))
                : 0;

            if (hold <= 1 && bits <= 0)
            {
                return;
            }

            double steps = bits > 0 ? Math.Pow(2.0, bits) : 0.0;
            double halfSteps = steps / 2.0;
            double held = 0.0;

            for (int index = 0; index < samples.Length; index += 1)
            {
                if (index % hold == 0)
                {
                    double sample = samples[index];
                    held = steps > 0.0 ? SfxMath.Round(sample * halfSteps) / halfSteps : sample;
                }

                samples[index] = (float)held;
            }
        }

        /// <summary>One-knob tilt around 1 kHz: negative darkens, positive brightens.</summary>
        internal static void ApplyTone(float[] samples, double tilt, int sampleRate)
        {
            double amount = Math.Max(-1.0, Math.Min(1.0, tilt));
            if (Math.Abs(amount) < 0.005)
            {
                return;
            }

            double alpha = 1.0 - Math.Exp((-2.0 * Math.PI * ToneCutoffHz) / sampleRate);
            double lowPassed = 0.0;

            for (int index = 0; index < samples.Length; index += 1)
            {
                double sample = samples[index];
                lowPassed += (sample - lowPassed) * alpha;
                double highBand = sample - lowPassed;
                samples[index] = (float)(sample + highBand * amount * ToneAmount);
            }
        }

        /// <summary>Feedback echo. The first repeat lands <c>timeSeconds</c> after the dry signal.</summary>
        internal static void ApplyDelay(
            float[] samples,
            int sampleRate,
            double timeSeconds,
            double feedback,
            double mix)
        {
            double wet = SfxMath.ClampUnit(mix);
            if (wet <= 0.0)
            {
                return;
            }

            int delaySamples = Math.Max(1, (int)SfxMath.Round(timeSeconds * sampleRate));
            if (delaySamples >= samples.Length)
            {
                return;
            }

            var buffer = new float[delaySamples];
            double feedbackAmount = Math.Min(DelayMaxFeedback, Math.Max(0.0, feedback));
            int writeIndex = 0;
            double dampingState = 0.0;

            for (int index = 0; index < samples.Length; index += 1)
            {
                double dry = samples[index];
                double echoed = buffer[writeIndex];
                dampingState = echoed * (1.0 - DelayDamping) + dampingState * DelayDamping;
                buffer[writeIndex] = (float)(dry + dampingState * feedbackAmount);
                samples[index] = (float)(dry + echoed * wet);
                writeIndex = (writeIndex + 1) % delaySamples;
            }
        }

        /// <summary>Small mono Schroeder reverb: four damped combs into two allpasses.</summary>
        internal static void ApplyReverb(float[] samples, int sampleRate, double size, double mix)
        {
            double wet = SfxMath.ClampUnit(mix);
            if (wet <= 0.0)
            {
                return;
            }

            double spread = 0.6 + SfxMath.ClampUnit(size) * 0.8;
            double decay = 0.35 + SfxMath.ClampUnit(size) * 0.6;

            var combs = new Comb[ReverbCombDelaysMs.Length];
            for (int index = 0; index < ReverbCombDelaysMs.Length; index += 1)
            {
                combs[index] = new Comb
                {
                    Buffer = new float[Math.Max(1, (int)SfxMath.Round((ReverbCombDelaysMs[index] * spread * sampleRate) / 1000.0))],
                    Index = 0,
                    DampingState = 0.0,
                };
            }

            var allpasses = new Allpass[ReverbAllpassDelaysMs.Length];
            for (int index = 0; index < ReverbAllpassDelaysMs.Length; index += 1)
            {
                allpasses[index] = new Allpass
                {
                    Buffer = new float[Math.Max(1, (int)SfxMath.Round((ReverbAllpassDelaysMs[index] * spread * sampleRate) / 1000.0))],
                    Index = 0,
                };
            }

            var wetSignal = new float[samples.Length];

            for (int index = 0; index < samples.Length; index += 1)
            {
                double input = samples[index];
                double wetSum = 0.0;

                for (int combIndex = 0; combIndex < combs.Length; combIndex += 1)
                {
                    Comb comb = combs[combIndex];
                    double delayed = comb.Buffer[comb.Index];
                    comb.DampingState = delayed * (1.0 - ReverbCombDamping) + comb.DampingState * ReverbCombDamping;
                    comb.Buffer[comb.Index] = (float)(input * ReverbCombSpread + comb.DampingState * decay);
                    comb.Index = (comb.Index + 1) % comb.Buffer.Length;
                    wetSum += delayed;
                }

                double stage = wetSum / combs.Length;

                for (int allpassIndex = 0; allpassIndex < allpasses.Length; allpassIndex += 1)
                {
                    Allpass allpass = allpasses[allpassIndex];
                    double delayed = allpass.Buffer[allpass.Index];
                    double output = -stage + delayed;
                    allpass.Buffer[allpass.Index] = (float)(stage + delayed * ReverbAllpassFeedback);
                    allpass.Index = (allpass.Index + 1) % allpass.Buffer.Length;
                    stage = output;
                }

                wetSignal[index] = (float)stage;
            }

            for (int index = 0; index < samples.Length; index += 1)
            {
                samples[index] = (float)(samples[index] + wetSignal[index] * wet);
            }
        }

        /// <summary>Scales the whole buffer down only when it exceeds the ceiling.</summary>
        internal static void ApplyPeakCeiling(float[] samples, double ceiling = PeakCeiling)
        {
            double peak = 0.0;

            for (int index = 0; index < samples.Length; index += 1)
            {
                double magnitude = Math.Abs(samples[index]);
                if (magnitude > peak)
                {
                    peak = magnitude;
                }
            }

            if (peak <= ceiling || peak == 0.0)
            {
                return;
            }

            double scale = ceiling / peak;
            for (int index = 0; index < samples.Length; index += 1)
            {
                samples[index] = (float)(samples[index] * scale);
            }
        }

        /// <summary>Linear fade over the final milliseconds so exports never click.</summary>
        internal static void ApplyFadeOut(float[] samples, int sampleRate, double milliseconds = 6.0)
        {
            int count = Math.Min(
                samples.Length,
                Math.Max(1, (int)SfxMath.Round((sampleRate * milliseconds) / 1000.0)));
            int start = samples.Length - count;

            for (int offset = 0; offset < count; offset += 1)
            {
                double gain = 1.0 - (offset + 1.0) / count;
                samples[start + offset] = (float)(samples[start + offset] * gain);
            }
        }

        // Reference types: the comb/allpass index and damping state must persist
        // across loop iterations, so structs would mutate copies of the state.
        private sealed class Comb
        {
            internal float[] Buffer;
            internal int Index;
            internal double DampingState;
        }

        private sealed class Allpass
        {
            internal float[] Buffer;
            internal int Index;
        }
    }
}

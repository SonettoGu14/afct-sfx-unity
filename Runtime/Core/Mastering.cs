using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Export-only mastering helpers, ported literally from <c>src/lib/mastering.ts</c>.
    /// The live render is never mastered; these run on the way out.
    /// </summary>
    public static class Mastering
    {
        internal const double DefaultTrimThresholdDb = -60.0;
        internal const double DefaultKeepMs = 5.0;
        internal const double DefaultTailMs = 20.0;
        internal const double MaxTargetPeakDb = -0.1;

        /// <summary>Largest absolute sample value in the buffer.</summary>
        public static double MeasurePeak(float[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            double peak = 0.0;
            for (int index = 0; index < samples.Length; index += 1)
            {
                double value = Math.Abs(samples[index]);
                if (value > peak)
                {
                    peak = value;
                }
            }

            return peak;
        }

        /// <summary>Peak level in dBFS, or negative infinity for silence.</summary>
        public static double MeasurePeakDb(float[] samples)
        {
            return GainToDb(MeasurePeak(samples));
        }

        /// <summary>
        /// Trims leading and trailing silence, keeping a pre-roll before the first
        /// transient and a short tail after the last audible sample.
        /// </summary>
        public static float[] TrimSilence(
            float[] samples,
            int sampleRate,
            double thresholdDb = DefaultTrimThresholdDb,
            double preRollMs = DefaultKeepMs,
            double tailMs = DefaultTailMs)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            double threshold = DbToGain(thresholdDb);
            int keep = Math.Max(0, (int)SfxMath.Round((preRollMs / 1000.0) * sampleRate));
            int tail = Math.Max(0, (int)SfxMath.Round((tailMs / 1000.0) * sampleRate));

            int first = -1;
            int last = -1;

            for (int index = 0; index < samples.Length; index += 1)
            {
                if (Math.Abs(samples[index]) >= threshold)
                {
                    if (first == -1)
                    {
                        first = index;
                    }

                    last = index;
                }
            }

            if (first == -1)
            {
                return new float[0];
            }

            int start = Math.Max(0, first - keep);
            int end = Math.Min(samples.Length, last + 1 + tail);
            int length = Math.Max(0, end - start);
            var trimmed = new float[length];
            Array.Copy(samples, start, trimmed, 0, length);
            return trimmed;
        }

        /// <summary>
        /// Scales the clip so its loudest sample sits at <paramref name="targetPeakDb"/>.
        /// A non-finite target disables the gain stage; the ceiling never exceeds -0.1 dBFS.
        /// </summary>
        public static float[] ApplyTargetPeak(float[] samples, double targetPeakDb)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (!SfxMath.IsFinite(targetPeakDb))
            {
                return samples;
            }

            double peak = MeasurePeak(samples);
            if (peak <= 0.0)
            {
                return samples;
            }

            double ceiling = DbToGain(Math.Min(MaxTargetPeakDb, targetPeakDb));
            double gain = ceiling / peak;
            var scaled = new float[samples.Length];

            for (int index = 0; index < samples.Length; index += 1)
            {
                scaled[index] = (float)Math.Max(-1.0, Math.Min(1.0, samples[index] * gain));
            }

            return scaled;
        }

        /// <summary>Applies the export pipeline: optional trim, then optional peak targeting.</summary>
        public static float[] MasterForExport(
            float[] samples,
            int sampleRate,
            bool trim,
            double targetPeakDb)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            float[] trimmed = trim ? TrimSilence(samples, sampleRate) : samples;
            return ApplyTargetPeak(trimmed, targetPeakDb);
        }

        internal static double DbToGain(double db)
        {
            return Math.Pow(10.0, db / 20.0);
        }

        internal static double GainToDb(double gain)
        {
            return gain > 0.0 ? 20.0 * Math.Log10(gain) : double.NegativeInfinity;
        }
    }
}

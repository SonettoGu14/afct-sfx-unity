using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// The public render entry point. Mirrors <c>renderSfx</c> in <c>src/lib/sfx.ts</c>:
    /// clamp, dispatch, pad the tail, cap at six seconds, run the FX chain, then clamp.
    /// </summary>
    public static class SfxLabEngine
    {
        /// <summary>Renders one sound with the given options.</summary>
        public static SfxRenderResult Render(SfxParams p, SfxRenderOptions o = null)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }

            SfxRenderOptions options = o ?? new SfxRenderOptions();
            Mulberry32 random = SfxRandom.ForSeed(options.Seed);
            SfxParams safeParams = SfxClamp.Apply(p);
            float[] baseSamples = safeParams.engine == SfxEngine.Footsteppr
                ? FootstepRenderer.Render(safeParams, options, random)
                : SfxrRenderer.Render(safeParams, options, random);

            return Finalize(baseSamples, safeParams, options);
        }

        /// <summary>Clamps every parameter into its valid range, merging onto defaults.</summary>
        public static SfxParams Clamp(SfxParams p)
        {
            return SfxClamp.Apply(p);
        }

        /// <summary>Serializes the parameters to indented JSON, like <c>JSON.stringify(p, null, 2)</c>.</summary>
        public static string Serialize(SfxParams p)
        {
            return SfxJson.Write(p);
        }

        /// <summary>Parses JSON produced by <see cref="Serialize"/> (tolerant, clamped).</summary>
        public static SfxParams Deserialize(string json)
        {
            return SfxClamp.Apply(SfxJson.Parse(json));
        }

        private static SfxRenderResult Finalize(
            float[] baseSamples,
            SfxParams p,
            SfxRenderOptions options)
        {
            double tailSamples = Math.Max(0.0, SfxMath.Round(FxChain.FxTailSeconds(p) * options.SampleRate));
            double maxSamples = SfxMath.Round(FxChain.MaxRenderSeconds * options.SampleRate);
            double totalSamples = Math.Min(maxSamples, baseSamples.Length + tailSamples);
            int total = (int)totalSamples;

            var samples = new float[total];
            Array.Copy(baseSamples, samples, Math.Min(baseSamples.Length, total));

            FxChain.ApplyChain(samples, p, options.SampleRate);

            long clippedSamples = 0;
            double peak = 0.0;

            for (int index = 0; index < samples.Length; index += 1)
            {
                double sample = samples[index];
                if (Math.Abs(sample) > 1.0)
                {
                    clippedSamples += 1;
                }

                double clampedSample = SfxMath.Clamp(sample, -1.0, 1.0);
                peak = Math.Max(peak, Math.Abs(clampedSample));
                samples[index] = (float)clampedSample;
            }

            return new SfxRenderResult
            {
                Samples = samples,
                Stats = new SfxRenderStats
                {
                    DurationSeconds = totalSamples / options.SampleRate,
                    Samples = total,
                    ClippedSamples = clippedSamples,
                    Peak = peak,
                    EstimatedByteSize = 44L + (long)totalSamples * (options.BitDepth / 8),
                },
            };
        }
    }
}

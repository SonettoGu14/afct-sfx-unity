namespace AFCT.SfxLab
{
    /// <summary>Options for a single render.</summary>
    public sealed class SfxRenderOptions
    {
        /// <summary>Output sample rate in Hz.</summary>
        public int SampleRate = 44100;

        /// <summary>PCM bit depth used for the reported byte size and WAV export.</summary>
        public int BitDepth = 16;

        /// <summary>
        /// When set, every render-time random draw comes from <c>mulberry32(Seed)</c>,
        /// so noise components are reproducible. When null a shared unseeded source is used.
        /// </summary>
        public int? Seed;
    }

    /// <summary>Read-only measurements describing a rendered buffer.</summary>
    public sealed class SfxRenderStats
    {
        public double DurationSeconds;
        public long Samples;
        public long ClippedSamples;
        public double Peak;
        public long EstimatedByteSize;
    }

    /// <summary>A rendered mono buffer plus its measurements.</summary>
    public sealed class SfxRenderResult
    {
        public float[] Samples;
        public SfxRenderStats Stats;
    }
}

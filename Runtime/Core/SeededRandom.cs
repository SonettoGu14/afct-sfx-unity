using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Mulberry32, bit-for-bit identical to <c>mulberry32</c> in <c>src/lib/prng.ts</c>.
    /// </summary>
    public sealed class Mulberry32
    {
        private uint _state;

        public Mulberry32(uint seed)
        {
            _state = seed;
        }

        /// <summary>Current 32-bit state (advances on every draw).</summary>
        public uint State
        {
            get { return _state; }
        }

        /// <summary>Draws the next value in [0, 1).</summary>
        public double NextUnit()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint value = _state;
                value = (value ^ (value >> 15)) * (value | 1u);
                uint mixed = value;
                value = mixed ^ (mixed + (mixed ^ (mixed >> 7)) * (mixed | 61u));
                return ((value ^ (value >> 14)) / 4294967296.0);
            }
        }

        /// <summary>Draws the next value in [-1, 1), matching <c>randomSigned()</c>.</summary>
        public double NextSigned()
        {
            return NextUnit() * 2.0 - 1.0;
        }

        /// <summary>Uniform draw in [min, max), matching <c>randomRange()</c>.</summary>
        public double NextRange(double min, double max)
        {
            return min + NextUnit() * (max - min);
        }
    }

    /// <summary>Deterministic seed helpers mirroring <c>src/lib/prng.ts</c>.</summary>
    public static class SeededRandom
    {
        /// <summary>
        /// Draws <paramref name="count"/> successive values from <c>mulberry32(seed)</c>.
        /// </summary>
        public static double[] Sequence(uint seed, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var random = new Mulberry32(seed);
            var values = new double[count];
            for (int index = 0; index < count; index += 1)
            {
                values[index] = random.NextUnit();
            }

            return values;
        }

        /// <summary>FNV-1a hash of a string, matching <c>hashSeed</c>.</summary>
        public static uint HashSeed(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            unchecked
            {
                uint hash = 0x811C9DC5u;
                for (int index = 0; index < input.Length; index += 1)
                {
                    hash ^= input[index];
                    hash *= 0x01000193u;
                }

                return hash;
            }
        }

        /// <summary>A fresh non-deterministic 32-bit seed, matching <c>randomSeed</c>.</summary>
        public static uint RandomSeed()
        {
            return (uint)SfxRandom.Range(0xFFFFFFFFu);
        }
    }

    /// <summary>Holds the ambient source used when no explicit seed is supplied.</summary>
    internal static class SfxRandom
    {
        private static readonly Random s_entropy = new Random();
        private static readonly Mulberry32 s_shared = CreateShared();

        internal static Mulberry32 Shared
        {
            get { return s_shared; }
        }

        internal static int NextInt()
        {
            lock (s_entropy)
            {
                return s_entropy.Next(0, int.MaxValue);
            }
        }

        internal static uint Range(uint max)
        {
            double value = 0;
            lock (s_entropy)
            {
                value = Math.Floor(s_entropy.NextDouble() * max);
            }

            return unchecked((uint)(long)value);
        }

        internal static Mulberry32 ForSeed(int? seed)
        {
            return seed.HasValue ? new Mulberry32(unchecked((uint)seed.Value)) : s_shared;
        }

        private static Mulberry32 CreateShared()
        {
            long ticks = DateTime.UtcNow.Ticks;
            int salt = NextInt();
            return new Mulberry32(unchecked((uint)ticks) ^ unchecked((uint)salt));
        }
    }
}

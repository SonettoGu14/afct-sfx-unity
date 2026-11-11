using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Numeric behaviour shared by the engine, matching the JavaScript helpers in
    /// <c>src/lib/sfx.ts</c>, <c>src/lib/effects.ts</c> and <c>src/lib/mastering.ts</c>.
    /// </summary>
    internal static class SfxMath
    {
        internal const double Pi = Math.PI;
        internal const double TwoPi = Math.PI * 2.0;

        /// <summary>JavaScript <c>Number.isFinite</c>.</summary>
        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>
        /// JavaScript <c>Math.round</c>: halfway cases round toward positive infinity
        /// (unlike .NET's banker's rounding).
        /// </summary>
        internal static double Round(double value)
        {
            return Math.Floor(value + 0.5);
        }

        /// <summary>The engine's clamp: a non-finite value falls back to <paramref name="min"/>.</summary>
        internal static double Clamp(double value, double min, double max)
        {
            if (!IsFinite(value))
            {
                return min;
            }

            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>The FX chain's clamp: a non-finite value becomes zero.</summary>
        internal static double ClampUnit(double value)
        {
            if (!IsFinite(value))
            {
                return 0.0;
            }

            return Math.Min(1.0, Math.Max(0.0, value));
        }

        /// <summary>JavaScript <c>Math.sign</c> as a number (negative zero becomes zero).</summary>
        internal static double Sign(double value)
        {
            return Math.Sign(value);
        }
    }
}

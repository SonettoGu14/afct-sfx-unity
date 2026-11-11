using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Pure-data signal primitives from <c>src/lib/sfx.ts</c>. The web versions operate on
    /// <c>Float32Array</c>, so every result is rounded to a float here as well; local
    /// accumulators stay double, exactly like the JS numbers feeding the writes.
    /// </summary>
    internal static class PdSignals
    {
        internal const int CosTableSize = 2048;
        private static readonly float[] CosTable = CreateCosTable();

        internal static float[] Constant(int length, double value)
        {
            var signal = new float[length];
            float quantized = (float)value;
            for (int index = 0; index < length; index += 1)
            {
                signal[index] = quantized;
            }

            return signal;
        }

        internal static float[] Noise(int length, Mulberry32 random)
        {
            var signal = new float[length];
            for (int index = 0; index < length; index += 1)
            {
                signal[index] = (float)random.NextSigned();
            }

            return signal;
        }

        internal static float[] Add(float[] left, float[] right)
        {
            var result = new float[left.Length];
            for (int index = 0; index < left.Length; index += 1)
            {
                result[index] = (float)(At(left, index) + At(right, index));
            }

            return result;
        }

        internal static float[] Mul(float[] left, float[] right)
        {
            var result = new float[left.Length];
            for (int index = 0; index < left.Length; index += 1)
            {
                result[index] = (float)(At(left, index) * At(right, index));
            }

            return result;
        }

        internal static float[] MulScalar(float[] signal, double value)
        {
            var result = new float[signal.Length];
            for (int index = 0; index < signal.Length; index += 1)
            {
                result[index] = (float)(At(signal, index) * value);
            }

            return result;
        }

        internal static float[] Div(float[] left, float[] right)
        {
            var result = new float[left.Length];
            for (int index = 0; index < left.Length; index += 1)
            {
                result[index] = (float)(At(left, index) / At(right, index));
            }

            return result;
        }

        internal static float[] Clip(float[] signal, double min, double max)
        {
            var result = new float[signal.Length];
            for (int index = 0; index < signal.Length; index += 1)
            {
                double sample = At(signal, index);
                if (double.IsNaN(sample))
                {
                    result[index] = 0f;
                }
                else if (double.IsPositiveInfinity(sample))
                {
                    result[index] = (float)max;
                }
                else if (double.IsNegativeInfinity(sample))
                {
                    result[index] = (float)min;
                }
                else if (sample > max)
                {
                    result[index] = (float)max;
                }
                else if (sample < min)
                {
                    result[index] = (float)min;
                }
                else
                {
                    result[index] = (float)sample;
                }
            }

            return result;
        }

        internal static float[] Osc(float[] frequencySignal, int sampleRate)
        {
            var result = new float[frequencySignal.Length];
            double conversionFactor = (2.0 * Math.PI) / sampleRate;
            double phase = 0.0;

            for (int index = 0; index < frequencySignal.Length; index += 1)
            {
                phase += At(frequencySignal, index) * conversionFactor;
                while (phase >= Math.PI)
                {
                    phase -= Math.PI * 2.0;
                }

                while (phase < -Math.PI)
                {
                    phase += Math.PI * 2.0;
                }

                double tableIndex = ((phase + Math.PI) / (Math.PI * 2.0)) * CosTableSize;
                double indexFloor = Math.Floor(tableIndex);
                int floorInt = (int)indexFloor;
                int wrappedIndex = ((floorInt % CosTableSize) + CosTableSize) % CosTableSize;
                int nextIndex = (wrappedIndex + 1) % CosTableSize;
                double fraction = tableIndex - indexFloor;
                double valueA = CosTable[wrappedIndex];
                double valueB = CosTable[nextIndex];
                result[index] = (float)(valueA + fraction * (valueB - valueA));
            }

            return result;
        }

        internal static float[] Lop(float[] signal, float[] cutoffSignal, int sampleRate)
        {
            var result = new float[signal.Length];
            double conversionFactor = (2.0 * Math.PI) / sampleRate;
            double last = signal.Length > 0 ? signal[0] : 0.0;

            for (int index = 0; index < signal.Length; index += 1)
            {
                double coefficient = At(cutoffSignal, index) * conversionFactor;
                coefficient = SfxMath.Clamp(coefficient, 0.0, 1.0);
                last = coefficient * At(signal, index) + (1.0 - coefficient) * last;
                result[index] = (float)last;
            }

            return result;
        }

        internal static float[] Hip(float[] signal, float[] cutoffSignal, int sampleRate)
        {
            var result = new float[signal.Length];
            double conversionFactor = (2.0 * Math.PI) / sampleRate;
            double last = signal.Length > 0 ? signal[0] : 0.0;

            for (int index = 0; index < signal.Length; index += 1)
            {
                double frequency = At(cutoffSignal, index);
                double coefficient = 1.0 - frequency * conversionFactor;
                coefficient = SfxMath.Clamp(coefficient, 0.0, 1.0);

                if (coefficient < 1.0)
                {
                    double normal = 0.5 * (1.0 + coefficient);
                    double current = At(signal, index) + coefficient * last;
                    result[index] = (float)(normal * (current - last));
                    last = current;
                }
                else
                {
                    result[index] = (float)At(signal, index);
                }
            }

            return result;
        }

        internal static float[] Vcf(float[] signal, float[] resonanceFrequencySignal, float[] qSignal, int sampleRate)
        {
            var result = new float[signal.Length];
            double conversionFactor = (2.0 * Math.PI) / sampleRate;
            double real = 0.0;
            double imaginary = 0.0;

            for (int index = 0; index < signal.Length; index += 1)
            {
                double q = At(qSignal, index);
                double qInverse = q > 0.0 ? 1.0 / q : 0.0;
                double amplitudeCorrection = 2.0 - 2.0 / (q + 2.0);
                double angularFrequency = Math.Max(0.0, At(resonanceFrequencySignal, index) * conversionFactor);
                double resonance = qInverse > 0.0 ? 1.0 - angularFrequency * qInverse : 0.0;
                resonance = Math.Max(0.0, resonance);
                double oneMinusResonance = 1.0 - resonance;
                double coefficientReal = resonance * CosLookup(angularFrequency);
                double coefficientImaginary = resonance * SinLookup(angularFrequency);
                double previousReal = real;

                real = amplitudeCorrection * oneMinusResonance * At(signal, index)
                    + coefficientReal * previousReal
                    - coefficientImaginary * imaginary;
                imaginary = coefficientImaginary * previousReal + coefficientReal * imaginary;

                if (Math.Abs(real) < 0.0000000001)
                {
                    real = 0.0;
                }

                if (Math.Abs(imaginary) < 0.0000000001)
                {
                    imaginary = 0.0;
                }

                result[index] = (float)real;
            }

            return result;
        }

        internal static double CosLookup(double angle)
        {
            double tableIndex = angle * (CosTableSize / 6.28318);
            double indexFloor = Math.Floor(tableIndex);
            int floorInt = (int)indexFloor;
            int wrappedIndex = ((floorInt % CosTableSize) + CosTableSize) % CosTableSize;
            int nextIndex = wrappedIndex + 1;
            double fraction = tableIndex - indexFloor;
            double valueA = CosTable[wrappedIndex];
            double valueB = CosTable[nextIndex];
            return valueA + fraction * (valueB - valueA);
        }

        internal static double SinLookup(double angle)
        {
            double shiftedAngle = angle - Math.PI / 2.0;
            return CosLookup(shiftedAngle);
        }

        internal static float[] Resample(float[] signal, int fromSampleRate, int toSampleRate)
        {
            if (signal.Length <= 1 || fromSampleRate == toSampleRate)
            {
                return signal;
            }

            int nextLength = Math.Max(1, (int)SfxMath.Round(signal.Length * ((double)toSampleRate / fromSampleRate)));
            var result = new float[nextLength];

            for (int index = 0; index < nextLength; index += 1)
            {
                double sourcePosition = ((double)index / Math.Max(nextLength - 1, 1)) * Math.Max(signal.Length - 1, 0);
                int leftIndex = (int)Math.Floor(sourcePosition);
                int rightIndex = Math.Min(signal.Length - 1, leftIndex + 1);
                double fraction = sourcePosition - leftIndex;
                double leftValue = At(signal, leftIndex);
                double rightValue = rightIndex >= 0 ? At(signal, rightIndex) : leftValue;
                result[index] = (float)(leftValue + (rightValue - leftValue) * fraction);
            }

            return result;
        }

        private static double At(float[] signal, int index)
        {
            return index >= 0 && index < signal.Length ? signal[index] : 0.0;
        }

        private static float[] CreateCosTable()
        {
            var table = new float[CosTableSize + 1];
            for (int index = 0; index < CosTableSize; index += 1)
            {
                table[index] = (float)Math.Cos((2.0 * Math.PI * index) / CosTableSize);
            }

            table[0] = 1f;
            table[CosTableSize] = 1f;
            table[(int)Math.Floor(CosTableSize / 4.0)] = 0f;
            table[(int)Math.Floor((3.0 * CosTableSize) / 4.0)] = 0f;
            table[(int)Math.Floor(CosTableSize / 2.0)] = -1f;
            return table;
        }
    }
}

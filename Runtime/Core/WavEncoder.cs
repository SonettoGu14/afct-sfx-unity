using System;

namespace AFCT.SfxLab
{
    /// <summary>Mono PCM WAV encoding, matching <c>encodeSfxWavBytes</c> in <c>src/lib/sfx.ts</c>.</summary>
    public static class WavEncoder
    {
        /// <summary>Encodes mono samples into a complete RIFF/WAVE byte array.</summary>
        public static byte[] Encode(float[] samples, int sampleRate, int bitDepth)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (bitDepth != 8 && bitDepth != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bitDepth), "Only 8 and 16 bit PCM are supported.");
            }

            const int channelCount = 1;
            int bytesPerSample = bitDepth / 8;
            int byteRate = sampleRate * channelCount * bytesPerSample;
            int dataSize = samples.Length * bytesPerSample;
            var buffer = new byte[44 + dataSize];

            WriteAscii(buffer, 0, "RIFF");
            WriteUInt32(buffer, 4, (uint)(36 + dataSize));
            WriteAscii(buffer, 8, "WAVE");
            WriteAscii(buffer, 12, "fmt ");
            WriteUInt32(buffer, 16, 16u);
            WriteUInt16(buffer, 20, 1);
            WriteUInt16(buffer, 22, channelCount);
            WriteUInt32(buffer, 24, (uint)sampleRate);
            WriteUInt32(buffer, 28, (uint)byteRate);
            WriteUInt16(buffer, 32, (ushort)(channelCount * bytesPerSample));
            WriteUInt16(buffer, 34, (ushort)bitDepth);
            WriteAscii(buffer, 36, "data");
            WriteUInt32(buffer, 40, (uint)dataSize);

            int offset = 44;
            for (int index = 0; index < samples.Length; index += 1)
            {
                double sample = SfxMath.Clamp(samples[index], -1.0, 1.0);
                if (bitDepth == 16)
                {
                    double value = sample < 0.0
                        ? SfxMath.Round(sample * 0x8000)
                        : SfxMath.Round(sample * 0x7FFF);
                    short quantized = unchecked((short)(int)value);
                    buffer[offset] = (byte)(quantized & 0xFF);
                    buffer[offset + 1] = (byte)((quantized >> 8) & 0xFF);
                    offset += 2;
                }
                else
                {
                    double value = SfxMath.Round((sample * 0.5 + 0.5) * 255.0);
                    buffer[offset] = (byte)SfxMath.Clamp(value, 0.0, 255.0);
                    offset += 1;
                }
            }

            return buffer;
        }

        private static void WriteAscii(byte[] buffer, int offset, string value)
        {
            for (int index = 0; index < value.Length; index += 1)
            {
                buffer[offset + index] = (byte)value[index];
            }
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }
}

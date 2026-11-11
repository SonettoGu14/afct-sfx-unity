using System;
using System.IO;
using AFCT.SfxLab;
using NUnit.Framework;

namespace AFCT.SfxLab.Tests
{
    /// <summary>
    /// Compares every rendered fixture against the TypeScript engine's golden float stream.
    /// </summary>
    public sealed class GoldenAudioTests
    {
        private const double MaxSampleDifference = 1e-4;

        [Test]
        public void FixtureTableIsComplete()
        {
            Assert.AreEqual(12, AudioFixtures.All.Length);
        }

        [Test]
        public void EveryAudioFixtureMatchesGolden()
        {
            string goldenDirectory = FindGoldenDirectory();

            foreach (AudioFixtures.Case fixture in AudioFixtures.All)
            {
                string path = Path.Combine(goldenDirectory, fixture.Id + ".f32");
                Assert.IsTrue(File.Exists(path), "Missing golden file: " + path);

                float[] expected = ReadFloat32(path);
                SfxRenderResult result = SfxLabEngine.Render(
                    fixture.Params,
                    new SfxRenderOptions
                    {
                        SampleRate = fixture.SampleRate,
                        BitDepth = fixture.BitDepth,
                        Seed = fixture.Seed,
                    });

                Assert.AreEqual(
                    expected.Length,
                    result.Samples.Length,
                    fixture.Id + ": sample count");

                int differingIndex = -1;
                double worstDifference = 0.0;
                for (int index = 0; index < expected.Length; index += 1)
                {
                    double difference = Math.Abs((double)expected[index] - result.Samples[index]);
                    if (double.IsNaN(difference) || difference > MaxSampleDifference)
                    {
                        differingIndex = index;
                        worstDifference = difference;
                        break;
                    }

                    if (difference > worstDifference)
                    {
                        worstDifference = difference;
                    }
                }

                if (differingIndex >= 0)
                {
                    Assert.Fail(
                        fixture.Id + ": sample " + differingIndex + " expected "
                        + expected[differingIndex].ToString("R") + " actual "
                        + result.Samples[differingIndex].ToString("R") + " (|diff| = "
                        + worstDifference.ToString("R") + ")");
                }

                Assert.AreEqual(
                    expected.Length,
                    (int)result.Stats.Samples,
                    fixture.Id + ": stats sample count");
                Assert.AreEqual(
                    expected.Length / (double)fixture.SampleRate,
                    result.Stats.DurationSeconds,
                    1e-12,
                    fixture.Id + ": stats duration");
                Assert.AreEqual(
                    44L + (long)expected.Length * (fixture.BitDepth / 8),
                    result.Stats.EstimatedByteSize,
                    fixture.Id + ": stats byte size");
                Assert.IsTrue(
                    result.Stats.Peak >= 0.0 && result.Stats.Peak <= 1.0,
                    fixture.Id + ": peak out of range (" + result.Stats.Peak + ")");
            }
        }

        [Test]
        public void SameSeedRendersIdentically()
        {
            AudioFixtures.Case fixture = AudioFixtures.All[0];
            var options = new SfxRenderOptions
            {
                SampleRate = fixture.SampleRate,
                BitDepth = fixture.BitDepth,
                Seed = 1234,
            };

            float[] first = SfxLabEngine.Render(fixture.Params, options).Samples;
            float[] second = SfxLabEngine.Render(fixture.Params, options).Samples;

            Assert.AreEqual(first.Length, second.Length);
            for (int index = 0; index < first.Length; index += 1)
            {
                if (first[index] != second[index])
                {
                    Assert.Fail("Seeded render differs at sample " + index);
                }
            }
        }

        private static float[] ReadFloat32(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Assert.AreEqual(0, bytes.Length % 4, "Golden file length is not a multiple of 4: " + path);

            var samples = new float[bytes.Length / 4];
            for (int index = 0; index < samples.Length; index += 1)
            {
                samples[index] = BitConverter.ToSingle(bytes, index * 4);
            }

            return samples;
        }

        private static string FindGoldenDirectory()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Tests", "EditMode", "Golden");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate Tests/EditMode/Golden walking up from " + AppContext.BaseDirectory);
        }
    }
}

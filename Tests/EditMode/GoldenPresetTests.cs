using System;
using System.Reflection;
using AFCT.SfxLab;
using NUnit.Framework;

namespace AFCT.SfxLab.Tests
{
    /// <summary>
    /// Compares every preset recipe against the TypeScript engine's golden output.
    /// </summary>
    public sealed class GoldenPresetTests
    {
        [Test]
        public void FixtureTableIsComplete()
        {
            Assert.AreEqual(84, PresetFixtures.All.Length);
        }

        [Test]
        public void EveryPresetFixtureMatches()
        {
            foreach (PresetFixtures.Case fixture in PresetFixtures.All)
            {
                SfxPresetId preset = (SfxPresetId)Enum.Parse(typeof(SfxPresetId), fixture.Preset);
                SfxParams actual = SfxPresets.Create(preset, null, fixture.Seed);
                SfxAssert.ParamsEqual(fixture.Preset + "/" + fixture.Seed, fixture.Params, actual);
            }
        }

        [Test]
        public void EveryPresetAndSeedCombinationIsCovered()
        {
            int[] seeds = { 1234, 42, 987654321 };
            Assert.AreEqual(28, SfxPresets.Order.Length);
            foreach (SfxPresetId preset in SfxPresets.Order)
            {
                foreach (int seed in seeds)
                {
                    bool found = false;
                    foreach (PresetFixtures.Case fixture in PresetFixtures.All)
                    {
                        if (fixture.Preset == preset.ToString() && fixture.Seed == seed)
                        {
                            found = true;
                            break;
                        }
                    }

                    Assert.IsTrue(found, "Missing fixture for " + preset + " seed " + seed);
                }
            }
        }
    }

    /// <summary>Field-by-field parameter assertions shared by the golden and behavioural tests.</summary>
    internal static class SfxAssert
    {
        internal static void ParamsEqual(string label, SfxParams expected, SfxParams actual)
        {
            Assert.IsTrue(expected != null, label + ": expected params missing");
            Assert.IsTrue(actual != null, label + ": actual params missing");

            FieldInfo[] fields = typeof(SfxParams).GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.IsTrue(fields.Length > 0, label + ": SfxParams has no public instance fields");

            foreach (FieldInfo field in fields)
            {
                object expectedValue = field.GetValue(expected);
                object actualValue = field.GetValue(actual);

                if (expectedValue is double expectedDouble)
                {
                    double actualDouble = (double)actualValue;
                    double tolerance = 1e-9 * Math.Max(1.0, Math.Abs(expectedDouble));
                    Assert.AreEqual(
                        expectedDouble,
                        actualDouble,
                        tolerance,
                        label + "." + field.Name);
                }
                else
                {
                    Assert.AreEqual(expectedValue, actualValue, label + "." + field.Name);
                }
            }
        }
    }
}

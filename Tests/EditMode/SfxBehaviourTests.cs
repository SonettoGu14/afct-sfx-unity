using System;
using AFCT.SfxLab;
using NUnit.Framework;

namespace AFCT.SfxLab.Tests
{
    /// <summary>
    /// Behavioural checks for clamping, serialization, preset metadata and the supporting utilities.
    /// </summary>
    public sealed class SfxBehaviourTests
    {
        [Test]
        public void ClampPullsOutOfRangeValuesIntoBounds()
        {
            var p = new SfxParams { attack = 5.0, decay = -5.0, startFrequency = 2.0, fxTone = -9.0 };
            SfxParams clamped = SfxLabEngine.Clamp(p);

            Assert.AreEqual(1.0, clamped.attack, 1e-12);
            Assert.AreEqual(0.0, clamped.decay, 1e-12);
            Assert.AreEqual(1.0, clamped.startFrequency, 1e-12);
            Assert.AreEqual(-1.0, clamped.fxTone, 1e-12);
        }

        [Test]
        public void ClampTreatsNonFiniteValuesAsTheirMinimum()
        {
            var p = new SfxParams
            {
                attack = double.NaN,
                decay = double.PositiveInfinity,
                sustain = double.NegativeInfinity,
                masterVolume = double.NaN,
            };

            SfxParams clamped = SfxLabEngine.Clamp(p);

            // clamp() maps every non-finite value to the field minimum, +Infinity included.
            Assert.AreEqual(0.0, clamped.attack, 1e-12);
            Assert.AreEqual(0.0, clamped.decay, 1e-12);
            Assert.AreEqual(0.0, clamped.sustain, 1e-12);
            Assert.AreEqual(0.0, clamped.masterVolume, 1e-12);
        }

        [Test]
        public void ClampQuantizesFxNormalizeToZeroOrOne()
        {
            Assert.AreEqual(1.0, SfxLabEngine.Clamp(new SfxParams { fxNormalize = 0.75 }).fxNormalize, 1e-12);
            Assert.AreEqual(0.0, SfxLabEngine.Clamp(new SfxParams { fxNormalize = 0.25 }).fxNormalize, 1e-12);
            Assert.AreEqual(1.0, SfxLabEngine.Clamp(new SfxParams { fxNormalize = 0.5 }).fxNormalize, 1e-12);
            Assert.AreEqual(1.0, SfxLabEngine.Clamp(new SfxParams { fxNormalize = double.NaN }).fxNormalize, 1e-12);
        }

        [Test]
        public void ClampIsIdempotent()
        {
            var p = new SfxParams { attack = 3.0, deltaSlide = -4.0, squareDuty = 7.0, fxDelayTime = 42.0 };
            SfxParams once = SfxLabEngine.Clamp(p);
            SfxParams twice = SfxLabEngine.Clamp(once);
            SfxAssert.ParamsEqual("clamp-idempotent", once, twice);
        }

        [Test]
        public void SerializeRoundTripPreservesEveryField()
        {
            SfxParams original = SfxPresets.Create(SfxPresetId.Explosion, null, 1234);
            string json = SfxLabEngine.Serialize(original);
            SfxParams parsed = SfxLabEngine.Deserialize(json);

            SfxAssert.ParamsEqual("serialize-round-trip", original, parsed);
        }

        [Test]
        public void DeserializeEmptyObjectYieldsDefaults()
        {
            SfxParams parsed = SfxLabEngine.Deserialize("{}");
            SfxAssert.ParamsEqual("deserialize-defaults", new SfxParams(), parsed);
        }

        [Test]
        public void SerializeWritesIndentedJsonInFieldOrder()
        {
            string json = SfxLabEngine.Serialize(new SfxParams());
            Assert.IsTrue(json.StartsWith("{\n  \"engine\": ", StringComparison.Ordinal), "Unexpected JSON start: " + json.Substring(0, Math.Min(40, json.Length)));
            Assert.IsTrue(json.EndsWith("\n}", StringComparison.Ordinal), "Unexpected JSON end");
        }

        [Test]
        public void PresetMetadataCoversEveryPreset()
        {
            Assert.AreEqual(28, SfxPresets.Order.Length);
            Assert.AreEqual(28, Enum.GetValues(typeof(SfxPresetId)).Length);

            var seen = new System.Collections.Generic.HashSet<SfxPresetId>();
            foreach (SfxPresetId preset in SfxPresets.Order)
            {
                Assert.IsTrue(seen.Add(preset), "Duplicate preset in Order: " + preset);
                Assert.IsTrue(!string.IsNullOrEmpty(SfxPresets.DisplayName(preset)), "Missing display name: " + preset);

                string category = SfxPresets.Category(preset);
                bool knownCategory = category == "UI"
                    || category == "Action"
                    || category == "Cinematic"
                    || category == "Footsteps"
                    || category == "Utility";
                Assert.IsTrue(knownCategory, "Unknown category for " + preset + ": " + category);
            }

            Assert.AreEqual("Pickup Coin", SfxPresets.DisplayName(SfxPresetId.PickupCoin));
            Assert.AreEqual("Power Up", SfxPresets.DisplayName(SfxPresetId.Powerup));
            Assert.AreEqual("Footstep Metal", SfxPresets.DisplayName(SfxPresetId.FootstepMetal));
            Assert.AreEqual("UI", SfxPresets.Category(SfxPresetId.Click));
            Assert.AreEqual("Action", SfxPresets.Category(SfxPresetId.Explosion));
            Assert.AreEqual("Cinematic", SfxPresets.Category(SfxPresetId.Whoosh));
            Assert.AreEqual("Footsteps", SfxPresets.Category(SfxPresetId.FootstepSnow));
            Assert.AreEqual("Utility", SfxPresets.Category(SfxPresetId.Random));
        }

        [Test]
        public void EveryPresetRendersWithinClampedBounds()
        {
            foreach (SfxPresetId preset in SfxPresets.Order)
            {
                SfxParams created = SfxPresets.Create(preset, null, 1234);
                SfxParams clamped = SfxLabEngine.Clamp(created);
                SfxAssert.ParamsEqual("preset-bounds/" + preset, created, clamped);

                SfxRenderResult result = SfxLabEngine.Render(
                    created,
                    new SfxRenderOptions { SampleRate = 22050, BitDepth = 16, Seed = 1234 });

                Assert.IsTrue(result.Samples.Length > 0, "Empty render for " + preset);
                Assert.IsTrue(result.Stats.Peak <= 1.0, "Peak above 1 for " + preset);
                Assert.AreEqual(result.Samples.Length, (int)result.Stats.Samples, preset + ": stats samples");
                Assert.IsTrue(
                    result.Stats.ClippedSamples >= 0 && result.Stats.ClippedSamples <= result.Samples.Length,
                    preset + ": clipped count out of range");
            }
        }

        [Test]
        public void VariantWithMutatePresetMutatesSeedDeterministically()
        {
            var current = new SfxParams { attack = 0.5, decay = 0.5 };
            SfxParams first = SfxPresets.CreateVariant(current, (int)SfxPresetId.Mutate, 42);
            SfxParams second = SfxPresets.CreateVariant(current, (int)SfxPresetId.Mutate, 42);
            SfxParams different = SfxPresets.CreateVariant(current, (int)SfxPresetId.Mutate, 43);

            SfxAssert.ParamsEqual("mutate-deterministic", first, second);
            Assert.IsTrue(first.attack != different.attack || first.decay != different.decay, "Different seeds produced identical mutations");
        }

        [Test]
        public void SeededRandomSequenceIsDeterministic()
        {
            double[] first = SeededRandom.Sequence(1234u, 5);
            double[] second = SeededRandom.Sequence(1234u, 5);
            double[] other = SeededRandom.Sequence(4321u, 5);

            Assert.AreEqual(5, first.Length);
            for (int index = 0; index < first.Length; index += 1)
            {
                Assert.AreEqual(first[index], second[index], 0.0, "Sequence differs at " + index);
                Assert.IsTrue(first[index] >= 0.0 && first[index] < 1.0, "Value out of [0,1) at " + index);
            }

            Assert.IsTrue(first[0] != other[0], "Different seeds produced the same first value");
        }

        [Test]
        public void WavEncoderWritesMonoRiffHeaderAndSamples()
        {
            float[] samples = { 0.0f, 0.5f, -0.5f, 1.0f, -1.0f };

            byte[] pcm16 = WavEncoder.Encode(samples, 44100, 16);
            Assert.AreEqual(44 + samples.Length * 2, pcm16.Length);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(pcm16, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(pcm16, 8, 4));
            Assert.AreEqual("data", System.Text.Encoding.ASCII.GetString(pcm16, 36, 4));
            Assert.AreEqual(44100, BitConverter.ToInt32(pcm16, 24));
            Assert.AreEqual(16, BitConverter.ToInt32(pcm16, 16));
            Assert.AreEqual(16, BitConverter.ToInt16(pcm16, 34));

            byte[] pcm8 = WavEncoder.Encode(samples, 22050, 8);
            Assert.AreEqual(44 + samples.Length, pcm8.Length);
            Assert.AreEqual(8, BitConverter.ToInt16(pcm8, 34));
        }
    }
}

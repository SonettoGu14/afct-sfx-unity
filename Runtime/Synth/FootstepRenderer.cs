using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// The footstep engine, ported literally from <c>renderFootstepprSamples</c> and the
    /// terrain graphs in <c>src/lib/sfx.ts</c>. All eight terrains run at an internal
    /// 44.1 kHz and are linearly resampled to the requested rate.
    /// </summary>
    internal static class FootstepRenderer
    {
        private const int InternalSampleRate = 44100;

        internal static float[] Render(SfxParams p, SfxRenderOptions options, Mulberry32 random)
        {
            double durationSeconds = 0.1 + 0.7 * (1.0 - p.footstepSwiftness);
            int totalSamples = Math.Max(1, (int)Math.Floor(durationSeconds * InternalSampleRate));
            float[] envelope = CreateFootstepEnvelopeSignal(totalSamples, InternalSampleRate, p);
            float[] rawSignal = RenderTerrain(p.footstepTerrain, envelope, InternalSampleRate, random);
            float[] scaledSignal = PdSignals.MulScalar(rawSignal, p.masterVolume);
            float[] boostedSignal = PdSignals.MulScalar(scaledSignal, 4.0);

            return options.SampleRate == InternalSampleRate
                ? boostedSignal
                : PdSignals.Resample(boostedSignal, InternalSampleRate, options.SampleRate);
        }

        private static float[] CreateFootstepEnvelopeSignal(int totalSamples, int sampleRate, SfxParams p)
        {
            double stepLength = (double)totalSamples / sampleRate;
            Func<double, double> heelEnvelope = ResizeFn(StepFn(p.footstepHeel), 0.0, 1.0, 0.0, 0.3333);
            Func<double, double> rollEnvelope = ResizeFn(StepFn(p.footstepRoll), 0.0, 1.0, 0.125, 0.875);
            Func<double, double> ballEnvelope = ResizeFn(StepFn(p.footstepBall), 0.0, 1.0, 0.6667, 1.0);
            Func<double, double> baseEnvelope = time => heelEnvelope(time) + rollEnvelope(time) + ballEnvelope(time);
            Func<double, double> resizedEnvelope = ResizeFn(baseEnvelope, 0.0, 1.0, 0.0, stepLength);
            var envelope = new float[totalSamples];

            for (int index = 0; index < totalSamples; index += 1)
            {
                envelope[index] = (float)resizedEnvelope(index / (double)sampleRate);
            }

            return envelope;
        }

        private static float[] RenderTerrain(
            SfxFootstepTerrain terrain,
            float[] envelope,
            int sampleRate,
            Mulberry32 random)
        {
            switch (terrain)
            {
                case SfxFootstepTerrain.Snow:
                    return RenderSnowTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Grass:
                    return RenderGrassTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Dirt:
                    return RenderDirtTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Gravel:
                    return RenderGravelTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Mud:
                    return RenderMudTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Wood:
                    return RenderWoodTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Metal:
                    return RenderMetalTerrain(envelope, sampleRate, random);
                case SfxFootstepTerrain.Water:
                    return RenderWaterTerrain(envelope, sampleRate, random);
                default:
                    return new float[envelope.Length];
            }
        }

        private static float[] RenderSnowTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] crunchNoise = PdSignals.Noise(length, random);
            float[] crunchLow = PdSignals.Lop(crunchNoise, PdSignals.Constant(length, 110), sampleRate);
            float[] crunchHigh = PdSignals.Lop(crunchNoise, PdSignals.Constant(length, 900), sampleRate);
            float[] crunchRatio = PdSignals.Div(crunchLow, crunchHigh);

            float[] powderNoise = PdSignals.Noise(length, random);
            float[] powderBed = PdSignals.Lop(powderNoise, PdSignals.Constant(length, 50), sampleRate);
            float[] powderTop = PdSignals.Lop(powderNoise, PdSignals.Constant(length, 70), sampleRate);
            float[] powderRatio = PdSignals.Div(powderBed, powderTop);

            float[] shapeNoise = PdSignals.Noise(length, random);
            float[] shapeLow = PdSignals.Lop(shapeNoise, PdSignals.Constant(length, 10), sampleRate);
            float[] shapeScaled = PdSignals.MulScalar(shapeLow, 17.0);
            float[] powderShape = PdSignals.Add(
                PdSignals.Mul(shapeScaled, shapeScaled),
                PdSignals.Constant(length, 0.5));

            float[] combined = PdSignals.Mul(PdSignals.Mul(crunchRatio, powderRatio), powderShape);
            float[] clipped = PdSignals.Clip(combined, -1.0, 1.0);
            float[] brightened = PdSignals.Hip(clipped, PdSignals.Constant(length, 300), sampleRate);
            float[] sweep = PdSignals.Add(
                PdSignals.MulScalar(envelope, 9000.0),
                PdSignals.Constant(length, 700));
            float[] resonant = PdSignals.Vcf(
                brightened,
                sweep,
                PdSignals.Constant(length, 0.5),
                sampleRate);
            return PdSignals.MulScalar(PdSignals.Mul(resonant, envelope), 0.2);
        }

        private static float[] RenderGrassTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);
            float[] envelopeFourth = PdSignals.Mul(envelopeSquared, envelopeSquared);
            float[] bodyFrequency = PdSignals.Add(
                PdSignals.MulScalar(envelopeFourth, 600.0),
                PdSignals.Constant(length, 30));
            float[] bodyOsc = PdSignals.Osc(bodyFrequency, sampleRate);
            float[] body = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Clip(bodyOsc, 0.0, 0.5), envelopeFourth),
                0.8);

            float[] rustleNoise = PdSignals.Noise(length, random);
            float[] noiseLow16 = PdSignals.Lop(rustleNoise, PdSignals.Constant(length, 16), sampleRate);
            float[] noiseLow300 = PdSignals.Lop(rustleNoise, PdSignals.Constant(length, 300), sampleRate);
            float[] noiseLow2000 = PdSignals.Lop(rustleNoise, PdSignals.Constant(length, 2000), sampleRate);
            float[] rustleRatio = PdSignals.Div(noiseLow300, noiseLow2000);
            float[] rustleHigh = PdSignals.Hip(rustleRatio, PdSignals.Constant(length, 2500), sampleRate);
            float[] rustleSquared = PdSignals.Mul(rustleHigh, rustleHigh);
            float[] rustleFourth = PdSignals.Mul(rustleSquared, rustleSquared);
            float[] rustleInput = PdSignals.Clip(PdSignals.MulScalar(rustleFourth, 0.00001), -0.9, 0.9);
            float[] rustleSweep = PdSignals.Clip(
                PdSignals.Add(
                    PdSignals.MulScalar(noiseLow16, 23800.0),
                    PdSignals.Constant(length, 3400)),
                2000,
                10000);
            float[] rustleFiltered = PdSignals.Vcf(
                rustleInput,
                rustleSweep,
                PdSignals.Constant(length, 1),
                sampleRate);
            float[] rustle = PdSignals.Mul(
                PdSignals.MulScalar(PdSignals.Hip(rustleFiltered, PdSignals.Constant(length, 900), sampleRate), 0.3),
                envelope);

            return PdSignals.Add(body, rustle);
        }

        private static float[] RenderDirtTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);
            float[] envelopeFourth = PdSignals.Mul(envelopeSquared, envelopeSquared);
            float[] thudFrequency = PdSignals.Add(
                PdSignals.MulScalar(envelopeFourth, 500.0),
                PdSignals.Constant(length, 40));
            float[] thud = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Osc(thudFrequency, sampleRate), envelopeFourth),
                0.5);

            float[] dirtNoise = PdSignals.Noise(length, random);
            float[] dirtDriver = PdSignals.Mul(
                PdSignals.Add(envelope, PdSignals.Constant(length, 0.3)),
                PdSignals.MulScalar(PdSignals.Lop(dirtNoise, PdSignals.Constant(length, 80), sampleRate), 70.0));
            float[] gritFrequency = PdSignals.Add(
                PdSignals.MulScalar(dirtDriver, 70.0),
                PdSignals.Constant(length, 70));
            float[] gritTone = PdSignals.Osc(gritFrequency, sampleRate);
            float[] grit = PdSignals.MulScalar(
                PdSignals.Clip(PdSignals.Hip(gritTone, PdSignals.Constant(length, 200), sampleRate), -1.0, 1.0),
                0.04);

            return PdSignals.Add(thud, grit);
        }

        private static float[] RenderGravelTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] gravelNoise = PdSignals.Noise(length, random);
            float[] low300 = PdSignals.Lop(gravelNoise, PdSignals.Constant(length, 300), sampleRate);
            float[] low2000 = PdSignals.Lop(gravelNoise, PdSignals.Constant(length, 2000), sampleRate);
            float[] gritRatio = PdSignals.Div(low300, low2000);
            float[] gritHigh = PdSignals.Hip(gritRatio, PdSignals.Constant(length, 400), sampleRate);
            float[] gritSquared = PdSignals.Mul(gritHigh, gritHigh);
            float[] gritInput = PdSignals.Clip(PdSignals.MulScalar(gritSquared, 0.01), -0.9, 0.9);
            float[] gritSweep = PdSignals.Clip(
                PdSignals.Add(
                    PdSignals.MulScalar(PdSignals.Lop(gravelNoise, PdSignals.Constant(length, 50), sampleRate), 50000.0),
                    PdSignals.MulScalar(envelope, 1000.0)),
                500,
                10000);
            float[] gritFiltered = PdSignals.Vcf(
                gritInput,
                gritSweep,
                PdSignals.Constant(length, 3),
                sampleRate);
            return PdSignals.Mul(
                PdSignals.MulScalar(PdSignals.Hip(gritFiltered, PdSignals.Constant(length, 200), sampleRate), 2.0),
                envelope);
        }

        private static float[] RenderMudTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);
            float[] envelopeFourth = PdSignals.Mul(envelopeSquared, envelopeSquared);

            float[] squelchNoise = PdSignals.Noise(length, random);
            float[] squelchLow = PdSignals.Lop(squelchNoise, PdSignals.Constant(length, 70), sampleRate);
            float[] squelchMid = PdSignals.Lop(squelchNoise, PdSignals.Constant(length, 260), sampleRate);
            float[] squelchRatio = PdSignals.Div(squelchLow, squelchMid);
            float[] squelchSweep = PdSignals.Clip(
                PdSignals.Add(
                    PdSignals.MulScalar(envelopeFourth, 900.0),
                    PdSignals.Constant(length, 140)),
                90,
                2400);
            float[] squelch = PdSignals.Mul(
                PdSignals.MulScalar(
                    PdSignals.Vcf(
                        PdSignals.Clip(squelchRatio, -1.0, 1.0),
                        squelchSweep,
                        PdSignals.Constant(length, 0.6),
                        sampleRate),
                    0.6),
                envelope);

            float[] bubbleFrequency = PdSignals.Add(
                PdSignals.MulScalar(envelopeSquared, 700.0),
                PdSignals.Constant(length, 55));
            float[] bubble = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Osc(bubbleFrequency, sampleRate), envelopeSquared),
                0.35);

            float[] thudFrequency = PdSignals.Add(
                PdSignals.MulScalar(envelopeFourth, 260.0),
                PdSignals.Constant(length, 34));
            float[] thud = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Osc(thudFrequency, sampleRate), envelopeSquared),
                0.5);

            return PdSignals.Add(PdSignals.Add(squelch, bubble), thud);
        }

        private static float[] RenderWoodTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);
            float[] envelopeFourth = PdSignals.Mul(envelopeSquared, envelopeSquared);

            float[] knockFrequency = PdSignals.Add(
                PdSignals.MulScalar(envelopeSquared, 320.0),
                PdSignals.Constant(length, 90));
            float[] knock = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Osc(knockFrequency, sampleRate), envelopeSquared),
                1.6);

            float[] clickNoise = PdSignals.Noise(length, random);
            float[] click = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Hip(clickNoise, PdSignals.Constant(length, 2200), sampleRate), envelopeFourth),
                6.0);

            float[] bodyNoise = PdSignals.Lop(
                PdSignals.Noise(length, random),
                PdSignals.Constant(length, 180),
                sampleRate);
            float[] body = PdSignals.MulScalar(PdSignals.Mul(bodyNoise, envelopeSquared), 0.2);

            return PdSignals.Add(PdSignals.Add(knock, click), body);
        }

        private static float[] RenderMetalTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);

            float[] ringWobble = PdSignals.MulScalar(
                PdSignals.Lop(PdSignals.Noise(length, random), PdSignals.Constant(length, 40), sampleRate),
                240.0);
            float[] ringFrequency = PdSignals.Add(
                PdSignals.Add(PdSignals.MulScalar(envelopeSquared, 2600.0), PdSignals.Constant(length, 520)),
                ringWobble);
            float[] ring = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Osc(ringFrequency, sampleRate), envelopeSquared),
                0.5);

            float[] tingNoise = PdSignals.Noise(length, random);
            float[] ting = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Hip(tingNoise, PdSignals.Constant(length, 3800), sampleRate), envelopeSquared),
                0.55);

            float[] clankSweep = PdSignals.Clip(
                PdSignals.Add(
                    PdSignals.MulScalar(envelope, 4200.0),
                    PdSignals.Constant(length, 900)),
                700,
                8000);
            float[] clank = PdSignals.MulScalar(
                PdSignals.Vcf(PdSignals.Clip(tingNoise, -1.0, 1.0), clankSweep, PdSignals.Constant(length, 2), sampleRate),
                0.34);

            return PdSignals.Add(PdSignals.Add(ring, ting), clank);
        }

        private static float[] RenderWaterTerrain(float[] envelope, int sampleRate, Mulberry32 random)
        {
            int length = envelope.Length;
            float[] envelopeSquared = PdSignals.Mul(envelope, envelope);
            float[] envelopeFourth = PdSignals.Mul(envelopeSquared, envelopeSquared);

            float[] splashNoise = PdSignals.Noise(length, random);
            float[] splashSweep = PdSignals.Clip(
                PdSignals.Add(
                    PdSignals.MulScalar(envelope, 5200.0),
                    PdSignals.Constant(length, 900)),
                600,
                9000);
            float[] splash = PdSignals.MulScalar(
                PdSignals.Mul(
                    PdSignals.Vcf(
                        PdSignals.Clip(splashNoise, -1.0, 1.0),
                        splashSweep,
                        PdSignals.Constant(length, 1.2),
                        sampleRate),
                    envelope),
                0.9);

            float[] droplet = PdSignals.MulScalar(
                PdSignals.Mul(
                    PdSignals.Hip(PdSignals.Noise(length, random), PdSignals.Constant(length, 2600), sampleRate),
                    envelopeFourth),
                0.5);

            float[] swell = PdSignals.MulScalar(
                PdSignals.Mul(PdSignals.Lop(splashNoise, PdSignals.Constant(length, 260), sampleRate), envelopeSquared),
                0.5);

            return PdSignals.Add(PdSignals.Add(splash, droplet), swell);
        }

        private static Func<double, double> StepFn(double threshold)
        {
            return time =>
            {
                if (time <= 0.0 || time >= 1.0)
                {
                    return 0.0;
                }

                return -1.5 * (1.0 - time) * (threshold * time * time * time - threshold * time);
            };
        }

        private static Func<double, double> ResizeFn(
            Func<double, double> fn,
            double inputStart,
            double inputEnd,
            double outputStart,
            double outputEnd)
        {
            return time => fn(
                ((time - outputStart) / (outputEnd - outputStart)) * (inputEnd - inputStart) + inputStart);
        }
    }
}

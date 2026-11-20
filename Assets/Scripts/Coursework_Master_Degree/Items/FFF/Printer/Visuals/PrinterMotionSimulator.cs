using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Cheap procedural nozzle path for the active print. The path stays inside
    /// the placed object's per-layer bed-local bounds and uses a continuous
    /// global layer parameter so layer changes do not reset the X/Z phase or
    /// teleport the head.
    ///
    /// Each job gets a seeded motion profile: two primary Lissajous harmonics
    /// plus a slower harmonic and low-frequency Perlin drift. Progress comes
    /// from PrinterJob.Progress01, whose duration already includes the console
    /// SPEED preset; do not scale visual speed again here.
    /// </summary>
    internal sealed class PrinterMotionSimulator
    {
        private const float ReturnSpeedMetersPerSecond = 0.25f;
        private const float ZHopMeters = 0.0006f;

        private PrinterMotionBase motion;
        private Vector2 fallbackMin;
        private Vector2 fallbackMax;
        private PrintLayerBounds[] layerBounds;
        private Bounds bedBounds;
        private float totalHeight;
        private float layerHeight;
        private int layerCount;
        private bool returning;

        private float phaseX;
        private float phaseZ;
        private float frequencyX;
        private float frequencyZ;
        private float harmonicWeight;
        private float harmonicFrequencyX;
        private float harmonicFrequencyZ;
        private float harmonicPhaseX;
        private float harmonicPhaseZ;
        private float noiseOffsetX;
        private float noiseOffsetY;
        private float noiseOffsetZ;

        public void Begin(
            PrinterMotionBase targetMotion,
            Bounds targetBedBounds,
            float targetLayerHeight,
            int targetSeed,
            PrintLayerBounds[] targetLayerBounds)
        {
            motion = targetMotion;
            bedBounds = targetBedBounds;
            layerHeight = Mathf.Max(0.00001f, targetLayerHeight);
            layerBounds = targetLayerBounds;
            returning = false;

            if (motion == null)
            {
                layerCount = 0;
                return;
            }

            Vector3 volume = motion.BuildVolume;
            float limitX = Mathf.Max(0.0f, volume.x * 0.5f);
            float limitZ = Mathf.Max(0.0f, volume.z * 0.5f);

            fallbackMin = new Vector2(bedBounds.min.x, bedBounds.min.z);
            fallbackMax = new Vector2(bedBounds.max.x, bedBounds.max.z);

            fallbackMin.x = Mathf.Clamp(fallbackMin.x, -limitX, limitX);
            fallbackMax.x = Mathf.Clamp(fallbackMax.x, -limitX, limitX);
            fallbackMin.y = Mathf.Clamp(fallbackMin.y, -limitZ, limitZ);
            fallbackMax.y = Mathf.Clamp(fallbackMax.y, -limitZ, limitZ);

            if (fallbackMin.x > fallbackMax.x)
            {
                fallbackMin.x = fallbackMax.x =
                    Mathf.Clamp(bedBounds.center.x, -limitX, limitX);
            }

            if (fallbackMin.y > fallbackMax.y)
            {
                fallbackMin.y = fallbackMax.y =
                    Mathf.Clamp(bedBounds.center.z, -limitZ, limitZ);
            }

            totalHeight = Mathf.Max(0.0f, bedBounds.size.y);
            layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(totalHeight / layerHeight));

            InitializePath(targetSeed);
        }

        public void Resume()
        {
            returning = motion != null;
        }

        public void Tick(float progress, float delta)
        {
            if (motion == null)
            {
                return;
            }

            Vector3 target = CalculateTarget(progress);

            if (returning)
            {
                Vector3 current = motion.GetNozzlePrintPosition();
                float step = Mathf.Max(
                    0.02f,
                    ReturnSpeedMetersPerSecond *
                    Mathf.Max(0.0001f, delta));
                Vector3 next = Vector3.MoveTowards(current, target, step);

                motion.ApplyNozzlePosition(next);

                if ((next - target).sqrMagnitude <= 0.0000001f)
                {
                    returning = false;
                }

                return;
            }

            motion.ApplyNozzlePosition(target);
        }

        private void InitializePath(int targetSeed)
        {
            System.Random random = new System.Random(targetSeed);

            phaseX = NextAngle(random);
            phaseZ = NextAngle(random);
            frequencyX = Mathf.Lerp(1.3f, 2.7f, Next01(random));
            frequencyZ = Mathf.Lerp(2.0f, 3.8f, Next01(random));
            harmonicWeight = Mathf.Lerp(0.10f, 0.32f, Next01(random));
            harmonicFrequencyX = frequencyX *
                Mathf.Lerp(0.35f, 0.65f, Next01(random));
            harmonicFrequencyZ = frequencyZ *
                Mathf.Lerp(0.35f, 0.65f, Next01(random));
            harmonicPhaseX = NextAngle(random);
            harmonicPhaseZ = NextAngle(random);
            noiseOffsetX = Next01(random) * 1000.0f;
            noiseOffsetY = Next01(random) * 1000.0f;
            noiseOffsetZ = Next01(random) * 1000.0f;
        }

        private Vector3 CalculateTarget(float progress)
        {
            if (totalHeight <= 0.0001f || layerCount <= 0)
            {
                Vector2 center = (fallbackMin + fallbackMax) * 0.5f;
                return new Vector3(center.x, bedBounds.min.y, center.y);
            }

            float p = Mathf.Clamp01(progress);
            float layerPosition = p * layerCount;
            int layer = Mathf.Min(
                layerCount - 1,
                Mathf.FloorToInt(layerPosition));
            float layerT = Mathf.Clamp01(layerPosition - layer);

            PrintLayerBounds current = GetLayerBounds(layer);
            PrintLayerBounds next = GetLayerBounds(
                Mathf.Min(layer + 1, layerCount - 1));

            // Stay inside the current layer's bounds for most of the layer,
            // then blend into the next layer near the top. This matches the
            // per-layer silhouette without a hard bounds switch.
            float blend = Mathf.SmoothStep(
                0.0f,
                1.0f,
                Mathf.Clamp01((layerT - 0.8f) / 0.2f));
            float minX = Mathf.Lerp(current.Min.x, next.Min.x, blend);
            float maxX = Mathf.Lerp(current.Max.x, next.Max.x, blend);
            float minZ = Mathf.Lerp(current.Min.y, next.Min.y, blend);
            float maxZ = Mathf.Lerp(current.Max.y, next.Max.y, blend);

            // Seeded low-frequency drift makes each job feel different while
            // staying continuous across layers.
            float driftX = Mathf.PerlinNoise(
                layerPosition * 0.37f + noiseOffsetX,
                noiseOffsetY) * 2.0f - 1.0f;
            float driftZ = Mathf.PerlinNoise(
                layerPosition * 0.29f + noiseOffsetZ,
                noiseOffsetY + 19.7f) * 2.0f - 1.0f;

            float angleX = (layerPosition * frequencyX +
                phaseX + driftX * 0.18f) * Mathf.PI * 2.0f;
            float angleZ = (layerPosition * frequencyZ +
                phaseZ + driftZ * 0.18f) * Mathf.PI * 2.0f;

            float secondaryX = Mathf.Sin(
                (layerPosition * harmonicFrequencyX + harmonicPhaseX) *
                Mathf.PI * 2.0f);
            float secondaryZ = Mathf.Cos(
                (layerPosition * harmonicFrequencyZ + harmonicPhaseZ) *
                Mathf.PI * 2.0f);

            float primaryWeight = 1.0f - harmonicWeight;
            float x01 = 0.5f + 0.5f * (primaryWeight *
                Mathf.Sin(angleX) + harmonicWeight * secondaryX);
            float z01 = 0.5f + 0.5f * (primaryWeight *
                Mathf.Cos(angleZ) + harmonicWeight * secondaryZ);

            x01 = Mathf.Clamp01(x01 + driftX * 0.05f);
            z01 = Mathf.Clamp01(z01 + driftZ * 0.05f);

            float x = Mathf.Lerp(minX, maxX, x01);
            float z = Mathf.Lerp(minZ, maxZ, z01);
            float y = bedBounds.min.y + Mathf.Min(
                totalHeight,
                layerPosition * layerHeight);

            // Lift only near the top of the layer, then return to zero with
            // zero slope at the boundary. The old ramp snapped down when the
            // next layer started.
            float hopT = Mathf.Clamp01((layerT - 0.8f) / 0.2f);
            float hop = Mathf.SmoothStep(0.0f, 1.0f, hopT);
            y += ZHopMeters * Mathf.Sin(Mathf.PI * hop);

            return new Vector3(x, y, z);
        }

        private PrintLayerBounds GetLayerBounds(int layer)
        {
            if (layerBounds != null && layerBounds.Length > 0)
            {
                return layerBounds[Mathf.Clamp(
                    layer,
                    0,
                    layerBounds.Length - 1)];
            }

            return new PrintLayerBounds(fallbackMin, fallbackMax);
        }

        private static float Next01(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static float NextAngle(System.Random random)
        {
            return Next01(random) * Mathf.PI * 2.0f;
        }
    }
}

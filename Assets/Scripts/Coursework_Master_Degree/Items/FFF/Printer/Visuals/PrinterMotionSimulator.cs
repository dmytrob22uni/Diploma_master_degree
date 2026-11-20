using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Jobs;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Cheap procedural nozzle path for the active print. The path stays inside
    /// the placed object's bed-local bounds and follows a precomputed per-layer
    /// silhouette. It uses a continuous global layer parameter so layer
    /// changes do not reset the X/Z phase or teleport the head.
    ///
    /// Progress comes from PrinterJob.Progress01, whose duration already
    /// includes the console SPEED preset. Do not scale visual speed again here.
    /// </summary>
    internal sealed class PrinterMotionSimulator
    {
        private const float ReturnSpeedMetersPerSecond = 0.25f;
        private const float ZHopMeters = 0.0006f;

        private PrinterMotionBase motion;
        private Vector2 fallbackMin;
        private Vector2 fallbackMax;
        private PrintLayerBounds[] layerBounds;
        private float totalHeight;
        private float layerHeight;
        private int layerCount;
        private int seed;
        private bool returning;

        public void Begin(
            PrinterMotionBase targetMotion,
            PrintPlacement placement,
            Bounds localBounds,
            float uniformScale,
            float targetLayerHeight,
            int targetSeed,
            PrintLayerBounds[] targetLayerBounds)
        {
            motion = targetMotion;
            layerHeight = Mathf.Max(0.00001f, targetLayerHeight);
            layerBounds = targetLayerBounds;
            seed = targetSeed;
            returning = false;

            if (motion == null)
            {
                layerCount = 0;
                return;
            }

            float scale = Mathf.Max(0.0001f, uniformScale);
            Vector3 scaledSize = localBounds.size * scale;
            float yaw = placement != null ? placement.Yaw : 0.0f;
            float radians = yaw * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(radians));
            float sin = Mathf.Abs(Mathf.Sin(radians));

            Vector3 localCenter = localBounds.center * scale;
            Vector3 rotatedCenter =
                Quaternion.Euler(0.0f, yaw, 0.0f) * localCenter;

            Vector3 center = placement != null
                ? placement.LocalPosition + rotatedCenter
                : rotatedCenter;

            float halfX = (scaledSize.x * cos + scaledSize.z * sin) * 0.5f;
            float halfZ = (scaledSize.x * sin + scaledSize.z * cos) * 0.5f;

            Vector3 volume = motion.BuildVolume;
            float limitX = Mathf.Max(0.0f, volume.x * 0.5f);
            float limitZ = Mathf.Max(0.0f, volume.z * 0.5f);

            fallbackMin = new Vector2(center.x - halfX, center.z - halfZ);
            fallbackMax = new Vector2(center.x + halfX, center.z + halfZ);

            fallbackMin.x = Mathf.Clamp(fallbackMin.x, -limitX, limitX);
            fallbackMax.x = Mathf.Clamp(fallbackMax.x, -limitX, limitX);
            fallbackMin.y = Mathf.Clamp(fallbackMin.y, -limitZ, limitZ);
            fallbackMax.y = Mathf.Clamp(fallbackMax.y, -limitZ, limitZ);

            if (fallbackMin.x > fallbackMax.x)
            {
                fallbackMin.x = fallbackMax.x =
                    Mathf.Clamp(center.x, -limitX, limitX);
            }

            if (fallbackMin.y > fallbackMax.y)
            {
                fallbackMin.y = fallbackMax.y =
                    Mathf.Clamp(center.z, -limitZ, limitZ);
            }

            totalHeight = Mathf.Max(0.0f, scaledSize.y);
            layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(totalHeight / layerHeight));
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

        private Vector3 CalculateTarget(float progress)
        {
            if (totalHeight <= 0.0001f || layerCount <= 0)
            {
                Vector2 center = (fallbackMin + fallbackMax) * 0.5f;
                return new Vector3(center.x, 0.0f, center.y);
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

            // Keep the angle continuous across layers. The old path added a
            // per-layer phase offset, which made X/Z jump at every boundary.
            float phase = (seed % 997) * 0.017f;
            float angleX = (layerPosition * 2.0f + phase) *
                Mathf.PI * 2.0f;
            float angleZ = (layerPosition * 3.0f + phase * 0.7f) *
                Mathf.PI * 2.0f;

            float x = Mathf.Lerp(
                minX,
                maxX,
                0.5f + 0.5f * Mathf.Sin(angleX));
            float z = Mathf.Lerp(
                minZ,
                maxZ,
                0.5f + 0.5f * Mathf.Cos(angleZ));
            float y = Mathf.Min(
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
    }
}

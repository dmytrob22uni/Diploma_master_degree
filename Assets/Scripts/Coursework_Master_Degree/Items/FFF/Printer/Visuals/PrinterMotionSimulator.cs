using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Jobs;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Cheap procedural nozzle path for the active print. The path stays inside
    /// the placed object's bed-local bounding box and uses only the abstract
    /// print-space motion API.
    /// </summary>
    internal sealed class PrinterMotionSimulator
    {
        private const float ReturnSpeedMetersPerSecond = 0.25f;
        private const float ZHopMeters = 0.0006f;

        private PrinterMotionBase motion;
        private Vector2 min;
        private Vector2 max;
        private float totalHeight;
        private float layerHeight;
        private int seed;
        private bool returning;

        public void Begin(
            PrinterMotionBase targetMotion,
            PrintPlacement placement,
            Bounds localBounds,
            float uniformScale,
            float targetLayerHeight,
            int targetSeed)
        {
            motion = targetMotion;
            layerHeight = Mathf.Max(0.00001f, targetLayerHeight);
            seed = targetSeed;
            returning = false;

            if (motion == null)
            {
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

            min = new Vector2(center.x - halfX, center.z - halfZ);
            max = new Vector2(center.x + halfX, center.z + halfZ);

            min.x = Mathf.Clamp(min.x, -limitX, limitX);
            max.x = Mathf.Clamp(max.x, -limitX, limitX);
            min.y = Mathf.Clamp(min.y, -limitZ, limitZ);
            max.y = Mathf.Clamp(max.y, -limitZ, limitZ);

            if (min.x > max.x)
            {
                min.x = max.x = Mathf.Clamp(center.x, -limitX, limitX);
            }

            if (min.y > max.y)
            {
                min.y = max.y = Mathf.Clamp(center.z, -limitZ, limitZ);
            }

            totalHeight = Mathf.Max(0.0f, scaledSize.y);
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
            float p = Mathf.Clamp01(progress);

            if (totalHeight <= 0.0001f)
            {
                return new Vector3(
                    (min.x + max.x) * 0.5f,
                    0.0f,
                    (min.y + max.y) * 0.5f);
            }

            int layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(totalHeight / layerHeight));
            float layerPosition = p * layerCount;
            int layer = Mathf.Min(
                layerCount - 1,
                Mathf.FloorToInt(layerPosition));
            float layerT = Mathf.Clamp01(layerPosition - layer);
            float phase = (seed % 997) * 0.017f + layer * 2.399963f;

            float x = Mathf.Lerp(
                min.x,
                max.x,
                0.5f + 0.5f * Mathf.Sin(
                    (layerT * 2.0f + phase) * Mathf.PI * 2.0f));
            float z = Mathf.Lerp(
                min.y,
                max.y,
                0.5f + 0.5f * Mathf.Cos(
                    (layerT * 3.0f + phase * 0.7f) * Mathf.PI * 2.0f));
            float y = Mathf.Min(
                totalHeight,
                (layer + layerT) * layerHeight);

            if (layerT > 0.9f)
            {
                y += ZHopMeters * ((layerT - 0.9f) / 0.1f);
            }

            return new Vector3(x, y, z);
        }
    }
}

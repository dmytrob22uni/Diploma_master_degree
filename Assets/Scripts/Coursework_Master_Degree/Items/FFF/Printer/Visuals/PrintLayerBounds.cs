using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Jobs;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Bed-local XZ bounds of one printed layer. The motion path stays inside
    /// these bounds so it follows the object's silhouette instead of the full
    /// object bounding box on every layer.
    /// </summary>
    internal readonly struct PrintLayerBounds
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        public PrintLayerBounds(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Precomputes per-layer XZ bounds from the captured mesh. The bounds are
    /// conservative: a triangle contributes to every layer its Y range
    /// crosses, so the head never leaves the object because of a vertex-only
    /// sampling gap.
    /// </summary>
    internal static class PrintLayerBoundsBuilder
    {
        public static PrintLayerBounds[] Build(
            PrintedMeshData mesh,
            Bounds localBounds,
            float uniformScale,
            float layerHeight,
            PrintPlacement placement,
            Vector3 buildVolume)
        {
            float scale = Mathf.Max(0.0001f, uniformScale);
            float height = Mathf.Max(
                0.0f,
                localBounds.size.y * scale);
            float layer = Mathf.Max(0.00001f, layerHeight);
            int layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(height / layer));

            Vector2 overallMin = new Vector2(
                localBounds.min.x,
                localBounds.min.z);
            Vector2 overallMax = new Vector2(
                localBounds.max.x,
                localBounds.max.z);

            TransformToBed(
                overallMin,
                overallMax,
                scale,
                placement,
                buildVolume,
                out Vector2 fallbackMin,
                out Vector2 fallbackMax);

            if (mesh == null ||
                !mesh.IsValid ||
                height <= 0.0001f ||
                layerCount <= 0)
            {
                return new[]
                {
                    new PrintLayerBounds(fallbackMin, fallbackMax)
                };
            }

            float[] minX = new float[layerCount];
            float[] maxX = new float[layerCount];
            float[] minZ = new float[layerCount];
            float[] maxZ = new float[layerCount];
            bool[] hasTriangle = new bool[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                minX[i] = float.PositiveInfinity;
                maxX[i] = float.NegativeInfinity;
                minZ[i] = float.PositiveInfinity;
                maxZ[i] = float.NegativeInfinity;
            }

            float localBottom = localBounds.min.y;
            float localLayerHeight = layer / scale;

            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[t]];
                Vector3 b = mesh.Vertices[mesh.Triangles[t + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[t + 2]];

                float triangleMinY = Mathf.Min(
                    a.y,
                    Mathf.Min(b.y, c.y));
                float triangleMaxY = Mathf.Max(
                    a.y,
                    Mathf.Max(b.y, c.y));

                int firstLayer = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (triangleMinY - localBottom) / localLayerHeight),
                    0,
                    layerCount - 1);
                int lastLayer = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (triangleMaxY - localBottom) / localLayerHeight),
                    0,
                    layerCount - 1);

                if (lastLayer < firstLayer)
                {
                    int swap = firstLayer;
                    firstLayer = lastLayer;
                    lastLayer = swap;
                }

                float triangleMinX = Mathf.Min(
                    a.x,
                    Mathf.Min(b.x, c.x));
                float triangleMaxX = Mathf.Max(
                    a.x,
                    Mathf.Max(b.x, c.x));
                float triangleMinZ = Mathf.Min(
                    a.z,
                    Mathf.Min(b.z, c.z));
                float triangleMaxZ = Mathf.Max(
                    a.z,
                    Mathf.Max(b.z, c.z));

                for (int i = firstLayer; i <= lastLayer; i++)
                {
                    hasTriangle[i] = true;
                    minX[i] = Mathf.Min(minX[i], triangleMinX);
                    maxX[i] = Mathf.Max(maxX[i], triangleMaxX);
                    minZ[i] = Mathf.Min(minZ[i], triangleMinZ);
                    maxZ[i] = Mathf.Max(maxZ[i], triangleMaxZ);
                }
            }

            int firstPopulated = -1;

            for (int i = 0; i < layerCount; i++)
            {
                if (!hasTriangle[i])
                {
                    continue;
                }

                if (firstPopulated < 0)
                {
                    firstPopulated = i;
                }
            }

            if (firstPopulated < 0)
            {
                return new[]
                {
                    new PrintLayerBounds(fallbackMin, fallbackMax)
                };
            }

            // Empty bands inherit the nearest populated layer. This happens
            // at the top of a tapered object or between disconnected islands.
            for (int i = 0; i < firstPopulated; i++)
            {
                minX[i] = minX[firstPopulated];
                maxX[i] = maxX[firstPopulated];
                minZ[i] = minZ[firstPopulated];
                maxZ[i] = maxZ[firstPopulated];
            }

            for (int i = firstPopulated + 1; i < layerCount; i++)
            {
                if (hasTriangle[i])
                {
                    continue;
                }

                minX[i] = minX[i - 1];
                maxX[i] = maxX[i - 1];
                minZ[i] = minZ[i - 1];
                maxZ[i] = maxZ[i - 1];
            }

            PrintLayerBounds[] result = new PrintLayerBounds[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                TransformToBed(
                    new Vector2(minX[i], minZ[i]),
                    new Vector2(maxX[i], maxZ[i]),
                    scale,
                    placement,
                    buildVolume,
                    out Vector2 bedMin,
                    out Vector2 bedMax);

                result[i] = new PrintLayerBounds(bedMin, bedMax);
            }

            return result;
        }

        private static void TransformToBed(
            Vector2 localMin,
            Vector2 localMax,
            float scale,
            PrintPlacement placement,
            Vector3 buildVolume,
            out Vector2 bedMin,
            out Vector2 bedMax)
        {
            float yaw = placement != null ? placement.Yaw : 0.0f;
            float radians = yaw * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(radians));
            float sin = Mathf.Abs(Mathf.Sin(radians));

            Vector2 localCenter = (localMin + localMax) * 0.5f;
            Vector2 localSize = localMax - localMin;
            Vector3 scaledCenter = new Vector3(
                localCenter.x,
                0.0f,
                localCenter.y) * scale;
            Vector3 rotatedCenter =
                Quaternion.Euler(0.0f, yaw, 0.0f) * scaledCenter;
            Vector3 center = (placement != null
                    ? placement.LocalPosition
                    : Vector3.zero) +
                rotatedCenter;

            float halfX = (localSize.x * scale * cos +
                localSize.y * scale * sin) * 0.5f;
            float halfZ = (localSize.x * scale * sin +
                localSize.y * scale * cos) * 0.5f;

            float limitX = Mathf.Max(0.0f, buildVolume.x * 0.5f);
            float limitZ = Mathf.Max(0.0f, buildVolume.z * 0.5f);

            float minX = Mathf.Clamp(
                center.x - halfX,
                -limitX,
                limitX);
            float maxX = Mathf.Clamp(
                center.x + halfX,
                -limitX,
                limitX);
            float minZ = Mathf.Clamp(
                center.z - halfZ,
                -limitZ,
                limitZ);
            float maxZ = Mathf.Clamp(
                center.z + halfZ,
                -limitZ,
                limitZ);

            if (minX > maxX)
            {
                minX = maxX = Mathf.Clamp(center.x, -limitX, limitX);
            }

            if (minZ > maxZ)
            {
                minZ = maxZ = Mathf.Clamp(center.z, -limitZ, limitZ);
            }

            bedMin = new Vector2(minX, minZ);
            bedMax = new Vector2(maxX, maxZ);
        }
    }
}

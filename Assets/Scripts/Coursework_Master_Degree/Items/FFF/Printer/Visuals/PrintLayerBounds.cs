using UnityEngine;

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
    /// Precomputes per-layer XZ bounds from the captured mesh, transformed
    /// through the actual printed-object-to-bed matrix. The bounds are
    /// conservative: a triangle contributes to every layer its Y range
    /// crosses, so the head never leaves the object because of a vertex-only
    /// sampling gap.
    /// </summary>
    internal static class PrintLayerBoundsBuilder
    {
        public static PrintLayerBounds[] Build(
            PrintedMeshData mesh,
            Bounds localBounds,
            Matrix4x4 localToBed,
            float layerHeight,
            Vector3 buildVolume,
            out Bounds bedBounds)
        {
            bedBounds = TransformBounds(localBounds, localToBed);

            float height = Mathf.Max(0.0f, bedBounds.size.y);
            float layer = Mathf.Max(0.00001f, layerHeight);
            int layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(height / layer));

            ClampToVolume(
                new Vector2(bedBounds.min.x, bedBounds.min.z),
                new Vector2(bedBounds.max.x, bedBounds.max.z),
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

            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                Vector3 a = localToBed.MultiplyPoint3x4(
                    mesh.Vertices[mesh.Triangles[t]]);
                Vector3 b = localToBed.MultiplyPoint3x4(
                    mesh.Vertices[mesh.Triangles[t + 1]]);
                Vector3 c = localToBed.MultiplyPoint3x4(
                    mesh.Vertices[mesh.Triangles[t + 2]]);

                float triangleMinY = Mathf.Min(
                    a.y,
                    Mathf.Min(b.y, c.y));
                float triangleMaxY = Mathf.Max(
                    a.y,
                    Mathf.Max(b.y, c.y));

                int firstLayer = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (triangleMinY - bedBounds.min.y) / layer),
                    0,
                    layerCount - 1);
                int lastLayer = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (triangleMaxY - bedBounds.min.y) / layer),
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
                ClampToVolume(
                    new Vector2(minX[i], minZ[i]),
                    new Vector2(maxX[i], maxZ[i]),
                    buildVolume,
                    out Vector2 bedMin,
                    out Vector2 bedMax);

                result[i] = new PrintLayerBounds(bedMin, bedMax);
            }

            return result;
        }

        private static Bounds TransformBounds(
            Bounds localBounds,
            Matrix4x4 localToBed)
        {
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            Vector3 bedMin = localToBed.MultiplyPoint3x4(min);
            Vector3 bedMax = bedMin;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                Vector3 bedCorner = localToBed.MultiplyPoint3x4(corner);

                bedMin = Vector3.Min(bedMin, bedCorner);
                bedMax = Vector3.Max(bedMax, bedCorner);
            }

            return new Bounds(
                (bedMin + bedMax) * 0.5f,
                bedMax - bedMin);
        }

        private static void ClampToVolume(
            Vector2 min,
            Vector2 max,
            Vector3 buildVolume,
            out Vector2 clampedMin,
            out Vector2 clampedMax)
        {
            float limitX = Mathf.Max(0.0f, buildVolume.x * 0.5f);
            float limitZ = Mathf.Max(0.0f, buildVolume.z * 0.5f);

            float minX = Mathf.Clamp(min.x, -limitX, limitX);
            float maxX = Mathf.Clamp(max.x, -limitX, limitX);
            float minZ = Mathf.Clamp(min.y, -limitZ, limitZ);
            float maxZ = Mathf.Clamp(max.y, -limitZ, limitZ);

            if (minX > maxX)
            {
                float center = Mathf.Clamp(
                    (min.x + max.x) * 0.5f,
                    -limitX,
                    limitX);
                minX = maxX = center;
            }

            if (minZ > maxZ)
            {
                float center = Mathf.Clamp(
                    (min.y + max.y) * 0.5f,
                    -limitZ,
                    limitZ);
                minZ = maxZ = center;
            }

            clampedMin = new Vector2(minX, minZ);
            clampedMax = new Vector2(maxX, maxZ);
        }
    }
}

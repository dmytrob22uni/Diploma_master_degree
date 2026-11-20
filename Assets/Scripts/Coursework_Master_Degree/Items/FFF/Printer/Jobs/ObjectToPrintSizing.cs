using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Geometry derived from a catalog prefab at its authored scale. Used by
    /// the estimate model to distinguish shell material from infill without
    /// authoring per-object coefficients.
    /// </summary>
    public readonly struct ObjectToPrintGeometry
    {
        public readonly Vector3 Size;
        public readonly float VolumeCubicMeters;
        public readonly float SurfaceAreaSquareMeters;

        public ObjectToPrintGeometry(
            Vector3 size,
            float volumeCubicMeters,
            float surfaceAreaSquareMeters)
        {
            Size = size;
            VolumeCubicMeters = Mathf.Max(0.0f, volumeCubicMeters);
            SurfaceAreaSquareMeters = Mathf.Max(0.0f, surfaceAreaSquareMeters);
        }

        public bool IsValid =>
            VolumeCubicMeters > 0.0000001f &&
            SurfaceAreaSquareMeters > 0.0000001f;
    }

    /// <summary>
    /// Derives object dimensions, solid volume and surface area from the
    /// catalog prefab. Results are cached per meta; no size/geometry authoring
    /// is required for new objects.
    /// </summary>
    public static class ObjectToPrintSizing
    {
        private static readonly Dictionary<ObjectToPrintMetaSO, ObjectToPrintGeometry>
            cache = new Dictionary<ObjectToPrintMetaSO, ObjectToPrintGeometry>();

        public static Vector3 GetSizeMeters(ObjectToPrintMetaSO meta)
        {
            return GetGeometry(meta).Size;
        }

        public static ObjectToPrintGeometry GetGeometry(
            ObjectToPrintMetaSO meta)
        {
            if (meta == null)
            {
                return default;
            }

            if (cache.TryGetValue(meta, out ObjectToPrintGeometry cached))
            {
                return cached;
            }

            Vector3 size = Vector3.zero;
            float volume = 0.0f;
            float surfaceArea = 0.0f;

            if (meta.Prefab != null)
            {
                GameObject instance = Object.Instantiate(meta.Prefab);
                instance.SetActive(true);

                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                bool found = false;
                Bounds bounds = new Bounds(
                    instance.transform.position,
                    Vector3.zero);

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null ||
                        !renderer.enabled ||
                        renderer.bounds.size.sqrMagnitude < 0.0000001f)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (found)
                {
                    size = bounds.size;
                }

                MeshFilter[] filters =
                    instance.GetComponentsInChildren<MeshFilter>(true);

                for (int i = 0; i < filters.Length; i++)
                {
                    MeshFilter filter = filters[i];

                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    Mesh mesh = filter.sharedMesh;

                    if (!mesh.isReadable)
                    {
                        continue;
                    }

                    Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    float meshVolume = 0.0f;

                    for (int t = 0; t < triangles.Length; t += 3)
                    {
                        Vector3 a = matrix.MultiplyPoint3x4(
                            vertices[triangles[t]]);
                        Vector3 b = matrix.MultiplyPoint3x4(
                            vertices[triangles[t + 1]]);
                        Vector3 c = matrix.MultiplyPoint3x4(
                            vertices[triangles[t + 2]]);

                        meshVolume += Vector3.Dot(
                            a,
                            Vector3.Cross(b, c)) / 6.0f;
                        surfaceArea +=
                            Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    }

                    volume += Mathf.Abs(meshVolume);
                }

                Object.Destroy(instance);
            }

            ObjectToPrintGeometry geometry = new ObjectToPrintGeometry(
                size,
                volume,
                surfaceArea);

            cache[meta] = geometry;
            return geometry;
        }
    }
}

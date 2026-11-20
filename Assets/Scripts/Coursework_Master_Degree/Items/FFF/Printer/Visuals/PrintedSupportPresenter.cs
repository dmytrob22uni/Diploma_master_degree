using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Coursework_Master_Degree.Items.FFF.Jobs;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Generates simple vertical support columns under downward-facing
    /// triangles. Supports are children of the printed object, so they follow
    /// the bed and survive until M5 adds support removal.
    /// </summary>
    internal sealed class PrintedSupportPresenter
    {
        private sealed class SupportColumn
        {
            public Transform Transform;
            public float Height;
        }

        private static Mesh columnMesh;
        private static readonly int RevealHeightId =
            Shader.PropertyToID("_reveal_height");

        private readonly List<SupportColumn> columns =
            new List<SupportColumn>();
        private readonly Dictionary<long, Vector3> clusters =
            new Dictionary<long, Vector3>();
        private readonly List<Vector3> supportPoints =
            new List<Vector3>();

        private float radius;

        public void Begin(
            GameObject objectRoot,
            PrintedMeshData mesh,
            Material material,
            SupportsMode mode,
            int maxColumns,
            float clusterSize,
            float overhangNormalThreshold,
            float columnRadius,
            Bounds localBounds)
        {
            columns.Clear();
            clusters.Clear();
            supportPoints.Clear();
            radius = Mathf.Max(0.0002f, columnRadius);

            if (objectRoot == null ||
                mesh == null ||
                !mesh.IsValid ||
                mode != SupportsMode.Auto ||
                maxColumns <= 0)
            {
                return;
            }

            float clustering = Mathf.Max(0.001f, clusterSize);
            float threshold = Mathf.Clamp01(overhangNormalThreshold);

            for (int i = 0; i < mesh.Triangles.Count; i += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[i]];
                Vector3 b = mesh.Vertices[mesh.Triangles[i + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);

                if (normal.sqrMagnitude <= 0.0000001f)
                {
                    continue;
                }

                normal.Normalize();

                if (normal.y >= -threshold)
                {
                    continue;
                }

                Vector3 point = (a + b + c) / 3.0f;

                if (point.y <= 0.002f ||
                    point.y >= localBounds.max.y - 0.001f)
                {
                    continue;
                }

                long key = ClusterKey(point, clustering);

                if (!clusters.TryGetValue(key, out Vector3 existing) ||
                    point.y < existing.y)
                {
                    clusters[key] = point;
                }
            }

            if (clusters.Count == 0)
            {
                return;
            }

            supportPoints.AddRange(clusters.Values);
            supportPoints.Sort(
                (left, right) => right.y.CompareTo(left.y));

            int count = Mathf.Min(maxColumns, supportPoints.Count);
            Mesh sharedMesh = GetColumnMesh();

            for (int i = 0; i < count; i++)
            {
                Vector3 point = supportPoints[i];
                GameObject columnObject = new GameObject("printed_support");
                columnObject.layer = objectRoot.layer;
                columnObject.transform.SetParent(objectRoot.transform, false);
                columnObject.transform.localPosition =
                    new Vector3(point.x, 0.0f, point.z);
                columnObject.transform.localRotation = Quaternion.identity;
                columnObject.transform.localScale =
                    new Vector3(radius, 0.0f, radius);

                MeshFilter filter = columnObject.AddComponent<MeshFilter>();
                filter.sharedMesh = sharedMesh;

                MeshRenderer renderer =
                    columnObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                if (material != null &&
                    material.HasProperty(RevealHeightId))
                {
                    MaterialPropertyBlock block =
                        new MaterialPropertyBlock();
                    block.SetFloat(RevealHeightId, 1000.0f);
                    renderer.SetPropertyBlock(block);
                }

                columns.Add(new SupportColumn
                {
                    Transform = columnObject.transform,
                    Height = point.y,
                });
            }
        }

        public void Tick(float printedLocalHeight)
        {
            float height = Mathf.Max(0.0f, printedLocalHeight);

            for (int i = 0; i < columns.Count; i++)
            {
                SupportColumn column = columns[i];

                if (column.Transform == null)
                {
                    continue;
                }

                float columnHeight = Mathf.Min(height, column.Height);
                bool visible = columnHeight > 0.0001f;

                column.Transform.gameObject.SetActive(visible);

                if (visible)
                {
                    column.Transform.localScale = new Vector3(
                        radius,
                        columnHeight,
                        radius);
                }
            }
        }

        private static long ClusterKey(Vector3 point, float size)
        {
            long x = Mathf.RoundToInt(point.x / size);
            long z = Mathf.RoundToInt(point.z / size);
            return (x << 32) ^ (uint)z;
        }

        private static Mesh GetColumnMesh()
        {
            if (columnMesh != null)
            {
                return columnMesh;
            }

            Mesh mesh = new Mesh
            {
                name = "printed_support_column",
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0.0f, -0.5f),
                new Vector3( 0.5f, 0.0f, -0.5f),
                new Vector3( 0.5f, 0.0f,  0.5f),
                new Vector3(-0.5f, 0.0f,  0.5f),
                new Vector3(-0.5f, 1.0f, -0.5f),
                new Vector3( 0.5f, 1.0f, -0.5f),
                new Vector3( 0.5f, 1.0f,  0.5f),
                new Vector3(-0.5f, 1.0f,  0.5f),
            };

            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 7, 6, 4, 6, 5,
                0, 5, 1, 0, 4, 5,
                1, 6, 2, 1, 5, 6,
                2, 7, 3, 2, 6, 7,
                3, 4, 0, 3, 7, 4,
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            columnMesh = mesh;
            return mesh;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>Combined mesh data in the printed object's root-local space.</summary>
    internal sealed class PrintedMeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        public Bounds LocalBounds;

        public bool IsValid =>
            Vertices.Count > 0 &&
            Triangles.Count >= 3;
    }

    /// <summary>
    /// Captures readable object meshes into one root-local mesh. The result is
    /// used for layer caps, partial colliders and support placement.
    /// </summary>
    internal static class PrintedMeshCapture
    {
        public static PrintedMeshData Capture(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            PrintedMeshData data = new PrintedMeshData();
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            MeshFilter[] filters =
                root.GetComponentsInChildren<MeshFilter>(true);

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
                    Debug.LogWarning(
                        $"[PrintedMeshCapture] Mesh '{mesh.name}' is not " +
                        "readable; it cannot contribute to caps or supports.",
                        filter);
                    continue;
                }

                Matrix4x4 matrix =
                    toRoot * filter.transform.localToWorldMatrix;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                int offset = data.Vertices.Count;

                for (int v = 0; v < vertices.Length; v++)
                {
                    data.Vertices.Add(matrix.MultiplyPoint3x4(vertices[v]));
                }

                for (int t = 0; t < triangles.Length; t++)
                {
                    data.Triangles.Add(offset + triangles[t]);
                }
            }

            if (!data.IsValid)
            {
                return null;
            }

            Vector3 min = data.Vertices[0];
            Vector3 max = data.Vertices[0];

            for (int i = 1; i < data.Vertices.Count; i++)
            {
                min = Vector3.Min(min, data.Vertices[i]);
                max = Vector3.Max(max, data.Vertices[i]);
            }

            data.LocalBounds = new Bounds(
                (min + max) * 0.5f,
                max - min);

            return data;
        }
    }

    /// <summary>
    /// Builds horizontal caps and capped partial meshes for a printed object.
    /// The algorithm is deliberately small: it is intended for display and
    /// simple hover colliders, not for real slicer geometry.
    /// </summary>
    internal sealed class PrintedMeshCapping
    {
        private const float PointTolerance = 0.0001f;
        private const float PlaneEpsilon = 0.000001f;

        private struct Segment
        {
            public Vector2 A;
            public Vector2 B;
        }

        private struct Edge
        {
            public int A;
            public int B;
        }

        private readonly List<Segment> segments = new List<Segment>();
        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<Vector2> trianglePoints =
            new List<Vector2>(3);
        private readonly List<Edge> edges = new List<Edge>();
        private readonly Dictionary<long, int> pointLookup =
            new Dictionary<long, int>();
        private readonly List<List<int>> adjacency =
            new List<List<int>>();
        private readonly List<int> loop = new List<int>();
        private readonly List<Vector2> loopPoints = new List<Vector2>();
        private readonly List<int> loopIndices = new List<int>();
        private bool[] usedEdges = System.Array.Empty<bool>();
        private readonly List<Vector3> capVertices = new List<Vector3>();
        private readonly List<int> capTriangles = new List<int>();
        private readonly List<Vector3> slicedVertices =
            new List<Vector3>();
        private readonly List<int> slicedTriangles = new List<int>();
        private readonly List<Vector3> clipPolygon =
            new List<Vector3>(4);

        public List<Vector3> CapVertices => capVertices;
        public List<int> CapTriangles => capTriangles;
        public List<Vector3> SlicedVertices => slicedVertices;
        public List<int> SlicedTriangles => slicedTriangles;

        /// <summary>
        /// Builds a cap at root-local plane Y. Returns false only when the
        /// fallback bounds quad is also unavailable.
        /// </summary>
        public bool BuildCap(PrintedMeshData mesh, float localY)
        {
            capVertices.Clear();
            capTriangles.Clear();
            segments.Clear();
            points.Clear();
            edges.Clear();
            pointLookup.Clear();
            ClearAdjacency();

            if (mesh == null || !mesh.IsValid)
            {
                return false;
            }

            for (int i = 0; i < mesh.Triangles.Count; i += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[i]];
                Vector3 b = mesh.Vertices[mesh.Triangles[i + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[i + 2]];

                float da = a.y - localY;
                float db = b.y - localY;
                float dc = c.y - localY;

                if ((da > PlaneEpsilon &&
                     db > PlaneEpsilon &&
                     dc > PlaneEpsilon) ||
                    (da < -PlaneEpsilon &&
                     db < -PlaneEpsilon &&
                     dc < -PlaneEpsilon))
                {
                    continue;
                }

                trianglePoints.Clear();
                AddIntersection(a, b, da, db);
                AddIntersection(b, c, db, dc);
                AddIntersection(c, a, dc, da);

                if (trianglePoints.Count != 2)
                {
                    continue;
                }

                Vector2 first = trianglePoints[0];
                Vector2 second = trianglePoints[1];
                AddPoint(first);
                AddPoint(second);
                segments.Add(new Segment { A = first, B = second });
            }

            BuildEdges();

            if (edges.Count > 0)
            {
                BuildLoops(localY);
            }

            if (capTriangles.Count == 0)
            {
                BuildBoundsFallback(mesh, localY);
            }

            return capTriangles.Count > 0;
        }

        /// <summary>
        /// Builds a closed mesh clipped at root-local plane Y. The original
        /// triangles below the plane are kept and the cut is capped, so the
        /// partial object has a usable collider at its actual printed height.
        /// </summary>
        public bool BuildSlicedMesh(PrintedMeshData mesh, float localY)
        {
            slicedVertices.Clear();
            slicedTriangles.Clear();

            if (mesh == null || !mesh.IsValid)
            {
                return false;
            }

            for (int i = 0; i < mesh.Triangles.Count; i += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[i]];
                Vector3 b = mesh.Vertices[mesh.Triangles[i + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[i + 2]];

                ClipTriangle(a, b, c, localY);
            }

            if (!BuildCap(mesh, localY))
            {
                return slicedTriangles.Count > 0;
            }

            int offset = slicedVertices.Count;

            for (int i = 0; i < capVertices.Count; i++)
            {
                slicedVertices.Add(capVertices[i]);
            }

            for (int i = 0; i < capTriangles.Count; i++)
            {
                slicedTriangles.Add(offset + capTriangles[i]);
            }

            return slicedTriangles.Count > 0;
        }

        private void AddIntersection(
            Vector3 a,
            Vector3 b,
            float da,
            float db)
        {
            bool aInside = da <= PlaneEpsilon;
            bool bInside = db <= PlaneEpsilon;

            if (aInside == bInside)
            {
                return;
            }

            float denominator = da - db;

            if (Mathf.Abs(denominator) <= 0.0000001f)
            {
                return;
            }

            float t = da / denominator;
            Vector3 point = Vector3.Lerp(a, b, Mathf.Clamp01(t));
            Vector2 flat = new Vector2(point.x, point.z);

            for (int i = 0; i < trianglePoints.Count; i++)
            {
                if ((trianglePoints[i] - flat).sqrMagnitude <
                    PointTolerance * PointTolerance)
                {
                    return;
                }
            }

            trianglePoints.Add(flat);
        }

        private void AddPoint(Vector2 point)
        {
            int ix = Mathf.RoundToInt(point.x / PointTolerance);
            int iz = Mathf.RoundToInt(point.y / PointTolerance);
            long key = ((long)ix << 32) ^ (uint)iz;

            if (pointLookup.ContainsKey(key))
            {
                return;
            }

            pointLookup.Add(key, points.Count);
            points.Add(point);
        }

        private void BuildEdges()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                Segment segment = segments[i];
                int a = FindPoint(segment.A);
                int b = FindPoint(segment.B);

                if (a < 0 || b < 0 || a == b)
                {
                    continue;
                }

                EnsureAdjacency(Mathf.Max(a, b) + 1);
                int edgeIndex = edges.Count;
                edges.Add(new Edge { A = a, B = b });
                adjacency[a].Add(edgeIndex);
                adjacency[b].Add(edgeIndex);
            }
        }

        private int FindPoint(Vector2 point)
        {
            int ix = Mathf.RoundToInt(point.x / PointTolerance);
            int iz = Mathf.RoundToInt(point.y / PointTolerance);
            long key = ((long)ix << 32) ^ (uint)iz;

            return pointLookup.TryGetValue(key, out int index)
                ? index
                : -1;
        }

        private void BuildLoops(float localY)
        {
            if (usedEdges.Length < edges.Count)
            {
                usedEdges = new bool[edges.Count];
            }
            else
            {
                System.Array.Clear(usedEdges, 0, edges.Count);
            }

            bool[] used = usedEdges;

            for (int edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
            {
                if (used[edgeIndex])
                {
                    continue;
                }

                loop.Clear();
                int start = edges[edgeIndex].A;
                int current = start;
                int currentEdge = edgeIndex;
                int guard = 0;

                while (guard++ <= edges.Count + 1)
                {
                    used[currentEdge] = true;
                    loop.Add(current);

                    Edge edge = edges[currentEdge];
                    int next = edge.A == current ? edge.B : edge.A;

                    if (next == start)
                    {
                        break;
                    }

                    int nextEdge = FindUnusedEdge(next, used);

                    if (nextEdge < 0)
                    {
                        loop.Add(next);
                        break;
                    }

                    current = next;
                    currentEdge = nextEdge;
                }

                if (loop.Count >= 3)
                {
                    AddLoop(localY);
                }
            }
        }

        private int FindUnusedEdge(int pointIndex, bool[] used)
        {
            if (pointIndex < 0 || pointIndex >= adjacency.Count)
            {
                return -1;
            }

            List<int> connected = adjacency[pointIndex];

            for (int i = 0; i < connected.Count; i++)
            {
                int edge = connected[i];

                if (!used[edge])
                {
                    return edge;
                }
            }

            return -1;
        }

        private void AddLoop(float localY)
        {
            loopPoints.Clear();

            for (int i = 0; i < loop.Count; i++)
            {
                int index = loop[i];

                if (index < 0 || index >= points.Count)
                {
                    continue;
                }

                Vector2 point = points[index];

                if (loopPoints.Count > 0 &&
                    (loopPoints[loopPoints.Count - 1] - point).sqrMagnitude <
                    PointTolerance * PointTolerance)
                {
                    continue;
                }

                loopPoints.Add(point);
            }

            if (loopPoints.Count > 1 &&
                (loopPoints[0] - loopPoints[loopPoints.Count - 1])
                    .sqrMagnitude < PointTolerance * PointTolerance)
            {
                loopPoints.RemoveAt(loopPoints.Count - 1);
            }

            if (loopPoints.Count < 3)
            {
                return;
            }

            float area = SignedArea(loopPoints);

            if (Mathf.Abs(area) <= PointTolerance * PointTolerance)
            {
                return;
            }

            if (area < 0.0f)
            {
                loopPoints.Reverse();
            }

            TriangulateLoop(loopPoints, localY);
        }

        private void TriangulateLoop(List<Vector2> polygon, float localY)
        {
            int baseIndex = capVertices.Count;

            for (int i = 0; i < polygon.Count; i++)
            {
                capVertices.Add(
                    new Vector3(polygon[i].x, localY, polygon[i].y));
            }

            loopIndices.Clear();

            for (int i = 0; i < polygon.Count; i++)
            {
                loopIndices.Add(i);
            }

            int guard = 0;
            int maxGuard = polygon.Count * polygon.Count + 8;

            while (loopIndices.Count > 3 && guard++ < maxGuard)
            {
                bool foundEar = false;

                for (int i = 0; i < loopIndices.Count; i++)
                {
                    int previous = loopIndices[
                        (i - 1 + loopIndices.Count) % loopIndices.Count];
                    int current = loopIndices[i];
                    int next = loopIndices[(i + 1) % loopIndices.Count];

                    if (Cross(polygon[previous], polygon[current], polygon[next]) <= 0.0f)
                    {
                        continue;
                    }

                    bool containsPoint = false;

                    for (int j = 0; j < loopIndices.Count; j++)
                    {
                        int candidate = loopIndices[j];

                        if (candidate == previous ||
                            candidate == current ||
                            candidate == next)
                        {
                            continue;
                        }

                        if (PointInTriangle(
                                polygon[candidate],
                                polygon[previous],
                                polygon[current],
                                polygon[next]))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                    {
                        continue;
                    }

                    AddCapTriangle(baseIndex, previous, current, next);
                    loopIndices.RemoveAt(i);
                    foundEar = true;
                    break;
                }

                if (!foundEar)
                {
                    break;
                }
            }

            if (loopIndices.Count == 3)
            {
                AddCapTriangle(
                    baseIndex,
                    loopIndices[0],
                    loopIndices[1],
                    loopIndices[2]);
                return;
            }

            for (int i = 1; i < loopIndices.Count - 1; i++)
            {
                AddCapTriangle(
                    baseIndex,
                    loopIndices[0],
                    loopIndices[i],
                    loopIndices[i + 1]);
            }
        }

        private void AddCapTriangle(int baseIndex, int a, int b, int c)
        {
            // The cap faces up. Reverse if the source loop winding produces
            // a downward normal.
            Vector3 va = capVertices[baseIndex + a];
            Vector3 vb = capVertices[baseIndex + b];
            Vector3 vc = capVertices[baseIndex + c];
            Vector3 normal = Vector3.Cross(vb - va, vc - va);

            if (normal.y < 0.0f)
            {
                capTriangles.Add(baseIndex + a);
                capTriangles.Add(baseIndex + c);
                capTriangles.Add(baseIndex + b);
                return;
            }

            capTriangles.Add(baseIndex + a);
            capTriangles.Add(baseIndex + b);
            capTriangles.Add(baseIndex + c);
        }

        private void BuildBoundsFallback(PrintedMeshData mesh, float localY)
        {
            capVertices.Clear();
            capTriangles.Clear();

            Bounds bounds = mesh.LocalBounds;

            if (bounds.size.x <= 0.0001f || bounds.size.z <= 0.0001f)
            {
                return;
            }

            float minX = bounds.min.x;
            float maxX = bounds.max.x;
            float minZ = bounds.min.z;
            float maxZ = bounds.max.z;

            capVertices.Add(new Vector3(minX, localY, minZ));
            capVertices.Add(new Vector3(minX, localY, maxZ));
            capVertices.Add(new Vector3(maxX, localY, maxZ));
            capVertices.Add(new Vector3(maxX, localY, minZ));
            capTriangles.Add(0);
            capTriangles.Add(1);
            capTriangles.Add(2);
            capTriangles.Add(0);
            capTriangles.Add(2);
            capTriangles.Add(3);
        }

        private void ClipTriangle(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            float localY)
        {
            Vector3[] input = { a, b, c };
            float[] distances =
            {
                a.y - localY,
                b.y - localY,
                c.y - localY,
            };

            bool allBelow = distances[0] <= PlaneEpsilon &&
                distances[1] <= PlaneEpsilon &&
                distances[2] <= PlaneEpsilon;
            bool allAbove = distances[0] >= -PlaneEpsilon &&
                distances[1] >= -PlaneEpsilon &&
                distances[2] >= -PlaneEpsilon;

            if (allAbove && !allBelow)
            {
                return;
            }

            if (allBelow)
            {
                int baseIndex = slicedVertices.Count;
                slicedVertices.Add(a);
                slicedVertices.Add(b);
                slicedVertices.Add(c);
                slicedTriangles.Add(baseIndex);
                slicedTriangles.Add(baseIndex + 1);
                slicedTriangles.Add(baseIndex + 2);
                return;
            }

            List<Vector3> polygon = clipPolygon;
            polygon.Clear();

            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                bool currentInside = distances[i] <= PlaneEpsilon;
                bool nextInside = distances[next] <= PlaneEpsilon;

                if (currentInside)
                {
                    polygon.Add(input[i]);
                }

                if (currentInside == nextInside)
                {
                    continue;
                }

                float denominator = distances[i] - distances[next];

                if (Mathf.Abs(denominator) <= 0.0000001f)
                {
                    continue;
                }

                float t = distances[i] / denominator;
                polygon.Add(Vector3.Lerp(input[i], input[next], Mathf.Clamp01(t)));
            }

            if (polygon.Count < 3)
            {
                return;
            }

            int start = slicedVertices.Count;

            for (int i = 0; i < polygon.Count; i++)
            {
                slicedVertices.Add(polygon[i]);
            }

            for (int i = 1; i < polygon.Count - 1; i++)
            {
                slicedTriangles.Add(start);
                slicedTriangles.Add(start + i);
                slicedTriangles.Add(start + i + 1);
            }
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) -
                (b.y - a.y) * (c.x - a.x);
        }

        private static float SignedArea(List<Vector2> polygon)
        {
            float area = 0.0f;

            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        private static bool PointInTriangle(
            Vector2 point,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            float c1 = Cross(a, b, point);
            float c2 = Cross(b, c, point);
            float c3 = Cross(c, a, point);

            return c1 >= 0.0f && c2 >= 0.0f && c3 >= 0.0f;
        }

        private void ClearAdjacency()
        {
            for (int i = 0; i < adjacency.Count; i++)
            {
                adjacency[i].Clear();
            }

            adjacency.Clear();
        }

        private void EnsureAdjacency(int count)
        {
            while (adjacency.Count < count)
            {
                adjacency.Add(new List<int>());
            }
        }
    }
}

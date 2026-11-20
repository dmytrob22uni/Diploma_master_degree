using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Editor
{
    /// <summary>
    /// Generates profile seeds for ObjectsToPrint metas from their prefabs.
    /// Dimensions are reported only; volume and overhang need readable meshes.
    /// </summary>
    public class PrintProfileGeneratorWindow : EditorWindow
    {
        private sealed class ScanResult
        {
            public ObjectToPrintMetaSO Meta;
            public Vector3 Size;
            public float VolumeCubicMeters;
            public float SurfaceArea;
            public float OverhangPercent;
            public float FilamentMeters;
            public float Seconds;
            public bool SupportsRequired;
            public string Note;
            public bool FromScene;

            public string SourceLabel => FromScene ? "Scene" : "Catalog";
        }

        private ObjectToPrintMetasListSO catalog;
        private float filamentDiameterMillimeters = 1.75f;
        private float referenceFlowMm3PerSecond = 12.0f;
        private float wallThicknessMillimeters = 1.2f;
        private float overhangAngleDegrees = 45.0f;
        private float overhangThresholdPercent = 5.0f;

        private readonly List<ScanResult> results = new List<ScanResult>();
        private Vector2 scroll;

        [MenuItem("Tools/Manufacturing/Print Profile Generator")]
        private static void Open()
        {
            GetWindow<PrintProfileGeneratorWindow>("Print Profiles");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Object To Print Profile Generator", EditorStyles.boldLabel);

            catalog = (ObjectToPrintMetasListSO)EditorGUILayout.ObjectField(
                "Catalog",
                catalog,
                typeof(ObjectToPrintMetasListSO),
                false);

            filamentDiameterMillimeters = EditorGUILayout.FloatField(
                "Filament diameter (mm)",
                filamentDiameterMillimeters);
            referenceFlowMm3PerSecond = EditorGUILayout.FloatField(
                "Reference flow (mm3/s)",
                referenceFlowMm3PerSecond);
            wallThicknessMillimeters = EditorGUILayout.FloatField(
                "Wall thickness (mm)",
                wallThicknessMillimeters);
            overhangAngleDegrees = EditorGUILayout.Slider(
                "Overhang angle",
                overhangAngleDegrees,
                10.0f,
                80.0f);
            overhangThresholdPercent = EditorGUILayout.Slider(
                "Supports above (%)",
                overhangThresholdPercent,
                0.0f,
                30.0f);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan"))
                {
                    Scan();
                }

                GUI.enabled = results.Count > 0;
                if (GUILayout.Button("Apply successful scans"))
                {
                    Apply();
                }

                GUI.enabled = true;
            }

            EditorGUILayout.HelpBox(
                "Volume, surface area and overhang require Read/Write enabled on the source model. " +
                "Dimensions are always reported but are derived at runtime; they are not written.",
                MessageType.Info);

            if (results.Count == 0)
            {
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < results.Count; i++)
            {
                ScanResult result = results[i];
                EditorGUILayout.LabelField(
                    result.Meta != null
                        ? $"{result.Meta.Name} ({result.SourceLabel})"
                        : "(missing)",
                    EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    "Size",
                    $"{result.Size.x * 100.0f:0.##} x " +
                    $"{result.Size.y * 100.0f:0.##} x " +
                    $"{result.Size.z * 100.0f:0.##} cm");

                if (!string.IsNullOrEmpty(result.Note))
                {
                    EditorGUILayout.HelpBox(result.Note, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Volume",
                        $"{result.VolumeCubicMeters * 1_000_000.0f:0.##} cm3");
                    EditorGUILayout.LabelField(
                        "Overhang",
                        $"{result.OverhangPercent:0.#}%");
                    float shellFraction = result.VolumeCubicMeters > 0.0f
                        ? Mathf.Clamp01(
                            result.SurfaceArea *
                            wallThicknessMillimeters * 0.001f /
                            result.VolumeCubicMeters)
                        : 0.0f;
                    EditorGUILayout.LabelField(
                        "Shell at scale 1",
                        $"{shellFraction * 100.0f:0.#}%");
                    EditorGUILayout.LabelField(
                        "Seeds",
                        $"{result.FilamentMeters:0.0} m, " +
                        $"{result.Seconds / 60.0f:0.#} min, " +
                        $"supports {result.SupportsRequired}");
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            results.Clear();

            List<ObjectToPrintMetaSO> metas = GetMetas();
            HashSet<ObjectToPrintMetaSO> sceneMetas = GetSceneMetas();

            for (int i = 0; i < metas.Count; i++)
            {
                if (metas[i] == null)
                {
                    continue;
                }

                results.Add(ScanMeta(
                    metas[i],
                    sceneMetas.Contains(metas[i])));
            }

            if (sceneMetas.Count == 0)
            {
                Debug.Log(
                    "[PrintProfileGenerator] No placed ObjectToPrintDataHolder " +
                    "items found in the open scene.");
            }

            Repaint();
        }

        private List<ObjectToPrintMetaSO> GetMetas()
        {
            List<ObjectToPrintMetaSO> metas = new List<ObjectToPrintMetaSO>();
            HashSet<ObjectToPrintMetaSO> seen = new HashSet<ObjectToPrintMetaSO>();

            if (catalog != null && catalog.ObjectToPrintMetasList != null)
            {
                for (int i = 0; i < catalog.ObjectToPrintMetasList.Length; i++)
                {
                    ObjectToPrintMetaSO meta = catalog.ObjectToPrintMetasList[i];

                    if (meta != null && seen.Add(meta))
                    {
                        metas.Add(meta);
                    }
                }
            }

            foreach (ObjectToPrintMetaSO meta in GetSceneMetas())
            {
                if (meta != null && seen.Add(meta))
                {
                    metas.Add(meta);
                }
            }

            if (metas.Count > 0)
            {
                return metas;
            }

            string[] guids = AssetDatabase.FindAssets("t:ObjectToPrintMetaSO");

            for (int i = 0; i < guids.Length; i++)
            {
                ObjectToPrintMetaSO meta =
                    AssetDatabase.LoadAssetAtPath<ObjectToPrintMetaSO>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));

                if (meta != null && seen.Add(meta))
                {
                    metas.Add(meta);
                }
            }

            return metas;
        }

        private static HashSet<ObjectToPrintMetaSO> GetSceneMetas()
        {
            HashSet<ObjectToPrintMetaSO> sceneMetas =
                new HashSet<ObjectToPrintMetaSO>();

            ObjectToPrintDataHolder[] placed =
                Object.FindObjectsByType<ObjectToPrintDataHolder>(
                    FindObjectsInactive.Include);

            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i] != null && placed[i].ObjectToPrintMetaData != null)
                {
                    sceneMetas.Add(placed[i].ObjectToPrintMetaData);
                }
            }

            return sceneMetas;
        }

        private ScanResult ScanMeta(
            ObjectToPrintMetaSO meta,
            bool fromScene)
        {
            ScanResult result = new ScanResult
            {
                Meta = meta,
                FromScene = fromScene
            };

            if (meta.Prefab == null)
            {
                result.Note = "No prefab assigned.";
                return result;
            }

            string path = AssetDatabase.GetAssetPath(meta.Prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
                bool hasBounds = false;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null ||
                        !renderer.enabled ||
                        renderer.bounds.size.sqrMagnitude < 0.0000001f)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                result.Size = hasBounds ? bounds.size : Vector3.zero;

                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
                double volume = 0.0;
                double surfaceArea = 0.0;
                double overhangArea = 0.0;
                int readableMeshes = 0;
                int skippedMeshes = 0;
                float cosThreshold = Mathf.Cos(
                    overhangAngleDegrees * Mathf.Deg2Rad);

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
                        skippedMeshes++;
                        continue;
                    }

                    readableMeshes++;

                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

                    for (int t = 0; t < triangles.Length; t += 3)
                    {
                        Vector3 a = matrix.MultiplyPoint(vertices[triangles[t]]);
                        Vector3 b = matrix.MultiplyPoint(vertices[triangles[t + 1]]);
                        Vector3 c = matrix.MultiplyPoint(vertices[triangles[t + 2]]);

                        volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;

                        Vector3 cross = Vector3.Cross(b - a, c - a);
                        double area = cross.magnitude * 0.5;
                        surfaceArea += area;

                        if (area > 0.0)
                        {
                            Vector3 normal = cross.normalized;

                            if (Vector3.Dot(normal, Vector3.down) > cosThreshold)
                            {
                                overhangArea += area;
                            }
                        }
                    }
                }

                if (readableMeshes == 0)
                {
                    result.Note =
                        "No readable meshes. Enable Read/Write on the source model " +
                        "to generate volume/overhang; dimensions are still reported.";
                    return result;
                }

                result.VolumeCubicMeters = (float)System.Math.Abs(volume);
                result.SurfaceArea = (float)surfaceArea;
                result.OverhangPercent = surfaceArea > 0.0
                    ? (float)(overhangArea / surfaceArea * 100.0)
                    : 0.0f;
                result.SupportsRequired =
                    result.OverhangPercent >= overhangThresholdPercent;

                float radiusMeters =
                    filamentDiameterMillimeters * 0.001f * 0.5f;
                float filamentArea =
                    Mathf.PI * radiusMeters * radiusMeters;

                if (filamentArea > 0.0f)
                {
                    result.FilamentMeters =
                        result.VolumeCubicMeters / filamentArea;
                }

                if (referenceFlowMm3PerSecond > 0.0f)
                {
                    result.Seconds =
                        result.VolumeCubicMeters * 1_000_000_000.0f /
                        referenceFlowMm3PerSecond;
                }

                if (skippedMeshes > 0)
                {
                    result.Note =
                        $"{skippedMeshes} mesh(es) skipped because Read/Write is disabled; " +
                        "the seed is based on readable meshes only.";
                }

                return result;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void Apply()
        {
            int applied = 0;
            StringBuilder report = new StringBuilder();

            for (int i = 0; i < results.Count; i++)
            {
                ScanResult result = results[i];

                if (result.Meta == null || result.VolumeCubicMeters <= 0.0f)
                {
                    continue;
                }

                Undo.RecordObject(result.Meta, "Generate print profile");

                if (result.Meta.PrintProfile == null)
                {
                    result.Meta.PrintProfile = new ObjectToPrintPrintProfile();
                }

                result.Meta.PrintProfile.FilamentMetersAt100PercentInfill =
                    result.FilamentMeters;
                result.Meta.PrintProfile.PrintTimeSecondsAt100PercentInfill =
                    result.Seconds;
                result.Meta.PrintProfile.SupportsRequired =
                    result.SupportsRequired;
                result.Meta.PrintProfile.SupportOverhangPercent =
                    result.OverhangPercent;
                result.Meta.PrintProfile.WallThicknessMillimeters =
                    Mathf.Max(0.0f, wallThicknessMillimeters);

                EditorUtility.SetDirty(result.Meta);
                applied++;
                report.AppendLine(
                    $"{result.Meta.Name}: {result.FilamentMeters:0.0} m, " +
                    $"{result.Seconds / 60.0f:0.#} min, " +
                    $"overhang {result.OverhangPercent:0.#}%");
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[PrintProfileGenerator] Applied {applied} profile seed(s).\n{report}");
            Repaint();
        }
    }
}

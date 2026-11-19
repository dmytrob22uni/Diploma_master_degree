using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.Game.Helper;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.ObjectsToPrint;

namespace Coursework_Master_Degree.Editor
{
    /// <summary>
    /// Lists placed device/printer/object/spool instances in the open scene so
    /// the profile generator and manual calibration can find them quickly.
    /// </summary>
    public class DetectPlacedItemsWindow : EditorWindow
    {
        private sealed class Entry
        {
            public Object Target;
            public string Kind;
            public string Label;
            public string Placement;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private Vector2 scroll;

        [MenuItem("Tools/Scene/Detect Placed Items")]
        private static void Open()
        {
            GetWindow<DetectPlacedItemsWindow>("Placed Items");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan open scene"))
                {
                    Scan();
                }

                if (GUILayout.Button("Run placement detector"))
                {
                    RunDetector();
                }
            }

            EditorGUILayout.HelpBox(
                "Detects ObjectToPrint placeables, printers, devices and spools " +
                "in loaded scenes. Prefab-stage contents are not included.",
                MessageType.Info);

            if (entries.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                $"{entries.Count} placed item(s)",
                EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        $"[{entry.Kind}] {entry.Label} — {entry.Placement}",
                        GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Select", GUILayout.Width(60.0f)))
                    {
                        Selection.activeObject = entry.Target;
                        EditorGUIUtility.PingObject(entry.Target);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            entries.Clear();

            ObjectToPrintDataHolder[] objects =
                Object.FindObjectsByType<ObjectToPrintDataHolder>(
                    FindObjectsInactive.Include);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    continue;
                }

                string meta = objects[i].ObjectToPrintMetaData != null
                    ? objects[i].ObjectToPrintMetaData.Name
                    : "no meta";

                entries.Add(new Entry
                {
                    Target = objects[i].gameObject,
                    Kind = "Object",
                    Label = $"{objects[i].name} ({meta})",
                    Placement = DescribePlacement(objects[i])
                });
            }

            Printer[] printers =
                Object.FindObjectsByType<Printer>(FindObjectsInactive.Include);

            for (int i = 0; i < printers.Length; i++)
            {
                if (printers[i] == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Target = printers[i].gameObject,
                    Kind = "Printer",
                    Label = printers[i].DisplayName,
                    Placement = DescribePlacement(printers[i].gameObject)
                });
            }

            Device[] devices =
                Object.FindObjectsByType<Device>(FindObjectsInactive.Include);

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Target = devices[i].gameObject,
                    Kind = "Device",
                    Label = devices[i].DisplayName,
                    Placement = DescribePlacement(devices[i].gameObject)
                });
            }

            ItemFFFSpoolDataHolder[] spools =
                Object.FindObjectsByType<ItemFFFSpoolDataHolder>(
                    FindObjectsInactive.Include);

            for (int i = 0; i < spools.Length; i++)
            {
                if (spools[i] == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Target = spools[i].gameObject,
                    Kind = "Spool",
                    Label = spools[i].name,
                    Placement = DescribePlacement(spools[i].gameObject)
                });
            }

            Debug.Log(
                $"[DetectPlacedItems] Found {entries.Count} placed item(s).");
        }

        private void RunDetector()
        {
            ItemsPlacementDetector[] detectors =
                Object.FindObjectsByType<ItemsPlacementDetector>(
                    FindObjectsInactive.Include);

            if (detectors.Length == 0)
            {
                Debug.LogWarning(
                    "[DetectPlacedItems] No ItemsPlacementDetector found in the open scene.");
                return;
            }

            for (int i = 0; i < detectors.Length; i++)
            {
                ItemsPlacementDetector detector = detectors[i];

                if (detector == null)
                {
                    continue;
                }

                bool logReasons = detector.LogReasons;
                detector.LogReasons = true;
                detector.DetectPlacedItems();
                detector.LogReasons = logReasons;
                EditorUtility.SetDirty(detector);
            }

            Scan();
        }

        private static string DescribePlacement(ObjectToPrintDataHolder holder)
        {
            return DescribePlacement(
                holder != null ? holder.gameObject : null);
        }

        private static string DescribePlacement(GameObject target)
        {
            if (target == null)
            {
                return "missing";
            }

            ISurfacePlaceable placeable = target.GetComponent<ISurfacePlaceable>();

            if (placeable == null)
            {
                for (int i = 0; i < target.transform.childCount; i++)
                {
                    placeable = target.transform.GetChild(i)
                        .GetComponent<ISurfacePlaceable>();

                    if (placeable != null)
                    {
                        break;
                    }
                }
            }

            if (placeable == null)
            {
                placeable = target.GetComponentInChildren<ISurfacePlaceable>(true);
            }

            if (placeable == null)
            {
                return "not a surface-placeable item";
            }

            if (!placeable.IsPlaced)
            {
                return "free / not placed";
            }

            string surface = placeable.ParentSurfaceDataHolder != null
                ? placeable.ParentSurfaceDataHolder.gameObject.name
                : "unknown surface";

            return $"placed on {surface}";
        }
    }
}

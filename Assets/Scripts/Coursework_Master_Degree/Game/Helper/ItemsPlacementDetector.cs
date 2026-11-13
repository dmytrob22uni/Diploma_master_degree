using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Detects which items are resting on a shelf before entering play mode and
    /// applies that state through <see cref="ISurfacePlaceable"/>.
    /// Items that are not supported by a shelf get a warning with the detected reason,
    /// so scene setup mistakes do not silently survive into the game.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    public class ItemsPlacementDetector : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip(
            "Layers of shelf colliders that can hold an item. " +
            "Only colliders registered in ItemSurfaceDataHolder are accepted, " +
            "so the floor layer can be included for surfaces such as the hangar floor. " +
            "Defaults to 'surface' + 'floor' when left empty.")]
        public LayerMask SurfaceLayer = (1 << 14) | (1 << 12);

        [Tooltip("Distance vertically up from each bounds corner to start the down raycast")]
        [Min(0.0f)]
        public float UpOffset = 0.05f;

        [Tooltip("Maximum distance to raycast down against a shelf")]
        [Min(0.0f)]
        public float DownCheck = 0.5f;

        [Tooltip("Maximum allowed vertical gap between an item corner and the shelf")]
        [Min(0.0f)]
        public float MaxGap = 0.02f;

        [Tooltip("Print why an item was not detected as placed on a shelf")]
        public bool LogReasons = true;

        private const float DiagnosticDistance = 10.0f;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                DetectPlacedItems();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                DetectPlacedItems();
            }
        }

        public void DetectPlacedItems()
        {
            int shelfMask = SurfaceLayer.value != 0
                ? SurfaceLayer.value
                : (1 << 14) | (1 << 12);

            var processed = new HashSet<GameObject>();
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);

            int placedCount = 0;
            int removedCount = 0;

            foreach (var behaviour in behaviours)
            {
                if (behaviour is not ISurfacePlaceable placeable)
                {
                    continue;
                }

                GameObject itemGameObject = behaviour.gameObject;
                if (!processed.Add(itemGameObject))
                {
                    continue;
                }

                if (ShouldSkip(behaviour))
                {
                    continue;
                }

                var result = EvaluatePlacement(itemGameObject, shelfMask);

                bool isChanged;
                if (result.Surface != null)
                {
                    isChanged = placeable.PlaceOnSurface(result.Surface);
                    if (isChanged)
                    {
                        placedCount++;
                    }
                }
                else
                {
                    isChanged = placeable.RemoveFromSurface();
                    if (isChanged)
                    {
                        removedCount++;
                    }

                    if (LogReasons)
                    {
                        Debug.LogWarning(
                            $"[ItemsPlacementDetector] '{itemGameObject.name}' is not placed on a shelf: " +
                            result.Reason,
                            itemGameObject);
                    }
                }

                if (isChanged)
                {
                    MarkChanged(behaviour);
                    MarkChanged(result.Surface);
                }
            }

            if (LogReasons && (placedCount > 0 || removedCount > 0))
            {
                Debug.Log(
                    $"[ItemsPlacementDetector] Placement state updated: " +
                    $"{placedCount} placed, {removedCount} removed from shelves.");
            }
        }

        private static bool ShouldSkip(MonoBehaviour behaviour)
        {
            // Parts already installed into a PartLocation are handled by the installation system.
            // Free parts / items nested inside another item (e.g. PC free parts reset points)
            // are also not independent scene placements.
            for (Transform parent = behaviour.transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.GetComponent<PartLocation>() != null)
                {
                    return true;
                }

                if (parent.GetComponent<ItemDataHolder>() != null ||
                    parent.GetComponent<ItemFreePartDataHolder>() != null ||
                    parent.GetComponent<ObjectToPrintDataHolder>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private PlacementResult EvaluatePlacement(GameObject itemGameObject, int shelfMask)
        {
            if (!TryGetWorldBounds(itemGameObject, out var bounds))
            {
                return PlacementResult.Invalid(
                    "it has no active collider or renderer to determine its bounds from");
            }

            Vector3[] corners =
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            };

            Collider sharedCollider = null;
            ItemSurfaceDataHolder sharedSurface = null;
            var supportingSurfaces = new HashSet<ItemSurfaceDataHolder>();
            int supportedCorners = 0;
            bool hasDifferentCollider = false;
            bool hasUnsupportedCorner = false;
            float largestGap = 0.0f;
            Vector3 firstUnsupportedCorner = default;

            foreach (var corner in corners)
            {
                Vector3 origin = corner + (Vector3.up * UpOffset);

                if (!TryRaycastDown(origin, DownCheck, shelfMask, itemGameObject, out var hit))
                {
                    if (!hasUnsupportedCorner)
                    {
                        firstUnsupportedCorner = corner;
                        hasUnsupportedCorner = true;
                    }

                    continue;
                }

                var surface = hit.collider.GetComponentInParent<ItemSurfaceDataHolder>();
                if (surface == null || !surface.IsShelfCollider(hit.collider))
                {
                    if (!hasUnsupportedCorner)
                    {
                        firstUnsupportedCorner = corner;
                        hasUnsupportedCorner = true;
                    }

                    continue;
                }

                supportedCorners++;
                supportingSurfaces.Add(surface);

                if (sharedCollider == null)
                {
                    sharedCollider = hit.collider;
                    sharedSurface = surface;
                }
                else if (hit.collider != sharedCollider)
                {
                    hasDifferentCollider = true;
                }

                largestGap = Mathf.Max(largestGap, corner.y - hit.point.y);
            }

            if (supportedCorners == corners.Length)
            {
                if (supportingSurfaces.Count > 1)
                {
                    return PlacementResult.Invalid(
                        $"it spans multiple surfaces ({string.Join(", ", GetSurfaceNames(supportingSurfaces))})");
                }

                if (hasDifferentCollider)
                {
                    return PlacementResult.Invalid(
                        $"it spans multiple shelves of '{sharedSurface.gameObject.name}'");
                }

                if (largestGap > MaxGap)
                {
                    return PlacementResult.Invalid(
                        $"it floats {largestGap:0.###} above the shelf " +
                        $"'{sharedSurface.gameObject.name}' (max allowed gap is {MaxGap:0.###})");
                }

                return PlacementResult.Valid(sharedSurface);
            }

            return PlacementResult.Invalid(DescribeUnsupportedPlacement(
                itemGameObject,
                corners,
                supportedCorners,
                firstUnsupportedCorner,
                supportingSurfaces));
        }

        private string DescribeUnsupportedPlacement(
            GameObject itemGameObject,
            Vector3[] corners,
            int supportedCorners,
            Vector3 firstUnsupportedCorner,
            HashSet<ItemSurfaceDataHolder> supportingSurfaces)
        {
            if (supportedCorners > 0)
            {
                return
                    $"it overhangs the shelf (only {supportedCorners} of {corners.Length} corners are supported" +
                    (supportingSurfaces.Count > 0
                        ? $" by '{string.Join(", ", GetSurfaceNames(supportingSurfaces))}'"
                        : string.Empty) +
                    ")";
            }

            Vector3 origin = firstUnsupportedCorner + (Vector3.up * UpOffset);
            if (!TryRaycastDown(
                    origin,
                    DiagnosticDistance,
                    Physics.DefaultRaycastLayers,
                    itemGameObject,
                    out var hit))
            {
                return "it floats in the air (there is no collider underneath it)";
            }

            var surface = hit.collider.GetComponentInParent<ItemSurfaceDataHolder>();
            if (surface != null)
            {
                if (surface.IsShelfCollider(hit.collider))
                {
                    return $"it is above shelf '{surface.gameObject.name}', but the shelf is outside the down-check range";
                }

                return $"it rests on a non-shelf part of '{surface.gameObject.name}'";
            }

            var parentItem = hit.collider.GetComponentInParent<ItemDataHolder>();
            if (parentItem != null)
            {
                return $"it is stacked on item '{parentItem.gameObject.name}' instead of a shelf";
            }

            string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
            if (string.IsNullOrEmpty(layerName))
            {
                layerName = hit.collider.gameObject.layer.ToString();
            }

            return
                $"it rests on '{hit.collider.gameObject.name}' (layer '{layerName}'), " +
                "which is not a shelf";
        }

        private static IEnumerable<string> GetSurfaceNames(HashSet<ItemSurfaceDataHolder> surfaces)
        {
            foreach (var surface in surfaces)
            {
                yield return surface.gameObject.name;
            }
        }

        private static bool TryGetWorldBounds(GameObject itemGameObject, out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = default;

            foreach (var collider in itemGameObject.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBounds)
            {
                return true;
            }

            foreach (var renderer in itemGameObject.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
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

            return hasBounds;
        }

        private static bool TryRaycastDown(
            Vector3 origin,
            float distance,
            int layerMask,
            GameObject ignoredRoot,
            out RaycastHit closestHit)
        {
            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            bool isFound = false;

            var hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                distance,
                layerMask,
                QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (ignoredRoot != null &&
                    hit.collider.transform.IsChildOf(ignoredRoot.transform))
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHit = hit;
                    isFound = true;
                }
            }

            return isFound;
        }

        private static void MarkChanged(Object target)
        {
#if UNITY_EDITOR
            if (target == null || Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorUtility.SetDirty(target);

            if (target is Component component && component.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
#endif
        }

        private readonly struct PlacementResult
        {
            public readonly ItemSurfaceDataHolder Surface;
            public readonly string Reason;

            private PlacementResult(ItemSurfaceDataHolder surface, string reason)
            {
                Surface = surface;
                Reason = reason;
            }

            public static PlacementResult Valid(ItemSurfaceDataHolder surface)
            {
                return new PlacementResult(surface, string.Empty);
            }

            public static PlacementResult Invalid(string reason)
            {
                return new PlacementResult(null, reason);
            }
        }
    }
}

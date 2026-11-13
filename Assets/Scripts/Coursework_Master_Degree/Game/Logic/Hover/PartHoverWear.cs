using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ScriptableObjects.Items;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class PartHoverWear : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for parts and their wear ghosts only. " +
            "Printer frame, item, surface, etc. are intentionally ignored.")]
        public LayerMask PartLayer;

        [Header("Interface Units")]
        public TextMeshProUGUI ItemHoverWearNameText;
        public TextMeshProUGUI ItemHoverWearDescriptionText;
        public TextMeshProUGUI ItemHoverWearLevelText;

        private const float EyesMoveThresholdSqr = 0.0001f;
        private const float EyesRotationThreshold = 0.1f;
        private const float ScrollDeadZone = 0.01f;

        private RaycastHit[] cachedHits = new RaycastHit[32];
        private readonly List<HoveredPart> cachedHoveredParts = new();
        private readonly HashSet<PartCacheKey> cachedPartKeys = new();

        private Vector3 cachedEyesPosition;
        private Quaternion cachedEyesRotation;

        private int scrollIndex;

        private int lastScrollFrame = -1;
        private float lastScrollValue;

        private PartsAssembly targetAssembly;

        private void Awake()
        {
            // Fallback for prefabs set up before the dedicated part layer mask
            // was introduced on the component.
            if (PartLayer.value == 0)
            {
                int partLayer = LayerMask.NameToLayer("part");
                if (partLayer >= 0)
                {
                    PartLayer |= 1 << partLayer;
                }

                int wearLevelLayer = LayerMask.NameToLayer("wear_level");
                if (wearLevelLayer >= 0)
                {
                    PartLayer |= 1 << wearLevelLayer;
                }
            }

            ClearHover();
        }

        private void Update()
        {
            if (!IsEyesChanged())
            {
                return;
            }

            PerformHoverCheck();
        }

        private bool IsEyesChanged()
        {
            if (Eyes == null)
            {
                return false;
            }

            if (Vector3.SqrMagnitude(cachedEyesPosition - Eyes.position) > EyesMoveThresholdSqr)
            {
                return true;
            }

            if (Quaternion.Angle(cachedEyesRotation, Eyes.rotation) > EyesRotationThreshold)
            {
                return true;
            }

            return false;
        }

        private void PerformHoverCheck()
        {
            if (Eyes == null)
            {
                ClearCachedParts();
                ClearHover();

                return;
            }

            cachedEyesPosition = Eyes.position;
            cachedEyesRotation = Eyes.rotation;

            Ray ray = new(
                Eyes.position,
                Eyes.forward);

            int hitNumber = Physics.RaycastNonAlloc(
                ray,
                cachedHits,
                HoverDistance,
                PartLayer,
                QueryTriggerInteraction.Ignore);

            if (hitNumber == 0)
            {
                ClearCachedParts();
                ClearHover();

                return;
            }

            System.Array.Sort(
                cachedHits,
                0,
                hitNumber,
                RaycastHitDistanceComparer.Instance);

            // reset layer counter when the primary hovered part changed
            PartCacheKey previousPrimaryPart = cachedHoveredParts.Count > 0
                ? cachedHoveredParts[0].Key
                : default;

            cachedHoveredParts.Clear();
            cachedPartKeys.Clear();

            for (int index = 0; index < hitNumber; index++)
            {
                if (!TryGetHoveredPart(cachedHits[index], out HoveredPart hoveredPart))
                {
                    continue;
                }

                // Parts often consist of several colliders. Keep only the closest
                // hit for every unique physical part, so scrolling cycles parts
                // instead of cycling colliders of the same part.
                if (!cachedPartKeys.Add(hoveredPart.Key))
                {
                    continue;
                }

                cachedHoveredParts.Add(hoveredPart);
            }

            if (cachedHoveredParts.Count == 0)
            {
                ClearHover();

                return;
            }

            if (cachedHoveredParts[0].Key != previousPrimaryPart)
            {
                scrollIndex = 0;
            }

            scrollIndex = Mathf.Clamp(scrollIndex, 0, cachedHoveredParts.Count - 1);

            SetupInfo(cachedHoveredParts[scrollIndex]);
        }

        private bool TryGetHoveredPart(RaycastHit hit, out HoveredPart hoveredPart)
        {
            // A wear ghost represents an installed part, but the ghost itself
            // is not an ItemDataHolder. Resolve it back to the part instance.
            ItemGhostWearLevelDataHolder wearLevelGhost =
                hit.collider.GetComponentInParent<ItemGhostWearLevelDataHolder>();

            if (wearLevelGhost != null && wearLevelGhost.ItemInstance != null)
            {
                if (!IsPartOfTargetAssembly(wearLevelGhost.transform))
                {
                    hoveredPart = default;
                    return false;
                }

                ItemDataHolder wearPart = wearLevelGhost.ItemInstance.GetComponent<ItemDataHolder>();

                if (wearPart != null)
                {
                    hoveredPart = CreateHoveredPart(wearPart, hit.collider);
                    return true;
                }
            }

            ItemDataHolder item = hit.collider.GetComponentInParent<ItemDataHolder>();

            if (item != null)
            {
                if (!IsPartOfTargetAssembly(item.transform))
                {
                    hoveredPart = default;
                    return false;
                }

                hoveredPart = CreateHoveredPart(item, hit.collider);
                return true;
            }

            hoveredPart = default;
            return false;
        }

        private bool IsPartOfTargetAssembly(Transform partTransform)
        {
            // Hover info belongs to the inspected assembly only. Parts of
            // nearby items are intentionally ignored.
            if (targetAssembly == null)
            {
                return true;
            }

            return targetAssembly.IsPartOfAssembly(partTransform);
        }

        private static HoveredPart CreateHoveredPart(ItemDataHolder item, Collider closestHit)
        {
            return new HoveredPart(
                new PartCacheKey(item.gameObject, item.ItemMetaData),
                item.ItemMetaData,
                item.GetComponent<ItemWearState>(),
                closestHit);
        }

        private void SetupInfo(HoveredPart part)
        {
            ItemMetaSO metaData = part.MetaData;

            string partName = metaData != null
                ? metaData.Name
                : part.Key.Root != null
                    ? part.Key.Root.name
                    : default;

            string description = metaData != null
                ? metaData.Description
                : default;

            SetText(ItemHoverWearNameText, partName);
            SetText(ItemHoverWearDescriptionText, description);
            SetupWearLevel(part.WearState);
        }

        private void SetupWearLevel(ItemWearState wearState)
        {
            if (wearState == null)
            {
                ClearText(ItemHoverWearLevelText);

                return;
            }

            int wearLevelPercent = Mathf.RoundToInt(wearState.CurrentCondition);

            SetText(ItemHoverWearLevelText, $"{wearLevelPercent} %");
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
            text.gameObject.SetActive(true);
        }

        private void ClearCachedParts()
        {
            cachedHoveredParts.Clear();
            cachedPartKeys.Clear();
            scrollIndex = 0;
        }

        private void ClearHover()
        {
            ClearText(ItemHoverWearNameText);
            ClearText(ItemHoverWearDescriptionText);
            ClearText(ItemHoverWearLevelText);
        }

        private static void ClearText(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.text = default;
            text.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ClearCachedParts();
            ClearHover();

            cachedEyesPosition = default;
            cachedEyesRotation = default;
        }

        // === Player Controller invoked ===

        public void SetTargetAssembly(PartsAssembly partsAssembly)
        {
            targetAssembly = partsAssembly;

            ClearCachedParts();
            ClearHover();
        }

        public void ApplyAnotherHoverLayer(Vector2 value)
        {
            if (Mathf.Abs(value.y) < ScrollDeadZone)
            {
                return;
            }

            // ignore duplicated started / performed callbacks for the same scroll tick
            if (Time.frameCount == lastScrollFrame && Mathf.Approximately(lastScrollValue, value.y))
            {
                return;
            }

            lastScrollFrame = Time.frameCount;
            lastScrollValue = value.y;

            // The hover check is normally refreshed only when eyes move.
            // Make the first scroll after entering the state reliable even if
            // no hover pass has happened yet.
            if (cachedHoveredParts.Count == 0)
            {
                PerformHoverCheck();
            }

            if (cachedHoveredParts.Count == 0)
            {
                return;
            }

            int direction = value.y > 0.0f ? 1 : -1;
            scrollIndex = (scrollIndex + direction + cachedHoveredParts.Count) % cachedHoveredParts.Count;

            SetupInfo(cachedHoveredParts[scrollIndex]);
        }

        private readonly struct HoveredPart
        {
            public readonly PartCacheKey Key;
            public readonly ItemMetaSO MetaData;
            public readonly ItemWearState WearState;
            public readonly Collider ClosestHit;

            public HoveredPart(PartCacheKey key, ItemMetaSO metaData, ItemWearState wearState, Collider closestHit)
            {
                Key = key;
                MetaData = metaData;
                WearState = wearState;
                ClosestHit = closestHit;
            }
        }

        private readonly struct PartCacheKey : System.IEquatable<PartCacheKey>
        {
            public readonly GameObject Root;
            public readonly ItemMetaSO MetaData;

            public PartCacheKey(GameObject root, ItemMetaSO metaData)
            {
                Root = root;
                MetaData = metaData;
            }

            public bool Equals(PartCacheKey other)
            {
                return Root == other.Root && MetaData == other.MetaData;
            }

            public override bool Equals(object obj)
            {
                return obj is PartCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Root != null ? Root.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (MetaData != null ? MetaData.GetHashCode() : 0);

                    return hashCode;
                }
            }

            public static bool operator ==(PartCacheKey left, PartCacheKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(PartCacheKey left, PartCacheKey right)
            {
                return !left.Equals(right);
            }
        }

        private class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new();

            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
    }
}

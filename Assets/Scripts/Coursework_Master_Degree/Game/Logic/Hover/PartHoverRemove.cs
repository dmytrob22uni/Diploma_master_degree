using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class PartHoverRemove : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for parts and their installation ghosts only. " +
            "Printer frame, item, surface, etc. are intentionally ignored.")]
        public LayerMask PartLayer;

        [Header("Blocked Remove Settings")]
        [Tooltip("Seconds to show the removal ghosts of the parts blocking the removal")]
        public float BlockedDependenciesGhostDuration = 2.0f;

        [Header("Interface Units")]
        public TextMeshProUGUI ItemHoverRemoveNameText;
        public GameObject RemoveRoot;
        public TextMeshProUGUI ItemHoverRemoveText;

        private const string RemoveInfo = "Remove";
        private const string RemoveBlockedInfo = "Remove (Blocked by dependencies)";

        private const float EyesMoveThresholdSqr = 0.0001f;
        private const float EyesRotationThreshold = 0.1f;
        private const float ScrollDeadZone = 0.01f;

        private static readonly int InstallationStatePropertyID = Shader.PropertyToID("_installation_state");

        private RaycastHit[] cachedHits = new RaycastHit[32];
        private readonly List<HoveredPart> cachedHoveredParts = new();
        private readonly HashSet<PartCacheKey> cachedPartKeys = new();
        private readonly List<ShownPartGhost> shownPartGhosts = new();

        private Vector3 cachedEyesPosition;
        private Quaternion cachedEyesRotation;

        private int scrollIndex;

        private int lastScrollFrame = -1;
        private float lastScrollValue;

        private PartsAssembly targetAssembly;

        private PartCacheKey shownHoveredPartKey;
        private bool hasShownHoveredPart;

        private MaterialPropertyBlock installationPropertyBlock;
        private float blockedUntilTime = -1.0f;

        private bool IsHoverBlocked => blockedUntilTime > 0.0f;

        private void Awake()
        {
            // Fallback for prefabs set up before the dedicated part layer mask
            // was introduced on the component.
            if (PartLayer.value == 0)
            {
                int partLayer = LayerMask.NameToLayer("part");
                if (partLayer >= 0)
                {
                    PartLayer = 1 << partLayer;
                }
            }

            ClearHover();
        }

        private void Update()
        {
            if (IsHoverBlocked)
            {
                if (Time.time < blockedUntilTime)
                {
                    return;
                }

                EndBlockedHover();
            }

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
                ClearShownPartGhosts();
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
                ClearShownPartGhosts();
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
                ClearShownPartGhosts();
                ClearHover();

                return;
            }

            if (cachedHoveredParts[0].Key != previousPrimaryPart)
            {
                scrollIndex = 0;
            }

            scrollIndex = Mathf.Clamp(scrollIndex, 0, cachedHoveredParts.Count - 1);

            SetupHover(cachedHoveredParts[scrollIndex]);
        }

        private bool TryGetHoveredPart(RaycastHit hit, out HoveredPart hoveredPart)
        {
            // An installation ghost represents an installed part, but the ghost
            // itself is not an ItemDataHolder. Resolve it back to the part instance.
            ItemGhostInstallationDataHolder installationGhost =
                hit.collider.GetComponentInParent<ItemGhostInstallationDataHolder>();

            if (installationGhost != null && installationGhost.ItemInstance != null)
            {
                ItemDataHolder ghostPart = installationGhost.ItemInstance.GetComponent<ItemDataHolder>();

                if (ghostPart != null && TryCreateHoveredPart(ghostPart, hit.collider, out hoveredPart))
                {
                    return true;
                }
            }

            ItemDataHolder item = hit.collider.GetComponentInParent<ItemDataHolder>();

            if (item != null && TryCreateHoveredPart(item, hit.collider, out hoveredPart))
            {
                return true;
            }

            hoveredPart = default;
            return false;
        }

        private bool TryCreateHoveredPart(ItemDataHolder item, Collider closestHit, out HoveredPart hoveredPart)
        {
            if (!IsPartOfTargetAssembly(item.transform))
            {
                hoveredPart = default;
                return false;
            }

            PartLocation partLocation = item.GetComponentInParent<PartLocation>();

            if (partLocation == null || !partLocation.IsOccupied)
            {
                hoveredPart = default;
                return false;
            }

            hoveredPart = new HoveredPart(
                new PartCacheKey(item.gameObject, item.ItemMetaData),
                item.ItemMetaData,
                item,
                partLocation,
                closestHit);

            return true;
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

        private void SetupHover(HoveredPart part)
        {
            if (!hasShownHoveredPart || shownHoveredPartKey != part.Key)
            {
                ClearShownPartGhosts();

                ShowPartGhost(part.Part);

                shownHoveredPartKey = part.Key;
                hasShownHoveredPart = true;
            }

            bool isBlocked = GetOccupiedDependencyPartLocations(part.PartLocation).Count > 0;

            SetupInfo(part, isBlocked);
        }

        private void SetupInfo(HoveredPart part, bool isBlocked)
        {
            ItemMetaSO metaData = part.MetaData;

            string partName = metaData != null
                ? metaData.Name
                : part.Key.Root != null
                    ? part.Key.Root.name
                    : default;

            SetText(ItemHoverRemoveNameText, partName);
            SetText(ItemHoverRemoveText, isBlocked ? RemoveBlockedInfo : RemoveInfo);

            if (RemoveRoot != null)
            {
                RemoveRoot.SetActive(true);
            }
        }

        private void ShowPartGhost(ItemDataHolder part)
        {
            if (part == null || IsPartGhostShown(part))
            {
                return;
            }

            ItemGhostDataHolder partGhost = GetPartInstallationGhost(part);
            if (partGhost == null)
            {
                return;
            }

            SetupInstallationState(partGhost);

            ShownPartGhost shownPartGhost = new(partGhost, part);
            HidePartMeshRenderers(shownPartGhost, part);

            shownPartGhosts.Add(shownPartGhost);
        }

        private bool IsPartGhostShown(ItemDataHolder part)
        {
            foreach (ShownPartGhost shownPartGhost in shownPartGhosts)
            {
                if (shownPartGhost.Part == part)
                {
                    return true;
                }
            }

            return false;
        }

        private ItemGhostDataHolder GetPartInstallationGhost(ItemDataHolder part)
        {
            Transform parent = targetAssembly != null
                ? targetAssembly.transform
                : part.transform.parent;

            if (part.TryGetGhostInstance(ItemGhostType.Installation, out ItemGhostDataHolder installationGhost) &&
                installationGhost != null)
            {
                // Keep the mutual part-to-ghost reference in sync, because the
                // stored ghost is reused across hovers and inspections.
                installationGhost.ItemInstance = part.gameObject;

                installationGhost.transform.SetParent(parent, true);
                installationGhost.transform.SetPositionAndRotation(
                    part.transform.position,
                    part.transform.rotation);

                installationGhost.gameObject.SetActive(true);

                return installationGhost;
            }

            if (!IsGhostTypeAvailable(part.ItemGhostMetaSO, ItemGhostType.Installation))
            {
                return null;
            }

            return GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                part,
                ItemGhostType.Installation,
                part.transform.position,
                part.transform.rotation,
                parent);
        }

        private void SetupInstallationState(ItemGhostDataHolder partGhost)
        {
            if (partGhost.MeshRenderersList == null)
            {
                return;
            }

            installationPropertyBlock ??= new MaterialPropertyBlock();
            installationPropertyBlock.Clear();
            installationPropertyBlock.SetFloat(InstallationStatePropertyID, 1.0f);

            foreach (MeshRenderer meshRenderer in partGhost.MeshRenderersList)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(installationPropertyBlock);
            }
        }

        private static void HidePartMeshRenderers(ShownPartGhost shownPartGhost, ItemDataHolder part)
        {
            foreach (MeshRenderer meshRenderer in part.GetComponentsInChildren<MeshRenderer>())
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                shownPartGhost.PartMeshRenderers.Add(meshRenderer);
                shownPartGhost.PartMeshRenderersStates.Add(meshRenderer.enabled);

                meshRenderer.enabled = false;
            }
        }

        private void ClearShownPartGhosts()
        {
            foreach (ShownPartGhost shownPartGhost in shownPartGhosts)
            {
                if (shownPartGhost.Ghost != null)
                {
                    shownPartGhost.Ghost.gameObject.SetActive(false);
                }

                for (int index = 0; index < shownPartGhost.PartMeshRenderers.Count; index++)
                {
                    MeshRenderer meshRenderer = shownPartGhost.PartMeshRenderers[index];
                    if (meshRenderer == null)
                    {
                        continue;
                    }

                    meshRenderer.enabled = shownPartGhost.PartMeshRenderersStates[index];
                }
            }

            shownPartGhosts.Clear();

            shownHoveredPartKey = default;
            hasShownHoveredPart = false;
        }

        private static List<PartLocation> GetOccupiedDependencyPartLocations(PartLocation partLocation)
        {
            if (partLocation == null ||
                !partLocation.IsHaveDependencies ||
                partLocation.DependencyLocationsList == null)
            {
                return new List<PartLocation>();
            }

            return partLocation.GetOccupiedDependencyLocations();
        }

        private static bool IsGhostTypeAvailable(ItemGhostMetaSO itemGhostMeta, ItemGhostType type)
        {
            if (itemGhostMeta == null || itemGhostMeta.ItemGhostTypesToPrefabsList == null)
            {
                return false;
            }

            foreach (var itemGhostTypeToPrefab in itemGhostMeta.ItemGhostTypesToPrefabsList)
            {
                if (itemGhostTypeToPrefab.ItemGhostType == type &&
                    itemGhostTypeToPrefab.ItemGhostGameObject != null)
                {
                    return true;
                }
            }

            return false;
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
            ClearText(ItemHoverRemoveNameText);
            ClearText(ItemHoverRemoveText);

            if (RemoveRoot != null)
            {
                RemoveRoot.SetActive(false);
            }
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

        private void EndBlockedHover()
        {
            blockedUntilTime = -1.0f;

            ClearShownPartGhosts();
            ClearCachedParts();
            ClearHover();

            cachedEyesPosition = default;
            cachedEyesRotation = default;
        }

        private void OnDisable()
        {
            ClearCachedParts();
            ClearShownPartGhosts();
            ClearHover();

            blockedUntilTime = -1.0f;

            cachedEyesPosition = default;
            cachedEyesRotation = default;

            ResetScrollThrottle();
        }

        // === Player Controller invoked ===

        public void SetTargetAssembly(PartsAssembly partsAssembly)
        {
            targetAssembly = partsAssembly;

            blockedUntilTime = -1.0f;

            ClearCachedParts();
            ClearShownPartGhosts();
            ClearHover();

            ResetScrollThrottle();
        }

        private void ResetScrollThrottle()
        {
            lastScrollFrame = -1;
            lastScrollValue = default;
        }

        public void ApplyAnotherHoverLayer(Vector2 value)
        {
            if (IsHoverBlocked)
            {
                return;
            }

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

            SetupHover(cachedHoveredParts[scrollIndex]);
        }

        public void ApplyRemovePart()
        {
            if (IsHoverBlocked)
            {
                return;
            }

            if (cachedHoveredParts.Count == 0)
            {
                PerformHoverCheck();
            }

            if (cachedHoveredParts.Count == 0)
            {
                return;
            }

            HoveredPart part = cachedHoveredParts[Mathf.Clamp(scrollIndex, 0, cachedHoveredParts.Count - 1)];
            PartLocation partLocation = part.PartLocation;

            if (partLocation == null)
            {
                return;
            }

            List<PartLocation> occupiedDependencies = GetOccupiedDependencyPartLocations(partLocation);
            if (occupiedDependencies.Count > 0)
            {
                ShowBlockedDependencies(occupiedDependencies);
                return;
            }

            if (!partLocation.CanRemove())
            {
                return;
            }

            GameObject removedPartGameObject = partLocation.Remove();
            if (removedPartGameObject == null)
            {
                return;
            }

            ItemDataHolder removedPart = removedPartGameObject.GetComponent<ItemDataHolder>();
            if (removedPart != null)
            {
                removedPart.IsInstalled = false;
                removedPart.ParentItemDataHolder = default;
            }

            // Parts are authored to be placed with their prefab pivot.
            // Drop the part location rotation, so Item Place starts from
            // the canonical orientation instead of the installed one.
            removedPartGameObject.transform.rotation = Quaternion.identity;

            ClearShownPartGhosts();
            ClearHover();

            removedPartGameObject.SetActive(false);

            GameStateManager.Instance.ChangeState(new GameStateItemPlace(removedPartGameObject));
        }

        private void ShowBlockedDependencies(List<PartLocation> occupiedDependencies)
        {
            ClearHover();

            foreach (PartLocation dependency in occupiedDependencies)
            {
                if (dependency.InstalledPart == null)
                {
                    continue;
                }

                ItemDataHolder dependencyPart = dependency.InstalledPart.GetComponent<ItemDataHolder>();
                if (dependencyPart == null)
                {
                    continue;
                }

                ShowPartGhost(dependencyPart);
            }

            blockedUntilTime = Time.time + Mathf.Max(0.0f, BlockedDependenciesGhostDuration);

            ClearCachedParts();
        }

        private readonly struct HoveredPart
        {
            public readonly PartCacheKey Key;
            public readonly ItemMetaSO MetaData;
            public readonly ItemDataHolder Part;
            public readonly PartLocation PartLocation;
            public readonly Collider ClosestHit;

            public HoveredPart(
                PartCacheKey key,
                ItemMetaSO metaData,
                ItemDataHolder part,
                PartLocation partLocation,
                Collider closestHit)
            {
                Key = key;
                MetaData = metaData;
                Part = part;
                PartLocation = partLocation;
                ClosestHit = closestHit;
            }
        }

        private sealed class ShownPartGhost
        {
            public readonly ItemGhostDataHolder Ghost;
            public readonly ItemDataHolder Part;
            public readonly List<MeshRenderer> PartMeshRenderers = new();
            public readonly List<bool> PartMeshRenderersStates = new();

            public ShownPartGhost(ItemGhostDataHolder ghost, ItemDataHolder part)
            {
                Ghost = ghost;
                Part = part;
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

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
    public class PartHoverInstall : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layer of the part installation colliders. A hit collider on this " +
            "layer is resolved to the part location it belongs to.")]
        public LayerMask PartLocationLayer;

        [Header("Blocked Install Settings")]
        [Tooltip("Seconds to show the installation ghosts of the missing prerequisite parts")]
        public float BlockedDependenciesGhostDuration = 2.0f;

        [Header("Interface Units")]
        public TextMeshProUGUI PartInstallNameText;
        public GameObject InstallRoot;

        private const float EyesMoveThresholdSqr = 0.0001f;
        private const float EyesRotationThreshold = 0.1f;
        private const float ScrollDeadZone = 0.01f;

        private static readonly int InstallationStatePropertyID = Shader.PropertyToID("_installation_state");

        private readonly List<ItemGhostDataHolder> blockedPrerequisiteGhosts = new();
        private readonly List<PartLocation> hoveredPartLocations = new();

        private RaycastHit[] cachedHits = new RaycastHit[32];

        private ItemDataHolder targetItem;
        private PartsAssembly targetAssembly;
        private ItemGhostDataHolder installationGhost;
        private PartLocation targetPartLocation;

        private PartLocation lastHoveredPartLocation;
        private PartLocation lastBlockedPartLocation;

        private MaterialPropertyBlock installationPropertyBlock;

        private Vector3 cachedEyesPosition;
        private Quaternion cachedEyesRotation;

        private float blockedUntilTime = -1.0f;

        private bool isHoverRefreshRequested;

        private int scrollIndex;
        private int lastScrollFrame = -1;
        private float lastScrollValue;

        private bool IsHoverBlocked => blockedUntilTime > 0.0f;

        private void Awake()
        {
            // Fallback for prefabs set up before the dedicated part location
            // layer mask was introduced on the component.
            if (PartLocationLayer.value == 0)
            {
                int partLocationLayer = LayerMask.NameToLayer("part_location");
                if (partLocationLayer >= 0)
                {
                    PartLocationLayer = 1 << partLocationLayer;
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

            // The hover check is normally refreshed only when eyes move.
            // Force it after entering the state or after a blocked ghost
            // sequence, so the ghost and hints are up to date immediately.
            if (!isHoverRefreshRequested && !IsEyesChanged())
            {
                return;
            }

            isHoverRefreshRequested = false;

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

        private bool IsCameraRaycastHit(out Ray ray, out RaycastHit hit)
        {
            ray = new Ray(
                Eyes.position,
                Eyes.forward);

            int hitNumber = Physics.RaycastNonAlloc(
                ray,
                cachedHits,
                HoverDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            bool isHitFound = false;
            float nearestDistance = float.MaxValue;

            hit = default;

            for (int index = 0; index < hitNumber; index++)
            {
                RaycastHit cachedHit = cachedHits[index];

                // The installation ghost can carry colliders of its own
                // (e.g. spool parts), so it must never block the hover ray.
                if (installationGhost != null &&
                    cachedHit.collider.transform.IsChildOf(installationGhost.transform))
                {
                    continue;
                }

                if (cachedHit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = cachedHit.distance;
                hit = cachedHit;
                isHitFound = true;
            }

            return isHitFound;
        }

        private void PerformHoverCheck()
        {
            if (Eyes == null || targetItem == null || installationGhost == null)
            {
                ClearTargetLocation();
                ClearHoveredPartLocations();
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
                PartLocationLayer,
                QueryTriggerInteraction.Ignore);

            if (hitNumber == 0)
            {
                // Keep the ghost visible in the air, but there is nothing to
                // install into, so the hint stays hidden.
                ShowGhostAtHoverFallback(ray);
                SetInstallHintVisible(false);
                ClearHoveredPartLocations();
                ClearTargetLocation();

                return;
            }

            System.Array.Sort(
                cachedHits,
                0,
                hitNumber,
                RaycastHitDistanceComparer.Instance);

            PartLocation previousPrimaryLocation = hoveredPartLocations.Count > 0
                ? hoveredPartLocations[0]
                : default;

            hoveredPartLocations.Clear();

            PartLocation nearestHitLocation = default;

            for (int index = 0; index < hitNumber; index++)
            {
                RaycastHit hit = cachedHits[index];

                // The installation ghost can carry colliders of its own
                // (e.g. spool parts), so it must never block the hover ray.
                if (installationGhost != null &&
                    hit.collider.transform.IsChildOf(installationGhost.transform))
                {
                    continue;
                }

                PartLocation partLocation = hit.collider.GetComponentInParent<PartLocation>();

                if (partLocation == null)
                {
                    continue;
                }

                if (nearestHitLocation == null)
                {
                    nearestHitLocation = partLocation;
                }

                // Every free part location expecting the held part is a valid
                // scroll layer. An occupied location cannot be installed into,
                // but the nearest one is still used as a ghost preview anchor.
                if (partLocation.IsOccupied ||
                    hoveredPartLocations.Contains(partLocation) ||
                    !IsPartLocationOfTargetAssembly(partLocation))
                {
                    continue;
                }

                hoveredPartLocations.Add(partLocation);
            }

            if (hoveredPartLocations.Count == 0)
            {
                if (nearestHitLocation != null)
                {
                    ShowGhostAtLocation(nearestHitLocation);
                }
                else
                {
                    ShowGhostAtHoverFallback(ray);
                }

                SetInstallHintVisible(false);
                ClearHoveredPartLocations();
                ClearTargetLocation();

                return;
            }

            // The location list is already sorted by distance, so the first
            // entry is the one the player is aiming at directly. Reset the
            // scroll layer when that primary location changes.
            if (hoveredPartLocations[0] != previousPrimaryLocation)
            {
                scrollIndex = 0;
            }

            scrollIndex = Mathf.Clamp(scrollIndex, 0, hoveredPartLocations.Count - 1);

            SetupHover(hoveredPartLocations[scrollIndex]);
        }

        private bool IsPartLocationOfTargetAssembly(PartLocation partLocation)
        {
            if (partLocation == null || targetItem == null)
            {
                return false;
            }

            bool isExpectedLocation;

            if (targetAssembly != null)
            {
                // The configured assembly owns the locations even when they
                // are nested under another assembly's transform.
                isExpectedLocation = targetAssembly.IsPartLocationOfAssembly(partLocation);
            }
            else
            {
                PartsAssembly partsAssembly = partLocation.GetComponentInParent<PartsAssembly>();

                if (partsAssembly == null)
                {
                    return false;
                }

                isExpectedLocation = partsAssembly.IsPartLocationOfAssembly(partLocation);
            }

            return isExpectedLocation &&
                partLocation.IsExpectedItemMetaData(targetItem.gameObject);
        }

        private void SetupHover(PartLocation partLocation)
        {
            if (partLocation == null)
            {
                return;
            }

            if (partLocation != lastHoveredPartLocation)
            {
                lastHoveredPartLocation = partLocation;
                lastBlockedPartLocation = default;
            }

            List<PartLocation> missingPrerequisites = GetUninstalledInstallPrerequisites(partLocation);

            if (missingPrerequisites.Count > 0)
            {
                if (lastBlockedPartLocation != partLocation)
                {
                    ShowBlockedPrerequisites(partLocation, missingPrerequisites);
                    lastBlockedPartLocation = partLocation;

                    return;
                }

                // The blocked ghosts were already shown for this location.
                // Keep the part ghost red, but do not repeat the timed ghost
                // sequence while the player keeps looking at the same place.
                ShowGhostAtLocation(partLocation);
                SetInstallHintVisible(false);
                ClearTargetLocation();

                return;
            }

            lastBlockedPartLocation = default;

            ShowGhostAtLocation(partLocation);
            SetGhostInstallationState(0.0f);
            targetPartLocation = partLocation;
            SetInstallHintVisible(true);
        }

        private void ShowGhostAtHoverFallback(Ray ray)
        {
            if (IsCameraRaycastHit(out _, out RaycastHit hit))
            {
                ShowGhostAtWorld(hit.point);

                return;
            }

            ShowGhostAtWorld(ray.GetPoint(HoverDistance));
        }

        private void ShowBlockedPrerequisites(
            PartLocation partLocation,
            List<PartLocation> missingPrerequisites)
        {
            ClearBlockedPrerequisiteGhosts();
            ClearTargetLocation();

            ShowGhostAtLocation(partLocation);
            SetInstallHintVisible(false);

            foreach (PartLocation prerequisiteLocation in missingPrerequisites)
            {
                ItemGhostDataHolder prerequisiteGhost = InstantiatePrerequisiteGhost(prerequisiteLocation);
                if (prerequisiteGhost == null)
                {
                    continue;
                }

                blockedPrerequisiteGhosts.Add(prerequisiteGhost);
            }

            blockedUntilTime = Time.time + Mathf.Max(0.0f, BlockedDependenciesGhostDuration);
        }

        private ItemGhostDataHolder InstantiatePrerequisiteGhost(PartLocation prerequisiteLocation)
        {
            ItemMetaSO prerequisiteMetaData = GetExpectedPartMetaData(prerequisiteLocation);
            if (prerequisiteMetaData == null || prerequisiteMetaData.Prefab == null)
            {
                return null;
            }

            ItemDataHolder prerequisitePrefab = prerequisiteMetaData.Prefab.GetComponent<ItemDataHolder>();
            if (prerequisitePrefab == null ||
                !IsGhostTypeAvailable(prerequisitePrefab.ItemGhostMetaSO, ItemGhostType.Installation))
            {
                return null;
            }

            Transform prerequisiteTransform = prerequisiteLocation.transform;

            ItemGhostDataHolder prerequisiteGhost = GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                prerequisitePrefab.ItemGhostMetaSO,
                ItemGhostType.Installation,
                prerequisiteTransform.position,
                prerequisiteTransform.rotation,
                prerequisiteTransform);

            GameStateManager.Instance.ItemInstancer.SnapItemGhostToAnchor(
                prerequisiteGhost,
                prerequisiteTransform);

            SetupInstallationState(prerequisiteGhost, 1.0f);

            return prerequisiteGhost;
        }

        private static ItemMetaSO GetExpectedPartMetaData(PartLocation partLocation)
        {
            if (partLocation.IsSingleMetaDataReference)
            {
                return partLocation.PartMetaData;
            }

            if (partLocation.PartMetaDatasList == null || partLocation.PartMetaDatasList.Count == 0)
            {
                return null;
            }

            return partLocation.PartMetaDatasList[0];
        }

        private static List<PartLocation> GetUninstalledInstallPrerequisites(PartLocation partLocation)
        {
            List<PartLocation> list = new List<PartLocation>();

            // Only install prerequisites block installation. Removal blockers
            // are the reverse relation: they must be removed before this part
            // can be removed, but they are installed after this part.
            AddUnoccupiedLocations(partLocation.InstallPrerequisiteLocations, list);

            return list;
        }

        private static void AddUnoccupiedLocations(List<PartLocation> source, List<PartLocation> list)
        {
            if (source == null)
            {
                return;
            }

            foreach (PartLocation partLocation in source)
            {
                if (partLocation == null || partLocation.IsOccupied || list.Contains(partLocation))
                {
                    continue;
                }

                list.Add(partLocation);
            }
        }

        private void ShowGhostAtLocation(PartLocation partLocation)
        {
            if (installationGhost == null || partLocation == null)
            {
                return;
            }

            Transform locationTransform = partLocation.transform;

            // A part location is a socket. Snapping the ghost in its local
            // space makes the preview adopt the same fitment scale as the
            // part which will be installed there.
            GameStateManager.Instance.ItemInstancer.SnapItemGhostToAnchor(
                installationGhost,
                locationTransform);

            installationGhost.gameObject.SetActive(true);

            SetGhostInstallationState(1.0f);
        }

        private void ShowGhostAtWorld(Vector3 position)
        {
            if (installationGhost == null)
            {
                return;
            }

            installationGhost.transform.SetParent(default, true);
            installationGhost.transform.SetPositionAndRotation(
                position,
                targetItem.transform.rotation);

            // In the air there is no target socket, so the preview should
            // match the part which is currently being carried.
            GameStateManager.Instance.ItemInstancer.SyncGhostWorldScale(
                installationGhost.transform,
                targetItem.transform);

            installationGhost.gameObject.SetActive(true);

            SetGhostInstallationState(1.0f);
        }

        private void SetupInstallationState(ItemGhostDataHolder itemGhost, float installationState)
        {
            if (itemGhost == null || itemGhost.MeshRenderersList == null)
            {
                return;
            }

            installationPropertyBlock ??= new MaterialPropertyBlock();
            installationPropertyBlock.Clear();
            installationPropertyBlock.SetFloat(InstallationStatePropertyID, installationState);

            foreach (MeshRenderer meshRenderer in itemGhost.MeshRenderersList)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(installationPropertyBlock);
            }
        }

        private void SetGhostInstallationState(float installationState)
        {
            SetupInstallationState(installationGhost, installationState);
        }

        private ItemGhostDataHolder GetOrCreateInstallationGhost(ItemDataHolder item)
        {
            if (item.TryGetGhostInstance(ItemGhostType.Installation, out ItemGhostDataHolder itemGhost) &&
                itemGhost != null)
            {
                return itemGhost;
            }

            if (!IsGhostTypeAvailable(item.ItemGhostMetaSO, ItemGhostType.Installation))
            {
                return null;
            }

            return GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                item,
                ItemGhostType.Installation,
                item.transform.position,
                item.transform.rotation);
        }

        private static bool IsGhostTypeAvailable(ItemGhostMetaSO itemGhostMeta, ItemGhostType type)
        {
            return itemGhostMeta != null && itemGhostMeta.IsHaveGhostType(type);
        }

        private static void HidePlacementGhost(ItemDataHolder item)
        {
            if (item == null)
            {
                return;
            }

            // Combined free-part / regular items show the free-part placement
            // ghost while carried, so hide every placement variant.
            ItemPlacementUtility.HidePlacementGhosts(item.gameObject);
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

        private static void ClearText(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.text = default;
            text.gameObject.SetActive(false);
        }

        private void SetInstallHintVisible(bool isActive)
        {
            if (InstallRoot == null)
            {
                return;
            }

            InstallRoot.SetActive(isActive);
        }

        private void ClearTargetLocation()
        {
            targetPartLocation = default;
        }

        private void ClearPartLocationHover()
        {
            lastHoveredPartLocation = default;
            lastBlockedPartLocation = default;
        }

        private void ClearHoveredPartLocations()
        {
            hoveredPartLocations.Clear();
            scrollIndex = 0;
            ClearPartLocationHover();
        }

        private void ClearBlockedPrerequisiteGhosts()
        {
            foreach (ItemGhostDataHolder prerequisiteGhost in blockedPrerequisiteGhosts)
            {
                if (prerequisiteGhost == null)
                {
                    continue;
                }

                prerequisiteGhost.gameObject.SetActive(false);
                Destroy(prerequisiteGhost.gameObject);
            }

            blockedPrerequisiteGhosts.Clear();
        }

        private void ClearHover()
        {
            SetInstallHintVisible(false);
            ClearText(PartInstallNameText);
        }

        private void EndBlockedHover()
        {
            blockedUntilTime = -1.0f;

            ClearBlockedPrerequisiteGhosts();

            isHoverRefreshRequested = true;
        }

        private void OnDisable()
        {
            ClearTargetLocation();
            ClearHoveredPartLocations();
            ClearBlockedPrerequisiteGhosts();
            ClearHover();

            if (installationGhost != null)
            {
                installationGhost.gameObject.SetActive(false);
            }

            installationGhost = default;
            targetItem = default;
            targetAssembly = default;

            blockedUntilTime = -1.0f;

            cachedEyesPosition = default;
            cachedEyesRotation = default;

            isHoverRefreshRequested = false;

            ResetScrollThrottle();
        }

        private void ResetScrollThrottle()
        {
            lastScrollFrame = -1;
            lastScrollValue = default;
        }

        // === Player Controller invoked ===

        public void SetTargetItem(ItemDataHolder item, PartsAssembly partsAssembly = default)
        {
            ClearTargetLocation();
            ClearHoveredPartLocations();
            ClearBlockedPrerequisiteGhosts();
            ClearHover();

            if (installationGhost != null && installationGhost != GetExistingInstallationGhost(item))
            {
                installationGhost.gameObject.SetActive(false);
            }

            targetItem = item;
            targetAssembly = partsAssembly;
            installationGhost = default;

            blockedUntilTime = -1.0f;

            cachedEyesPosition = default;
            cachedEyesRotation = default;

            isHoverRefreshRequested = true;

            ResetScrollThrottle();

            if (item == null)
            {
                return;
            }

            HidePlacementGhost(item);

            installationGhost = GetOrCreateInstallationGhost(item);
            if (installationGhost != null)
            {
                installationGhost.gameObject.SetActive(true);
                SetGhostInstallationState(1.0f);
            }

            ItemMetaSO metaData = item.ItemMetaData;
            SetText(
                PartInstallNameText,
                metaData != null ? metaData.Name : item.gameObject.name);
        }

        private static ItemGhostDataHolder GetExistingInstallationGhost(ItemDataHolder item)
        {
            if (item == null)
            {
                return null;
            }

            if (item.TryGetGhostInstance(ItemGhostType.Installation, out ItemGhostDataHolder itemGhost))
            {
                return itemGhost;
            }

            return null;
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
            if (hoveredPartLocations.Count == 0)
            {
                PerformHoverCheck();
            }

            if (hoveredPartLocations.Count == 0)
            {
                return;
            }

            int direction = value.y > 0.0f ? 1 : -1;
            scrollIndex =
                (scrollIndex + direction + hoveredPartLocations.Count) % hoveredPartLocations.Count;

            // Switching layers must be able to replay the blocked prerequisite
            // ghosts before the installation hint is shown again.
            lastHoveredPartLocation = default;
            lastBlockedPartLocation = default;

            SetupHover(hoveredPartLocations[scrollIndex]);
        }

        public void ApplyInstallPart()
        {
            if (targetItem == null || targetPartLocation == null)
            {
                return;
            }

            if (IsHoverBlocked)
            {
                return;
            }

            if (!targetPartLocation.CanInstall(targetItem.gameObject))
            {
                return;
            }

            PartsAssembly partsAssembly = targetAssembly != null
                ? targetAssembly
                : targetPartLocation.GetComponentInParent<PartsAssembly>();
            ItemDataHolder partsAssemblyItem = partsAssembly != null
                ? partsAssembly.GetComponent<ItemDataHolder>()
                : default;

            // A combined free-part item may still be registered on a shelf.
            // Installing it moves it into the assembly, so clear both its
            // free-part and regular item surface states first.
            ItemPlacementUtility.RemoveFromSurface(targetItem.gameObject);

            targetPartLocation.Install(targetItem.gameObject);

            targetItem.gameObject.SetActive(true);
            targetItem.IsInstalled = true;
            targetItem.ParentItemDataHolder = partsAssemblyItem;

            if (installationGhost != null)
            {
                installationGhost.gameObject.SetActive(false);
            }

            ClearBlockedPrerequisiteGhosts();
            ClearHover();

            GameStateManager.Instance.ChangeState(GetReturnGameState());
        }

        private static GameState GetReturnGameState()
        {
            if (GameStateManager.Instance.CurrentGameState is GameStatePartInstall partInstall &&
                partInstall.ReturnGameState != null)
            {
                return partInstall.ReturnGameState;
            }

            return new GameStateFreeWalk();
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

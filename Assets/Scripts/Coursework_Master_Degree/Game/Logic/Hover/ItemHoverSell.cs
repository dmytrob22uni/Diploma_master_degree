using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class ItemHoverSell : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for surface, item, part, object to print, etc.")]
        public LayerMask ItemLayer;

        [Header("Sell Rules")]
        [Range(0.0f, 100.0f)]
        [Tooltip("Item condition at which it is still sold with income")]
        public float WearLevelThresholdPercent = 70.0f;
        [Range(0.0f, 100.0f)]
        [Tooltip("Installed part condition below which the assembly loses income")]
        public float PartWearLevelThresholdPercent = 20.0f;
        [Min(1)]
        [Tooltip("Number of worn installed parts needed to lose income")]
        public int WornPartsCountThreshold = 1;

        [Header("Interface Units")]
        public TextMeshProUGUI ItemHoverSellText;
        public TextMeshProUGUI ItemHoverSellNameText;
        public TextMeshProUGUI ItemHoverSellDescriptionText;
        public GameObject SellRoot;

        private const string SellInfo = "Sell";
        private const string SurfaceOccupiedReason = "Items on surface";
        private const string MissingPartsReason = "Missing parts";
        private const string WornPartsReason = "Worn parts";
        private const string WornOutReason = "Too worn out";
        private const string LowQualityReason = "Low quality";
        private const string ConnectedToPrinterReason = "Connected to a printer";

        private static readonly int SellingStatePropertyID = Shader.PropertyToID("_selling_state");

        private MaterialPropertyBlock sellingPropertyBlock;

        private bool isHoverClear;
        private bool hasCachedTarget;
        private SellTarget cachedTarget;

        private ItemGhostDataHolder shownItemGhost;
        private ObjectToPrintGhostDataHolder shownObjectToPrintGhost;

        private readonly List<MeshRenderer> originalMeshRenderers = new();
        private readonly List<bool> originalMeshRenderersStates = new();

        private void Awake()
        {
            sellingPropertyBlock = new MaterialPropertyBlock();

            ClearAll();
        }

        private void Update()
        {
            PerformHoverCheck();
        }

        private bool IsCameraRaycastHit(out RaycastHit hit)
        {
            Ray ray = new(
                Eyes.position,
                Eyes.forward);

            return Physics.Raycast(
                ray,
                out hit,
                HoverDistance,
                ItemLayer,
                QueryTriggerInteraction.Ignore);
        }

        private void PerformHoverCheck()
        {
            if (Eyes == null || !IsCameraRaycastHit(out RaycastHit hit))
            {
                if (!isHoverClear)
                {
                    ClearAll();
                }

                return;
            }

            if (!TryCreateTarget(hit.collider, out SellTarget target))
            {
                if (!isHoverClear)
                {
                    ClearAll();
                }

                return;
            }

            if (hasCachedTarget && IsSameTarget(cachedTarget, target))
            {
                cachedTarget = target;

                SetupSellingState(target.State);
                SetupInfo(target);

                return;
            }

            ClearShownGhost();

            cachedTarget = target;
            hasCachedTarget = true;

            ShowGhost(target);
            SetupInfo(target);
        }

        private bool TryCreateTarget(Collider collider, out SellTarget target)
        {
            target = default;

            // Selling ghosts are shown in place of the original objects, so a
            // raycast often hits the ghost first. Resolve it back to the item.
            ItemGhostSellingDataHolder itemGhost =
                collider.GetComponentInParent<ItemGhostSellingDataHolder>();

            if (itemGhost != null && itemGhost.ItemInstance != null)
            {
                ItemDataHolder ghostedItem = itemGhost.ItemInstance.GetComponent<ItemDataHolder>();

                if (ghostedItem != null)
                {
                    target = CreateItemTarget(ghostedItem);
                    return true;
                }
            }

            ObjectToPrintGhostSellingDataHolder objectToPrintGhost =
                collider.GetComponentInParent<ObjectToPrintGhostSellingDataHolder>();

            if (objectToPrintGhost != null && objectToPrintGhost.ObjectToPrintInstance != null)
            {
                ObjectToPrintDataHolder ghostedObjectToPrint =
                    objectToPrintGhost.ObjectToPrintInstance.GetComponent<ObjectToPrintDataHolder>();

                if (ghostedObjectToPrint != null)
                {
                    target = CreateObjectToPrintTarget(ghostedObjectToPrint);
                    return true;
                }
            }

            ItemDataHolder item = collider.GetComponentInParent<ItemDataHolder>();

            if (item != null)
            {
                target = CreateItemTarget(item);
                return true;
            }

            ObjectToPrintDataHolder objectToPrint =
                collider.GetComponentInParent<ObjectToPrintDataHolder>();

            if (objectToPrint != null)
            {
                target = CreateObjectToPrintTarget(objectToPrint);
                return true;
            }

            return false;
        }

        private SellTarget CreateItemTarget(ItemDataHolder item)
        {
            // Installed parts cannot be sold on their own. Sell the root item
            // the part belongs to, as ItemHoverInfo does for take interaction.
            while (item.IsInstalled && item.ParentItemDataHolder != null)
            {
                item = item.ParentItemDataHolder;
            }

            SellTarget target = new()
            {
                Item = item,
                Name = item.ItemMetaData != null ? item.ItemMetaData.Name : item.name,
                Description = item.ItemMetaData != null ? item.ItemMetaData.Description : default,
            };

            Device device = item.GetComponent<Device>();

            if (device != null && device.BoundPrinters.Count > 0)
            {
                target.State = SellingState.Wrong;
                target.Reason = ConnectedToPrinterReason;

                return target;
            }

            Printer printer = Printer.FindFor(item.gameObject);

            if (printer != null && printer.IsBusy)
            {
                target.State = SellingState.Wrong;
                target.Reason = "Printer is busy";

                return target;
            }

            if (item.IsSurface)
            {
                ItemSurfaceDataHolder surface = item.GetComponent<ItemSurfaceDataHolder>();

                if (surface != null && !surface.IsEmptyShelves())
                {
                    target.State = SellingState.Wrong;
                    target.Reason = SurfaceOccupiedReason;

                    return target;
                }
            }

            EvaluateItem(item, ref target);

            return target;
        }

        private SellTarget CreateObjectToPrintTarget(ObjectToPrintDataHolder objectToPrint)
        {
            SellTarget target = new()
            {
                ObjectToPrint = objectToPrint,
                Name = objectToPrint.ObjectToPrintMetaData != null
                    ? objectToPrint.ObjectToPrintMetaData.Name
                    : objectToPrint.name,
                Description = objectToPrint.ObjectToPrintMetaData != null
                    ? objectToPrint.ObjectToPrintMetaData.Description
                    : default,
                State = objectToPrint.IsHaveQuality
                    ? SellingState.Income
                    : SellingState.NoIncome,
                Reason = objectToPrint.IsHaveQuality
                    ? default
                    : LowQualityReason,
            };

            return target;
        }

        private void EvaluateItem(ItemDataHolder item, ref SellTarget target)
        {
            if (item.IsHaveWearLevel &&
                item.TryGetComponent<ItemWearState>(out ItemWearState wearState) &&
                wearState.CurrentCondition < WearLevelThresholdPercent)
            {
                target.State = SellingState.NoIncome;
                target.Reason = WornOutReason;

                return;
            }

            PartsAssembly assembly = item.GetComponent<PartsAssembly>();

            if (assembly == null)
            {
                target.State = SellingState.Income;
                target.Reason = default;

                return;
            }

            bool isAllRegularPartsInstalled = assembly.IsAllRegularPartLocationsOccupied();
            bool isAllOptionalPartsInstalled =
                !assembly.IsHaveOptionalParts || assembly.IsAllOptionalPartLocationsOccupied();

            if (!isAllRegularPartsInstalled || !isAllOptionalPartsInstalled)
            {
                target.State = SellingState.NoIncome;
                target.Reason = MissingPartsReason;

                return;
            }

            if (CountWornParts(assembly) >= Mathf.Max(1, WornPartsCountThreshold))
            {
                target.State = SellingState.NoIncome;
                target.Reason = WornPartsReason;

                return;
            }

            target.State = SellingState.Income;
            target.Reason = default;
        }

        private bool IsPartWorn(PartLocation partLocation)
        {
            if (partLocation == null || partLocation.InstalledPart == null)
            {
                return false;
            }

            ItemDataHolder part = partLocation.InstalledPart.GetComponent<ItemDataHolder>();

            if (part == null || !part.IsHaveWearLevel)
            {
                return false;
            }

            ItemWearState wearState = part.GetComponent<ItemWearState>();

            return wearState != null &&
                wearState.CurrentCondition < PartWearLevelThresholdPercent;
        }

        private int CountWornParts(PartsAssembly assembly)
        {
            int wornPartsNumber = 0;

            foreach (PartLocation partLocation in assembly.RegularPartLocationsList)
            {
                if (IsPartWorn(partLocation))
                {
                    wornPartsNumber++;
                }
            }

            if (assembly.IsHaveOptionalParts && assembly.OptionalPartLocationsList != null)
            {
                foreach (OptionalPart optionalPart in assembly.OptionalPartLocationsList)
                {
                    if (optionalPart == null || optionalPart.PartLocationsList == null)
                    {
                        continue;
                    }

                    foreach (PartLocation partLocation in optionalPart.PartLocationsList)
                    {
                        if (IsPartWorn(partLocation))
                        {
                            wornPartsNumber++;
                        }
                    }
                }
            }

            return wornPartsNumber;
        }

        private static bool IsSameTarget(SellTarget left, SellTarget right)
        {
            return left.Item == right.Item &&
                left.ObjectToPrint == right.ObjectToPrint;
        }

        private void ShowGhost(SellTarget target)
        {
            if (target.Item != null)
            {
                ShowItemGhost(target.Item, target.State);

                return;
            }

            if (target.ObjectToPrint != null)
            {
                ShowObjectToPrintGhost(target.ObjectToPrint, target.State);
            }
        }

        private void ShowItemGhost(ItemDataHolder item, SellingState state)
        {
            ItemGhostDataHolder itemGhost = GetItemSellingGhost(item);

            if (itemGhost == null)
            {
                return;
            }

            itemGhost.ItemInstance = item.gameObject;

            itemGhost.transform.SetParent(item.transform.parent, true);

            GameStateManager.Instance.ItemInstancer.SyncItemGhostToItem(
                itemGhost,
                item);

            itemGhost.gameObject.SetActive(true);

            SetupSellingState(itemGhost.MeshRenderersList, state);

            HideOriginalMeshRenderers(item.gameObject);

            shownItemGhost = itemGhost;
        }

        private void ShowObjectToPrintGhost(ObjectToPrintDataHolder objectToPrint, SellingState state)
        {
            ObjectToPrintGhostDataHolder objectToPrintGhost =
                GetObjectToPrintSellingGhost(objectToPrint);

            if (objectToPrintGhost == null)
            {
                return;
            }

            objectToPrintGhost.ObjectToPrintInstance = objectToPrint.gameObject;

            objectToPrintGhost.transform.SetParent(objectToPrint.transform.parent, true);

            GameStateManager.Instance.ItemInstancer.SyncGhostToSource(
                objectToPrintGhost.transform,
                objectToPrint.transform);

            objectToPrintGhost.gameObject.SetActive(true);

            SetupSellingState(objectToPrintGhost.MeshRenderersList, state);

            HideOriginalMeshRenderers(objectToPrint.gameObject);

            shownObjectToPrintGhost = objectToPrintGhost;
        }

        private ItemGhostDataHolder GetItemSellingGhost(ItemDataHolder item)
        {
            if (item.TryGetGhostInstance(ItemGhostType.Selling, out ItemGhostDataHolder itemGhost) &&
                itemGhost != null)
            {
                return itemGhost;
            }

            if (!IsItemGhostTypeAvailable(item.ItemGhostMetaSO, ItemGhostType.Selling))
            {
                return null;
            }

            return GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                item,
                ItemGhostType.Selling,
                item.transform.position,
                item.transform.rotation,
                item.transform.parent);
        }

        private ObjectToPrintGhostDataHolder GetObjectToPrintSellingGhost(
            ObjectToPrintDataHolder objectToPrint)
        {
            if (objectToPrint.TryGetGhostInstance(
                    ObjectToPrintGhostType.Selling,
                    out ObjectToPrintGhostDataHolder objectToPrintGhost) &&
                objectToPrintGhost != null)
            {
                return objectToPrintGhost;
            }

            if (!IsObjectToPrintGhostTypeAvailable(
                    objectToPrint.ObjectToPrintGhostMetaSO,
                    ObjectToPrintGhostType.Selling))
            {
                return null;
            }

            return GameStateManager.Instance.ItemInstancer.InstantiateObjectToPrintGhost(
                objectToPrint,
                ObjectToPrintGhostType.Selling,
                objectToPrint.transform.position,
                objectToPrint.transform.rotation,
                objectToPrint.transform.parent);
        }

        private static bool IsItemGhostTypeAvailable(
            ItemGhostMetaSO itemGhostMeta,
            ItemGhostType type)
        {
            if (itemGhostMeta == null || itemGhostMeta.ItemGhostTypesToPrefabsList == null)
            {
                return false;
            }

            foreach (ItemGhostTypeToPrefab itemGhostTypeToPrefab in
                itemGhostMeta.ItemGhostTypesToPrefabsList)
            {
                if (itemGhostTypeToPrefab.ItemGhostType == type &&
                    itemGhostTypeToPrefab.ItemGhostGameObject != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsObjectToPrintGhostTypeAvailable(
            ObjectToPrintGhostMetaSO objectToPrintGhostMeta,
            ObjectToPrintGhostType type)
        {
            if (objectToPrintGhostMeta == null ||
                objectToPrintGhostMeta.ObjectToPrintTypesToPrefabsList == null)
            {
                return false;
            }

            foreach (ObjectToPrintTypeToPrefab objectToPrintTypeToPrefab in
                objectToPrintGhostMeta.ObjectToPrintTypesToPrefabsList)
            {
                if (objectToPrintTypeToPrefab.ObjectToPrintGhostType == type &&
                    objectToPrintTypeToPrefab.ObjectToPrintGhostGameObject != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetupSellingState(SellingState state)
        {
            if (shownItemGhost != null)
            {
                SetupSellingState(shownItemGhost.MeshRenderersList, state);
            }

            if (shownObjectToPrintGhost != null)
            {
                SetupSellingState(shownObjectToPrintGhost.MeshRenderersList, state);
            }
        }

        private void SetupSellingState(List<MeshRenderer> meshRenderers, SellingState state)
        {
            if (meshRenderers == null)
            {
                return;
            }

            sellingPropertyBlock.Clear();
            sellingPropertyBlock.SetFloat(SellingStatePropertyID, (float)state);

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(sellingPropertyBlock);
            }
        }

        private void HideOriginalMeshRenderers(GameObject original)
        {
            originalMeshRenderers.Clear();
            originalMeshRenderersStates.Clear();

            foreach (MeshRenderer meshRenderer in original.GetComponentsInChildren<MeshRenderer>())
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                originalMeshRenderers.Add(meshRenderer);
                originalMeshRenderersStates.Add(meshRenderer.enabled);

                meshRenderer.enabled = false;
            }
        }

        private void ClearShownGhost()
        {
            if (shownItemGhost != null)
            {
                shownItemGhost.gameObject.SetActive(false);
            }

            if (shownObjectToPrintGhost != null)
            {
                shownObjectToPrintGhost.gameObject.SetActive(false);
            }

            for (int index = 0; index < originalMeshRenderers.Count; index++)
            {
                MeshRenderer meshRenderer = originalMeshRenderers[index];

                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.enabled = originalMeshRenderersStates[index];
            }

            originalMeshRenderers.Clear();
            originalMeshRenderersStates.Clear();

            shownItemGhost = default;
            shownObjectToPrintGhost = default;
        }

        private void SetupInfo(SellTarget target)
        {
            string sellInfo = string.IsNullOrEmpty(target.Reason)
                ? SellInfo
                : $"{SellInfo} ({target.Reason})";

            SetText(ItemHoverSellText, sellInfo);
            SetText(ItemHoverSellNameText, target.Name);
            SetText(ItemHoverSellDescriptionText, target.Description);

            if (SellRoot != null)
            {
                SellRoot.SetActive(true);
            }

            isHoverClear = false;
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

        private void ClearHover()
        {
            ClearText(ItemHoverSellText);
            ClearText(ItemHoverSellNameText);
            ClearText(ItemHoverSellDescriptionText);

            if (SellRoot != null)
            {
                SellRoot.SetActive(false);
            }

            isHoverClear = true;
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

        private void ClearCached()
        {
            cachedTarget = default;
            hasCachedTarget = false;
        }

        private void ClearAll()
        {
            ClearCached();
            ClearShownGhost();
            ClearHover();
        }

        private void OnDisable()
        {
            ClearAll();
        }

        // === Player Controller invoked ===

        public void ApplySellItem()
        {
            if (!hasCachedTarget || cachedTarget.State == SellingState.Wrong)
            {
                return;
            }

            SellTarget target = cachedTarget;

            ClearAll();

            if (target.Item != null)
            {
                Printer printer = Printer.FindFor(target.Item.gameObject);

                if (printer != null && printer.IsBusy)
                {
                    return;
                }

                Device device = target.Item.GetComponent<Device>();

                if (device == null)
                {
                    device = target.Item.GetComponentInChildren<Device>(true);
                }

                if (device != null)
                {
                    GameStateManager.Instance.DeviceController?
                        .Console?.DisposeView(device);
                }

                target.Item.RemoveFromSurface();
                RemoveNestedFreePartPlacements(target.Item);
                DestroyItemGhosts(target.Item);
                target.Item.gameObject.SetActive(false);

                return;
            }

            if (target.ObjectToPrint != null)
            {
                target.ObjectToPrint.RemoveFromSurface();
                DestroyObjectToPrintGhosts(target.ObjectToPrint);
                target.ObjectToPrint.gameObject.SetActive(false);
            }
        }

        private static void RemoveNestedFreePartPlacements(ItemDataHolder item)
        {
            // Free parts are nested in the sold item's hierarchy but keep their
            // own registration on whichever surface they were placed on. Drop
            // those registrations, otherwise surfaces would keep references to
            // the sold item's parts.
            foreach (ItemFreePartDataHolder freePart in
                item.GetComponentsInChildren<ItemFreePartDataHolder>(true))
            {
                freePart.RemoveFromSurface();
            }
        }

        private static void DestroyItemGhosts(ItemDataHolder item)
        {
            // The sold item is never shown again, so its ghosts are no longer
            // needed. This includes ghosts of installed parts and free parts
            // nested into the sold item's hierarchy.
            foreach (ItemDataHolder hierarchyItem in
                item.GetComponentsInChildren<ItemDataHolder>(true))
            {
                DestroyItemGhostInstances(hierarchyItem);
            }

            foreach (ItemFreePartDataHolder freePart in
                item.GetComponentsInChildren<ItemFreePartDataHolder>(true))
            {
                DestroyFreePartGhostInstance(freePart);
            }

            foreach (ObjectToPrintDataHolder objectToPrint in
                item.GetComponentsInChildren<ObjectToPrintDataHolder>(true))
            {
                DestroyObjectToPrintGhosts(objectToPrint);
            }
        }

        private static void DestroyItemGhostInstances(ItemDataHolder item)
        {
            if (item.ItemGhostTypesToInstancesList == null)
            {
                return;
            }

            foreach (ItemGhostTypeToInstance itemGhostTypeToInstance in
                item.ItemGhostTypesToInstancesList)
            {
                ItemGhostDataHolder itemGhost = itemGhostTypeToInstance?.ItemGhostInstance;

                if (itemGhost == null)
                {
                    continue;
                }

                Destroy(itemGhost.gameObject);
            }

            item.ItemGhostTypesToInstancesList.Clear();
        }

        private static void DestroyFreePartGhostInstance(ItemFreePartDataHolder freePart)
        {
            if (freePart.ItemFreePartGhostPlacementInstance == null)
            {
                return;
            }

            Destroy(freePart.ItemFreePartGhostPlacementInstance.gameObject);

            freePart.ItemFreePartGhostPlacementInstance = default;
        }

        private static void DestroyObjectToPrintGhosts(
            ObjectToPrintDataHolder objectToPrint)
        {
            if (objectToPrint.ObjectToPrintGhostTypesToInstancesList == null)
            {
                return;
            }

            foreach (ObjectToPrintGhostTypeToInstance objectToPrintGhostTypeToInstance in
                objectToPrint.ObjectToPrintGhostTypesToInstancesList)
            {
                ObjectToPrintGhostDataHolder objectToPrintGhost =
                    objectToPrintGhostTypeToInstance?.ObjectToPrintInstance;

                if (objectToPrintGhost == null)
                {
                    continue;
                }

                Destroy(objectToPrintGhost.gameObject);
            }

            objectToPrint.ObjectToPrintGhostTypesToInstancesList.Clear();
        }

        private enum SellingState
        {
            Income = 0,
            NoIncome = 1,
            Wrong = 2,
        }

        private struct SellTarget
        {
            public ItemDataHolder Item;
            public ObjectToPrintDataHolder ObjectToPrint;
            public string Name;
            public string Description;
            public SellingState State;
            public string Reason;
        }
    }
}

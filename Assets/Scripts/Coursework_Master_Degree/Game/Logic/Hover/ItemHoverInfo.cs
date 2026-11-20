using TMPro;
using UnityEngine;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class ItemHoverInfo : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for item, e.g.:\n" +
            "Surface\n" +
            "Item\n" +
            "Part\n" +
            "Printer\n" +
            "Enclosure\n" +
            "ObjectToPrint\n" +
            "etc.")]
        public LayerMask ItemLayer;

        [Header("Interface Units")]
        public TextMeshProUGUI ItemHoverInfoText;
        public GameObject TakeRoot;
        public GameObject InspectWearRoot;

        [Header("Printer Status")]
        public TextMeshProUGUI PrinterStateProgressText;
        public TextMeshProUGUI PrinterNozzleTemperatureText;
        public TextMeshProUGUI PrinterBedTemperatureText;
        public TextMeshProUGUI PrinterSpoolMetersText;
        public TextMeshProUGUI PrinterPausePenaltyText;

        private const float PrinterStatusRefreshInterval = 0.15f;
        private Printer cachedBusyPrinter;
        private float lastPrinterStatusRefreshTime;

        private Collider cachedCollider;

        private GameObject cachedItemGameObject;
        public GameObject CachedItemGameObject => cachedItemGameObject;

        private ItemDataHolder cachedWearItem;
        public bool CanInspectWear => cachedWearItem != null;

        private bool isHoverClear;

        private void Awake()
        {
            ClearHover();
        }

        private void Update()
        {
            PerformHoverCheck();
        }

        private bool IsCameraRaycastHit(out RaycastHit hit)
        {
            Ray ray = new Ray(
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
            if (IsCameraRaycastHit(out RaycastHit hit))
            {
                if (hit.collider == cachedCollider)
                {
                    if (cachedBusyPrinter == null)
                    {
                        return;
                    }

                    if (cachedBusyPrinter.IsBusy)
                    {
                        if (Time.unscaledTime -
                            lastPrinterStatusRefreshTime >=
                            PrinterStatusRefreshInterval)
                        {
                            lastPrinterStatusRefreshTime = Time.unscaledTime;
                            UpdatePrinterStatus(cachedBusyPrinter);
                        }

                        return;
                    }
                }
                else
                {
                    cachedCollider = hit.collider;
                }

                // prepare data collection
                ClearCached();
                ClearHover();

                if (hit.collider.attachedRigidbody == null)
                {
                    return;
                }

                if (hit.collider.attachedRigidbody.TryGetComponent<ItemFreePartDataHolder>(out var itemFreePart) &&
                    itemFreePart.IsFreePartActive)
                {
                    if (itemFreePart.gameObject == cachedItemGameObject)
                    {
                        return;
                    }

                    cachedItemGameObject = itemFreePart.gameObject;

                    // A free part can also be a wearable standalone item
                    // (e.g. the printer battery closet adapter), so its wear
                    // inspection must remain available while it is detached.
                    cachedWearItem =
                        itemFreePart.TryGetComponent(out ItemDataHolder freePartItem) &&
                        IsWearInspectAvailable(freePartItem)
                            ? freePartItem
                            : default;

                    string info = itemFreePart.Name;

                    if (ShowBusyPrinterStatus(itemFreePart.gameObject))
                    {
                        return;
                    }

                    SetupInfoAndTake(info);
                }
                else if (hit.collider.attachedRigidbody.TryGetComponent<ItemDataHolder>(out var item))
                {
                    if (item.gameObject == cachedItemGameObject)
                    {
                        return;
                    }

                    bool isInstalledPart = item.IsInstalled;
                    while (isInstalledPart)
                    {
                        item = item.ParentItemDataHolder;
                        isInstalledPart = item.IsInstalled;
                    }

                    if (item.IsSurface)
                    {
                        var surface = item.GetComponent<ItemSurfaceDataHolder>();
                        if (!surface.IsEmptyShelves())
                        {
                            SetupTake(false);

                            return;
                        }
                    }

                    cachedItemGameObject = item.gameObject;
                    cachedWearItem = IsWearInspectAvailable(item)
                        ? item
                        : default;

                    string info = item.ItemMetaData.Name;

                    if (ShowBusyPrinterStatus(item.gameObject))
                    {
                        return;
                    }

                    SetupInfoAndTake(info);
                }
                else if (hit.collider.attachedRigidbody.TryGetComponent<ObjectToPrintDataHolder>(out var objectToPrint))
                {
                    if (objectToPrint.gameObject == cachedItemGameObject)
                    {
                        return;
                    }

                    cachedItemGameObject = objectToPrint.gameObject;

                    string info = objectToPrint.ObjectToPrintMetaData.Name;

                    PrintedObjectDataHolder printedObject =
                        objectToPrint.GetComponent<PrintedObjectDataHolder>();

                    if (printedObject != null &&
                        printedObject.IsOnPrintBed &&
                        printedObject.IsFinalized)
                    {
                        // A finished/partial print is deliberately removable
                        // while the printer reports Finished/partial. The
                        // object owns the removal interaction, not the busy
                        // printer status.
                        SetupInfoAndTake(info);
                        return;
                    }

                    if (ShowBusyPrinterStatus(objectToPrint.gameObject))
                    {
                        return;
                    }

                    SetupInfoAndTake(info);
                }

                return;
            }

            if (!isHoverClear)
            {
                ClearCachedCollider();
                ClearCached();
                ClearHover();
            }
        }

        private static bool IsWearInspectAvailable(ItemDataHolder item)
        {
            // Assemblies keep their own interaction list; only standalone
            // wearable items are inspected through the parts wear state.
            if (item == null ||
                item.IsInstalled ||
                item.GetComponent<PartsAssembly>() != null ||
                !item.IsHaveWearLevel)
            {
                return false;
            }

            if (item.ItemGhostMetaSO == null ||
                !item.ItemGhostMetaSO.IsHaveGhostType(ItemGhostType.WearLevel))
            {
                return false;
            }

            return item.GetComponentInChildren<ItemWearState>(true) != null;
        }

        private bool ShowBusyPrinterStatus(GameObject target)
        {
            Printer printer = Printer.FindFor(target);

            if (printer == null || !printer.IsBusy)
            {
                return false;
            }

            cachedBusyPrinter = printer;
            ItemHoverInfoText.text = printer.DisplayName;
            ItemHoverInfoText.gameObject.SetActive(true);

            UpdatePrinterStatus(printer);

            TakeRoot.SetActive(false);
            SetupInspectWear(false);
            isHoverClear = false;
            return true;
        }

        private void UpdatePrinterStatus(Printer printer)
        {
            if (printer == null)
            {
                ClearPrinterStatus();
                return;
            }

            SetPrinterStatusText(
                PrinterStateProgressText,
                printer.StateProgressText,
                true);
            SetPrinterStatusText(
                PrinterNozzleTemperatureText,
                printer.NozzleTemperatureText,
                true);
            SetPrinterStatusText(
                PrinterBedTemperatureText,
                printer.BedTemperatureText,
                true);
            SetPrinterStatusText(
                PrinterSpoolMetersText,
                printer.SpoolMetersText,
                false);
            SetPrinterStatusText(
                PrinterPausePenaltyText,
                printer.PausePenaltyText,
                false);
        }

        private static void SetPrinterStatusText(
            TextMeshProUGUI text,
            string value,
            bool showWhenEmpty)
        {
            if (text == null)
            {
                return;
            }

            bool visible = showWhenEmpty || !string.IsNullOrEmpty(value);
            text.gameObject.SetActive(visible);

            if (visible)
            {
                text.text = value;
            }
        }

        private void ClearPrinterStatus()
        {
            cachedBusyPrinter = null;
            SetPrinterStatusText(PrinterStateProgressText, string.Empty, false);
            SetPrinterStatusText(PrinterNozzleTemperatureText, string.Empty, false);
            SetPrinterStatusText(PrinterBedTemperatureText, string.Empty, false);
            SetPrinterStatusText(PrinterSpoolMetersText, string.Empty, false);
            SetPrinterStatusText(PrinterPausePenaltyText, string.Empty, false);
        }

        private void SetupInfoAndTake(string info)
        {
            ItemHoverInfoText.text = info;
            ItemHoverInfoText.gameObject.SetActive(true);

            TakeRoot.SetActive(true);
            SetupInspectWear(cachedWearItem != null);

            isHoverClear = false;
        }

        private void SetupTake(bool isActive)
        {
            TakeRoot.SetActive(isActive);
            SetupInspectWear(false);

            isHoverClear = false;
        }

        private void SetupInspectWear(bool isActive)
        {
            if (InspectWearRoot == null)
            {
                return;
            }

            InspectWearRoot.SetActive(isActive);
        }

        private void ClearCachedCollider()
        {
            cachedCollider = default;
        }

        private void ClearCached()
        {
            cachedItemGameObject = default;
            cachedWearItem = default;
        }

        private void ClearHover()
        {
            ItemHoverInfoText.text = default;
            ItemHoverInfoText.gameObject.SetActive(false);

            TakeRoot.SetActive(false);
            SetupInspectWear(false);
            ClearPrinterStatus();

            isHoverClear = true;
        }

        private void OnDisable()
        {
            ClearCachedCollider();
            ClearCached();
            ClearHover();
        }

        // === Player Controller invoked ===

        public void ApplyTakeItem()
        {
            if (cachedItemGameObject == default)
            {
                return;
            }

            bool fromPrintBed = false;
            PrintedObjectDataHolder printedObject =
                cachedItemGameObject.GetComponent<PrintedObjectDataHolder>();

            if (printedObject != null && printedObject.IsOnPrintBed)
            {
                Printer printer = printedObject.Printer;

                if (printer == null ||
                    !printer.TryReleasePrintedObject(cachedItemGameObject))
                {
                    return;
                }

                fromPrintBed = true;
            }
            else
            {
                Printer printer = Printer.FindFor(cachedItemGameObject);

                if (printer != null && printer.IsBusy)
                {
                    return;
                }
            }

            // A wearable free part is carried together with its PartLocation
            // carrier. Regular free parts are their own movable transform.
            Transform movableTransform =
                ItemPlacementUtility.GetFreePartMovableTransform(
                    cachedItemGameObject);

            cachedItemGameObject.SetActive(false);

            GameStateManager.Instance.ChangeState(
                new GameStateItemPlace(
                    cachedItemGameObject,
                    movableTransform,
                    !fromPrintBed));
        }

        public void ApplyInspectWear()
        {
            if (cachedWearItem == null)
            {
                return;
            }

            GameStateManager.Instance.ChangeState(
                new GameStatePartsWear(default, cachedWearItem));
        }
    }
}

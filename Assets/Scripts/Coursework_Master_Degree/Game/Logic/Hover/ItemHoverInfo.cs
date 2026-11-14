using TMPro;
using UnityEngine;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.Items.Wear;
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
                    return;
                }

                cachedCollider = hit.collider;

                // prepare data collection
                ClearCached();
                ClearHover();

                if (hit.collider.attachedRigidbody == null)
                {
                    return;
                }

                if (hit.collider.attachedRigidbody.TryGetComponent<ItemFreePartDataHolder>(out var itemFreePart))
                {
                    if (itemFreePart.gameObject == cachedItemGameObject)
                    {
                        return;
                    }

                    cachedItemGameObject = itemFreePart.gameObject;

                    string info = itemFreePart.Name;
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

            return item.GetComponent<ItemWearState>() != null;
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

            cachedItemGameObject.SetActive(false);
            GameStateManager.Instance.ChangeState(new GameStateItemPlace(cachedItemGameObject));
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

using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine.State.ItemPlace;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateItemPlace : GameState
    {
        // Controllers
        private readonly PlayerController playerController;
        private readonly InterfaceController interfaceController;

        private readonly ItemPlaceData itemPlaceData;
        private readonly Influence influence;

        public ItemDataHolder ItemDataHolder { get; }
        public bool IsPartInstallable { get; }
        public GameState ReturnGameState => itemPlaceData.ReturnGameState;
        public PartsAssembly InstallTargetAssembly => itemPlaceData.InstallTargetAssembly;
        public string InstallBlockReason => itemPlaceData.InstallBlockReason;

        public void ShowInstallBlockedReason()
        {
            if (!string.IsNullOrEmpty(InstallBlockReason) &&
                playerController != null &&
                playerController.ItemHoverPlace != null)
            {
                playerController.ItemHoverPlace.ShowInstallBlockedReason(
                    InstallBlockReason);
            }
        }

        public GameStateItemPlace(
            GameObject itemGameObject,
            Transform movableTransform = null)
        {
            playerController = GameStateManager.Instance.PlayerController;
            interfaceController = GameStateManager.Instance.InterfaceController;

            ItemDataHolder itemDataHolder = itemGameObject.GetComponent<ItemDataHolder>();

            // A core item owns its free parts (PC monitor, mouse, keyboard,
            // etc.). Reset them to the authored slots before the ghost is
            // extracted, so moving the core keeps its parts together.
            itemDataHolder?.ResetFreePartsToLocations();

            ExtractGhostData(
                itemGameObject,
                out var itemName,
                out var ghostGameObject,
                out var ghostMeshRenderers,
                out var ghostCorners);
            ItemSurfaceDataHolder requiredSurface =
                GetRequiredSurface(itemGameObject, out var isSurfaceRestricted);
            itemPlaceData = new ItemPlaceData()
            {
                ItemGameObject = itemGameObject,
                MovableTransform = movableTransform != null
                    ? movableTransform
                    : itemGameObject.transform,
                ReturnGameState = GetReturnGameState(),
                IsCancelAvailable = IsItemPlaceCancelAvailable(),
                IsInstallModeAvailable = IsInstallablePart(itemDataHolder),
                ItemName = itemName,
                GhostGameObject = ghostGameObject,
                GhostMeshRenderers = ghostMeshRenderers,
                GhostCorners = ghostCorners,
                RequiredSurface = requiredSurface,
                IsSurfaceRestricted = isSurfaceRestricted
            };
            influence = CreateInfluenceFrom(itemGameObject);

            ItemDataHolder = itemDataHolder;
            IsPartInstallable = itemPlaceData.IsInstallModeAvailable;
        }

        private static bool IsInstallablePart(ItemDataHolder item)
        {
            // An installed free part is carried together with its PartLocation
            // carrier. It is already in an assembly, so offering installation
            // while it is being moved would detach it from the carrier.
            return item != null &&
                !item.IsInstalled &&
                item.ItemGhostMetaSO != null &&
                item.ItemGhostMetaSO.IsHaveGhostType(ItemGhostType.Installation);
        }

        private static ItemSurfaceDataHolder GetRequiredSurface(
            GameObject itemGameObject,
            out bool isSurfaceRestricted)
        {
            isSurfaceRestricted = false;

            if (!itemGameObject.TryGetComponent(out ItemFreePartDataHolder _))
            {
                return default;
            }

            // A nested free part must stay on the surface its core item is
            // placed on, so e.g. a PC mouse cannot be moved to an unrelated
            // table. A standalone item which also exposes a free-part
            // component (e.g. a wearable printer adapter) has no core item
            // above it, so it can be placed freely.
            // The free part is deactivated before this state is created, so
            // inactive parents have to be searched as well.
            Transform parent = itemGameObject.transform.parent;
            ItemDataHolder coreItem = parent != null
                ? parent.GetComponentInParent<ItemDataHolder>(true)
                : default;

            if (coreItem == null)
            {
                return default;
            }

            while (coreItem.IsInstalled && coreItem.ParentItemDataHolder != null)
            {
                coreItem = coreItem.ParentItemDataHolder;
            }

            isSurfaceRestricted = true;

            return coreItem.ParentSurfaceDataHolder;
        }

        private GameState GetReturnGameState()
        {
            var gameStateManager = GameStateManager.Instance;

            if (gameStateManager.CurrentGameState is GameStatePartsRemove)
            {
                return GameStatePartsRemove.GetReturnGameState();
            }

            if (gameStateManager.CurrentGameState is GameStateItemsMarket)
            {
                return gameStateManager.PreviousGameState;
            }

            return gameStateManager.CurrentGameState;
        }

        private static bool IsItemPlaceCancelAvailable()
        {
            // Buying an item or re-placing a removed part is a forced
            // placement flow, so it must not expose the cancel hint.
            GameState currentGameState = GameStateManager.Instance.CurrentGameState;

            return currentGameState is not GameStateItemsMarket &&
                currentGameState is not GameStatePartsRemove;
        }

        private void ExtractGhostData(GameObject itemGameObject, out string itemName, out GameObject hostGameObject, out List<MeshRenderer> ghostMeshRenderers, out List<Transform> ghostCorners)
        {
            itemName = default;
            hostGameObject = default;
            ghostMeshRenderers = default;
            ghostCorners = default;

            List<GameObject> cornersRaw = default;

            if (itemGameObject.TryGetComponent<ItemFreePartDataHolder>(out var itemFreePart))
            {
                itemName = itemFreePart.Name;

                if (!itemFreePart.TryGetGhostInstance(out var itemFreePartGhost))
                {
                    itemFreePartGhost =
                        GameStateManager.Instance.ItemInstancer
                        .InstantiateItemFreePartGhost(
                            itemFreePart,
                            itemFreePart.transform.position,
                            itemFreePart.transform.rotation);
                }
                GameStateManager.Instance.ItemInstancer.SyncGhostToSource(
                    itemFreePartGhost.transform,
                    itemFreePart.transform);

                itemFreePartGhost.gameObject.SetActive(true);
                hostGameObject = itemFreePartGhost.gameObject;

                ghostMeshRenderers = itemFreePartGhost.MeshRenderersList;
                cornersRaw = (itemFreePartGhost as ItemFreePartGhostPlacementDataHolder).CornersList;
            }
            else if (itemGameObject.TryGetComponent<ItemDataHolder>(out var item))
            {
                itemName = item.ItemMetaData.Name;

                if (!item.TryGetGhostInstance(ItemGhostType.Placement, out var itemGhost))
                {
                    itemGhost =
                        GameStateManager.Instance.ItemInstancer
                        .InstantiateItemGhost(
                            item,
                            ItemGhostType.Placement,
                            item.transform.position,
                            item.transform.rotation);
                }
                GameStateManager.Instance.ItemInstancer.SyncItemGhostToItem(
                    itemGhost,
                    item);

                itemGhost.gameObject.SetActive(true);
                hostGameObject = itemGhost.gameObject;

                ghostMeshRenderers = itemGhost.MeshRenderersList;
                cornersRaw = (itemGhost as ItemGhostPlacementDataHolder).CornersList;
            }
            else if (itemGameObject.TryGetComponent<ObjectToPrintDataHolder>(out var objectToPrint))
            {
                itemName = objectToPrint.ObjectToPrintMetaData.Name;

                if (!objectToPrint.TryGetGhostInstance(ObjectToPrintGhostType.Placement, out var objectToPrintGhost))
                {
                    objectToPrintGhost =
                        GameStateManager.Instance.ItemInstancer
                        .InstantiateObjectToPrintGhost(
                            objectToPrint,
                            ObjectToPrintGhostType.Placement,
                            objectToPrint.transform.position,
                            objectToPrint.transform.rotation);
                }
                GameStateManager.Instance.ItemInstancer.SyncGhostToSource(
                    objectToPrintGhost.transform,
                    objectToPrint.transform);

                objectToPrintGhost.gameObject.SetActive(true);
                hostGameObject = objectToPrintGhost.gameObject;

                ghostMeshRenderers = objectToPrintGhost.MeshRenderersList;
                cornersRaw = (objectToPrintGhost as ObjectToPrintGhostPlacementDataHolder).CornersList;
            }

            ghostCorners = TranslateCorners(cornersRaw);
        }

        private List<Transform> TranslateCorners(List<GameObject> cornersRaw)
        {
            var cornersTranslated = new List<Transform>();

            if (cornersRaw == null)
            {
                return cornersTranslated;
            }

            foreach (var cornerRaw in cornersRaw)
            {
                cornersTranslated.Add(cornerRaw.transform);
            }

            return cornersTranslated;
        }

        private Influence CreateInfluenceFrom(GameObject itemGameObject)
        {
            if (itemGameObject.TryGetComponent<ItemDataHolder>(out var item))
            {
                return Influence.CreateFrom(item);
            }
            else
            {
                return new Influence();
            }
        }

        public override void OnEnter()
        {
            HideCursor();

            ShowPlacementGhost();

            GameStateManager.Instance.InputReader.EnableItemPlaceStateInput();

            playerController.SetItemPlaceState(true, itemPlaceData, influence);
            interfaceController.SetItemPlaceState(true, IsPartInstallable);
        }

        private void ShowPlacementGhost()
        {
            // The ghost is hidden when entering part install. Re-entering
            // this state (e.g. after quitting part install) must restore it.
            if (itemPlaceData.GhostGameObject == null)
            {
                return;
            }

            itemPlaceData.GhostGameObject.SetActive(true);
        }

        public override void OnExit()
        {
            interfaceController.SetItemPlaceState(false, IsPartInstallable);
            playerController.SetItemPlaceState(false);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

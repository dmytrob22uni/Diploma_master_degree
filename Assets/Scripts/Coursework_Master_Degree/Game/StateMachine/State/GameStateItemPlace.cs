using System;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Ghosts;
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

        public GameStateItemPlace(GameObject itemGameObject)
        {
            playerController = GameStateManager.Instance.PlayerController;
            interfaceController = GameStateManager.Instance.InterfaceController;

            ExtractGhostData(
                itemGameObject,
                out var clearParentSurface,
                out var setParentSurface,
                out var itemName,
                out var ghostGameObject,
                out var ghostMeshRenderers,
                out var ghostCorners);
            itemPlaceData = new ItemPlaceData()
            {
                ItemGameObject = itemGameObject,
                ClearParentSurface = clearParentSurface,
                SetParentSurface = setParentSurface,
                ItemName = itemName,
                GhostGameObject = ghostGameObject,
                GhostMeshRenderers = ghostMeshRenderers,
                GhostCorners = ghostCorners
            };
            influence = CreateInfluenceFrom(itemGameObject);
        }

        private void ExtractGhostData(GameObject itemGameObject, out Action clearParentSurface, out Action<ItemSurfaceDataHolder> setParentSurface, out string itemName, out GameObject hostGameObject, out List<MeshRenderer> ghostMeshRenderers, out List<Transform> ghostCorners)
        {
            clearParentSurface = default;
            setParentSurface = default;
            itemName = default;
            hostGameObject = default;
            ghostMeshRenderers = default;
            ghostCorners = default;

            List<GameObject> cornersRaw = default;

            if (itemGameObject.TryGetComponent<ItemFreePartDataHolder>(out var itemFreePart))
            {
                clearParentSurface =
                    () =>
                    {
                        var surface = itemFreePart.ParentSurfaceDataHolder;
                        surface.TryRemoveItemFromShelf(itemFreePart.gameObject);
                        itemFreePart.IsPlaced = false;
                        itemFreePart.ParentSurfaceDataHolder = default;
                    };
                setParentSurface =
                    (surface) =>
                    {
                        surface.PlaceItemOnShelf(itemFreePart.gameObject);
                        itemFreePart.IsPlaced = true;
                        itemFreePart.ParentSurfaceDataHolder = surface;
                    };
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
                itemFreePartGhost.gameObject.SetActive(true);
                hostGameObject = itemFreePartGhost.gameObject;

                ghostMeshRenderers = itemFreePartGhost.MeshRenderersList;
                cornersRaw = (itemFreePartGhost as ItemFreePartGhostPlacementDataHolder).CornersList;
            }
            else if (itemGameObject.TryGetComponent<ItemDataHolder>(out var item))
            {
                clearParentSurface =
                    () =>
                    {
                        var surface = item.ParentSurfaceDataHolder;
                        surface.TryRemoveItemFromShelf(item.gameObject);
                        item.IsPlaced = false;
                        item.ParentSurfaceDataHolder = default;
                    };
                setParentSurface =
                    (surface) =>
                    {
                        surface.PlaceItemOnShelf(item.gameObject);
                        item.IsPlaced = true;
                        item.ParentSurfaceDataHolder = surface;
                    };
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
                itemGhost.gameObject.SetActive(true);
                hostGameObject = itemGhost.gameObject;

                ghostMeshRenderers = itemGhost.MeshRenderersList;
                cornersRaw = (itemGhost as ItemGhostPlacementDataHolder).CornersList;
            }
            else if (itemGameObject.TryGetComponent<ObjectToPrintDataHolder>(out var objectToPrint))
            {
                clearParentSurface =
                    () =>
                    {
                        var surface = objectToPrint.ParentSurfaceDataHolder;
                        surface.TryRemoveItemFromShelf(objectToPrint.gameObject);
                        objectToPrint.IsPlaced = false;
                        objectToPrint.ParentSurfaceDataHolder = default;
                    };
                setParentSurface =
                    (surface) =>
                    {
                        surface.PlaceItemOnShelf(objectToPrint.gameObject);
                        objectToPrint.IsPlaced = true;
                        objectToPrint.ParentSurfaceDataHolder = surface;
                    };
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

            GameStateManager.Instance.InputReader.EnableItemPlaceStateInput();

            playerController.SetItemPlaceState(true, itemPlaceData, influence);
            interfaceController.SetItemPlaceState(true);
        }

        public override void OnExit()
        {
            interfaceController.SetItemPlaceState(false);
            playerController.SetItemPlaceState(false);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

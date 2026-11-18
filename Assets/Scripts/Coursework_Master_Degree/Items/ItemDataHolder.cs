using System;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Influences;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items
{
    public class ItemDataHolder : MonoBehaviour, ISurfacePlaceable
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemMetaSO ItemMetaData;
        public ItemToPlayerInfluencesSO ItemToPlayerInfluencesSO;

        public ItemGhostMetaSO ItemGhostMetaSO;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public List<ItemGhostTypeToInstance> ItemGhostTypesToInstancesList;

        [Tooltip("Hint to search for ItemSurfaceDataHolder")]
        public bool IsSurface = false;

        [Tooltip("Used to expose list for storing free parts reset locations")]
        public bool IsHaveFreeParts = false;

        [ShowIf("IsHaveFreeParts")]
        [EnableIf("IsHaveFreeParts")]
        public List<ItemPartToLocation> ItemPartsToLocationsList;

        [Tooltip("Tip to look for IWearLevel")]
        public bool IsHaveWearLevel = false;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public bool IsInstalled;
        [EnableIf("NotEnabled")]
        [ShowIf("IsInstalled")]
        [Tooltip("Set via script")]
        public ItemDataHolder ParentItemDataHolder;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public bool IsPlaced;
        [EnableIf("NotEnabled")]
        [ShowIf("IsPlaced")]
        [Tooltip("Set via script")]
        public ItemSurfaceDataHolder ParentSurfaceDataHolder;

        bool ISurfacePlaceable.IsPlaced => IsPlaced;
        ItemSurfaceDataHolder ISurfacePlaceable.ParentSurfaceDataHolder => ParentSurfaceDataHolder;

        public bool PlaceOnSurface(ItemSurfaceDataHolder surface)
        {
            if (surface == null)
            {
                return false;
            }

            bool isChanged = false;

            if (ParentSurfaceDataHolder != null && ParentSurfaceDataHolder != surface)
            {
                isChanged |= ParentSurfaceDataHolder.TryRemoveItemFromShelf(gameObject);
            }

            isChanged |= surface.TryPlaceItemOnShelf(gameObject);

            if (!IsPlaced)
            {
                IsPlaced = true;
                isChanged = true;
            }

            if (ParentSurfaceDataHolder != surface)
            {
                ParentSurfaceDataHolder = surface;
                isChanged = true;
            }

            return isChanged;
        }

        public void ResetFreePartsToLocations()
        {
            if (!IsHaveFreeParts || ItemPartsToLocationsList == null)
            {
                return;
            }

            foreach (ItemPartToLocation mapping in ItemPartsToLocationsList)
            {
                if (mapping == null ||
                    mapping.ItemPart == null ||
                    mapping.Location == null)
                {
                    continue;
                }

                GameObject itemPart = mapping.ItemPart;

                // A wearable free part is installed into a PartLocation
                // carrier. Move the carrier, not the part itself, so the
                // location collider and the installed part travel together.
                Transform movableTransform =
                    ItemPlacementUtility.GetFreePartMovableTransform(itemPart);

                if (movableTransform == null)
                {
                    continue;
                }

                movableTransform.SetParent(mapping.Location, true);
                movableTransform.SetPositionAndRotation(
                    mapping.Location.position,
                    mapping.Location.rotation);

                // The free part is no longer resting on its old shelf.
                ItemPlacementUtility.RemoveFromSurface(itemPart);
                ItemPlacementUtility.HidePlacementGhosts(itemPart);
            }
        }

        public bool TryGetFreePartLocation(GameObject itemPart, out Transform location)
        {
            location = default;

            if (!IsHaveFreeParts ||
                ItemPartsToLocationsList == null ||
                itemPart == null)
            {
                return false;
            }

            foreach (ItemPartToLocation mapping in ItemPartsToLocationsList)
            {
                if (mapping == null ||
                    mapping.ItemPart != itemPart ||
                    mapping.Location == null)
                {
                    continue;
                }

                location = mapping.Location;
                return true;
            }

            return false;
        }

        public bool RemoveFromSurface()
        {
            bool isChanged = false;

            if (ParentSurfaceDataHolder != null)
            {
                isChanged |= ParentSurfaceDataHolder.TryRemoveItemFromShelf(gameObject);
            }

            if (IsPlaced)
            {
                IsPlaced = false;
                isChanged = true;
            }

            if (ParentSurfaceDataHolder != null)
            {
                ParentSurfaceDataHolder = default;
                isChanged = true;
            }

            return isChanged;
        }

        public bool TryAddGhostType(ItemGhostType type, ItemGhostDataHolder itemGhost)
        {
            if (IsContainGhostType(type))
            {
                return false;
            }
            else
            {
                AddGhostType(type, itemGhost);
                return true;
            }
        }

        public void AddGhostType(ItemGhostType type, ItemGhostDataHolder itemGhost)
        {
            ItemGhostTypesToInstancesList.Add(new ItemGhostTypeToInstance(type, itemGhost));
        }

        private bool IsContainGhostType(ItemGhostType type)
        {
            foreach (var itemGhostTypeToInstance in ItemGhostTypesToInstancesList)
            {
                if (itemGhostTypeToInstance.ItemGhostType == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetGhostInstance(ItemGhostType type, out ItemGhostDataHolder itemGhost)
        {
            foreach (var itemGhostTypeToInstance in ItemGhostTypesToInstancesList)
            {
                if (itemGhostTypeToInstance.ItemGhostType == type)
                {
                    itemGhost = itemGhostTypeToInstance.ItemGhostInstance;
                    return true;
                }
            }

            itemGhost = default;
            return false;
        }

        public ItemGhostDataHolder GetGhostInstance(ItemGhostType type)
        {
            foreach (var itemGhostTypeToInstance in ItemGhostTypesToInstancesList)
            {
                if (itemGhostTypeToInstance.ItemGhostType == type)
                {
                    return itemGhostTypeToInstance.ItemGhostInstance;
                }
            }

            throw new NullReferenceException($"Item {ItemMetaData.Name} doesn't have ghost of type \'{type}\' yet");
        }

        public bool TryGetGhostType(ItemGhostDataHolder itemGhost, out ItemGhostType type)
        {
            foreach (var itemGhostTypeToInstance in ItemGhostTypesToInstancesList)
            {
                if (itemGhostTypeToInstance.ItemGhostInstance == itemGhost)
                {
                    type = itemGhostTypeToInstance.ItemGhostType;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public ItemGhostType TryGetGhostType(ItemGhostDataHolder itemGhost)
        {
            foreach (var itemGhostTypeToInstance in ItemGhostTypesToInstancesList)
            {
                if (itemGhostTypeToInstance.ItemGhostInstance == itemGhost)
                {
                    return itemGhostTypeToInstance.ItemGhostType;
                }
            }

            throw new NullReferenceException($"Item {ItemMetaData.Name} doesn't possess this ghost");
        }
    }
}

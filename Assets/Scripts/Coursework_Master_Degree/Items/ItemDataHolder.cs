using System;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Influences;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Unity.VisualScripting;
using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Items
{
    public class ItemDataHolder : MonoBehaviour
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

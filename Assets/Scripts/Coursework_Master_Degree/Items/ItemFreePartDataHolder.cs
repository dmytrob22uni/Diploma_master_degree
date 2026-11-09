using System;
using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Items
{
    public class ItemFreePartDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public string Name;

        public GameObject ItemFreePartGhostPlacementPrefab;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public ItemFreePartGhostPlacementDataHolder ItemFreePartGhostPlacementInstance;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public bool IsPlaced;
        [EnableIf("NotEnabled")]
        [ShowIf("IsPlaced")]
        [Tooltip("Set via script")]
        public ItemSurfaceDataHolder ParentSurfaceDataHolder;

        public bool TryAddGhost(ItemFreePartGhostPlacementDataHolder itemFreePartGhost)
        {
            if (IsContainGhost())
            {
                return false;
            }
            else
            {
                AddGhost(itemFreePartGhost);
                return true;
            }
        }

        public void AddGhost(ItemFreePartGhostPlacementDataHolder itemFreePartGhost)
        {
            ItemFreePartGhostPlacementInstance = itemFreePartGhost;
        }

        private bool IsContainGhost()
        {
            if (ItemFreePartGhostPlacementInstance == null)
            {
                return false;
            }

            return true;
        }

        public bool TryGetGhostInstance(out ItemFreePartGhostPlacementDataHolder itemFreePartGhost)
        {
            if (ItemFreePartGhostPlacementInstance == null)
            {
                itemFreePartGhost = default;
                return false;
            }

            itemFreePartGhost = ItemFreePartGhostPlacementInstance;
            return true;
        }

        public ItemFreePartGhostPlacementDataHolder GetGhostInstance()
        {
            if (ItemFreePartGhostPlacementInstance == null)
            {
                throw new NullReferenceException($"Item {Name} doesn't have ghost of type \'Placement\' yet");
            }

            return ItemFreePartGhostPlacementInstance;
        }
    }
}

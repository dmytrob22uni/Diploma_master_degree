using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.Surface
{
    public class ItemSurfaceDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemType itemType;

        public List<Collider> ShelvesCollidersList = new List<Collider>();

        [EnableIf("NotEnabled")]
        [Tooltip("Capacity and belonging is controlled via internal functions. Exposed for debugging")]
        public List<GameObject> ItemsOnShelvesList = new List<GameObject>();

        public bool IsShelfCollider(Collider collider)
        {
            return ShelvesCollidersList.Contains(collider);
        }

        public bool IsItemOnShelf(GameObject item)
        {
            return ItemsOnShelvesList.Contains(item);
        }

        public bool TryPlaceItemOnShelf(GameObject item)
        {
            if (!IsItemOnShelf(item))
            {
                PlaceItemOnShelf(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void PlaceItemOnShelf(GameObject item)
        {
            ItemsOnShelvesList.Add(item);
        }

        public bool TryRemoveItemFromShelf(GameObject item)
        {
            if (IsItemOnShelf(item))
            {
                RemoveItemFromShelf(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveItemFromShelf(GameObject item)
        {
            ItemsOnShelvesList.Remove(item);
        }

        public bool IsEmptyShelves()
        {
            return ItemsOnShelvesList.Count == 0;
        }
    }
}

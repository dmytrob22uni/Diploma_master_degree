using UnityEngine;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items
{
    /// <summary>
    /// Handles items which expose more than one placeable component on the same
    /// GameObject. A free part can also be a regular item (and a wearable
    /// part), so its free-part and item surface states must be kept in sync.
    /// Only components on the item root are touched; nested free parts keep
    /// their own placement state.
    /// </summary>
    public static class ItemPlacementUtility
    {
        public static bool PlaceOnSurface(GameObject item, ItemSurfaceDataHolder surface)
        {
            if (item == null || surface == null)
            {
                return false;
            }

            bool isChanged = false;

            foreach (MonoBehaviour behaviour in item.GetComponents<MonoBehaviour>())
            {
                if (behaviour is ISurfacePlaceable placeable)
                {
                    isChanged |= placeable.PlaceOnSurface(surface);
                }
            }

            return isChanged;
        }

        public static bool RemoveFromSurface(GameObject item)
        {
            if (item == null)
            {
                return false;
            }

            bool isChanged = false;

            foreach (MonoBehaviour behaviour in item.GetComponents<MonoBehaviour>())
            {
                if (behaviour is ISurfacePlaceable placeable)
                {
                    isChanged |= placeable.RemoveFromSurface();
                }
            }

            return isChanged;
        }

        public static void HidePlacementGhosts(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            // Combined items use the free-part ghost while carried, while
            // regular items use the item placement ghost. Hide every variant
            // so switching to installation cannot leave a preview behind.
            if (item.TryGetComponent(out ItemDataHolder itemDataHolder) &&
                itemDataHolder.TryGetGhostInstance(ItemGhostType.Placement, out ItemGhostDataHolder itemGhost) &&
                itemGhost != null)
            {
                itemGhost.gameObject.SetActive(false);
            }

            if (item.TryGetComponent(out ItemFreePartDataHolder freePart) &&
                freePart.TryGetGhostInstance(out ItemFreePartGhostPlacementDataHolder freePartGhost) &&
                freePartGhost != null)
            {
                freePartGhost.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Returns the transform which represents the free part while it is
        /// moved. Regular free parts are their own movable root. A wearable
        /// free part is installed into a PartLocation carrier, so the carrier
        /// is moved instead: the part stays inside it and the location collider
        /// follows the actual item.
        /// </summary>
        public static Transform GetFreePartMovableTransform(GameObject itemPart)
        {
            if (itemPart == null)
            {
                return default;
            }

            Transform parent = itemPart.transform.parent;

            if (parent != null &&
                parent.TryGetComponent(out PartLocation partLocation) &&
                partLocation.InstalledPart == itemPart)
            {
                return partLocation.transform;
            }

            return itemPart.transform;
        }

        /// <summary>
        /// Returns a free part to its authored reset point. A wearable free
        /// part moves its PartLocation carrier so the socket travels with it.
        /// </summary>
        public static void ResetFreePart(GameObject itemPart, Transform location)
        {
            if (itemPart == null || location == null)
            {
                return;
            }

            Transform movableTransform =
                GetFreePartMovableTransform(itemPart);

            if (movableTransform == null)
            {
                return;
            }

            movableTransform.SetParent(location, true);
            movableTransform.SetPositionAndRotation(
                location.position,
                location.rotation);

            RemoveFromSurface(itemPart);
            HidePlacementGhosts(itemPart);
        }
    }
}

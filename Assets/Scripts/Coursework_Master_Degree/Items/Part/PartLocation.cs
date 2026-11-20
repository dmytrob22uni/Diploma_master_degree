using UnityEngine;
using Coursework_Master_Degree.Items.FFF;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;

namespace Coursework_Master_Degree.Items.Part
{
    public class PartLocation : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        // Install prerequisites. CanInstall() requires every location in
        // this list to be occupied.
        [Tooltip("Locations that must be installed before this part can be installed")]
        public bool HasInstallPrerequisites = false;
        [ShowIf("HasInstallPrerequisites")]
        public List<PartLocation> InstallPrerequisiteLocations;

        // Removal blockers. CanRemove() requires every location in this list
        // to be empty.
        [Tooltip("Locations that must be removed before this part can be removed")]
        public bool HasRemovalBlockers = false;
        [ShowIf("HasRemovalBlockers")]
        public List<PartLocation> RemovalBlockerLocations;

        // Meta data reference(-s)
        public bool IsSingleMetaDataReference = true;
        [ShowIf("IsSingleMetaDataReference")]
        public ItemMetaSO PartMetaData;
        [ShowIf("IsMultipleMetaDataReferences")]
        public List<ItemMetaSO> PartMetaDatasList;

        // Installed part
        [Tooltip("Set via hierarchy manager and on manual setup")]
        public GameObject InstalledPart;

        // Occupation check
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script. Exposed for debugging")]
        public bool IsOccupied;

        public bool IsOverridePartPurposeDescription;
        [TextArea]
        [ShowIf("IsOverridePartPurposeDescription")]
        public string PartPurposeDescription = "Part purpose description TO REPLACE";

        public bool IsOverrideMissingPartDescription;
        [TextArea]
        [ShowIf("IsOverrideMissingPartDescription")]
        public string MissingPartDescription = "Missing part description TO REPLACE";

        private void OnValidate()
        {
            IsOccupied = IsPartInstalled();
        }

        private void Awake()
        {
            IsOccupied = IsPartInstalled();
        }

        // On Validate
        private bool IsMultipleMetaDataReferences()
        {
            return !IsSingleMetaDataReference;
        }

        public bool IsPartInstalled()
        {
            return InstalledPart != null;
        }

        // Installation checks

        public bool IsExpectedItemMetaData(GameObject part)
        {
            if (!part.TryGetComponent<ItemDataHolder>(out ItemDataHolder partDataHolder))
            {
                Debug.Log("This part doesn't have Item Data Holder component on it. Probably, it is not even an item, not to say part");
                return false;
            }
            else
            {
                if (IsSingleMetaDataReference)
                {
                    return partDataHolder.ItemMetaData == PartMetaData;
                }
                else
                {
                    foreach (var metaData in PartMetaDatasList)
                    {
                        if (partDataHolder.ItemMetaData == metaData)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        public bool HasFreeInstallPrerequisites()
        {
            if (InstallPrerequisiteLocations == null)
            {
                return false;
            }

            foreach (PartLocation prerequisite in InstallPrerequisiteLocations)
            {
                if (!prerequisite.IsOccupied)
                {
                    return true;
                }
            }

            return false;
        }

        public List<PartLocation> GetFreeInstallPrerequisites()
        {
            List<PartLocation> list = new List<PartLocation>();

            if (InstallPrerequisiteLocations == null)
            {
                return list;
            }

            foreach (PartLocation prerequisite in InstallPrerequisiteLocations)
            {
                if (!prerequisite.IsOccupied)
                {
                    list.Add(prerequisite);
                }
            }

            return list;
        }

        public bool CanInstall(GameObject part)
        {
            Printer printer = Printer.FindFor(gameObject);
            string serviceBlock = printer != null
                ? printer.GetPartServiceBlockReason(this)
                : null;

            if (!string.IsNullOrEmpty(serviceBlock))
            {
                Debug.Log(serviceBlock);
                return false;
            }

            if (!IsExpectedItemMetaData(part))
            {
                Debug.Log("Meta Data object of the passed part is not among expected");
                return false;
            }

            ItemDataHolder partItem = part.GetComponent<ItemDataHolder>();
            string enclosureBlockReason =
                Printer.GetPrinterEnclosureInstallBlockReason(
                    partItem,
                    GetComponentInParent<PartsAssembly>());

            if (!string.IsNullOrEmpty(enclosureBlockReason))
            {
                Debug.Log(enclosureBlockReason);
                return false;
            }

            if (HasFreeInstallPrerequisites())
            {
                Debug.Log("Trying to install a part with missing install prerequisites. Can't do that");
                return false;
            }

            if (IsOccupied)
            {
                Debug.Log("Trying to install part in occupied location. Can't do that");
                return false;
            }

            return true;
        }

        // Removing checks

        public bool HasOccupiedRemovalBlockers()
        {
            if (RemovalBlockerLocations == null)
            {
                return false;
            }

            foreach (PartLocation blocker in RemovalBlockerLocations)
            {
                if (blocker.IsOccupied)
                {
                    return true;
                }
            }

            return false;
        }

        public List<PartLocation> GetOccupiedRemovalBlockers()
        {
            List<PartLocation> list = new List<PartLocation>();

            if (RemovalBlockerLocations == null)
            {
                return list;
            }

            foreach (PartLocation blocker in RemovalBlockerLocations)
            {
                if (blocker.IsOccupied)
                {
                    list.Add(blocker);
                }
            }

            return list;
        }

        public bool CanRemove()
        {
            Printer printer = Printer.FindFor(gameObject);
            string serviceBlock = printer != null
                ? printer.GetPartServiceBlockReason(this)
                : null;

            if (!string.IsNullOrEmpty(serviceBlock))
            {
                Debug.Log(serviceBlock);
                return false;
            }

            if (HasOccupiedRemovalBlockers())
            {
                Debug.Log("This part has removal blockers installed. Can't remove it");
                return false;
            }

            if (!IsOccupied)
            {
                Debug.Log("Trying to remove part from free location. Can't do that");
                return false;
            }

            return true;
        }

        // Operations

        public void Install(GameObject part)
        {
            if (!CanInstall(part))
            {
                return;
            }

            // A wearable free part may have moved its PartLocation carrier
            // while being carried. Put the carrier back to its authored
            // free-part home before installing the part into it.
            ResetToFreePartLocation(part);

            AttachPart(part);

            part.GetComponent<Printer>()?.DockToEnclosure();

            Printer.FindFor(gameObject)?.NotifyPartChanged(this);
        }

        public GameObject Remove()
        {
            if (!CanRemove())
            {
                return null;
            }

            GameObject part = InstalledPart;

            part.GetComponent<Printer>()?.UndockFromEnclosure();

            DetachPart();

            Printer.FindFor(gameObject)?.NotifyPartChanged(this);

            // The free-part carrier can have been moved with the part while
            // it was used as a free part. Return it to the authored home so
            // the assembly socket stays where the core item expects it.
            ResetToFreePartLocation(part);

            return part;
        }

        /// <summary>
        /// Moves the currently installed part into another empty socket.
        /// Used by enclosure docking to relocate printer accessories without
        /// running player-facing installation validation or free-part resets.
        /// </summary>
        public bool MoveInstalledPartTo(PartLocation target)
        {
            if (target == null ||
                target == this ||
                !IsOccupied ||
                InstalledPart == null ||
                target.IsOccupied)
            {
                return false;
            }

            GameObject part = InstalledPart;

            DetachPart();
            target.AttachPart(part);

            return true;
        }

        private void AttachPart(GameObject part)
        {
            // The location is the socket: the part must adopt its position,
            // rotation and fitment scale instead of keeping the world scale it
            // had while being carried.
            part.transform.SetParent(gameObject.transform, false);
            part.transform.SetAsFirstSibling();
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;

            InstalledPart = part;
            IsOccupied = true;
        }

        private void DetachPart()
        {
            if (InstalledPart != null)
            {
                InstalledPart.transform.SetParent(null);
            }

            InstalledPart = null;
            IsOccupied = false;
        }

        public void ResetToFreePartLocation(GameObject itemPart)
        {
            ItemDataHolder coreItem =
                GetComponentInParent<ItemDataHolder>(true);

            if (coreItem == null ||
                !coreItem.TryGetFreePartLocation(itemPart, out Transform location))
            {
                return;
            }

            transform.SetParent(location, true);
            transform.SetPositionAndRotation(
                location.position,
                location.rotation);
        }
    }
}

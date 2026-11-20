using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.Items.FFF;

namespace Coursework_Master_Degree.Items.Part
{
    public class PartsAssembly : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public bool IsHaveOptionalParts;
        [ShowIf("IsHaveOptionalParts")]
        [Tooltip("Parts of which not less than N [0, int.MaxValue] is / are needed")]
        public List<OptionalPart> OptionalPartLocationsList;

        public List<PartLocation> RegularPartLocationsList;

        private void Awake()
        {
            SetupItemsInstallationStates();
        }

        private void SetupItemsInstallationStates()
        {
            ItemDataHolder localItem = GetComponent<ItemDataHolder>();

            foreach (var optionalPart in OptionalPartLocationsList)
            {
                foreach (var part in optionalPart.PartLocationsList)
                {
                    if (part.IsOccupied)
                    {
                        ItemDataHolder item = part.InstalledPart.GetComponent<ItemDataHolder>();
                        item.IsInstalled = true;
                        item.ParentItemDataHolder = localItem;
                    }
                }
            }

            foreach (var part in RegularPartLocationsList)
            {
                if (part.IsOccupied)
                {
                    ItemDataHolder item = part.InstalledPart.GetComponent<ItemDataHolder>();
                    item.IsInstalled = true;
                    item.ParentItemDataHolder = localItem;
                }
            }
        }

        /// <summary>
        /// Returns the enclosure assembly that owns this item when the item is
        /// an installed printer (or one of its internal parts). Returns null
        /// for a standalone printer or a non-enclosure parent.
        /// </summary>
        public static PartsAssembly FindEnclosingPrinterEnclosure(
            ItemDataHolder item)
        {
            return item != null
                ? FindEnclosingPrinterEnclosure(
                    item.GetComponentInParent<Printer>())
                : null;
        }

        /// <summary>
        /// Returns the enclosure assembly that owns this printer when it is
        /// installed in one. Returns null for a standalone printer.
        /// </summary>
        public static PartsAssembly FindEnclosingPrinterEnclosure(
            Printer printer)
        {
            if (printer == null)
            {
                return null;
            }

            PartLocation location =
                printer.GetComponentInParent<PartLocation>();

            if (location == null)
            {
                return null;
            }

            PartsAssembly assembly =
                location.GetComponentInParent<PartsAssembly>();

            if (assembly == null ||
                assembly == printer.Assembly ||
                assembly.GetComponentInParent<PrinterEnclosure>() == null)
            {
                return null;
            }

            return assembly;
        }

        /// <summary>
        /// Resolves a part inside an installed printer to the printer root when
        /// the inspected assembly is its enclosure.
        /// </summary>
        public static bool TryGetInstalledPrinterItem(
            ItemDataHolder item,
            PartsAssembly targetAssembly,
            out ItemDataHolder printerItem)
        {
            printerItem = null;

            if (item == null || targetAssembly == null ||
                FindEnclosingPrinterEnclosure(item) != targetAssembly)
            {
                return false;
            }

            Printer printer = item.GetComponentInParent<Printer>();

            if (printer == null)
            {
                return false;
            }

            printerItem = printer.GetComponent<ItemDataHolder>();
            return printerItem != null;
        }

        // assembly parts

        public bool IsPartOfAssembly(Transform partTransform)
        {
            if (partTransform == null)
            {
                return false;
            }

            return partTransform == transform || partTransform.IsChildOf(transform);
        }

        public bool IsPartLocationOfAssembly(PartLocation partLocation)
        {
            if (partLocation == null)
            {
                return false;
            }

            foreach (var part in RegularPartLocationsList)
            {
                if (part == partLocation)
                {
                    return true;
                }
            }

            if (OptionalPartLocationsList == null)
            {
                return false;
            }

            foreach (var optionalPart in OptionalPartLocationsList)
            {
                foreach (var part in optionalPart.PartLocationsList)
                {
                    if (part == partLocation)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsExpectedPart(ItemMetaSO partMetaData)
        {
            if (partMetaData == null)
            {
                return false;
            }

            if (RegularPartLocationsList != null && IsHaveRegularPart(partMetaData))
            {
                return true;
            }

            return IsHaveOptionalParts &&
                OptionalPartLocationsList != null &&
                IsHaveOptionalPart(partMetaData);
        }

        // single regular location

        public bool IsHaveRegularPart(ItemMetaSO partMetaData)
        {
            foreach (var part in RegularPartLocationsList)
            {
                if (part.IsSingleMetaDataReference)
                {
                    if (part.PartMetaData == partMetaData)
                    {
                        return true;
                    }
                }
                else
                {
                    if (part.PartMetaDatasList.Contains(partMetaData))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<PartLocation> GetRegularPartLocations(ItemMetaSO partMetaData)
        {
            List<PartLocation> list = new List<PartLocation>();

            bool IsHaveRegularPartOfKind = false;
            foreach (var part in RegularPartLocationsList)
            {
                bool isSameMetaData = false;
                if (part.IsSingleMetaDataReference)
                {
                    if (part.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (part.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    IsHaveRegularPartOfKind = true;
                    list.Add(part);
                }
            }

            if (!IsHaveRegularPartOfKind)
            {
                Debug.LogWarning($"There is no part of kind \"{partMetaData.Name}\"");
                return null;
            }

            return list;
        }

        public List<PartLocation> GetFreeRegularPartLocations(ItemMetaSO partMetaData)
        {
            List<PartLocation> list = new List<PartLocation>();

            bool IsHaveRegularPartOfKind = false;
            foreach (var part in RegularPartLocationsList)
            {
                bool isSameMetaData = false;
                if (part.IsSingleMetaDataReference)
                {
                    if (part.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (part.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    IsHaveRegularPartOfKind = true;

                    if (!part.IsOccupied)
                    {
                        list.Add(part);
                    }
                }
            }

            if (!IsHaveRegularPartOfKind)
            {
                Debug.LogWarning($"There is no part of kind \"{partMetaData.Name}\"");
                return null;
            }

            return list;
        }

        public List<PartLocation> GetOccupiedRegularPartLocations(ItemMetaSO partMetaData)
        {
            List<PartLocation> list = new List<PartLocation>();

            bool IsHaveRegularPartOfKind = false;
            foreach (var part in RegularPartLocationsList)
            {
                bool isSameMetaData = false;
                if (part.IsSingleMetaDataReference)
                {
                    if (part.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (part.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    IsHaveRegularPartOfKind = true;

                    if (part.IsOccupied)
                    {
                        list.Add(part);
                    }
                }
            }

            if (!IsHaveRegularPartOfKind)
            {
                Debug.LogWarning($"There is no part of kind \"{partMetaData.Name}\"");
                return null;
            }

            return list;
        }

        public bool IsAllRegularPartLocationsOccupied(ItemMetaSO partMetaData)
        {
            bool IsHaveRegularPartOfKind = false;
            foreach (var part in RegularPartLocationsList)
            {
                bool isSameMetaData = false;
                if (part.IsSingleMetaDataReference)
                {
                    if (part.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (part.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    IsHaveRegularPartOfKind = true;

                    if (!part.IsOccupied)
                    {
                        return false;
                    }
                }
            }

            if (!IsHaveRegularPartOfKind)
            {
                Debug.LogWarning($"There is no part of kind \"{partMetaData.Name}\"");
                return false;
            }

            return true;
        }

        // single optional location

        public bool IsHaveOptionalPart(ItemMetaSO partMetaData)
        {
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        return true;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public OptionalPart GetOptionalPart(ItemMetaSO partMetaData)
        {
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        return optionalPart;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        return optionalPart;
                    }
                }
            }

            Debug.LogWarning($"There is no optional part of kind \"{partMetaData.Name}\"");
            return null;
        }

        public List<PartLocation> GetOptionalPartLocations(ItemMetaSO partMetaData)
        {
            OptionalPart optionalPartByMetaData = null;
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                bool isSameMetaData = false;
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    optionalPartByMetaData = optionalPart;
                    break;
                }
            }

            if (optionalPartByMetaData == null)
            {
                Debug.LogWarning($"There is no optional part of kind \"{partMetaData.Name}\"");
                return null;
            }

            return optionalPartByMetaData.PartLocationsList;
        }

        public List<PartLocation> GetFreeOptionalPartLocations(ItemMetaSO partMetaData)
        {
            List<PartLocation> list = new List<PartLocation>();

            OptionalPart optionalPartByMetaData = null;
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                bool isSameMetaData = false;
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    optionalPartByMetaData = optionalPart;
                    break;
                }
            }

            if (optionalPartByMetaData == null)
            {
                Debug.LogWarning($"There is no optional part of kind \"{partMetaData.Name}\"");
                return null;
            }

            foreach (var part in optionalPartByMetaData.PartLocationsList)
            {
                if (!part.IsOccupied)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public List<PartLocation> GetOccupiedOptionalLocations(ItemMetaSO partMetaData)
        {
            List<PartLocation> list = new List<PartLocation>();

            OptionalPart optionalPartByMetaData = null;
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                bool isSameMetaData = false;
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    optionalPartByMetaData = optionalPart;
                    break;
                }
            }

            if (optionalPartByMetaData == null)
            {
                Debug.LogWarning($"There is no optional part of kind \"{partMetaData.Name}\"");
                return null;
            }

            foreach (var part in optionalPartByMetaData.PartLocationsList)
            {
                if (part.IsOccupied)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public bool IsAllOptionalPartLocationsOccupied(ItemMetaSO partMetaData)
        {
            OptionalPart optionalPartByMetaData = null;
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                bool isSameMetaData = false;
                if (optionalPart.IsSingleMetaDataReference)
                {
                    if (optionalPart.PartMetaData == partMetaData)
                    {
                        isSameMetaData = true;
                    }
                }
                else
                {
                    if (optionalPart.PartMetaDatasList.Contains(partMetaData))
                    {
                        isSameMetaData = true;
                    }
                }

                if (isSameMetaData)
                {
                    optionalPartByMetaData = optionalPart;
                    break;
                }
            }

            if (optionalPartByMetaData == null)
            {
                Debug.LogWarning($"There is no optional part of kind \"{partMetaData.Name}\"");
                return false;
            }

            int installedPartsNumber = 0;

            foreach (var part in optionalPartByMetaData.PartLocationsList)
            {
                if (part.IsOccupied)
                {
                    installedPartsNumber++;
                }
            }

            if (installedPartsNumber < optionalPartByMetaData.EssentialPartsNumber)
            {
                return false;
            }

            return true;
        }

        // massive optional locations

        public List<PartLocation> GetFreeOptionalPartLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (var optionalPart in OptionalPartLocationsList)
            {
                foreach (var part in optionalPart.PartLocationsList)
                {
                    if (!part.IsOccupied)
                    {
                        list.Add(part);
                    }
                }
            }

            return list;
        }

        public List<PartLocation> GetOccupiedOptionalPartLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (var optionalPart in OptionalPartLocationsList)
            {
                foreach (var part in optionalPart.PartLocationsList)
                {
                    if (part.IsOccupied)
                    {
                        list.Add(part);
                    }
                }
            }

            return list;
        }

        public bool IsAllOptionalPartLocationsOccupied()
        {
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                int installedPartsNumber = 0;

                foreach (var part in optionalPart.PartLocationsList)
                {
                    if (part.IsOccupied)
                    {
                        installedPartsNumber++;
                    }
                }

                if (installedPartsNumber < optionalPart.EssentialPartsNumber)
                {
                    return false;
                }
            }

            return true;
        }

        // massive regular locations

        public List<PartLocation> GetFreeRegularPartLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (var part in RegularPartLocationsList)
            {
                if (!part.IsOccupied)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public List<PartLocation> GetOccupiedRegularPartLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (var part in RegularPartLocationsList)
            {
                if (part.IsOccupied)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public bool IsAllRegularPartLocationsOccupied()
        {
            foreach (var part in RegularPartLocationsList)
            {
                if (!part.IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

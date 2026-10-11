using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;

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

        private void OnValidate()
        {
            foreach (var optionalPart in OptionalPartLocationsList)
            {
                optionalPart.OnValidate();
            }
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

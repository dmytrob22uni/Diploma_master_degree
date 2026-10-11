using System;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;

namespace Coursework_Master_Degree.Items.Part
{
    [Serializable]
    public class OptionalPart
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public bool IsSingleMetaDataReference = true;
        [ShowIf("IsSingleMetaDataReference")]
        public ItemMetaSO PartMetaData;
        [ShowIf("IsMultipleMetaDataReferences")]
        public List<ItemMetaSO> PartMetaDatasList;

        [Tooltip("Number of parts to have installed at least")]
        public int EssentialPartsNumber;

        [Tooltip("Part locations where to look for")]
        public List<PartLocation> PartLocationsList;

        private bool IsMultipleMetaDataReferences()
        {
            return !IsSingleMetaDataReference;
        }

        public void OnValidate()
        {
            foreach (var part in PartLocationsList)
            {
                if (IsSingleMetaDataReference)
                {
                    if (part.PartMetaData != PartMetaData)
                    {
                        Debug.LogWarning(
                            $"Optional part location store reference of unsupported " +
                            "types. Check the reference and make sure to set the part location " +
                            "of supported type");
                        break;
                    }
                }
                else
                {
                    if (!PartMetaDatasList.Contains(part.PartMetaData))
                    {
                        Debug.LogWarning(
                            $"Optional part locations list contains references of unsupported " +
                            "types. Check the list and make sure only part locations of supported " +
                            "type are present");
                        break;
                    }
                }
            }
        }
    }
}

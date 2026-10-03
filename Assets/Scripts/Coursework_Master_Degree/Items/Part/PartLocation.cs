using System;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;
using NUnit.Framework;

namespace Coursework_Master_Degree.Items.Part
{
    [Serializable]
    public class PartLocation : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        // Dependent itself to
        [Tooltip("Does any parts define this part as dependency")]
        public bool IsDependency = false;
        [SerializeField]
        [ShowIf("IsDependency")]
        public List<PartLocation> DependentLocationsList;

        // Dependency parts of it
        [Tooltip("Does this part have any dependency parts")]
        public bool IsHaveDependencies = false;
        [SerializeField]
        [ShowIf("IsHaveDependencies")]
        public List<PartLocation> DependencyLocationsList;

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
        [SerializeField]
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script. Exposed for debugging")]
        public bool IsOccupied;

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

        public bool IsHaveFreeDependentLocations()
        {
            foreach (PartLocation dependent in DependentLocationsList)
            {
                if (!dependent.IsOccupied)
                {
                    return true;
                }
            }

            return false;
        }

        public List<PartLocation> GetFreeDependentLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (PartLocation dependent in DependentLocationsList)
            {
                if (!dependent.IsOccupied)
                {
                    list.Add(dependent);
                }
            }

            return list;
        }

        public bool CanInstall(GameObject part)
        {
            if (!IsExpectedItemMetaData(part))
            {
                Debug.Log("Meta Data object of the passed part is not among expected");
                return false;
            }

            if (IsHaveFreeDependentLocations())
            {
                Debug.Log("Trying to install part with no dependent part(-s) installed. Can't do that");
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

        public bool IsHaveOccupiedDependencyLocations()
        {
            foreach (PartLocation dependency in DependencyLocationsList)
            {
                if (dependency.IsOccupied)
                {
                    return true;
                }
            }

            return false;
        }

        public List<PartLocation> GetOccupiedDependencyLocations()
        {
            List<PartLocation> list = new List<PartLocation>();

            foreach (PartLocation dependency in DependencyLocationsList)
            {
                if (dependency.IsOccupied)
                {
                    list.Add(dependency);
                }
            }

            return list;
        }

        public bool CanRemove()
        {
            if (IsHaveOccupiedDependencyLocations())
            {
                Debug.Log("This part has dependency locations occupied. Can't remove it");
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

            part.transform.SetParent(gameObject.transform);
            part.transform.SetAsFirstSibling();
            part.transform.position = Vector3.zero;
            part.transform.rotation = Quaternion.identity;

            InstalledPart = part;

            IsOccupied = true;
        }

        public GameObject Remove()
        {
            if (!CanRemove())
            {
                return null;
            }

            GameObject part = InstalledPart;

            InstalledPart.transform.SetParent(null);
            InstalledPart = null;

            IsOccupied = false;

            return part;
        }
    }
}

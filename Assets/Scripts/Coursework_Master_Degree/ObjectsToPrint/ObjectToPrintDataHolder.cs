using System;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Surface;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint
{
    public class ObjectToPrintDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ObjectToPrintMetaSO ObjectToPrintMetaData;

        public ObjectToPrintGhostMetaSO ObjectToPrintGhostMetaSO;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public float PrintBedPosition;
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public float PrintBedRotation;
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public float PrintBedScale;
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public Material PrintingMaterial;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public List<ObjectToPrintGhostTypeToInstance> ObjectToPrintGhostTypesToInstancesList;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public bool IsPlaced;
        [EnableIf("NotEnabled")]
        [ShowIf("IsPlaced")]
        [Tooltip("Set via script")]
        public ItemSurfaceDataHolder ParentSurfaceDataHolder;

        public bool TryAddGhostType(ObjectToPrintGhostType type, ObjectToPrintGhostDataHolder objectToPrintGhost)
        {
            if (IsContainGhostType(type))
            {
                return false;
            }
            else
            {
                AddGhostType(type, objectToPrintGhost);
                return true;
            }
        }

        public void AddGhostType(ObjectToPrintGhostType type, ObjectToPrintGhostDataHolder objectToPrintGhost)
        {
            ObjectToPrintGhostTypesToInstancesList.Add(new ObjectToPrintGhostTypeToInstance(type, objectToPrintGhost));
        }

        private bool IsContainGhostType(ObjectToPrintGhostType type)
        {
            foreach (var objectToPrintGhostTypeToInstance in ObjectToPrintGhostTypesToInstancesList)
            {
                if (objectToPrintGhostTypeToInstance.ObjectToPrintGhostType == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetGhostInstance(ObjectToPrintGhostType type, out ObjectToPrintGhostDataHolder objectToPrintGhost)
        {
            foreach (var objectToPrintGhostTypeToInstance in ObjectToPrintGhostTypesToInstancesList)
            {
                if (objectToPrintGhostTypeToInstance.ObjectToPrintGhostType == type)
                {
                    objectToPrintGhost = objectToPrintGhostTypeToInstance.ObjectToPrintInstance;
                    return true;
                }
            }

            objectToPrintGhost = default;
            return false;
        }

        public ObjectToPrintGhostDataHolder GetGhostInstance(ObjectToPrintGhostType type)
        {
            foreach (var objectToPrintGhostTypeToInstance in ObjectToPrintGhostTypesToInstancesList)
            {
                if (objectToPrintGhostTypeToInstance.ObjectToPrintGhostType == type)
                {
                    return objectToPrintGhostTypeToInstance.ObjectToPrintInstance;
                }
            }

            throw new NullReferenceException($"Item {ObjectToPrintMetaData.Name} doesn't have ghost of type \'{type}\' yet");
        }

        public bool TryGetGhostType(ObjectToPrintGhostDataHolder objectToPrintGhost, out ObjectToPrintGhostType type)
        {
            foreach (var objectToPrintGhostTypeToInstance in ObjectToPrintGhostTypesToInstancesList)
            {
                if (objectToPrintGhostTypeToInstance.ObjectToPrintInstance == objectToPrintGhost)
                {
                    type = objectToPrintGhostTypeToInstance.ObjectToPrintGhostType;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public ObjectToPrintGhostType GetGhostType(ObjectToPrintGhostDataHolder objectToPrintGhost)
        {
            foreach (var objectToPrintGhostTypeToInstance in ObjectToPrintGhostTypesToInstancesList)
            {
                if (objectToPrintGhostTypeToInstance.ObjectToPrintInstance == objectToPrintGhost)
                {
                    return objectToPrintGhostTypeToInstance.ObjectToPrintGhostType;
                }
            }

            throw new NullReferenceException($"Item {ObjectToPrintMetaData.Name} doesn't possess this ghost");
        }
    }
}

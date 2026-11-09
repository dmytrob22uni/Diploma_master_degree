using System;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    [Serializable]
    public class ObjectToPrintGhostTypeToInstance
    {
        public ObjectToPrintGhostType ObjectToPrintGhostType;
        public ObjectToPrintGhostDataHolder ObjectToPrintInstance;

        public ObjectToPrintGhostTypeToInstance(ObjectToPrintGhostType objectToPrintGhostType, ObjectToPrintGhostDataHolder objectToPrintInstance)
        {
            ObjectToPrintGhostType = objectToPrintGhostType;
            ObjectToPrintInstance = objectToPrintInstance;
        }
    }
}

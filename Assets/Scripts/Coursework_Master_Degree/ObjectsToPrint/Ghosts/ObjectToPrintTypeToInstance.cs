using System;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    [Serializable]
    public class ObjectToPrintTypeToInstance
    {
        public ObjectToPrintGhostType ObjectToPrintType;
        public GameObject ObjectToPrintGameObject;
    }
}

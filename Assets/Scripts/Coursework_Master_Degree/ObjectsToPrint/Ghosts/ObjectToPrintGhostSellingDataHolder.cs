using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    public class ObjectToPrintGhostSellingDataHolder : ObjectToPrintGhostDataHolder
    {
        private void OnValidate()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.Selling;
        }

        private void Awake()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.Selling;
        }
    }
}

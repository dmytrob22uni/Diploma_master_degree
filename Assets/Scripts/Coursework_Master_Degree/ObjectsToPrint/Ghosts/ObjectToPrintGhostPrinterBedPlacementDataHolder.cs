using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    public class ObjectToPrintGhostPrinterBedPlacementDataHolder : ObjectToPrintGhostDataHolder
    {
        public List<GameObject> CornersList;

        private void OnValidate()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.PrinterBedPlacement;
        }

        private void Awake()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.PrinterBedPlacement;
        }
    }
}

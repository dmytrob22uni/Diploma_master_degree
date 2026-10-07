using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    public class ObjectToPrintGhostPrintingDataHolder : ObjectToPrintGhostDataHolder
    {
        private void OnValidate()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.Quality;
        }

        private void Awake()
        {
            base.ObjectToPrintGhostType = ObjectToPrintGhostType.Quality;
        }
    }
}

using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost;

namespace Coursework_Master_Degree.ObjectsToPrint
{
    public class ObjectToPrintDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ObjectToPrintMetaSO ObjectToPrintData;

        public ObjectToPrintGhostMetaSO ObjectToPrintGhostMetaSO;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public List<ObjectToPrintTypeToInstance> ObjectToPrintTypesToInstancesList;
    }
}

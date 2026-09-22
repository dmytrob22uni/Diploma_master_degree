using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.ObjectsToPrint.Ghost
{
    public class ObjectToPrintGhostDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ObjectToPrintGhostMetaSO ObjectToPrintGhostMetaData;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public ObjectToPrintGhostType ObjectToPrintGhostType;
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public GameObject ObjectToPrintInstanceInstance;

        public List<MeshRenderer> MeshRenderersList;
    }
}

using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.ObjectsToPrint
{
    public class ObjectToPrintDataHolder : MonoBehaviour
    {
        public ObjectToPrintMetaSO ObjectToPrintData;
        public List<MeshRenderer> MeshRenderersList;
        public List<GameObject> CornersList;
    }
}

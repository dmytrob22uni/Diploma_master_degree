using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Types;

namespace Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint
{
    [CreateAssetMenu(fileName = "object_to_print_meta_SO", menuName = "Game Items/Object To Print Meta SO", order = 2)]
    public class ObjectToPrintMetaSO : ScriptableObject
    {
        public string Id;

        public string Name;
        [TextArea]
        public string Description;

        public GameObject Prefab;
        public Texture2D Icon;

        public ObjectToPrintType[] ObjectToPrintTypes;
    }
}

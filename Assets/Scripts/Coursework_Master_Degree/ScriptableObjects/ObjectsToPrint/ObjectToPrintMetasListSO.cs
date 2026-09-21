using UnityEngine;

namespace Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint
{
    [CreateAssetMenu(fileName = "object_to_print_metas_list_SO", menuName = "Game Items/Object To Print Metas List SO", order = 3)]
    public class ObjectToPrintMetasListSO : ScriptableObject
    {
        public string ObjectsToPrintListName;
        public ObjectToPrintMetaSO[] ObjectToPrintMetasList;
    }
}

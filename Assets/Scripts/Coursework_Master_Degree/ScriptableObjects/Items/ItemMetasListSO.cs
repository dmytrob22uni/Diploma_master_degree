using UnityEngine;

namespace Coursework_Master_Degree.ScriptableObjects.Items
{
    [CreateAssetMenu(fileName = "item_metas_list_SO", menuName = "Game Items/Item Metas List SO", order = 1)]
    public class ItemMetasListSO : ScriptableObject
    {
        public string ItemsListName;
        public ItemMetaSO[] ItemMetasList;
    }
}

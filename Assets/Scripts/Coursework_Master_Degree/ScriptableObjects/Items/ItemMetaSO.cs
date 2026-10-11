using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.ScriptableObjects.Items
{
    [CreateAssetMenu(fileName = "item_meta_SO", menuName = "Game Items/Item Meta SO", order = 0)]
    public class ItemMetaSO : ScriptableObject
    {
        public string Id;

        public string Name;
        [TextArea]
        public string Description;
        [TextArea]
        public string PurposeDescription = "Purpose description TO REPLACE";
        [TextArea]
        public string MissingDescription = "Missing description TO REPLACE";

        public float Price;

        public GameObject Prefab;

        public Texture2D Icon;

        public ItemType[] ItemTypes;
    }
}

using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostWearLevelDataHolder : ItemGhostDataHolder
    {
        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.WearLevel;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.WearLevel;
        }
    }
}

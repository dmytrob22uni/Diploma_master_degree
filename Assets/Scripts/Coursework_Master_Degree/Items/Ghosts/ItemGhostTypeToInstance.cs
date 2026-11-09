using System;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    [Serializable]
    public class ItemGhostTypeToInstance
    {
        public ItemGhostType ItemGhostType;
        public ItemGhostDataHolder ItemGhostInstance;

        public ItemGhostTypeToInstance(ItemGhostType itemGhostType, ItemGhostDataHolder itemGhostInstance)
        {
            ItemGhostType = itemGhostType;
            ItemGhostInstance = itemGhostInstance;
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostSellingDataHolder : ItemGhostDataHolder
    {
        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.Selling;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.Selling;
        }
    }
}

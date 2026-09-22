using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostPlacementDataHolder : ItemGhostDataHolder
    {
        public List<GameObject> CornersList;

        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.Placement;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.Placement;
        }
    }
}

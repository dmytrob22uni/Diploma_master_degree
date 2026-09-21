using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Ghosts
{
    [CreateAssetMenu(fileName = "item_ghost_meta_SO", menuName = "Game Items/Ghosts/Item Ghost Meta SO", order = 0)]
    public class ItemGhostMetaSO : ScriptableObject
    {
        public List<ItemGhostTypeToPrefab> ItemGhostTypesToPrefabsList;
    }
}

using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Influences;

namespace Coursework_Master_Degree.Items
{
    public class ItemDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemMetaSO ItemMetaData;
        public ItemToPlayerInfluencesSO ItemToPlayerInfluencesSO;

        public ItemGhostMetaSO ItemGhostMetaSO;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public List<ItemGhostTypeToInstance> ItemGhostTypesToInstancesList;

        [Tooltip("Hint to search for ItemSurfaceDataHolder")]
        public bool IsSurface = false;

        [Tooltip("Used to expose list for storing free parts reset locations")]
        public bool IsHaveFreeParts = false;

        [ShowIf("IsHaveFreeParts")]
        [EnableIf("IsHaveFreeParts")]
        public List<ItemPartToLocation> ItemPartsToLocationsList;

        [Tooltip("Tip to look for IWearLevel")]
        public bool IsHaveWearLevel = false;
    }
}

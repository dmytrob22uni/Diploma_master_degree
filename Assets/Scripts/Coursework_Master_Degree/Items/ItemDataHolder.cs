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

        public bool IsSurface = false;
        [ShowIf("IsSurface")]
        public List<GameObject> ShelvesList;
    }
}

using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemGhostMetaSO ItemGhostMetaData;

        [EnableIf("NotEnabled")]
        public ItemGhostType ItemGhostType;
        [EnableIf("NotEnabled")]
        public GameObject ItemInstance;

        public List<MeshRenderer> MeshRenderersList;
    }
}

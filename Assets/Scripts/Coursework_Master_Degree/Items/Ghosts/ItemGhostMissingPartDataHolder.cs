using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostMissingPartDataHolder : ItemGhostDataHolder
    {
        [EnableIf("NotEnabled")]
        [Tooltip("Set via script. Used to restore expected part description on hover")]
        public ItemMetaSO PartMetaData;

        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;

        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.MissingPart;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.MissingPart;
        }
    }
}

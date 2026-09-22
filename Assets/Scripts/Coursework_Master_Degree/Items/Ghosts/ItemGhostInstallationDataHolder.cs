using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemGhostInstallationDataHolder : ItemGhostDataHolder
    {
        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.Installation;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.Installation;
        }
    }
}

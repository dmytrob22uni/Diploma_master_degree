using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using NaughtyAttributes;

namespace Coursework_Master_Degree.Items
{
    public class ItemFreePartDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public GameObject ItemFreePartGhostPlacementPrefab;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public GameObject ItemFreePartGhostPlacementInstance;
    }
}

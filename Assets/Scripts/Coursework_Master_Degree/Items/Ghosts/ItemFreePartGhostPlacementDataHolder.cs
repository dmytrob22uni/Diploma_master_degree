using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

namespace Coursework_Master_Degree.Items.Ghosts
{
    public class ItemFreePartGhostPlacementDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        [EnableIf("NotEnabled")]
        [Tooltip("Set via script")]
        public GameObject ItemFreePartInstance;

        public List<MeshRenderer> MeshRenderersList;

        public List<GameObject> CornersList;
    }
}

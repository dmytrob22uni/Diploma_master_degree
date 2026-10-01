using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.Computing
{
    public class ItemComputingDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemType ItemType;

        public GameObject ScreenGameObject;

        [ShowIf("ItemType", ItemType.Laptop)]
        [EnableIf("NotEnabled")]
        [Tooltip("Toggled via internal function. Exposed for debugging")]
        public bool IsLidOpened = true;

        [EnableIf("NotEnabled")]
        [Tooltip("Toggled via internal function. Exposed for debugging")]
        public bool IsTurnedOn = false;

        public void ToggleLidOpenedState()
        {
            IsLidOpened =! IsLidOpened;
        }

        public void ToggleTurnedOnState()
        {
            IsTurnedOn =! IsTurnedOn;
        }
    }
}

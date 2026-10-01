using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Unity.VisualScripting;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.Power
{
    public class ItemPowerDataHolder : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public ItemType ItemType;

        [ShowIf("IsPowerReceivingItem")]
        [EnableIf("NotEnabled")]
        [Tooltip("Determines power delivery state")]
        public bool IsPowered = false;

        [ShowIf("IsPowerDeliveringItem")]
        public List<Transform> PlugLocationsList = new List<Transform>();

        [ShowIf("IsEmittingItem")]
        public MeshRenderer EmitterMeshRenderer;

        [ShowIf("IsPowerControllingItem")]
        [EnableIf("NotEnabled")]
        [Tooltip("Determines power control state")]
        public bool IsOn = false;

        [ShowIf("IsPowerControllingItem")]
        public ItemPartToRotations TumblerToRotations;

        public bool IsPowerReceivingItem()
        {
            return
                ItemType == ItemType.ExtensionCord ||
                ItemType == ItemType.FluorescentLamp ||
                ItemType == ItemType.LEDLamp ||
                ItemType == ItemType.WallSocket;
        }

        public bool IsPowerDeliveringItem()
        {
            return
                ItemType == ItemType.ExtensionCord ||
                ItemType == ItemType.WallSocket;
        }

        public bool IsEmittingItem()
        {
            return
                ItemType == ItemType.FluorescentLamp ||
                ItemType == ItemType.LEDLamp;
        }

        public bool IsPowerControllingItem()
        {
            return
                ItemType == ItemType.WallSwitch;
        }

        private void OnValidate()
        {
            if (IsAutoPowerDeliveringItem())
            {
                IsPowered = true;
            }
            else
            {
                IsPowered = false;
            }
        }

        private void Awake()
        {
            if (IsAutoPowerDeliveringItem())
            {
                IsPowered = true;
            }
            else
            {
                IsPowered = false;
            }
        }

        public bool IsAutoPowerDeliveringItem()
        {
            return
                ItemType == ItemType.WallSocket;
        }

        public void TogglePoweredState()
        {
            IsPowered =! IsPowered;
        }

        public void ToggleOnState()
        {
            IsOn =! IsOn;
        }
    }
}

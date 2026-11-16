using System;
using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.Contracts;

namespace Coursework_Master_Degree.Items.Computing
{
    /// <summary>
    /// Computing device root. Interacted through any child collider because
    /// ItemHoverInteract resolves the Device on the prefab root.
    /// M0 wires the interaction entry point only; M1 opens the console.
    /// </summary>
    [DisallowMultipleComponent]
    public class Device : MonoBehaviour, IInteractable
    {
        [Header("References")]
        public ItemComputingDataHolder Computing;
        public DeviceScreenDisplay Screen;
        public DeviceScreenAnchor ScreenAnchor;

        /// <summary>Raised when the player chooses "Use device". M1 subscribes.</summary>
        public event Action<Device> UseRequested;

        private readonly List<InteractionOption> interactions =
            new List<InteractionOption>();

        private InteractionOption useDeviceOption;

        public bool IsDisplayOn => Screen != null && Screen.IsOn;

        private void Awake()
        {
            AutoAssign();
            EnsureInteractions();
        }

        public IReadOnlyList<InteractionOption> GetInteractions()
        {
            EnsureInteractions();
            return interactions;
        }

        public void SetScreenOn(bool on)
        {
            if (Screen != null)
            {
                Screen.SetOn(on);
                return;
            }

            if (Computing != null)
            {
                Computing.IsTurnedOn = on;
            }
        }

        [ContextMenu("Turn Screen Off")]
        public void TurnScreenOff()
        {
            SetScreenOn(false);
        }

        [ContextMenu("Turn Screen On")]
        public void TurnScreenOn()
        {
            SetScreenOn(true);
        }

        private void EnsureInteractions()
        {
            if (useDeviceOption != null)
            {
                return;
            }

            useDeviceOption = new InteractionOption
            {
                Info = "Use device",
                OnExecute = RequestUse
            };

            interactions.Add(useDeviceOption);
        }

        private void RequestUse()
        {
            if (UseRequested != null)
            {
                UseRequested.Invoke(this);
                return;
            }

            Debug.Log(
                "[Device] Console opens in M1. M0 only verifies the F prompt.",
                this);
        }

        private void AutoAssign()
        {
            if (Computing == null)
            {
                Computing = GetComponent<ItemComputingDataHolder>();
            }

            if (Screen == null)
            {
                Screen = GetComponent<DeviceScreenDisplay>();
            }

            if (ScreenAnchor == null)
            {
                ScreenAnchor = GetComponentInChildren<DeviceScreenAnchor>(true);
            }
        }
    }
}

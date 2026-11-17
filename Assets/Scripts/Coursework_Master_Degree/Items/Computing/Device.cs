using System;
using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Items.Computing
{
    /// <summary>
    /// Computing device root. Interacted through any child collider because
    /// ItemHoverInteract resolves the Device on the prefab root.
    /// The device acts as a console for any number of printers, while each
    /// printer keeps ownership of its own job.
    /// </summary>
    [DisallowMultipleComponent]
    public class Device : MonoBehaviour, IInteractable
    {
        [Header("References")]
        public ItemDataHolder ItemData;
        public ItemComputingDataHolder Computing;
        public DeviceScreenDisplay Screen;
        public DeviceScreenAnchor ScreenAnchor;

        /// <summary>Raised when the player chooses "Use device".</summary>
        public event Action<Device> UseRequested;

        /// <summary>Raised when the bound printer list changes.</summary>
        public event Action<Device> BindingsChanged;

        /// <summary>Raised when the active printer changes.</summary>
        public event Action<Device> ActivePrinterChanged;

        /// <summary>Raised after the physical display is turned on or off.</summary>
        public event Action<Device> ScreenStateChanged;

        private readonly List<InteractionOption> interactions =
            new List<InteractionOption>();

        private readonly List<Printer> boundPrinters = new List<Printer>();

        private static readonly List<Device> activeDevices =
            new List<Device>();

        /// <summary>
        /// Devices that are currently enabled. The console presenter watches
        /// this list so every display keeps showing its interface, including
        /// devices spawned after the scene has started.
        /// </summary>
        public static IReadOnlyList<Device> ActiveDevices => activeDevices;

        private InteractionOption useDeviceOption;

        public bool IsDisplayOn => Screen != null ? Screen.IsOn : isScreenOnFallback;

        private bool isScreenOnFallback = false;

        public string DisplayName
        {
            get
            {
                if (ItemData != null &&
                    ItemData.ItemMetaData != null &&
                    !string.IsNullOrWhiteSpace(ItemData.ItemMetaData.Name))
                {
                    return ItemData.ItemMetaData.Name;
                }

                if (Computing != null)
                {
                    return Computing.ItemType.ToString();
                }

                return gameObject.name;
            }
        }

        public IReadOnlyList<Printer> BoundPrinters
        {
            get
            {
                PruneMissingPrinters();
                return boundPrinters;
            }
        }

        public Printer ActivePrinter { get; private set; }

        private void Awake()
        {
            AutoAssign();
            EnsureInteractions();
        }

        private void OnEnable()
        {
            activeDevices.RemoveAll(device => device == null);

            if (!activeDevices.Contains(this))
            {
                activeDevices.Add(this);
            }
        }

        private void OnDisable()
        {
            activeDevices.Remove(this);
        }

        private void OnDestroy()
        {
            activeDevices.Remove(this);

            for (int i = boundPrinters.Count - 1; i >= 0; i--)
            {
                Printer printer = boundPrinters[i];
                if (printer != null)
                {
                    printer.UnbindFromDevice(this);
                }
            }

            boundPrinters.Clear();
            ActivePrinter = null;
        }

        public IReadOnlyList<InteractionOption> GetInteractions()
        {
            EnsureInteractions();
            return interactions;
        }

        public bool TryBindPrinter(Printer printer, out string error)
        {
            if (printer == null)
            {
                error = "No printer selected.";
                return false;
            }

            PruneMissingPrinters();

            if (boundPrinters.Contains(printer))
            {
                error = "This printer is already connected to this device.";
                return false;
            }

            if (printer.BoundDevice != null)
            {
                error = printer.BoundDevice == this
                    ? "This printer is already connected to this device."
                    : "This printer is already controlled by another device.";
                return false;
            }

            if (!printer.BindToDevice(this))
            {
                error = "This printer cannot be connected right now.";
                return false;
            }

            boundPrinters.Add(printer);

            if (ActivePrinter == null)
            {
                ActivePrinter = printer;
                ActivePrinterChanged?.Invoke(this);
            }

            BindingsChanged?.Invoke(this);
            error = null;
            return true;
        }

        public bool UnbindPrinter(Printer printer)
        {
            if (printer == null)
            {
                return false;
            }

            if (!boundPrinters.Remove(printer))
            {
                return false;
            }

            printer.UnbindFromDevice(this);

            if (ActivePrinter == printer)
            {
                ActivePrinter =
                    boundPrinters.Count > 0 ? boundPrinters[0] : null;

                ActivePrinterChanged?.Invoke(this);
            }

            BindingsChanged?.Invoke(this);
            return true;
        }

        public bool SetActivePrinter(Printer printer)
        {
            if (printer == null)
            {
                if (ActivePrinter == null)
                {
                    return false;
                }

                ActivePrinter = null;
                ActivePrinterChanged?.Invoke(this);
                return true;
            }

            if (!boundPrinters.Contains(printer) || ActivePrinter == printer)
            {
                return false;
            }

            ActivePrinter = printer;
            ActivePrinterChanged?.Invoke(this);
            return true;
        }

        public void SetScreenOn(bool on)
        {
            if (Screen != null)
            {
                Screen.SetOn(on);
            }
            else
            {
                isScreenOnFallback = on;

                if (Computing != null)
                {
                    Computing.IsTurnedOn = on;
                }
            }

            ScreenStateChanged?.Invoke(this);
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

        private void PruneMissingPrinters()
        {
            for (int i = boundPrinters.Count - 1; i >= 0; i--)
            {
                if (boundPrinters[i] != null)
                {
                    continue;
                }

                boundPrinters.RemoveAt(i);
            }

            if (ActivePrinter == null && boundPrinters.Count > 0)
            {
                ActivePrinter = boundPrinters[0];
                ActivePrinterChanged?.Invoke(this);
            }
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
            UseRequested?.Invoke(this);

            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning(
                    "[Device] GameStateManager is missing; cannot open the console.",
                    this);
                return;
            }

            GameStateManager.Instance.ChangeState(
                new GameStateDeviceInterface(this));
        }

        private void AutoAssign()
        {
            if (ItemData == null)
            {
                ItemData = GetComponent<ItemDataHolder>();
            }

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

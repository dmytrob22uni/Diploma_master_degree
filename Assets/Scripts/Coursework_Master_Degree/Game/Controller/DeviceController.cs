using System;
using UnityEngine;
using Coursework_Master_Degree.Game.Logic.Interface.DeviceBind;
using Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.Controller
{
    /// <summary>
    /// Coordinates the device console flow: focusing the screen, entering and
    /// leaving bind targeting, and translating LMB selections into device
    /// bindings. It is a controller, not a game state; the state machine only
    /// gains the contextual console and bind states.
    /// </summary>
    public class DeviceController : MonoBehaviour
    {
        [Header("References")]
        public PlayerController PlayerController;
        public DeviceFocusController FocusController;
        public DeviceConsolePresenter Console;
        public PrinterBindTargeting BindTargeting;

        /// <summary>
        /// Raised when a bind or unbind selection finishes so the active
        /// GameStateDeviceBind can return to the console.
        /// </summary>
        public event Action BindModeCompleted;

        private Device consoleDevice;
        private Device bindDevice;
        private DeviceBindMode bindMode;
        private bool consoleOpen;
        private bool consoleEventsSubscribed;
        private bool bindModeActive;
        private bool quitSubscribed;

        private void Awake()
        {
            if (PlayerController == null && GameStateManager.Instance != null)
            {
                PlayerController = GameStateManager.Instance.PlayerController;
            }
        }

        private void OnDisable()
        {
            if (bindModeActive)
            {
                ExitBindMode();
            }

            if (consoleOpen)
            {
                CloseConsole();
            }
        }

        private void Update()
        {
            if (bindModeActive)
            {
                DeviceBindPanel panel = GetBindPanel();

                if (panel != null)
                {
                    panel.SetTarget(BindTargeting != null
                        ? BindTargeting.HoveredPrinter
                        : null);
                }
            }

            if (!consoleOpen || consoleDevice != null)
            {
                return;
            }

            // The device was destroyed or moved away while its console was
            // open. Drop the console instead of holding a dangling reference.
            CloseConsole();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
            }
        }

        // === Console =================================================================================================

        public void OpenConsole(Device device)
        {
            if (device == null)
            {
                Debug.LogWarning("[DeviceController] Cannot open a console without a device.");
                return;
            }

            if (consoleOpen)
            {
                CloseConsole();
            }

            consoleDevice = device;
            consoleOpen = true;

            if (PlayerController != null)
            {
                PlayerController.SetDeviceInterfaceState(true);
            }

            SubscribeConsoleEvents();

            GameStateManager manager = GameStateManager.Instance;

            if (manager != null && !quitSubscribed)
            {
                manager.InputReader.OnQuitEvent += HandleQuitRequested;
                quitSubscribed = true;
            }

            if (FocusController != null)
            {
                Device focusedDevice = device;
                FocusController.Focus(device, () => OnConsoleFocused(focusedDevice));
            }
            else
            {
                OnConsoleFocused(device);
            }
        }

        public void CloseConsole()
        {
            if (!consoleOpen)
            {
                return;
            }

            consoleOpen = false;
            consoleDevice = null;

            UnsubscribeConsoleEvents();

            GameStateManager manager = GameStateManager.Instance;

            if (manager != null && quitSubscribed)
            {
                manager.InputReader.OnQuitEvent -= HandleQuitRequested;
                quitSubscribed = false;
            }

            if (Console != null)
            {
                Console.Unfocus();
            }

            if (FocusController != null)
            {
                FocusController.Release();
            }

            if (PlayerController != null)
            {
                PlayerController.SetDeviceInterfaceState(false);
            }
        }

        public void RequestCloseConsole()
        {
            if (!consoleOpen)
            {
                return;
            }

            CloseConsole();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
            }
        }

        // === Bind mode ===============================================================================================

        public void RequestBindMode(DeviceBindMode mode)
        {
            if (consoleDevice == null || GameStateManager.Instance == null)
            {
                return;
            }

            GameState returnState = GameStateManager.Instance.CurrentGameState;

            GameStateManager.Instance.ChangeState(
                new GameStateDeviceBind(consoleDevice, mode, returnState));
        }

        public void EnterBindMode(Device device, DeviceBindMode mode)
        {
            if (device == null)
            {
                BindModeCompleted?.Invoke();
                return;
            }

            if (bindModeActive)
            {
                ExitBindMode();
            }

            bindDevice = device;
            bindMode = mode;
            bindModeActive = true;

            if (PlayerController != null)
            {
                PlayerController.SetDeviceBindState(true);
            }

            GameStateManager manager = GameStateManager.Instance;

            if (manager != null)
            {
                if (mode == DeviceBindMode.Bind)
                {
                    manager.InputReader.OnBindDeviceEvent += HandleBindSelection;
                }
                else
                {
                    manager.InputReader.OnUnbindDeviceEvent += HandleBindSelection;
                }
            }

            if (BindTargeting != null)
            {
                BindTargeting.Begin(device, mode);
            }

            SetInterfaceBind(true);

            DeviceBindPanel panel = GetBindPanel();

            if (panel != null)
            {
                panel.Show(mode);
            }

            if (Console != null)
            {
                Console.SetBindLayout(device, true);
            }
        }

        public void ExitBindMode()
        {
            if (!bindModeActive)
            {
                return;
            }

            bindModeActive = false;

            GameStateManager manager = GameStateManager.Instance;

            if (manager != null)
            {
                manager.InputReader.OnBindDeviceEvent -= HandleBindSelection;
                manager.InputReader.OnUnbindDeviceEvent -= HandleBindSelection;
            }

            if (BindTargeting != null)
            {
                BindTargeting.End();
            }

            DeviceBindPanel panel = GetBindPanel();

            if (panel != null)
            {
                panel.Hide();
            }

            if (Console != null)
            {
                Console.SetBindLayout(bindDevice, false);
            }

            bindDevice = null;
            SetInterfaceBind(false);

            if (PlayerController != null)
            {
                PlayerController.SetDeviceBindState(false);
            }
        }

        // === Handlers ================================================================================================

        private void OnConsoleFocused(Device device)
        {
            if (!consoleOpen || device == null || consoleDevice != device)
            {
                return;
            }

            if (Console != null)
            {
                Camera eventCamera = FocusController != null
                    ? FocusController.EventCamera
                    : null;

                Console.Focus(device, eventCamera);
            }
        }

        private void HandleQuitRequested()
        {
            RequestCloseConsole();
        }

        private void HandleAddPrinterRequested()
        {
            RequestBindMode(DeviceBindMode.Bind);
        }

        private void HandleStartPrintRequested(
            Device device,
            Printer printer,
            ObjectToPrintMetaSO meta,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed)
        {
            Device targetDevice = device != null ? device : consoleDevice;

            if (!consoleOpen || targetDevice == null || printer == null)
            {
                return;
            }

            PrintJobValidationResult result = PrintJobValidator.Validate(
                targetDevice,
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed);

            if (!result.IsValid)
            {
                return;
            }

            PrintJobRequest request = new PrintJobRequest(
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed,
                result.EstimatedSeconds,
                result.EstimatedFilamentMeters,
                result.EstimatedQualityMin,
                result.EstimatedQualityMax);

            if (printer.TrySetPendingJob(request))
            {
                Console?.Refresh(targetDevice);
            }
        }

        private void HandleDisconnectPrinterRequested()
        {
            if (consoleDevice == null || consoleDevice.ActivePrinter == null)
            {
                return;
            }

            RequestBindMode(DeviceBindMode.Unbind);
        }

        private void HandleBindSelection()
        {
            if (!bindModeActive || bindDevice == null)
            {
                return;
            }

            Printer target = BindTargeting != null
                ? BindTargeting.HoveredPrinter
                : null;

            // Aiming at empty space is silent; only wrong targets explain themselves.
            if (target == null)
            {
                return;
            }

            if (bindMode == DeviceBindMode.Bind)
            {
                HandleBindRequest(target);
            }
            else
            {
                HandleUnbindRequest(target);
            }
        }

        private void HandleBindRequest(Printer printer)
        {
            if (printer.BoundDevice != null)
            {
                ShowBindError(printer.BoundDevice == bindDevice
                    ? "Already connected to this device."
                    : "Controlled by another device.");
                return;
            }

            if (!bindDevice.TryBindPrinter(printer, out string error))
            {
                ShowBindError(error);
                return;
            }

            CompleteBindMode();
        }

        private void HandleUnbindRequest(Printer printer)
        {
            if (printer.BoundDevice != bindDevice)
            {
                ShowBindError("Not connected to this device.");
                return;
            }

            if (!bindDevice.UnbindPrinter(printer))
            {
                ShowBindError("This printer could not be disconnected.");
                return;
            }

            // Unbinding never touches the printer state or its job.
            CompleteBindMode();
        }

        private void CompleteBindMode()
        {
            ExitBindMode();
            BindModeCompleted?.Invoke();
        }

        private void ShowBindError(string message)
        {
            DeviceBindPanel panel = GetBindPanel();

            if (panel != null)
            {
                panel.ShowReason(message);
            }
        }

        private DeviceBindPanel GetBindPanel()
        {
            GameStateManager manager = GameStateManager.Instance;

            return manager != null && manager.InterfaceController != null
                ? manager.InterfaceController.DeviceBindPanel
                : null;
        }

        private void SetInterfaceBind(bool enable)
        {
            GameStateManager manager = GameStateManager.Instance;

            if (manager != null && manager.InterfaceController != null)
            {
                manager.InterfaceController.SetDeviceBindState(enable);
            }
        }

        private void SubscribeConsoleEvents()
        {
            if (consoleEventsSubscribed || Console == null)
            {
                return;
            }

            Console.AddPrinterRequested += HandleAddPrinterRequested;
            Console.DisconnectPrinterRequested += HandleDisconnectPrinterRequested;
            Console.QuitRequested += HandleQuitRequested;
            Console.StartPrintRequested += HandleStartPrintRequested;
            consoleEventsSubscribed = true;
        }

        private void UnsubscribeConsoleEvents()
        {
            if (!consoleEventsSubscribed || Console == null)
            {
                return;
            }

            Console.AddPrinterRequested -= HandleAddPrinterRequested;
            Console.DisconnectPrinterRequested -= HandleDisconnectPrinterRequested;
            Console.QuitRequested -= HandleQuitRequested;
            Console.StartPrintRequested -= HandleStartPrintRequested;
            consoleEventsSubscribed = false;
        }
    }
}

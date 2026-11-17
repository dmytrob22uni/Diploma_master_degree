using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Items.Computing;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateDeviceInterface : GameState
    {
        public Device Device { get; }

        public GameStateDeviceInterface(Device device)
        {
            Device = device;
        }

        public override void OnEnter()
        {
            ShowCursor();

            GameStateManager manager = GameStateManager.Instance;
            manager.InputReader.EnableDeviceInterfaceStateInput();

            if (manager.DeviceController == null)
            {
                UnityEngine.Debug.LogError(
                    "[GameStateDeviceInterface] DeviceController is not wired in the scene.");
                return;
            }

            manager.DeviceController.OpenConsole(Device);
        }

        public override void OnExit()
        {
            GameStateManager manager = GameStateManager.Instance;

            if (manager.DeviceController != null)
            {
                manager.DeviceController.CloseConsole();
            }

            manager.InputReader.DisableAllInput();
            HideCursor();
        }
    }
}

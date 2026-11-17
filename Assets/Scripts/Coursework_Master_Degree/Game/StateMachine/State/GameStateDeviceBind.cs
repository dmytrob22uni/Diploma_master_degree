using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Items.Computing;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateDeviceBind : GameState
    {
        public Device Device { get; }
        public DeviceBindMode Mode { get; }
        public GameState ReturnState { get; }

        private bool isTransitioning;

        public GameStateDeviceBind(
            Device device,
            DeviceBindMode mode,
            GameState returnState)
        {
            Device = device;
            Mode = mode;
            ReturnState = returnState;
        }

        public override void OnEnter()
        {
            // Bind targeting is aimed with the center of the screen, so the
            // cursor stays hidden exactly like free walk.
            HideCursor();

            GameStateManager manager = GameStateManager.Instance;
            manager.InputReader.EnableDeviceBindStateInput();
            manager.InputReader.OnQuitEvent += HandleCancel;

            if (manager.DeviceController == null)
            {
                UnityEngine.Debug.LogError(
                    "[GameStateDeviceBind] DeviceController is not wired in the scene.");
                ReturnToConsole();
                return;
            }

            manager.DeviceController.BindModeCompleted += HandleBindModeCompleted;
            manager.DeviceController.EnterBindMode(Device, Mode);
        }

        public override void OnExit()
        {
            isTransitioning = true;

            GameStateManager manager = GameStateManager.Instance;
            manager.InputReader.OnQuitEvent -= HandleCancel;

            if (manager.DeviceController != null)
            {
                manager.DeviceController.BindModeCompleted -= HandleBindModeCompleted;
                manager.DeviceController.ExitBindMode();
            }

            manager.InputReader.DisableAllInput();
            HideCursor();
        }

        private void HandleCancel()
        {
            ReturnToConsole();
        }

        private void HandleBindModeCompleted()
        {
            ReturnToConsole();
        }

        private void ReturnToConsole()
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;

            GameState target = ReturnState ?? new GameStateFreeWalk();
            GameStateManager.Instance.ChangeState(target);
        }
    }
}

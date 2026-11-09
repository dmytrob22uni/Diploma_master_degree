using UnityEngine;
using Coursework_Master_Degree.Game.Controller;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateFreeWalk : GameState
    {
        // Controllers
        private readonly PlayerController playerController;
        private readonly InterfaceController interfaceController;

        public GameStateFreeWalk()
        {
            playerController = GameStateManager.Instance.PlayerController;
            interfaceController = GameStateManager.Instance.InterfaceController;
        }

        public override void OnEnter()
        {
            HideCursor();

            GameStateManager.Instance.InputReader.EnableFreeWalkStateInput();

            playerController.SetFreeWalkState(true);
            interfaceController.SetFreeWalkState(true);
        }

        public override void OnExit()
        {
            interfaceController.SetFreeWalkState(false);
            playerController.SetFreeWalkState(false);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

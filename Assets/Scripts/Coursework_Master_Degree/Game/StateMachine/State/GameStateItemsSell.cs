using UnityEngine;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateItemsSell : GameState
    {
        // Controllers
        private readonly PlayerController playerController;
        private readonly InterfaceController interfaceController;

        public GameStateItemsSell()
        {
            playerController = GameStateManager.Instance.PlayerController;
            interfaceController = GameStateManager.Instance.InterfaceController;
        }

        public override void OnEnter()
        {
            HideCursor();

            GameStateManager.Instance.InputReader.EnableItemsSellStateInput();

            playerController.SetItemsSellState(true);
            interfaceController.SetItemsSellState(true);
        }

        public override void OnExit()
        {
            interfaceController.SetItemsSellState(false);
            playerController.SetItemsSellState(false);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

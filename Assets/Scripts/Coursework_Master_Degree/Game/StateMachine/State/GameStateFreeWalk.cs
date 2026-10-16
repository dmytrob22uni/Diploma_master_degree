using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateFreeWalk : GameState
    {
        public GameStateFreeWalk(GameStateManager gameStateManager) : base(gameStateManager) { }

        public override void Enter()
        {
            gameStateManager.InputReader.EnableFreeWalkStateInput();

            HideCursor();
        }

        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public override void Update()
        {

        }

        public override void LateUpdate()
        {

        }

        public override void FixedUpdate()
        {

        }

        public override void Exit()
        {
            gameStateManager.InputReader.DisableAllInput();

            ShowCursor();
        }

        private void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}

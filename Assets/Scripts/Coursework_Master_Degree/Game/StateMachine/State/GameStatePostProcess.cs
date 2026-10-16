using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePostProcess : GameState
    {
        public GameStatePostProcess(GameStateManager gameStateManager) : base(gameStateManager) { }

        public override void Enter()
        {
            gameStateManager.InputReader.EnablePostProcessStateInput();
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
        }
    }
}

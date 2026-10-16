using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartInstall : GameState
    {
        public GameStatePartInstall(GameStateManager gameStateManager) : base(gameStateManager) { }

        public override void Enter()
        {
            gameStateManager.InputReader.EnablePartInstallStateInput();
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

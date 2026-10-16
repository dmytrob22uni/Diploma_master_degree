using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateItemsSell : GameState
    {
        public GameStateItemsSell(GameStateManager gameStateManager) : base(gameStateManager) { }

        public override void Enter()
        {
            gameStateManager.InputReader.EnableItemsSellStateInput();
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

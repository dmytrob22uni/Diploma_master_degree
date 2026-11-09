using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateObjectPlace : GameState
    {
        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnableObjectPlaceStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

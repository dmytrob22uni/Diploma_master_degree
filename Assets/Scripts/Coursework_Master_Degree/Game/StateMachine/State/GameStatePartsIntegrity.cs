using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartsIntegrity : GameState
    {
        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnablePartsIntegrityStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

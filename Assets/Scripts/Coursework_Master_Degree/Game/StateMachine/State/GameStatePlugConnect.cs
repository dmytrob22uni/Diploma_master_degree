using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePlugConnect : GameState
    {
        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnablePlugConnectStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateSwitchConnect : GameState
    {
        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnableSwitchConnectStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

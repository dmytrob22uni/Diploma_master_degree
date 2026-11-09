using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartInstall : GameState
    {
        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnablePartInstallStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

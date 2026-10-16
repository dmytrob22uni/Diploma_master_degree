using System;
using UnityEngine;
using Coursework_Master_Degree.Input;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.StateMachine
{
    public class GameStateManager : MonoBehaviour
    {
        public InputReader InputReader;

        [NonSerialized]
        public GameState PreviousGameState;
        [NonSerialized]
        public GameState CurrentGameState;

        private void Start()
        {
            ChangeState(new GameStateFreeWalk(this));
        }

        private void Update()
        {
            CurrentGameState.Update();
        }

        private void LateUpdate()
        {
            CurrentGameState.LateUpdate();
        }

        private void FixedUpdate()
        {
            CurrentGameState.FixedUpdate();
        }

        public void ChangeState(GameState gameState)
        {
            CurrentGameState?.Exit();

            PreviousGameState = CurrentGameState;
            CurrentGameState = gameState;

            CurrentGameState.Enter();
        }
    }
}

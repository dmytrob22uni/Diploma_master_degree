using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public abstract class GameState
    {
        protected GameStateManager gameStateManager;

        public GameState(GameStateManager gameStateManager)
        {
            this.gameStateManager = gameStateManager;
        }

        public virtual void Enter() { }

        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void FixedUpdate() { }

        public virtual void Exit() { }
    }
}

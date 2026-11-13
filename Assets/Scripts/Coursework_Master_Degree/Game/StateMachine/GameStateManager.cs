using System;
using UnityEngine;
using Coursework_Master_Degree.Input;
using Coursework_Master_Degree.Game.Helper;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.StateMachine
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Input Reader")]
        public InputReader InputReader;

        [Header("Game State Controllers")]
        public PlayerController PlayerController;
        public ItemController ItemController;
        public InterfaceController InterfaceController;

        [Header("Common Logics")]
        public ItemInstancer ItemInstancer;

        [Header("Parts State Setups")]
        public PartsIntegrityStateSetup PartsIntegrityStateSetup;
        public PartsRemoveStateSetup PartsRemoveStateSetup;
        public PartsWearStateSetup PartsWearStateSetup;

        [NonSerialized]
        public GameState PreviousGameState;
        [NonSerialized]
        public GameState CurrentGameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ChangeState(new GameStateFreeWalk());
        }

        public void ChangeState(GameState gameState)
        {
            CurrentGameState?.OnExit();

            PreviousGameState = CurrentGameState;
            CurrentGameState = gameState;

            CurrentGameState.OnEnter();
        }
    }
}

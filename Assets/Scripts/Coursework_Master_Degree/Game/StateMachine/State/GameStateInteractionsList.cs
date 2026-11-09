using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateInteractionsList : GameState
    {
        // Controllers
        private readonly InterfaceController interfaceController;
        private readonly ItemController itemController;

        // References
        private readonly IReadOnlyList<InteractionOption> interactions;

        public GameStateInteractionsList(IReadOnlyList<InteractionOption> interactions)
        {
            itemController = GameStateManager.Instance.ItemController;
            interfaceController = GameStateManager.Instance.InterfaceController;
            this.interactions = interactions;
        }

        public override void OnEnter()
        {
            ShowCursor();

            GameStateManager.Instance.InputReader.EnableInteractionsListStateInput();

            itemController.SetInteractionsListState(true, interactions);
            interfaceController.SetInteractionsListState(true);
        }

        public override void OnExit()
        {
            interfaceController.SetInteractionsListState(false);
            itemController.SetInteractionsListState(false, interactions);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

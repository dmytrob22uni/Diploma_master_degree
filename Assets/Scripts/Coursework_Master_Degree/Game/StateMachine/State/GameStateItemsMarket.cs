using UnityEngine;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStateItemsMarket : GameState
    {
        // Controllers
        private readonly ItemController itemController;
        private readonly InterfaceController interfaceController;

        public GameStateItemsMarket()
        {
            itemController = GameStateManager.Instance.ItemController;
            interfaceController = GameStateManager.Instance.InterfaceController;
        }

        public override void OnEnter()
        {
            ShowCursor();

            GameStateManager.Instance.InputReader.EnableItemsMarketStateInput();

            itemController.SetItemsMarketState(true);
            interfaceController.SetItemsMarketState(true);
        }

        public override void OnExit()
        {
            interfaceController.SetItemsMarketState(false);
            itemController.SetItemsMarketState(false);

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

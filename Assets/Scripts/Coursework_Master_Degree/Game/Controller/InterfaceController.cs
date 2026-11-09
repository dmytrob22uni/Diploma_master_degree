using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket;
using Coursework_Master_Degree.Game.Logic.Interface.InteractionsList;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Controller
{
    public class InterfaceController : MonoBehaviour
    {
        [Header("Logic Components")]
        [HorizontalLine(1.0f)]
        [Header("ItemsMarket Logics")]
        public ItemsMarketGenerator ItemsMarketGenerator;
        [Header("InteractionsList Logics")]
        public InteractionsListGenerator interactionsListGenerator;

        [Header("Interfaces")]
        [HorizontalLine(1.0f)]
        [Header("Common Interfaces")]
        public GameObject PointInterface;
        [Header("Game State Interfaces")]
        public GameObject FreeWalkInterface;
        public GameObject ItemsMarketInterface;
        public GameObject ItemPlaceInterface;
        public GameObject InteractionsListInterface;

        public void SetFreeWalkState(bool enable)
        {
            if (enable)
            {
                // FreeWalk Input
                GameStateManager.Instance.InputReader.OnItemsMarketEvent += HandleItemsMarket;

                // FreeWalk Interfaces
                PointInterface.SetActive(true);
                FreeWalkInterface.SetActive(true);
            }
            else
            {
                // FreeWalk Input
                GameStateManager.Instance.InputReader.OnItemsMarketEvent -= HandleItemsMarket;

                // FreeWalk Interfaces
                PointInterface.SetActive(false);
                FreeWalkInterface.SetActive(false);
            }
        }

        public void SetItemsMarketState(bool enable)
        {
            if (enable)
            {
                // ItemsMarket Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitItemsMarket;

                // ItemsMarket Logics
                ItemsMarketGenerator.enabled = true;

                // ItemsMarket Interfaces
                ItemsMarketInterface.SetActive(true);
            }
            else
            {
                // ItemsMarket Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitItemsMarket;

                // ItemsMarket Logics
                ItemsMarketGenerator.enabled = false;

                // ItemsMarket Interfaces
                ItemsMarketInterface.SetActive(false);
            }
        }

        public void SetItemPlaceState(bool enable)
        {
            if (enable)
            {
                // ItemPlace Interfaces
                PointInterface.SetActive(true);
                ItemPlaceInterface.SetActive(true);
            }
            else
            {
                // ItemPlace Interfaces
                PointInterface.SetActive(false);
                ItemPlaceInterface.SetActive(false);
            }
        }

        public void SetInteractionsListState(bool enabled)
        {
            if (enabled)
            {
                // InteractionsList Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitInteractionsList;

                // InteractionsList Logics
                interactionsListGenerator.enabled = true;

                // InteractionsList Interfaces
                InteractionsListInterface.SetActive(true);
            }
            else
            {
                // InteractionsList Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitInteractionsList;

                // InteractionsList Logics
                interactionsListGenerator.enabled = false;

                // InteractionsList Interfaces
                InteractionsListInterface.SetActive(false);
            }
        }

        // === Handler methods  ===

        // FreeWalk

        private void HandleItemsMarket()
        {
            GameStateManager.Instance.ChangeState(new GameStateItemsMarket());
        }

        // ItemsMarket

        private void HandleQuitItemsMarket()
        {
            ItemsMarketGenerator.ApplyQuitItemsMarket();

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }

        // InteractionsList

        private void HandleQuitInteractionsList()
        {
            interactionsListGenerator.ApplyQuitInteractionsList();

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }
    }
}

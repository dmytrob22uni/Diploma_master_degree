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
        public GameObject ItemsMarketCanvas;
        public GameObject ItemPlaceInterface;
        public GameObject InteractionsListInterface;
        public GameObject InteractionsListCanvas;
        public GameObject PartsWearInterface;
        public GameObject PartsRemoveInterface;
        public GameObject PartsIntegrityInterface;

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
                ItemsMarketCanvas.SetActive(true);
            }
            else
            {
                // ItemsMarket Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitItemsMarket;

                // ItemsMarket Logics
                ItemsMarketGenerator.enabled = false;

                // ItemsMarket Interfaces
                ItemsMarketInterface.SetActive(false);
                ItemsMarketCanvas.SetActive(false);
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

        public void SetInteractionsListState(bool enable)
        {
            if (enable)
            {
                // InteractionsList Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitInteractionsList;

                // InteractionsList Logics
                interactionsListGenerator.enabled = true;

                // InteractionsList Interfaces
                InteractionsListInterface.SetActive(true);
                InteractionsListCanvas.SetActive(true);
            }
            else
            {
                // InteractionsList Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitInteractionsList;

                // InteractionsList Logics
                interactionsListGenerator.enabled = false;

                // InteractionsList Interfaces
                InteractionsListInterface.SetActive(false);
                InteractionsListCanvas.SetActive(false);
            }
        }

        public void SetPartsWearState(bool enable)
        {
            if (enable)
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitPartsWear;
                GameStateManager.Instance.InputReader.OnRemoveModeFromPartsWearEvent += HandleRemoveModeFromPartsWear;

                // Interfaces
                PointInterface.SetActive(true);
                PartsWearInterface.SetActive(true);
            }
            else
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitPartsWear;
                GameStateManager.Instance.InputReader.OnRemoveModeFromPartsWearEvent -= HandleRemoveModeFromPartsWear;

                // Interfaces
                PointInterface.SetActive(false);
                PartsWearInterface.SetActive(false);
            }
        }

        public void SetPartsRemoveState(bool enable)
        {
            if (enable)
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitPartsRemove;

                // Interfaces
                PointInterface.SetActive(true);
                PartsRemoveInterface.SetActive(true);
            }
            else
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitPartsRemove;

                // Interfaces
                PointInterface.SetActive(false);
                PartsRemoveInterface.SetActive(false);
            }
        }

        public void SetPartsIntegrityState(bool enable)
        {
            if (enable)
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitPartsIntegrity;
                GameStateManager.Instance.InputReader.OnRemoveModeFromPartsIntegrityEvent += HandleRemoveModeFromPartsIntegrity;

                // Interfaces
                PointInterface.SetActive(true);
                PartsIntegrityInterface.SetActive(true);
            }
            else
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitPartsIntegrity;
                GameStateManager.Instance.InputReader.OnRemoveModeFromPartsIntegrityEvent -= HandleRemoveModeFromPartsIntegrity;

                // Interfaces
                PointInterface.SetActive(false);
                PartsIntegrityInterface.SetActive(false);
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

        // PartsWear

        private void HandleQuitPartsWear()
        {
            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }

        private void HandleRemoveModeFromPartsWear()
        {
            if (GameStateManager.Instance.CurrentGameState is not GameStatePartsWear partsWear)
            {
                return;
            }

            GameStateManager.Instance.ChangeState(
                new GameStatePartsRemove(
                    partsWear.PartsAssembly,
                    partsWear.ItemDataHolder));
        }

        // PartsRemove

        private void HandleQuitPartsRemove()
        {
            GameStateManager.Instance.ChangeState(GameStatePartsRemove.GetReturnGameState());
        }

        // PartsIntegrity

        private void HandleQuitPartsIntegrity()
        {
            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }

        private void HandleRemoveModeFromPartsIntegrity()
        {
            if (GameStateManager.Instance.CurrentGameState is not GameStatePartsIntegrity partsIntegrity)
            {
                return;
            }

            GameStateManager.Instance.ChangeState(
                new GameStatePartsRemove(
                    partsIntegrity.PartsAssembly,
                    partsIntegrity.ItemDataHolder));
        }
    }
}

using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket;
using Coursework_Master_Degree.Game.Logic.Interface.InteractionsList;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Game.Logic.Interface.DeviceBind;

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
        public DeviceBindPanel DeviceBindPanel;
        [Header("Game State Interfaces")]
        public GameObject FreeWalkInterface;
        public GameObject ItemsMarketInterface;
        public GameObject ItemsMarketCanvas;
        public GameObject ItemPlaceInterface;
        public GameObject PartInstallInterface;
        public GameObject InteractionsListInterface;
        public GameObject InteractionsListCanvas;
        public GameObject PartsWearInterface;
        public GameObject PartsWearRemoveModeRoot;
        public GameObject PartsRemoveInterface;
        public GameObject PartsIntegrityInterface;
        public GameObject ItemsSellCanvas;

        public void SetDeviceBindState(bool enable)
        {
            // The centre point and the bind prompt belong to the aimed bind state.
            PointInterface.SetActive(enable);

            if (DeviceBindPanel != null)
            {
                DeviceBindPanel.gameObject.SetActive(enable);
            }
        }

        public void SetFreeWalkState(bool enable)
        {
            if (enable)
            {
                // FreeWalk Input
                GameStateManager.Instance.InputReader.OnItemsMarketEvent += HandleItemsMarket;
                GameStateManager.Instance.InputReader.OnItemsSellEvent += HandleItemsSell;

                // FreeWalk Interfaces
                PointInterface.SetActive(true);
                FreeWalkInterface.SetActive(true);
            }
            else
            {
                // FreeWalk Input
                GameStateManager.Instance.InputReader.OnItemsMarketEvent -= HandleItemsMarket;
                GameStateManager.Instance.InputReader.OnItemsSellEvent -= HandleItemsSell;

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

        public void SetItemPlaceState(bool enable, bool isInstallModeAvailable = false)
        {
            if (enable)
            {
                // ItemPlace Input
                if (isInstallModeAvailable)
                {
                    GameStateManager.Instance.InputReader.OnInstallModeFromItemPlaceEvent += HandleInstallModeFromItemPlace;
                }

                // ItemPlace Interfaces
                PointInterface.SetActive(true);
                ItemPlaceInterface.SetActive(true);
            }
            else
            {
                // ItemPlace Input
                if (isInstallModeAvailable)
                {
                    GameStateManager.Instance.InputReader.OnInstallModeFromItemPlaceEvent -= HandleInstallModeFromItemPlace;
                }

                // ItemPlace Interfaces
                PointInterface.SetActive(false);
                ItemPlaceInterface.SetActive(false);
            }
        }

        public void SetPartInstallState(bool enable)
        {
            if (enable)
            {
                // PartInstall Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitPartInstall;

                // PartInstall Interfaces
                PointInterface.SetActive(true);
                PartInstallInterface.SetActive(true);
            }
            else
            {
                // PartInstall Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitPartInstall;

                // PartInstall Interfaces
                PointInterface.SetActive(false);
                PartInstallInterface.SetActive(false);
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

        public void SetPartsWearState(bool enable, bool isRemoveModeAvailable = true)
        {
            if (enable)
            {
                // Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitPartsWear;

                if (isRemoveModeAvailable)
                {
                    GameStateManager.Instance.InputReader.OnRemoveModeFromPartsWearEvent += HandleRemoveModeFromPartsWear;
                }

                // Interfaces
                PointInterface.SetActive(true);
                PartsWearInterface.SetActive(true);

                if (PartsWearRemoveModeRoot != null)
                {
                    PartsWearRemoveModeRoot.SetActive(isRemoveModeAvailable);
                }
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

        public void SetItemsSellState(bool enable)
        {
            if (enable)
            {
                // ItemsSell Input
                GameStateManager.Instance.InputReader.OnQuitEvent += HandleQuitItemsSell;

                // ItemsSell Interfaces
                PointInterface.SetActive(true);
                ItemsSellCanvas.SetActive(true);
            }
            else
            {
                // ItemsSell Input
                GameStateManager.Instance.InputReader.OnQuitEvent -= HandleQuitItemsSell;

                // ItemsSell Interfaces
                PointInterface.SetActive(false);
                ItemsSellCanvas.SetActive(false);
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

        private void HandleItemsSell()
        {
            GameStateManager.Instance.ChangeState(new GameStateItemsSell());
        }

        // ItemPlace

        private void HandleInstallModeFromItemPlace()
        {
            if (GameStateManager.Instance.CurrentGameState is not GameStateItemPlace itemPlace)
            {
                return;
            }

            ItemDataHolder item = itemPlace.ItemDataHolder;
            PartsAssembly partsAssembly = itemPlace.InstallTargetAssembly;

            if (item == null || !itemPlace.IsPartInstallable)
            {
                return;
            }

            if (!string.IsNullOrEmpty(itemPlace.InstallBlockReason))
            {
                itemPlace.ShowInstallBlockedReason();
                return;
            }

            if (partsAssembly == null)
            {
                return;
            }

            GameStateManager.Instance.ChangeState(
                new GameStatePartInstall(item, itemPlace.ReturnGameState, partsAssembly));
        }

        // PartInstall

        private void HandleQuitPartInstall()
        {
            if (GameStateManager.Instance.CurrentGameState is not GameStatePartInstall)
            {
                return;
            }

            // Part install is entered only from item placement, so quitting
            // resumes placing the same item instead of dropping it.
            if (GameStateManager.Instance.PreviousGameState is GameStateItemPlace itemPlace)
            {
                GameStateManager.Instance.ChangeState(itemPlace);

                return;
            }

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }

        // ItemsMarket

        private void HandleQuitItemsMarket()
        {
            ItemsMarketGenerator.ApplyQuitItemsMarket();

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }

        // ItemsSell

        private void HandleQuitItemsSell()
        {
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

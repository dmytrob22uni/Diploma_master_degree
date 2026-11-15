using UnityEngine;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartInstall : GameState
    {
        public ItemDataHolder ItemDataHolder { get; }
        public GameState ReturnGameState { get; }
        public PartsAssembly PartsAssembly { get; }
        public ItemDataHolder PartsAssemblyItem { get; }

        public GameStatePartInstall(
            ItemDataHolder itemDataHolder = default,
            GameState returnGameState = default,
            PartsAssembly partsAssembly = default)
        {
            ItemDataHolder = itemDataHolder;
            ReturnGameState = returnGameState;
            PartsAssembly = partsAssembly;
            PartsAssemblyItem = partsAssembly != null
                ? partsAssembly.GetComponent<ItemDataHolder>()
                : default;
        }

        public override void OnEnter()
        {
            HideCursor();

            HidePlacementGhost();

            GameStateManager.Instance.PartsInstallStateSetup.SetupPartsInstall(
                PartsAssembly,
                PartsAssemblyItem);

            GameStateManager.Instance.InputReader.EnablePartInstallStateInput();

            GameStateManager.Instance.PlayerController.SetPartInstallState(
                true,
                ItemDataHolder,
                PartsAssembly);
            GameStateManager.Instance.InterfaceController.SetPartInstallState(true);
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InterfaceController.SetPartInstallState(false);
            GameStateManager.Instance.PlayerController.SetPartInstallState(false);

            GameStateManager.Instance.PartsInstallStateSetup.ClearPartsInstall();

            GameStateManager.Instance.InputReader.DisableAllInput();
        }

        private void HidePlacementGhost()
        {
            if (ItemDataHolder == null)
            {
                return;
            }

            if (ItemDataHolder.TryGetGhostInstance(ItemGhostType.Placement, out ItemGhostDataHolder placementGhost) &&
                placementGhost != null)
            {
                placementGhost.gameObject.SetActive(false);
            }
        }
    }
}

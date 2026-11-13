using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartsWear : GameState
    {
        public PartsAssembly PartsAssembly { get; }
        public ItemDataHolder ItemDataHolder { get; }

        public GameStatePartsWear(
            PartsAssembly partsAssembly = default,
            ItemDataHolder itemDataHolder = default)
        {
            PartsAssembly = partsAssembly;
            ItemDataHolder = itemDataHolder;
        }

        public override void OnEnter()
        {
            HideCursor();

            GameStateManager.Instance.InputReader.EnablePartsWearStateInput();

            GameStateManager.Instance.PartsWearStateSetup.SetupPartsWear(
                PartsAssembly,
                ItemDataHolder);

            GameStateManager.Instance.PlayerController.SetPartsWearState(true, PartsAssembly);
            GameStateManager.Instance.InterfaceController.SetPartsWearState(true);
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InterfaceController.SetPartsWearState(false);
            GameStateManager.Instance.PlayerController.SetPartsWearState(false);

            GameStateManager.Instance.PartsWearStateSetup.ClearPartsWear();

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

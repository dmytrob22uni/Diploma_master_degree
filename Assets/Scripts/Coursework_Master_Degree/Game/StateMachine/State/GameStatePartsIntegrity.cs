using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartsIntegrity : GameState
    {
        public PartsAssembly PartsAssembly { get; }
        public ItemDataHolder ItemDataHolder { get; }

        public GameStatePartsIntegrity(
            PartsAssembly partsAssembly = default,
            ItemDataHolder itemDataHolder = default)
        {
            PartsAssembly = partsAssembly;
            ItemDataHolder = itemDataHolder;
        }

        public override void OnEnter()
        {
            HideCursor();

            GameStateManager.Instance.InputReader.EnablePartsIntegrityStateInput();

            GameStateManager.Instance.PartsIntegrityStateSetup.SetupPartsIntegrity(
                PartsAssembly,
                ItemDataHolder);

            GameStateManager.Instance.PlayerController.SetPartsIntegrityState(true);
            GameStateManager.Instance.InterfaceController.SetPartsIntegrityState(true);
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InterfaceController.SetPartsIntegrityState(false);
            GameStateManager.Instance.PlayerController.SetPartsIntegrityState(false);

            GameStateManager.Instance.PartsIntegrityStateSetup.ClearPartsIntegrity();

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

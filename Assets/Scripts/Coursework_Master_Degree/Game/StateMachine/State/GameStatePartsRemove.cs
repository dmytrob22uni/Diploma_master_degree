using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    public class GameStatePartsRemove : GameState
    {
        public PartsAssembly PartsAssembly { get; }
        public ItemDataHolder ItemDataHolder { get; }

        public GameStatePartsRemove(
            PartsAssembly partsAssembly = default,
            ItemDataHolder itemDataHolder = default)
        {
            PartsAssembly = partsAssembly;
            ItemDataHolder = itemDataHolder;
        }

        public static GameState GetReturnGameState()
        {
            GameState previousGameState = GameStateManager.Instance.PreviousGameState;

            if (previousGameState is GameStatePartsWear or GameStatePartsIntegrity)
            {
                return previousGameState;
            }

            return new GameStateFreeWalk();
        }

        public override void OnEnter()
        {
            HideCursor();

            GameStateManager.Instance.InputReader.EnablePartsRemoveStateInput();

            GameStateManager.Instance.PartsRemoveStateSetup.SetupPartsRemove(
                PartsAssembly,
                ItemDataHolder);

            GameStateManager.Instance.PlayerController.SetPartsRemoveState(true, PartsAssembly);
            GameStateManager.Instance.InterfaceController.SetPartsRemoveState(true);
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InterfaceController.SetPartsRemoveState(false);
            GameStateManager.Instance.PlayerController.SetPartsRemoveState(false);

            GameStateManager.Instance.PartsRemoveStateSetup.ClearPartsRemove();

            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

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

        public override void OnEnter()
        {
            GameStateManager.Instance.InputReader.EnablePartsRemoveStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

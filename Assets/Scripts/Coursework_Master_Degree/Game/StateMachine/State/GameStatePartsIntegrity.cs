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
            GameStateManager.Instance.InputReader.EnablePartsIntegrityStateInput();
        }

        public override void OnExit()
        {
            GameStateManager.Instance.InputReader.DisableAllInput();
        }
    }
}

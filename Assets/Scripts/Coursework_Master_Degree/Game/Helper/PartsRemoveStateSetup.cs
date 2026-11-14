using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Prepares an item with <see cref="PartsAssembly"/> for the parts remove game state.
    /// Shows the transparent frame and keeps the installed parts as they are.
    /// </summary>
    public class PartsRemoveStateSetup : PartsStateSetup
    {
        protected override string PartsExitTriggerName => "parts_remove_exit_trigger";

        protected override bool IsPartsStateActive()
        {
            return GameStateManager.Instance.CurrentGameState is GameStatePartsRemove;
        }

        protected override GameState GetExitGameState()
        {
            return GameStatePartsRemove.GetReturnGameState();
        }

        public void SetupPartsRemove(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            ClearPartsRemove();

            if (partsAssembly == null || item == null)
            {
                return;
            }

            SetupBackgroundGhost(item);
            SetupExitTrigger(item);
        }

        public void ClearPartsRemove()
        {
            ClearBackgroundGhost();
            ClearExitTrigger();
        }
    }
}

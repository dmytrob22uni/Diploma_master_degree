using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Prepares an item with <see cref="PartsAssembly"/> for the part install game state.
    /// Hides the complex item frame and shows its background ghost, so only the
    /// installation locations and the installed parts stay visible.
    /// </summary>
    public class PartsInstallStateSetup : PartsStateSetup
    {
        protected override string PartsExitTriggerName => "parts_install_exit_trigger";

        protected override bool IsPartsStateActive()
        {
            return GameStateManager.Instance.CurrentGameState is GameStatePartInstall;
        }

        protected override GameState GetExitGameState()
        {
            return new GameStateFreeWalk();
        }

        public void SetupPartsInstall(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            ClearPartsInstall();

            if (partsAssembly == null || item == null)
            {
                return;
            }

            SetupBackgroundGhost(item);
        }

        public void ClearPartsInstall()
        {
            ClearBackgroundGhost();
        }
    }
}

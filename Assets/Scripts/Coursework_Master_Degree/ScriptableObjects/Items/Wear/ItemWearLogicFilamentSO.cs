using UnityEngine;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_filament_SO", menuName = "Game Items/Wear/Item Wear Logic Filament SO", order = 1)]
    public class ItemWearLogicFilamentSO : ItemWearLogicSO
    {
        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaFilament filamentDelta = wearDelta as WearDeltaFilament;

            return filamentDelta.FilamentLength / filamentDelta.FilamentLengthSpool * 100.0f;
        }
    }
}

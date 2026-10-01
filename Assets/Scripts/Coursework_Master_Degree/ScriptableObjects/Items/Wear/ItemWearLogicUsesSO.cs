using UnityEngine;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_uses_SO", menuName = "Game Items/Wear/Item Wear Logic Uses SO", order = 8)]
    public class ItemWearLogicUsesSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip(
            "Condition percentage points lost per one standardized use.\n" +
            "A useful starting point is 100 / expected number of uses")]
        public float WearPerUse;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaUses delta = wearDelta as WearDeltaUses;

            return WearPerUse * Mathf.Max(0.0f, delta.Uses);
        }
    }
}

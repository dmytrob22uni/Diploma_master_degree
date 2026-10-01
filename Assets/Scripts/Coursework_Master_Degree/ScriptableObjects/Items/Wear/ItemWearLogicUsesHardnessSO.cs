using UnityEngine;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_uses_hardness_SO", menuName = "Game Items/Wear/Item Wear Logic Uses Hardness SO", order = 9)]
    public class ItemWearLogicUsesHardnessSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip("Base wear caused by one use on zero-hardness material")]
        public float WearPerUse;

        [Min(0.0f)]
        [Tooltip(
            "Additional wear when materialHardness = 1.\n" +
            "1.0 means hardest material doubles wear")]
        public float HardnessEffect = 1.0f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaUsesHardness delta = wearDelta as WearDeltaUsesHardness;

            float hardness = Mathf.Clamp01(delta.MaterialHardness);

            float hardnessMultiplier = 1.0f + hardness * HardnessEffect;

            return WearPerUse * Mathf.Max(0.0f, delta.Uses) * hardnessMultiplier;
        }
    }
}

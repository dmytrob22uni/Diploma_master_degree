using UnityEngine;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_time_SO", menuName = "Game Items/Wear/Item Wear Logic Time SO", order = 4)]
    public class ItemWearLogicTimeSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip(
            "Condition percentage points lost per operating hour.\n" +
            "Good starting point: 100 / expected lifetime in hours")]
        public float WearPerHour;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaTime delta = wearDelta as WearDeltaTime;

            float hours = Mathf.Max(0.0f, delta.DeltaTime) / 3600.0f;

            return WearPerHour * hours * wearDelta.WearRatio;
        }
    }
}

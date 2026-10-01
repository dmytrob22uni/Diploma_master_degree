using UnityEngine;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_time_fumes_SO", menuName = "Game Items/Wear/Item Wear Logic Time Fumes SO", order = 5)]
    public class ItemWearLogicTimeFumesSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip(
            "Condition percentage points lost per operating hour.\n" +
            "Good starting point: 100 / expected lifetime in hours")]
        public float WearPerHour;

        [Min(0.0f)]
        [Tooltip(
            "Extra wear at fumesDensity = 1.\n" +
            "1.0 means maximum fumes doubles the normal wear")]
        public float FumesEffect = 1.0f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaTimeFumes delta = wearDelta as WearDeltaTimeFumes;

            float hours = Mathf.Max(0.0f, delta.DeltaTime) / 3600.0f;

            float fumes = Mathf.Clamp01(delta.FumesDensity);

            float fumesMultiplier = 1.0f + fumes * FumesEffect;

            return WearPerHour * hours * fumesMultiplier * wearDelta.WearRatio;
        }
    }
}

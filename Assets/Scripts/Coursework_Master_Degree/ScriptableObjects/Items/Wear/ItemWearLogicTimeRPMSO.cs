using UnityEngine;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_time_rpm_SO", menuName = "Game Items/Wear/Item Wear Logic Time RPM SO", order = 6)]
    public class ItemWearLogicTimeRpmSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip(
            "Base wear per operating hour at zero RPM.\n" +
            "Good starting point: 100 / expected lifetime in hours")]
        public float WearPerHour;

        [Min(1.0f)]
        [Tooltip(
            "Normal/rated RPM of the fan.\n" +
            "Used only to normalize the RPM effect")]
        public float ReferenceRPM = 3000.0f;

        [Min(0.0f)]
        [Tooltip(
            "How much RPM increases wear.\n" +
            "1.0 = at reference RPM, wear is doubled\n" +
            "0.5 = at reference RPM, wear is 50% higher\n" +
            "0 = RPM has no effect")]
        public float RPMEffect = 1.0f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaTimeRPM delta = wearDelta as WearDeltaTimeRPM;

            float hours = Mathf.Max(0.0f, delta.DeltaTime) / 3600.0f;

            float rpmMultiplier = 1.0f + (Mathf.Max(0.0f, delta.RPM) / ReferenceRPM) * RPMEffect;

            return WearPerHour * hours * rpmMultiplier * wearDelta.WearRatio;
        }
    }
}

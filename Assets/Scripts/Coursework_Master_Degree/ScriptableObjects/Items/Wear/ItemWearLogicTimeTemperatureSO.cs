using UnityEngine;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_time_temperature_SO", menuName = "Game Items/Wear/Item Wear Logic Time Temperature SO", order = 7)]
    public class ItemWearLogicTimeTemperatureSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip(
            "Base condition loss per operating hour at or below " +
            "the reference temperature")]
        public float WearPerHour;

        [Tooltip("Temperature at which the base wear rate is used")]
        public float ReferenceTemperature = 25.0f;

        [Min(1.0f)]
        [Tooltip(
            "Temperature increase represented by one full temperature effect.\n" +
            "Example: 25°C means every 25°C above the reference " +
            "adds temperatureEffect * 100% to wear")]
        public float TemperatureRange = 25.0f;

        [Min(0.0f)]
        [Tooltip(
            "How strongly temperature affects wear.\n" +
            "1.0 = +100% wear per temperatureRange\n" +
            "0.5 = +50%\n" +
            "2.0 = +200%")]
        public float TemperatureEffect = 1.0f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaTimeTemperature delta = wearDelta as WearDeltaTimeTemperature;

            float hours = Mathf.Max(0.0f, delta.DeltaTime) / 3600.0f;

            float temperatureMultiplier =
                ItemWearMathTool.TemperatureMultiplier(
                    delta.Temperature,
                    ReferenceTemperature,
                    TemperatureRange,
                    TemperatureEffect
                );

            return WearPerHour * hours * temperatureMultiplier * wearDelta.WearRatio;
        }
    }
}

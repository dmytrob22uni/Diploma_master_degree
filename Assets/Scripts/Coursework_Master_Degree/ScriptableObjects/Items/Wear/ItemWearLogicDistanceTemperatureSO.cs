using UnityEngine;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_distance_temperature_SO", menuName = "Game Items/Wear/Item Wear Logic Distance Temperature SO", order = 0)]
    public class ItemWearLogicDistanceTemperatureSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip("Condition percentage points lost per kilometer travelled")]
        public float WearPerKilometer;

        [Tooltip("Temperature at which the base wear rate is used")]
        public float ReferenceTemperature = 30.0f;

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
        public float TemperatureEffect = 0.5f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaDistanceTemperature delta = wearDelta as WearDeltaDistanceTemperature;

            float kilometers = Mathf.Max(0.0f, delta.DistanceTravelled) / 1_000.0f;

            float temperatureMultiplier =
                ItemWearMathTool.TemperatureMultiplier(
                    delta.Temperature,
                    ReferenceTemperature,
                    TemperatureRange,
                    TemperatureEffect
                );

            return WearPerKilometer * kilometers * temperatureMultiplier * wearDelta.WearRatio;
        }
    }
}

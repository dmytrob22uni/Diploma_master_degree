using UnityEngine;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    [CreateAssetMenu(fileName = "item_wear_logic_nozzle_SO", menuName = "Game Items/Wear/Item Wear Logic Filament Temperature Abrasiveness SO", order = 2)]
    public class ItemWearLogicFilamentTemperatureAbrasivenessSO : ItemWearLogicSO
    {
        [Min(0.0f)]
        [Tooltip("Condition percentage points lost per metre of filament")]
        public float WearPerFilamentLength;

        [Tooltip("Temperature at which the base wear rate is used")]
        public float ReferenceTemperature = 200.0f;

        [Min(1.0f)]
        [Tooltip(
            "Temperature increase represented by one full temperature effect.\n" +
            "Example: 25°C means every 25°C above the reference " +
            "adds temperatureEffect * 100% to wear")]
        public float TemperatureRange = 50.0f;

        [Min(0.0f)]
        [Tooltip(
            "How strongly temperature affects wear.\n" +
            "1.0 = +100% wear per temperatureRange\n" +
            "0.5 = +50%\n" +
            "2.0 = +200%")]
        public float TemperatureEffect = 0.25f;

        [Min(0.0f)]
        [Tooltip(
            "Additional wear at materialAbrasiveness = 1.\n" +
            "1.0 means maximum abrasiveness doubles wear")]
        public float AbrasivenessEffect = 1.0f;

        public override float CalculateWear(WearDelta wearDelta)
        {
            WearDeltaFilamentTemperatureAbrasiveness delta = wearDelta as WearDeltaFilamentTemperatureAbrasiveness;

            float metres = Mathf.Max(0.0f, delta.FilamentLength);

            float abrasiveness = Mathf.Clamp01(delta.MaterialAbrasiveness);

            float abrasivenessMultiplier = 1.0f + abrasiveness * AbrasivenessEffect;

            float temperatureMultiplier =
                ItemWearMathTool.TemperatureMultiplier(
                    delta.Temperature,
                    ReferenceTemperature,
                    TemperatureRange,
                    TemperatureEffect
                );

            return WearPerFilamentLength * metres * abrasivenessMultiplier * temperatureMultiplier * wearDelta.WearRatio;
        }
    }
}

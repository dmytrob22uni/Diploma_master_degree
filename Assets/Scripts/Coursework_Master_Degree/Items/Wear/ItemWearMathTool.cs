using UnityEngine;

namespace Coursework_Master_Degree.Items.Wear
{
    public static class ItemWearMathTool
    {
        public static float TemperatureMultiplier(float temperature, float referenceTemperature, float temperatureRange, float temperatureEffect)
        {
            if (temperature <= referenceTemperature)
            {
                return 1.0f;
            }

            float normalizedTemperature = (temperature - referenceTemperature) / temperatureRange;

            return 1.0f + normalizedTemperature * temperatureEffect;
        }
    }
}

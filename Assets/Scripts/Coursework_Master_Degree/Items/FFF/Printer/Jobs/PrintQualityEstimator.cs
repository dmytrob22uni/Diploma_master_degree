using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// M2 raw-quality projection before post-processing. The full defect model
    /// arrives in M5; these deltas only reflect settings and hardware wear.
    /// </summary>
    public static class PrintQualityEstimator
    {
        private const float BaseQuality = 82.0f;
        private const float Variance = 5.0f;
        private const float MinimumQuality = 20.0f;

        public static void Calculate(
            ObjectToPrintPrintProfile profile,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SupportsMode supports,
            bool supportsRequired,
            float hardwarePenalty,
            out float minimum,
            out float maximum)
        {
            float score = BaseQuality +
                LayerHeightSettings.QualityDelta(layerHeight) +
                PrintSpeedSettings.QualityDelta(speed) -
                Mathf.Max(0.0f, hardwarePenalty);

            if (supports == SupportsMode.Auto)
            {
                score -= SupportsSettings.RawQualityMark;
            }

            bool missingRequiredSupports =
                supportsRequired && supports == SupportsMode.None;

            if (missingRequiredSupports)
            {
                score -= SupportsSettings.MissingRequiredPenalty;
            }

            float cap = missingRequiredSupports
                ? SupportsSettings.MissingRequiredQualityCap
                : 100.0f;

            minimum = Mathf.Clamp(score, MinimumQuality, cap);
            maximum = Mathf.Clamp(score + Variance, MinimumQuality, cap);
        }
    }
}

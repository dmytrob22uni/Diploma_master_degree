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

        /// <summary>
        /// Raw prints cap at 90 even with an enclosure. Post-processing can
        /// improve the final object beyond this raw ceiling.
        /// </summary>
        public const float MaximumRawQuality = 90.0f;
        public const float EnclosureQualityBonus = 5.0f;
        public const float NoEnclosurePenalty = 9.0f;

        public static void Calculate(
            ObjectToPrintPrintProfile profile,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SupportsMode supports,
            bool supportsRequired,
            float hardwarePenalty,
            out float minimum,
            out float maximum,
            bool hasEnclosure = false)
        {
            float score = BaseQuality +
                LayerHeightSettings.QualityDelta(layerHeight) +
                PrintSpeedSettings.QualityDelta(speed) -
                Mathf.Max(0.0f, hardwarePenalty) +
                (hasEnclosure
                    ? EnclosureQualityBonus
                    : -NoEnclosurePenalty);

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
                : MaximumRawQuality;

            minimum = Mathf.Clamp(score, MinimumQuality, cap);
            maximum = Mathf.Clamp(score + Variance, MinimumQuality, cap);
        }
    }
}

using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// M2 raw-quality projection before post-processing. The full defect model
    /// arrives in M5; these deltas only reflect settings and hardware wear.
    ///
    /// The projection deliberately excludes the enclosure contribution. A job
    /// stores the base range and the printer applies +5 / -9 exactly once when
    /// the print finishes, using the job's one-way enclosure outcome.
    /// </summary>
    public static class PrintQualityEstimator
    {
        private const float BaseQuality = 82.0f;
        private const float Variance = 5.0f;

        /// <summary>
        /// Raw prints cap at 90 even with an enclosure. Post-processing can
        /// improve the final object beyond this raw ceiling.
        /// </summary>
        public const float MinimumRawQuality = 20.0f;
        public const float MaximumRawQuality = 90.0f;
        public const float EnclosureQualityBonus = 5.0f;
        public const float NoEnclosurePenalty = 9.0f;
        public const float MaximumPauseQualityPenalty = 20.0f;

        public static void Calculate(
            ObjectToPrintPrintProfile profile,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SupportsMode supports,
            bool supportsRequired,
            float hardwarePenalty,
            out float minimum,
            out float maximum,
            out float cap)
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

            cap = missingRequiredSupports
                ? SupportsSettings.MissingRequiredQualityCap
                : MaximumRawQuality;

            // Unclamped so the enclosure contribution can be applied once at
            // finalization before the same raw cap.
            minimum = score;
            maximum = score + Variance;
        }

        public static float ApplyEnclosure(
            float value,
            float cap,
            bool hasEnclosure)
        {
            float contribution = hasEnclosure
                ? EnclosureQualityBonus
                : -NoEnclosurePenalty;

            return Mathf.Clamp(
                value + contribution,
                MinimumRawQuality,
                cap);
        }
    }
}

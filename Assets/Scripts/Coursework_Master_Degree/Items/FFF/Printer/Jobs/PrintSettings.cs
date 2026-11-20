using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Player-facing print thickness. Values are relative to a 0.20 mm
    /// baseline, which is the profile reference layer height.
    /// </summary>
    public enum LayerHeightPreset
    {
        Draft,
        Normal,
        Fine
    }

    /// <summary>
    /// Player-facing print speed. The profile stores the speed its time was
    /// measured at; these presets scale from it.
    /// </summary>
    public enum PrintSpeedPreset
    {
        Slow,
        Normal,
        Fast
    }

    public static class LayerHeightSettings
    {
        public const float ReferenceMillimeters = 0.20f;

        public static float Millimeters(LayerHeightPreset preset)
        {
            switch (preset)
            {
                case LayerHeightPreset.Draft:
                    return 0.28f;
                case LayerHeightPreset.Fine:
                    return 0.12f;
                default:
                    return ReferenceMillimeters;
            }
        }

        public static float TimeFactor(
            LayerHeightPreset preset,
            float referenceMillimeters)
        {
            float reference = referenceMillimeters > 0.001f
                ? referenceMillimeters
                : ReferenceMillimeters;

            return reference / Millimeters(preset);
        }

        public static int QualityDelta(LayerHeightPreset preset)
        {
            switch (preset)
            {
                case LayerHeightPreset.Draft:
                    return -10;
                case LayerHeightPreset.Fine:
                    return 4;
                default:
                    return 0;
            }
        }

        public static string Label(LayerHeightPreset preset)
        {
            switch (preset)
            {
                case LayerHeightPreset.Draft:
                    return "Draft";
                case LayerHeightPreset.Fine:
                    return "Fine";
                default:
                    return "Normal";
            }
        }
    }

    public static class PrintSpeedSettings
    {
        // The authored profile times were measured at 80 mm/s, which is now
        // the Slow preset. Keep this reference when rescaling estimates.
        public const float ReferenceMillimetersPerSecond = 80.0f;
        public const float NormalMillimetersPerSecond = 160.0f;
        public const float FastMillimetersPerSecond = 240.0f;

        // Acceleration and travel make speed changes sub-linear in time.
        public const float TimeFactorExponent = 0.8f;

        public static float MillimetersPerSecond(PrintSpeedPreset preset)
        {
            switch (preset)
            {
                case PrintSpeedPreset.Slow:
                    return ReferenceMillimetersPerSecond;
                case PrintSpeedPreset.Fast:
                    return FastMillimetersPerSecond;
                default:
                    return NormalMillimetersPerSecond;
            }
        }

        public static float TimeFactor(
            PrintSpeedPreset preset,
            float referenceMillimetersPerSecond)
        {
            float reference = referenceMillimetersPerSecond > 0.1f
                ? referenceMillimetersPerSecond
                : ReferenceMillimetersPerSecond;

            return Mathf.Pow(
                reference / MillimetersPerSecond(preset),
                TimeFactorExponent);
        }

        public static int QualityDelta(PrintSpeedPreset preset)
        {
            switch (preset)
            {
                case PrintSpeedPreset.Slow:
                    return 4;
                case PrintSpeedPreset.Fast:
                    return -10;
                default:
                    return 0;
            }
        }

        public static string Label(PrintSpeedPreset preset)
        {
            switch (preset)
            {
                case PrintSpeedPreset.Slow:
                    return "Slow";
                case PrintSpeedPreset.Fast:
                    return "Fast";
                default:
                    return "Normal";
            }
        }
    }
}

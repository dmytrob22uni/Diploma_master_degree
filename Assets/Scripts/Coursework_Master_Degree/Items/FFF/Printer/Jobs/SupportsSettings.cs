using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Slicer-style support choice. The object profile decides whether supports
    /// are needed; the player chooses whether to pay for them.
    /// </summary>
    public enum SupportsMode
    {
        None,
        Auto
    }

    public static class SupportsSettings
    {
        // Reference calibration from the Stanford bunny profile. The support
        // multipliers below are the cost at this overhang share; other objects
        // scale by their own measured overhang percentage.
        public const float ReferenceOverhangPercent = 15.48308f;
        public const float ReferenceFilamentMultiplier = 1.06f;
        public const float ReferenceTimeMultiplier = 1.12f;
        public const int RawQualityMark = 2;
        public const int MissingRequiredPenalty = 25;
        public const int MissingRequiredQualityCap = 55;

        public static float FilamentMultiplierFor(
            float overhangPercent,
            bool supportsRequired)
        {
            return 1.0f + (ReferenceFilamentMultiplier - 1.0f) *
                OverhangRatio(overhangPercent, supportsRequired);
        }

        public static float TimeMultiplierFor(
            float overhangPercent,
            bool supportsRequired)
        {
            return 1.0f + (ReferenceTimeMultiplier - 1.0f) *
                OverhangRatio(overhangPercent, supportsRequired);
        }

        private static float OverhangRatio(
            float overhangPercent,
            bool supportsRequired)
        {
            float overhang = Mathf.Clamp(overhangPercent, 0.0f, 100.0f);

            if (supportsRequired && overhang <= 0.001f)
            {
                overhang = ReferenceOverhangPercent;
            }

            return overhang / ReferenceOverhangPercent;
        }

        public static string Label(SupportsMode mode)
        {
            return mode == SupportsMode.Auto ? "Auto" : "None";
        }
    }
}

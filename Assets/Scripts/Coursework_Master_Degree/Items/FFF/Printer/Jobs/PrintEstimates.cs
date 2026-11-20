using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Deterministic M2 estimates. Pure functions so the console can show the
    /// same numbers before and after validation.
    /// </summary>
    public static class PrintEstimates
    {
        private const float DefaultWallThicknessMillimeters = 1.2f;

        public static float FilamentMeters(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            SupportsMode supports,
            float uniformScale = 1.0f,
            ObjectToPrintGeometry geometry = default)
        {
            if (profile == null)
            {
                return 0.0f;
            }

            float filament = profile.FilamentMetersAt100PercentInfill *
                InfillSettings.Density(infill);

            if (supports == SupportsMode.Auto)
            {
                filament *= SupportsSettings.FilamentMultiplierFor(
                    profile.SupportOverhangPercent,
                    profile.SupportsRequired);
            }

            return filament * ScaleFactor(profile, infill, uniformScale, geometry);
        }

        public static float TimeSeconds(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SupportsMode supports,
            float uniformScale = 1.0f,
            ObjectToPrintGeometry geometry = default)
        {
            if (profile == null)
            {
                return 0.0f;
            }

            float time = profile.PrintTimeSecondsAt100PercentInfill *
                InfillSettings.TimeFactor(infill) *
                LayerHeightSettings.TimeFactor(
                    layerHeight,
                    LayerHeightSettings.ReferenceMillimeters) *
                PrintSpeedSettings.TimeFactor(
                    speed,
                    PrintSpeedSettings.ReferenceMillimetersPerSecond);

            if (supports == SupportsMode.Auto)
            {
                time *= SupportsSettings.TimeMultiplierFor(
                    profile.SupportOverhangPercent,
                    profile.SupportsRequired);
            }

            return time * ScaleFactor(profile, infill, uniformScale, geometry);
        }

        /// <summary>
        /// Two-term shell/infill scaling. At scale 1 this is exactly 1 so the
        /// authored/calibrated profile remains authoritative. Small objects
        /// move toward s² for their solid shell; large objects toward s³ for
        /// their infill volume. Falls back to the old s³ rule when geometry or
        /// wall thickness is unavailable.
        /// </summary>
        private static float ScaleFactor(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            float uniformScale,
            ObjectToPrintGeometry geometry)
        {
            float scale = Mathf.Max(0.0f, uniformScale);
            float volumeScale = scale * scale * scale;

            if (profile == null ||
                !geometry.IsValid ||
                scale <= 0.0001f)
            {
                return volumeScale;
            }

            float wallThicknessMillimeters =
                profile.WallThicknessMillimeters > 0.01f
                    ? profile.WallThicknessMillimeters
                    : DefaultWallThicknessMillimeters;
            float wallThicknessMeters =
                wallThicknessMillimeters / 1000.0f;

            if (wallThicknessMeters <= 0.000001f)
            {
                return volumeScale;
            }

            float totalDensity = Mathf.Clamp01(InfillSettings.Density(infill));
            float solidVolume = geometry.VolumeCubicMeters;
            float shellVolume = Mathf.Min(
                solidVolume,
                geometry.SurfaceAreaSquareMeters * wallThicknessMeters);

            if (totalDensity <= 0.000001f || solidVolume <= 0.000001f)
            {
                return volumeScale;
            }

            // Share of the object's total material at scale 1 that is solid
            // shell rather than infill. The two terms then carry the shell
            // through s² and the core through s³.
            float shellShare = Mathf.Clamp01(
                shellVolume / (solidVolume * totalDensity));

            return shellShare * scale * scale +
                (1.0f - shellShare) * volumeScale;
        }
    }
}

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
        /// Share of the object's material at scale 1 that is solid shell
        /// rather than infill. This is the same value the scale rule uses, so
        /// editor diagnostics and runtime estimates cannot drift apart.
        /// </summary>
        public static float MaterialShellShare(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            ObjectToPrintGeometry geometry)
        {
            if (profile == null)
            {
                return 0.0f;
            }

            float wallThicknessMillimeters =
                profile.WallThicknessMillimeters > 0.01f
                    ? profile.WallThicknessMillimeters
                    : DefaultWallThicknessMillimeters;

            return MaterialShellShare(
                geometry.VolumeCubicMeters,
                geometry.SurfaceAreaSquareMeters,
                wallThicknessMillimeters,
                infill);
        }

        public static float MaterialShellShare(
            float volumeCubicMeters,
            float surfaceAreaSquareMeters,
            float wallThicknessMillimeters,
            InfillPattern infill)
        {
            float density = Mathf.Clamp01(InfillSettings.Density(infill));

            if (volumeCubicMeters <= 0.000001f ||
                surfaceAreaSquareMeters <= 0.000001f ||
                wallThicknessMillimeters <= 0.01f ||
                density <= 0.000001f)
            {
                return 0.0f;
            }

            float shellVolume = Mathf.Min(
                volumeCubicMeters,
                surfaceAreaSquareMeters * wallThicknessMillimeters * 0.001f);

            return Mathf.Clamp01(
                shellVolume / (volumeCubicMeters * density));
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

            float shellShare = MaterialShellShare(profile, infill, geometry);

            return shellShare * scale * scale +
                (1.0f - shellShare) * volumeScale;
        }
    }
}

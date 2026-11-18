using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Deterministic M2 estimates. Pure functions so the console can show the
    /// same numbers before and after validation.
    /// </summary>
    public static class PrintEstimates
    {
        public static float FilamentMeters(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            SupportsMode supports)
        {
            if (profile == null)
            {
                return 0.0f;
            }

            float filament = profile.FilamentMetersAt100PercentInfill *
                InfillSettings.Density(infill);

            return supports == SupportsMode.Auto
                ? filament * SupportsSettings.FilamentMultiplier
                : filament;
        }

        public static float TimeSeconds(
            ObjectToPrintPrintProfile profile,
            InfillPattern infill,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SupportsMode supports)
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

            return supports == SupportsMode.Auto
                ? time * SupportsSettings.TimeMultiplier
                : time;
        }
    }
}

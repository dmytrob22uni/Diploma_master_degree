namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Infill options offered by the console. The effective density and time
    /// factor are deliberately simple and tunable; see InfillSettings.
    /// </summary>
    public enum InfillPattern
    {
        None,
        Grid,
        Gyroid
    }

    /// <summary>
    /// Relative infill coefficients, calibrated against PrusaSlicer using the
    /// Stanford bunny profile. Values are relative to the solid (100% infill)
    /// profile, which has a factor of 1.
    /// </summary>
    public static class InfillSettings
    {
        public static float Density(InfillPattern pattern)
        {
            switch (pattern)
            {
                case InfillPattern.None:
                    return 0.1892f;
                case InfillPattern.Gyroid:
                    return 0.3531f;
                default:
                    return 0.3153f;
            }
        }

        public static float TimeFactor(InfillPattern pattern)
        {
            switch (pattern)
            {
                case InfillPattern.None:
                    return 0.2615f;
                case InfillPattern.Gyroid:
                    return 0.3765f;
                default:
                    return 0.3399f;
            }
        }

        public static string Label(InfillPattern pattern)
        {
            switch (pattern)
            {
                case InfillPattern.None:
                    return "None";
                case InfillPattern.Gyroid:
                    return "Gyroid";
                default:
                    return "Grid";
            }
        }
    }
}

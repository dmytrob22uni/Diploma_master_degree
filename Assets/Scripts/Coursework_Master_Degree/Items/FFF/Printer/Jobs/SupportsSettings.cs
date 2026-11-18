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
        public const float FilamentMultiplier = 1.06f;
        public const float TimeMultiplier = 1.12f;
        public const int RawQualityMark = 2;
        public const int MissingRequiredPenalty = 25;
        public const int MissingRequiredQualityCap = 55;

        public static string Label(SupportsMode mode)
        {
            return mode == SupportsMode.Auto ? "Auto" : "None";
        }
    }
}

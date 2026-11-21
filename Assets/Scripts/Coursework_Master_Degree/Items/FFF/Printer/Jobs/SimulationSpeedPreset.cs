namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Player-facing simulation speed for a printer. The multiplier only
    /// changes how fast simulated time advances; estimates and wear remain in
    /// simulated units.
    /// </summary>
    public enum SimulationSpeedPreset
    {
        RealTime,
        Fast,
        Quick,
        Turbo,
        Hyper
    }

    public static class SimulationSpeedSettings
    {
        public const float DefaultMultiplier = 1.0f;
        public const SimulationSpeedPreset DefaultPreset =
            SimulationSpeedPreset.RealTime;

        public static float Multiplier(SimulationSpeedPreset preset)
        {
            switch (preset)
            {
                case SimulationSpeedPreset.RealTime:
                    return 1.0f;
                case SimulationSpeedPreset.Fast:
                    return 4.0f;
                case SimulationSpeedPreset.Quick:
                    return 64.0f;
                case SimulationSpeedPreset.Turbo:
                    return 120.0f;
                default:
                    return 240.0f;
            }
        }

        public static string Label(SimulationSpeedPreset preset)
        {
            return $"{Multiplier(preset):0}x";
        }

        public static SimulationSpeedPreset Closest(float multiplier)
        {
            SimulationSpeedPreset best = DefaultPreset;
            float bestDelta = float.MaxValue;

            foreach (SimulationSpeedPreset preset in System.Enum.GetValues(
                typeof(SimulationSpeedPreset)))
            {
                float delta = UnityEngine.Mathf.Abs(
                    Multiplier(preset) - multiplier);

                if (delta < bestDelta)
                {
                    best = preset;
                    bestDelta = delta;
                }
            }

            return best;
        }
    }
}

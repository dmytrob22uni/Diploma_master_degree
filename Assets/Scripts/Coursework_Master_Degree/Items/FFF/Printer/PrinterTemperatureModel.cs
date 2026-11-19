using System;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.FFF
{
    [Serializable]
    public sealed class PrinterTemperatureState
    {
        public float Ambient = 21.0f;
        public float Chamber = 21.0f;
        public float Nozzle = 21.0f;
        public float Bed = 21.0f;

        public float NozzleTarget = 21.0f;
        public float BedTarget = 21.0f;
        public float ChamberTarget = 21.0f;

        public float NormalizedNozzleTemperature =>
            NozzleTarget > 0.001f
                ? Mathf.Clamp01(Nozzle / NozzleTarget)
                : 0.0f;

        public float NormalizedBedTemperature =>
            BedTarget > 0.001f
                ? Mathf.Clamp01(Bed / BedTarget)
                : 0.0f;

        public void Reset(float ambient)
        {
            Ambient = ambient;
            Chamber = ambient;
            Nozzle = ambient;
            Bed = ambient;
            NozzleTarget = ambient;
            BedTarget = ambient;
            ChamberTarget = ambient;
        }
    }

    public static class FilamentMaterialDefaults
    {
        public static void Targets(
            ItemType material,
            FilamentMaterialProfileSO profile,
            out float nozzle,
            out float bed,
            out float chamber)
        {
            if (profile != null)
            {
                nozzle = profile.NozzleTemperature;
                bed = profile.BedTemperature;
                chamber = profile.RecommendedChamberTemperature;
                return;
            }

            switch (material)
            {
                case ItemType.PETG:
                    nozzle = 240.0f;
                    bed = 80.0f;
                    chamber = 30.0f;
                    return;
                case ItemType.ABS:
                    nozzle = 260.0f;
                    bed = 100.0f;
                    chamber = 40.0f;
                    return;
                case ItemType.TPU:
                    nozzle = 230.0f;
                    bed = 50.0f;
                    chamber = 25.0f;
                    return;
                default:
                    nozzle = 215.0f;
                    bed = 60.0f;
                    chamber = 25.0f;
                    return;
            }
        }
    }

    /// <summary>
    /// Simulated thermal model. Heating is linear toward the material targets;
    /// cooling is exponential toward ambient. An enclosure slows cooling and
    /// lets the chamber hold a stable temperature.
    /// </summary>
    [Serializable]
    public sealed class PrinterTemperatureModel
    {
        [Min(0.001f)]
        [Tooltip("Nozzle heating rate in degrees Celsius per simulated second.")]
        public float NozzleHeatRateCPerSecond = 0.9f;

        [Min(0.001f)]
        [Tooltip("Bed heating rate in degrees Celsius per simulated second.")]
        public float BedHeatRateCPerSecond = 0.45f;

        [Min(0.001f)]
        [Tooltip("Exponential cooling coefficient per simulated second.")]
        public float CoolingRatePerSecond = 0.018f;

        [Range(0.05f, 1.0f)]
        [Tooltip("Cooling speed multiplier when an enclosure is present.")]
        public float EnclosureCoolingFactor = 0.35f;

        [Min(0.0f)]
        [Tooltip("How quickly the enclosure chamber approaches its target.")]
        public float ChamberRateCPerSecond = 0.08f;

        public PrinterTemperatureState State = new PrinterTemperatureState();

        public void Initialize(PrinterEnvironment environment)
        {
            if (State == null)
            {
                State = new PrinterTemperatureState();
            }

            float ambient = environment != null
                ? environment.AmbientTemperature
                : 21.0f;

            State.Reset(Mathf.Max(0.0f, ambient));
        }

        public void SetTargets(float nozzle, float bed, float chamber)
        {
            State.NozzleTarget = Mathf.Max(0.0f, nozzle);
            State.BedTarget = Mathf.Max(0.0f, bed);
            State.ChamberTarget = Mathf.Max(0.0f, chamber);
        }

        public void Tick(
            float simulatedDelta,
            PrinterEnvironment environment,
            bool heatersOn,
            bool activePrint)
        {
            if (simulatedDelta <= 0.0f)
            {
                return;
            }

            if (State == null)
            {
                State = new PrinterTemperatureState();
                State.Reset(environment != null
                    ? environment.AmbientTemperature
                    : 21.0f);
            }

            float ambient = environment != null
                ? Mathf.Max(0.0f, environment.AmbientTemperature)
                : 21.0f;
            bool hasEnclosure = environment != null &&
                environment.HasEnclosure;

            State.Ambient = ambient;

            if (hasEnclosure)
            {
                float chamberTarget = activePrint && heatersOn
                    ? Mathf.Max(ambient, State.ChamberTarget)
                    : ambient;

                State.Chamber = Mathf.MoveTowards(
                    State.Chamber,
                    chamberTarget,
                    Mathf.Max(0.0f, ChamberRateCPerSecond) * simulatedDelta);
            }
            else
            {
                State.Chamber = ambient;
            }

            float coolingAmbient = hasEnclosure
                ? State.Chamber
                : ambient;

            if (heatersOn)
            {
                State.Nozzle = Mathf.MoveTowards(
                    State.Nozzle,
                    State.NozzleTarget,
                    Mathf.Max(0.001f, NozzleHeatRateCPerSecond) *
                    simulatedDelta);

                State.Bed = Mathf.MoveTowards(
                    State.Bed,
                    State.BedTarget,
                    Mathf.Max(0.001f, BedHeatRateCPerSecond) *
                    simulatedDelta);
            }
            else
            {
                float coolingRate = Mathf.Max(0.001f, CoolingRatePerSecond);

                if (hasEnclosure)
                {
                    coolingRate *= Mathf.Clamp01(EnclosureCoolingFactor);
                }

                State.Nozzle = CoolToward(
                    State.Nozzle,
                    coolingAmbient,
                    coolingRate,
                    simulatedDelta);
                State.Bed = CoolToward(
                    State.Bed,
                    coolingAmbient,
                    coolingRate,
                    simulatedDelta);
            }

            State.Nozzle = Sanitize(State.Nozzle);
            State.Bed = Sanitize(State.Bed);
            State.Chamber = Sanitize(State.Chamber);
        }

        public bool IsAtTarget(float tolerance)
        {
            float safeTolerance = Mathf.Max(0.0f, tolerance);

            return Mathf.Abs(State.Nozzle - State.NozzleTarget) <= safeTolerance &&
                Mathf.Abs(State.Bed - State.BedTarget) <= safeTolerance;
        }

        public bool IsBelow(float temperature)
        {
            return State.Nozzle <= temperature && State.Bed <= temperature;
        }

        public float NormalizedNozzleTemperature =>
            State.NozzleTarget > 0.001f
                ? Mathf.Clamp01(State.Nozzle / State.NozzleTarget)
                : 0.0f;

        public float NormalizedBedTemperature =>
            State.BedTarget > 0.001f
                ? Mathf.Clamp01(State.Bed / State.BedTarget)
                : 0.0f;

        private static float CoolToward(
            float current,
            float ambient,
            float rate,
            float delta)
        {
            float factor = 1.0f - Mathf.Exp(-rate * delta);
            return Mathf.Lerp(current, ambient, factor);
        }

        private static float Sanitize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 21.0f;
            }

            return Mathf.Clamp(value, 0.0f, 400.0f);
        }
    }
}

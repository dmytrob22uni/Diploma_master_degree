using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;
using Coursework_Master_Degree.ScriptableObjects.Items.Wear;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Applies simulated wear to every installed printer/enclosure part.
    /// The delta type is selected from the part's authored wear logic, so a
    /// part receives the same kind of wear it was designed for.
    /// </summary>
    public sealed class PrinterWearIntegrator
    {
        private readonly List<ItemWearState> targets =
            new List<ItemWearState>();

        public void Refresh(Printer printer)
        {
            targets.Clear();

            if (printer == null)
            {
                return;
            }

            AddTargets(printer.Assembly != null
                ? printer.Assembly.gameObject
                : null);

            if (printer.Environment != null &&
                printer.Environment.Enclosure != null &&
                (printer.Assembly == null ||
                 printer.Environment.Enclosure.gameObject !=
                 printer.Assembly.gameObject))
            {
                AddTargets(printer.Environment.Enclosure.gameObject);
            }
        }

        private void AddTargets(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            ItemWearState[] found =
                root.GetComponentsInChildren<ItemWearState>(true);

            for (int i = 0; i < found.Length; i++)
            {
                ItemWearState wear = found[i];

                if (wear == null ||
                    wear.GetComponentInParent<ItemFFFSpoolDataHolder>(true) != null ||
                    targets.Contains(wear))
                {
                    continue;
                }

                targets.Add(wear);
            }
        }

        public void ApplyPrintWear(
            Printer printer,
            float simulatedSeconds,
            float filamentMeters,
            float materialHardness,
            float materialAbrasiveness)
        {
            if (printer == null || simulatedSeconds <= 0.0f)
            {
                return;
            }

            PrinterTemperatureState temperature = printer.Temperature;

            for (int i = 0; i < targets.Count; i++)
            {
                ItemWearState wear = targets[i];

                if (wear == null || wear.IsWornOut)
                {
                    continue;
                }

                ItemWearLogicSO logic = wear.ItemWearLogicSO;

                if (logic is ItemWearLogicFilamentTemperatureAbrasivenessSO)
                {
                    if (filamentMeters <= 0.0f)
                    {
                        continue;
                    }

                    wear.ApplyWear(
                        new WearDeltaFilamentTemperatureAbrasiveness
                        {
                            FilamentLength = filamentMeters,
                            Temperature = PartTemperature(
                                wear,
                                temperature,
                                TemperatureKind.Nozzle),
                            MaterialAbrasiveness = materialAbrasiveness,
                        });
                }
                else if (logic is ItemWearLogicTimeTemperatureSO)
                {
                    wear.ApplyWear(
                        new WearDeltaTimeTemperature
                        {
                            DeltaTime = simulatedSeconds,
                            Temperature = PartTemperature(
                                wear,
                                temperature,
                                TemperatureKind.Bed),
                        });
                }
                else if (logic is ItemWearLogicDistanceTemperatureSO)
                {
                    wear.ApplyWear(
                        new WearDeltaDistanceTemperature
                        {
                            DistanceTravelled = simulatedSeconds *
                                Mathf.Max(
                                    0.0f,
                                    printer.AxisTravelMetersPerSecond),
                            Temperature = temperature.Chamber,
                        });
                }
                else if (logic is ItemWearLogicTimeRpmSO)
                {
                    wear.ApplyWear(
                        new WearDeltaTimeRPM
                        {
                            DeltaTime = simulatedSeconds,
                            RPM = Mathf.Max(0.0f, printer.FanRPM),
                        });
                }
                else if (logic is ItemWearLogicTimeFumesSO)
                {
                    wear.ApplyWear(
                        new WearDeltaTimeFumes
                        {
                            DeltaTime = simulatedSeconds,
                            FumesDensity = Mathf.Max(
                                0.0f,
                                printer.FumesDensity),
                        });
                }
                else if (logic is ItemWearLogicTimeSO)
                {
                    wear.ApplyWear(new WearDeltaTime
                    {
                        DeltaTime = simulatedSeconds,
                    });
                }
            }
        }

        public void ApplyCompletedPrintUse(
            Printer printer,
            float materialHardness)
        {
            if (printer == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                ItemWearState wear = targets[i];

                if (wear == null || wear.IsWornOut)
                {
                    continue;
                }

                ItemWearLogicSO logic = wear.ItemWearLogicSO;

                if (logic is ItemWearLogicUsesSO)
                {
                    wear.ApplyWear(new WearDeltaUses { Uses = 1.0f });
                }
                else if (logic is ItemWearLogicUsesHardnessSO)
                {
                    wear.ApplyWear(
                        new WearDeltaUsesHardness
                        {
                            Uses = 1.0f,
                            MaterialHardness = Mathf.Clamp01(
                                materialHardness),
                        });
                }
            }
        }

        public void ApplyHardStopPenalty(
            Printer printer,
            float nozzleBase,
            float nozzleScale,
            float heaterBase,
            float heaterScale,
            float bedBase,
            float bedScale)
        {
            if (printer == null)
            {
                return;
            }

            PrinterTemperatureState temperature = printer.Temperature;

            ApplyConditionLoss(
                printer,
                "extruder_nozzle_location",
                nozzleBase +
                nozzleScale * temperature.NormalizedNozzleTemperature);

            ApplyConditionLoss(
                printer,
                "extruder_filament_heater_location",
                heaterBase +
                heaterScale * temperature.NormalizedNozzleTemperature);

            ApplyConditionLoss(
                printer,
                "print_bed_location",
                bedBase +
                bedScale * temperature.NormalizedBedTemperature);

            float plateLoss = bedBase +
                bedScale * temperature.NormalizedBedTemperature;

            if (printer.PrintPlateLocation != null &&
                printer.PrintPlateLocation.InstalledPart != null)
            {
                ItemWearState plateWear =
                    printer.PrintPlateLocation.InstalledPart
                        .GetComponentInChildren<ItemWearState>(true);

                plateWear?.ApplyConditionLoss(plateLoss);
            }
        }

        private static void ApplyConditionLoss(
            Printer printer,
            string locationName,
            float amount)
        {
            if (amount <= 0.0f)
            {
                return;
            }

            PartLocation[] locations =
                printer.Assembly != null
                    ? printer.Assembly.GetComponentsInChildren<PartLocation>(true)
                    : null;

            if (locations == null)
            {
                return;
            }

            for (int i = 0; i < locations.Length; i++)
            {
                PartLocation location = locations[i];

                if (location == null ||
                    location.gameObject.name != locationName ||
                    location.InstalledPart == null)
                {
                    continue;
                }

                ItemWearState wear =
                    location.InstalledPart.GetComponentInChildren<ItemWearState>(
                        true);

                wear?.ApplyConditionLoss(amount);
                return;
            }
        }

        private enum TemperatureKind
        {
            Nozzle,
            Bed,
        }

        private static float PartTemperature(
            ItemWearState wear,
            PrinterTemperatureState temperature,
            TemperatureKind fallback)
        {
            if (wear.GetComponentInParent<PrinterEnclosure>() != null)
            {
                return temperature.Chamber;
            }

            string name = wear.gameObject.name.ToLowerInvariant();

            if (name.Contains("nozzle") ||
                name.Contains("extruder") ||
                name.Contains("heater"))
            {
                return temperature.Nozzle;
            }

            if (name.Contains("bed") || name.Contains("plate"))
            {
                return temperature.Bed;
            }

            if (name.Contains("enclosure") ||
                name.Contains("fire") ||
                name.Contains("thermometer"))
            {
                return temperature.Chamber;
            }

            return fallback == TemperatureKind.Nozzle
                ? temperature.Nozzle
                : temperature.Bed;
        }
    }
}

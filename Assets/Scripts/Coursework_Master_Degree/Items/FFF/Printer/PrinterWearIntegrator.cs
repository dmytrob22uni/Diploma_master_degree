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
                            Temperature = temperature.Nozzle,
                            MaterialAbrasiveness = materialAbrasiveness,
                        });
                }
                else if (logic is ItemWearLogicTimeTemperatureSO
                    timeTemperatureLogic)
                {
                    wear.ApplyWear(
                        new WearDeltaTimeTemperature
                        {
                            DeltaTime = simulatedSeconds,
                            Temperature = PartTemperature(
                                wear,
                                timeTemperatureLogic.TemperatureSource,
                                temperature),
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
                printer.NozzleWearLocation,
                nozzleBase +
                nozzleScale * temperature.NormalizedNozzleTemperature);

            ApplyConditionLoss(
                printer.HeaterWearLocation,
                heaterBase +
                heaterScale * temperature.NormalizedNozzleTemperature);

            float bedLoss = bedBase +
                bedScale * temperature.NormalizedBedTemperature;

            ApplyConditionLoss(printer.PrintBedWearLocation, bedLoss);
            ApplyConditionLoss(printer.PrintPlateLocation, bedLoss);
        }

        private static void ApplyConditionLoss(
            PartLocation location,
            float amount)
        {
            if (location == null ||
                location.InstalledPart == null ||
                amount <= 0.0f)
            {
                return;
            }

            ItemWearState wear =
                location.InstalledPart.GetComponentInChildren<ItemWearState>(
                    true);

            wear?.ApplyConditionLoss(amount);
        }

        private static float PartTemperature(
            ItemWearState wear,
            WearTemperatureSource source,
            PrinterTemperatureState temperature)
        {
            switch (source)
            {
                case WearTemperatureSource.Nozzle:
                    return temperature.Nozzle;
                case WearTemperatureSource.Bed:
                    return temperature.Bed;
                case WearTemperatureSource.Chamber:
                    return temperature.Chamber;
                default:
                    return wear.GetComponentInParent<PrinterEnclosure>() != null
                        ? temperature.Chamber
                        : temperature.Bed;
            }
        }
    }
}

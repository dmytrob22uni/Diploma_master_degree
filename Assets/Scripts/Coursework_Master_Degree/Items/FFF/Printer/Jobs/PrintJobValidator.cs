using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// M2 job rules. Pure checks against the current printer hardware and
    /// catalog profile; no state is changed here.
    /// </summary>
    public static class PrintJobValidator
    {
        public const float WornOutConditionPercent = 0.0f;
        public const float LowConditionWarningPercent = 20.0f;
        public const float CriticalPartWarningPercent = 40.0f;

        public static PrintJobValidationResult Validate(
            Device device,
            Printer printer,
            ObjectToPrintMetaSO objectToPrint,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SpoolSlotId spoolSlotId,
            PrintPlacement placement = null)
        {
            PrintJobValidationResult result = new PrintJobValidationResult();

            if (objectToPrint == null)
            {
                result.Errors.Add("Select an object to print.");
                return result;
            }

            ObjectToPrintPrintProfile profile = objectToPrint.PrintProfile;

            if (profile == null)
            {
                result.Errors.Add("This object has no print profile.");
                return result;
            }

            float uniformScale = placement != null
                ? placement.UniformScale
                : 1.0f;

            ObjectToPrintGeometry geometry =
                ObjectToPrintSizing.GetGeometry(objectToPrint);

            result.EstimatedSeconds = PrintEstimates.TimeSeconds(
                profile,
                infill,
                layerHeight,
                speed,
                supports,
                uniformScale,
                geometry);
            result.EstimatedFilamentMeters =
                PrintEstimates.FilamentMeters(
                    profile,
                    infill,
                    supports,
                    uniformScale,
                    geometry);

            if (placement == null)
            {
                result.Errors.Add("Object is not placed on the bed.");
            }
            else if (!placement.IsValid || placement.UniformScale <= 0.0f)
            {
                result.Errors.Add("Object placement is invalid.");
            }

            float hardwarePenalty = printer != null
                ? HardwareQualityPenalty(printer)
                : 0.0f;

            bool hasEnclosure = printer != null && printer.HasEnclosure;

            PrintQualityEstimator.Calculate(
                profile,
                layerHeight,
                speed,
                supports,
                profile.SupportsRequired,
                hardwarePenalty,
                out result.QualityMinBase,
                out result.QualityMaxBase,
                out result.QualityCap);

            result.EstimatedQualityMin = PrintQualityEstimator.ApplyEnclosure(
                result.QualityMinBase,
                result.QualityCap,
                hasEnclosure);
            result.EstimatedQualityMax = PrintQualityEstimator.ApplyEnclosure(
                result.QualityMaxBase,
                result.QualityCap,
                hasEnclosure);

            if (profile.PrintTimeSecondsAt100PercentInfill <= 0.0f ||
                profile.FilamentMetersAt100PercentInfill <= 0.0f)
            {
                result.Warnings.Add(
                    "Print profile is not calibrated; estimates are placeholders.");
            }

            if (profile.SupportsRequired && supports == SupportsMode.None)
            {
                result.Warnings.Add(
                    "This object needs supports; overhangs will be rough.");
            }
            else if (!profile.SupportsRequired && supports == SupportsMode.Auto)
            {
                result.Warnings.Add("Supports are not mandatory here.");
            }

            if (printer == null)
            {
                result.Errors.Add("No printer selected.");
                return result;
            }

            if (!printer.IsCalibrated)
            {
                result.Warnings.Add(
                    "Printer is not calibrated; final quality may be 70–100% of the estimate.");
            }

            if (device != null && printer.BoundDevice != device)
            {
                result.Errors.Add("Printer is not connected to this device.");
            }

            if (printer.OperationalState != PrinterOperationalState.Idle)
            {
                result.Errors.Add(
                    $"Printer is busy ({printer.OperationalState}).");
            }

            if (printer.HasPendingJob)
            {
                result.Errors.Add("A job is already queued on this printer.");
            }

            if (printer.HasPartialPrint)
            {
                result.Errors.Add("A partial print is still on the bed.");
            }

            PrinterEnclosure enclosure = printer.DockedEnclosure;

            if (enclosure != null && !enclosure.AreDoorsClosed)
            {
                result.Errors.Add(enclosure.DoorWarning);
            }

            CheckParts(printer, result);

            if (printer.PrintPlateLocation == null ||
                !printer.PrintPlateLocation.IsOccupied ||
                printer.PrintPlateLocation.InstalledPart == null)
            {
                result.Errors.Add("Print plate is missing.");
            }

            CheckSpool(printer, spoolSlotId, speed, result);
            CheckBuildVolume(printer, objectToPrint, result);

            return result;
        }

        private static void CheckParts(
            Printer printer,
            PrintJobValidationResult result)
        {
            if (printer.Assembly == null)
            {
                result.Errors.Add("Printer assembly is missing.");
                return;
            }

            CheckAssemblyParts(
                printer.Assembly,
                printer.CriticalPartLocations,
                result);

            PrinterEnclosure enclosure = printer.DockedEnclosure;
            PartsAssembly enclosureAssembly = enclosure != null
                ? enclosure.GetComponent<PartsAssembly>()
                : null;

            if (enclosureAssembly != null)
            {
                CheckAssemblyParts(
                    enclosureAssembly,
                    enclosure.CriticalPartLocations,
                    result);
            }
        }

        private static void CheckAssemblyParts(
            PartsAssembly assembly,
            PartLocation[] criticalLocations,
            PrintJobValidationResult result)
        {
            criticalLocations ??= System.Array.Empty<PartLocation>();
            bool[] criticalFound = criticalLocations.Length > 0
                ? new bool[criticalLocations.Length]
                : System.Array.Empty<bool>();

            foreach (PartLocation location in PartLocations(assembly))
            {
                if (location == null)
                {
                    continue;
                }

                string locationName = location.gameObject.name;
                int criticalIndex = IndexOf(criticalLocations, location);

                if (criticalIndex >= 0)
                {
                    criticalFound[criticalIndex] = true;
                }

                if (!location.IsOccupied || location.InstalledPart == null)
                {
                    if (criticalIndex >= 0)
                    {
                        result.Errors.Add(
                            $"Missing critical part: {Friendly(locationName)}.");
                    }

                    continue;
                }

                // The installed printer is the job target, not a scanned
                // consumable part. Its own assembly is checked separately.
                if (location.InstalledPart
                        .GetComponentInChildren<Printer>(true) != null)
                {
                    continue;
                }

                // Spools are consumables; only the selected spool is checked
                // by CheckSpool. An unselected empty/worn spool must not block
                // the job.
                if (location.InstalledPart
                        .GetComponentInChildren<ItemFFFSpoolDataHolder>(true) != null)
                {
                    continue;
                }

                ItemDataHolder item =
                    location.InstalledPart.GetComponentInChildren<ItemDataHolder>(true);
                string name = item != null &&
                    item.ItemMetaData != null &&
                    !string.IsNullOrWhiteSpace(item.ItemMetaData.Name)
                    ? item.ItemMetaData.Name
                    : Friendly(locationName);

                ItemWearState wear =
                    location.InstalledPart.GetComponentInChildren<ItemWearState>(true);

                if (wear == null)
                {
                    continue;
                }

                float condition = Mathf.Clamp(
                    wear.CurrentCondition,
                    0.0f,
                    wear.MaxCondition);

                if (wear.IsWornOut || condition <= WornOutConditionPercent)
                {
                    result.Errors.Add(
                        $"Worn out: {name} ({locationName}, {condition:0}%).");
                }
                else if (condition <= LowConditionWarningPercent)
                {
                    result.Warnings.Add(
                        $"Low condition: {name} ({locationName}, {condition:0}%).");
                }
                else if (criticalIndex >= 0 &&
                    condition <= CriticalPartWarningPercent)
                {
                    result.Warnings.Add(
                        $"Critical part condition: {name} " +
                        $"({locationName}, {condition:0}%).");
                }
            }

            for (int i = 0; i < criticalLocations.Length; i++)
            {
                // Empty list entries are authored placeholders, not missing
                // parts. A genuinely unassigned reference is reported by the
                // editor configuration check instead of blocking every job.
                if (criticalLocations[i] == null)
                {
                    continue;
                }

                if (!criticalFound[i])
                {
                    result.Errors.Add(
                        $"Missing critical part location: " +
                        $"{Friendly(criticalLocations[i].gameObject.name)}.");
                }
            }
        }

        private static int IndexOf(PartLocation[] locations, PartLocation value)
        {
            if (locations == null)
            {
                return -1;
            }

            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void CheckSpool(
            Printer printer,
            SpoolSlotId spoolSlotId,
            PrintSpeedPreset speed,
            PrintJobValidationResult result)
        {
            if (printer.SpoolSlotCount == 0)
            {
                result.Errors.Add("No spool slots are configured on this printer.");
                return;
            }

            SpoolSlotDefinition slot = printer.GetSpoolSlot(spoolSlotId);

            if (slot == null)
            {
                result.Errors.Add(
                    !spoolSlotId.IsValid
                        ? "No spool slot is selected."
                        : $"Spool slot '{spoolSlotId.Value}' is not configured.");
                return;
            }

            string side = slot.DisplayLabel;
            PartLocation location = printer.GetSpoolLocation(slot);

            if (location == null)
            {
                result.Errors.Add($"Spool slot is not configured ({side}).");
                return;
            }

            if (!location.IsOccupied || location.InstalledPart == null)
            {
                result.Errors.Add(
                    $"No filament spool installed on the {side} side.");
                return;
            }

            ItemFFFSpoolDataHolder spool =
                location.InstalledPart.GetComponentInChildren<
                    ItemFFFSpoolDataHolder>(true);

            if (spool == null)
            {
                result.Errors.Add(
                    $"The {side} spool slot does not contain a filament spool.");
                return;
            }

            ItemWearState wear =
                spool.GetComponentInChildren<ItemWearState>(true);

            float condition = wear != null
                ? Mathf.Clamp(wear.CurrentCondition, 0.0f, wear.MaxCondition)
                : 100.0f;

            float remainingMeters =
                spool.FilamentLengthSpoolMeters * (condition / 100.0f);

            if (remainingMeters <= 0.001f)
            {
                result.Errors.Add($"The {side} filament spool is empty.");
            }
            else if (result.EstimatedFilamentMeters > remainingMeters)
            {
                result.Errors.Add(
                    $"Not enough filament on the {side} spool: " +
                    $"{result.EstimatedFilamentMeters:0.0} m needed, " +
                    $"{remainingMeters:0.0} m left.");
            }
            else if (condition > 0.0f &&
                result.EstimatedFilamentMeters > 0.0f &&
                remainingMeters < result.EstimatedFilamentMeters * 1.10f)
            {
                result.Warnings.Add(
                    $"Low filament on the {side} spool: only {remainingMeters:0.0} m left.");
            }
            else if (condition <= LowConditionWarningPercent)
            {
                result.Warnings.Add(
                    $"Low condition on the {side} spool ({condition:0}%).");
            }

            if (speed == PrintSpeedPreset.Fast &&
                (spool.MaterialType == ItemType.ABS ||
                 spool.MaterialType == ItemType.TPU))
            {
                result.Warnings.Add(
                    $"Fast speed may reduce quality with {Label(spool.MaterialType)}.");
            }
        }

        private static float HardwareQualityPenalty(Printer printer)
        {
            if (printer.Assembly == null)
            {
                return 0.0f;
            }

            float worst = 0.0f;

            foreach (PartLocation location in PartLocations(printer))
            {
                if (location == null ||
                    !location.IsOccupied ||
                    location.InstalledPart == null)
                {
                    continue;
                }

                if (location.InstalledPart
                        .GetComponentInChildren<Printer>(true) != null)
                {
                    continue;
                }

                if (location.InstalledPart
                        .GetComponentInChildren<ItemFFFSpoolDataHolder>(true) != null)
                {
                    continue;
                }

                ItemWearState wear =
                    location.InstalledPart.GetComponentInChildren<ItemWearState>(true);

                if (wear == null)
                {
                    continue;
                }

                float condition = Mathf.Clamp(
                    wear.CurrentCondition,
                    0.0f,
                    wear.MaxCondition);
                float penalty = (1.0f - condition / 100.0f) * 10.0f;
                worst = Mathf.Max(worst, penalty);
            }

            return worst;
        }

        private static void CheckBuildVolume(
            Printer printer,
            ObjectToPrintMetaSO objectToPrint,
            PrintJobValidationResult result)
        {
            if (printer.Motion == null)
            {
                result.Errors.Add("Printer motion is missing.");
                return;
            }

            Vector3 volume = printer.Motion.BuildVolume;

            if (volume.x <= 0.0f || volume.y <= 0.0f || volume.z <= 0.0f)
            {
                result.Warnings.Add("Build volume is not configured.");
                return;
            }

            Vector3 size = ObjectToPrintSizing.GetSizeMeters(objectToPrint);

            if (size.x <= 0.0f || size.y <= 0.0f || size.z <= 0.0f)
            {
                result.Errors.Add("Object prefab has no renderable bounds.");
                return;
            }

            if (size.x > volume.x || size.y > volume.y || size.z > volume.z)
            {
                result.Errors.Add(
                    $"Object does not fit: {size.x:0.00} x {size.y:0.00} x " +
                    $"{size.z:0.00} m, build volume " +
                    $"{volume.x:0.00} x {volume.y:0.00} x {volume.z:0.00} m.");
            }
        }

        private static IEnumerable<PartLocation> PartLocations(Printer printer)
        {
            foreach (PartLocation location in PartLocations(printer.Assembly))
            {
                yield return location;
            }

            PrinterEnclosure enclosure = printer.DockedEnclosure;
            PartsAssembly enclosureAssembly = enclosure != null
                ? enclosure.GetComponent<PartsAssembly>()
                : null;

            if (enclosureAssembly == null ||
                enclosureAssembly == printer.Assembly)
            {
                yield break;
            }

            foreach (PartLocation location in PartLocations(enclosureAssembly))
            {
                yield return location;
            }
        }

        private static IEnumerable<PartLocation> PartLocations(
            PartsAssembly assembly)
        {
            if (assembly == null)
            {
                yield break;
            }

            if (assembly.RegularPartLocationsList != null)
            {
                foreach (PartLocation location in assembly.RegularPartLocationsList)
                {
                    yield return location;
                }
            }

            if (assembly.OptionalPartLocationsList == null)
            {
                yield break;
            }

            foreach (OptionalPart optional in assembly.OptionalPartLocationsList)
            {
                if (optional?.PartLocationsList == null)
                {
                    continue;
                }

                foreach (PartLocation location in optional.PartLocationsList)
                {
                    yield return location;
                }
            }
        }

        private static string Friendly(string locationName)
        {
            switch (locationName)
            {
                case "extruder_nozzle_location":
                    return "Extruder nozzle";
                case "extruder_filament_heater_location":
                    return "Extruder heater";
                case "extruder_thermistor_location":
                    return "Extruder thermistor";
                case "print_bed_location":
                    return "Print bed";
                case "print_bed_thermistor_location":
                    return "Print bed thermistor";
                case "printer_print_plate_location":
                    return "Print plate";
                case "battery_closet_location":
                    return "Battery closet";
                case "battery_closet_adapter_location":
                    return "Battery closet adapter";
                case "thermometer_location":
                    return "Thermometer";
                case "HEPA_filter_fan_left_location":
                    return "HEPA filter fan (left)";
                case "HEPA_filter_fan_right_location":
                    return "HEPA filter fan (right)";
                case "HEPA_filter_trap_location":
                    return "HEPA filter trap";
                case "fire_suppression_tube_1_location":
                    return "Fire suppression tube 1";
                case "fire_suppression_tube_2_location":
                    return "Fire suppression tube 2";
                case "fire_suppression_tube_3_location":
                    return "Fire suppression tube 3";
                default:
                    return locationName.Replace('_', ' ');
            }
        }

        private static string Label(ItemType type)
        {
            switch (type)
            {
                case ItemType.PrintPlate:
                    return "Print plate";
                case ItemType.PrintBed:
                    return "Print bed";
                case ItemType.PrintBedThermistor:
                    return "Print bed thermistor";
                case ItemType.ExtruderNozzle:
                    return "Extruder nozzle";
                case ItemType.ExtruderFilamentHeater:
                    return "Extruder heater";
                case ItemType.ExtrderThermistor:
                    return "Extruder thermistor";
                case ItemType.PLA:
                    return "PLA";
                case ItemType.PETG:
                    return "PETG";
                case ItemType.ABS:
                    return "ABS";
                case ItemType.TPU:
                    return "TPU";
                default:
                    return type.ToString();
            }
        }
    }
}

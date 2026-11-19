using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.Items.Part;
using System.Collections.Generic;

namespace Coursework_Master_Degree.Items.FFF
{
    public enum PrinterOperationalState
    {
        Idle,
        Preheating,
        Printing,
        Paused,
        Cooling,
        Finished,
        Maintenance,
        Fault
    }

    /// <summary>
    /// Printer root. Owns the hardware references used for jobs, motion, wear,
    /// and binding. M1 adds the control relationship with a computing device;
    /// the printer keeps ownership of its job and state.
    /// </summary>
    [DisallowMultipleComponent]
    public class Printer : MonoBehaviour
    {
        [Header("Hardware")]
        public PartsAssembly Assembly;
        public ItemDataHolder ItemData;

        [Header("Motion and bed")]
        public PrinterMotionBase Motion;
        public PrintBedAnchor BedAnchor;

        [Header("Environment")]
        public PrinterEnvironment Environment;

        [Header("Binding visuals")]
        public PrinterBindHighlight BindHighlight;

        [Header("Job requirements")]
        [Tooltip(
            "Part locations that must be occupied before printing and that get " +
            "an earlier wear warning. Use references, not names.")]
        public PartLocation[] CriticalPartLocations;

        [Tooltip("Part location that holds the removable print plate.")]
        public PartLocation PrintPlateLocation;

        [Header("Spool slots")]
        [Tooltip("Part location for the left spool (spool_location_1).")]
        public PartLocation LeftSpoolLocation;

        [Tooltip("Part location for the right spool (spool_location_2).")]
        public PartLocation RightSpoolLocation;

        private static readonly string[] DefaultCriticalLocationNames =
        {
            "extruder_nozzle_location",
            "extruder_filament_heater_location",
            "extruder_thermistor_location",
            "extruder_fan_location",
            "print_bed_location",
            "print_bed_thermistor_location",
            "x_axis_belt_location",
            "y_axis_belt_location",
            "x_axis_rotor_location",
            "y_axis_rotor_location",
            "z_axis_rotor_left_location",
            "z_axis_rotor_right_location",
            "extruder_filament_bearing_location"
        };

        /// <summary>
        /// M2 stores a validated request here. M3 starts the job from it.
        /// </summary>
        public PrintJobRequest PendingJob { get; private set; }

        public bool HasPendingJob => PendingJob != null;

        /// <summary>
        /// Placeholder for M3. A hard stop will leave a partial print on the bed.
        /// </summary>
        public bool HasPartialPrint { get; set; }

        /// <summary>
        /// Device currently controlling this printer. A printer has at most one
        /// controller. Removing the binding never changes the printer state.
        /// </summary>
        public Device BoundDevice { get; private set; }

        public bool IsBound => BoundDevice != null;

        /// <summary>
        /// Placeholder until the printer FSM arrives in M3. Kept on the printer
        /// so the console can bind to a live status source now.
        /// </summary>
        public PrinterOperationalState OperationalState { get; private set; } =
            PrinterOperationalState.Idle;

        public string DisplayName
        {
            get
            {
                if (ItemData != null &&
                    ItemData.ItemMetaData != null &&
                    !string.IsNullOrWhiteSpace(ItemData.ItemMetaData.Name))
                {
                    return ItemData.ItemMetaData.Name;
                }

                return gameObject.name;
            }
        }

        public PrintBedAnchor ResolvedBedAnchor =>
            BedAnchor != null ? BedAnchor : Motion != null ? Motion.BedAnchor : null;

        public PartLocation GetSpoolLocation(SpoolSlot slot)
        {
            return slot == SpoolSlot.Right
                ? RightSpoolLocation
                : LeftSpoolLocation;
        }

        private void Reset()
        {
            AutoAssign();
        }

        private void Awake()
        {
            AutoAssign();
        }

        internal bool BindToDevice(Device device)
        {
            if (device == null)
            {
                return false;
            }

            if (BoundDevice == device)
            {
                return true;
            }

            if (BoundDevice != null)
            {
                return false;
            }

            BoundDevice = device;
            return true;
        }

        internal void UnbindFromDevice(Device device)
        {
            if (device == null || BoundDevice != device)
            {
                return;
            }

            BoundDevice = null;
        }

        internal bool TrySetPendingJob(PrintJobRequest request)
        {
            if (request == null || HasPendingJob)
            {
                return false;
            }

            PendingJob = request;
            return true;
        }

        internal void ClearPendingJob()
        {
            PendingJob = null;
        }

        [ContextMenu("Auto-Assign Critical Parts")]
        private void AutoAssignCriticalParts()
        {
            if (Assembly == null)
            {
                Assembly = GetComponent<PartsAssembly>();
            }

            if (Assembly == null)
            {
                Debug.LogWarning("[Printer] PartsAssembly is not assigned.", this);
                return;
            }

            PartLocation[] locations =
                Assembly.GetComponentsInChildren<PartLocation>(true);

            List<PartLocation> critical = new List<PartLocation>();
            PartLocation plate = null;

            foreach (PartLocation location in locations)
            {
                if (location == null)
                {
                    continue;
                }

                if (System.Array.IndexOf(
                        DefaultCriticalLocationNames,
                        location.gameObject.name) >= 0)
                {
                    critical.Add(location);
                }

                if (location.gameObject.name == "printer_print_plate_location")
                {
                    plate = location;
                }
            }

            CriticalPartLocations = critical.ToArray();
            PrintPlateLocation = plate;
        }

        [ContextMenu("Validate Configuration")]
        public void ValidateConfiguration()
        {
            if (Assembly == null)
            {
                Debug.LogWarning("[Printer] PartsAssembly is not assigned.", this);
            }

            if (ItemData == null)
            {
                Debug.LogWarning("[Printer] ItemDataHolder is not assigned.", this);
            }

            if (Motion == null)
            {
                Debug.LogWarning("[Printer] PrinterMotionBase is not assigned.", this);
            }

            if (ResolvedBedAnchor == null)
            {
                Debug.LogWarning(
                    "[Printer] No PrintBedAnchor assigned and motion has none.",
                    this);
            }

            if (Environment == null)
            {
                Debug.LogWarning("[Printer] PrinterEnvironment is not assigned.", this);
            }
        }

        private void AutoAssign()
        {
            if (Assembly == null)
            {
                Assembly = GetComponent<PartsAssembly>();
            }

            if (ItemData == null)
            {
                ItemData = GetComponent<ItemDataHolder>();
            }

            if (Motion == null)
            {
                Motion = GetComponent<PrinterMotionBase>();
            }

            if (BedAnchor == null)
            {
                BedAnchor = GetComponentInChildren<PrintBedAnchor>(true);
            }

            if (BindHighlight == null)
            {
                BindHighlight = GetComponent<PrinterBindHighlight>();
            }

            if (Environment == null)
            {
                Environment = GetComponent<PrinterEnvironment>();
            }
        }
    }
}

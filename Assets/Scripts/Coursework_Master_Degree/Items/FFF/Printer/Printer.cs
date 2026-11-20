using System;
using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Wear;

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
        Calibrating,
        Maintenance,
        Fault
    }

    /// <summary>
    /// Printer root. Owns the job, the simulated state machine and the
    /// hardware references used for jobs, motion, wear and binding.
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

        [Header("Enclosure docking")]
        [Tooltip("Printer socket for the battery closet.")]
        public PartLocation BatteryClosetLocation;

        [Tooltip("Printer socket for the battery closet adapter.")]
        public PartLocation BatteryClosetAdapterLocation;

        [Tooltip("Visual spool holder hidden while the printer is docked.")]
        public GameObject SpoolHolder;

        [Tooltip("Battery closet free part that follows the installed printer.")]
        public ItemFreePartDataHolder BatteryClosetPlug;

        [Tooltip("Wall socket free part that follows the installed printer.")]
        public ItemFreePartDataHolder WallSocketPlug;

        [Header("Service")]
        [Tooltip("Parts that cannot be swapped while a print is paused.")]
        public PartLocation[] ServiceBlockedDuringPrint;

        [Min(0.0f)]
        [Tooltip("Nozzle and bed must be at or below this temperature for service.")]
        public float SafeServiceTemperatureC = 50.0f;

        [Header("Simulation")]
        [Min(1.0f)]
        public float PrintSpeedMultiplier = SimulationSpeedSettings.DefaultMultiplier;

        [Min(0.1f)]
        public float TemperatureToleranceC = 2.0f;

        [Min(1.0f)]
        public float PreheatTimeoutSimulatedSeconds = 900.0f;

        [Min(0.0f)]
        public float PauseQualityPenalty = 4.0f;

        [Min(0.0f)]
        [Tooltip("Approximate axis travel used by distance-based wear until M4 paths exist.")]
        public float AxisTravelMetersPerSecond = 0.15f;

        [Min(0.0f)]
        public float FanRPM = 3000.0f;

        [Min(0.0f)]
        public float FumesDensity = 1.0f;

        [Header("Hard-stop condition loss")]
        [Min(0.0f)] public float HardStopNozzleBase = 0.2f;
        [Min(0.0f)] public float HardStopNozzleScale = 0.8f;
        [Min(0.0f)] public float HardStopHeaterBase = 0.1f;
        [Min(0.0f)] public float HardStopHeaterScale = 0.4f;
        [Min(0.0f)] public float HardStopBedBase = 0.1f;
        [Min(0.0f)] public float HardStopBedScale = 0.3f;

        [Header("Calibration")]
        [Min(2)]
        public int CalibrationGridSize = 5;

        [Min(0.0f)]
        public float CalibrationPointDwellSeconds = 0.15f;

        [Min(0.0f)]
        public float CalibrationPaddingMeters = 0.01f;

        [Min(0.01f)]
        [Tooltip("Vertical clearance above the head start position while moving between points.")]
        public float CalibrationLiftHeightMeters = 0.03f;

        [Min(0.01f)]
        [Tooltip("Real-world head travel speed during calibration.")]
        public float CalibrationMoveSpeedMetersPerSecond = 0.25f;

        [SerializeField]
        private PrinterTemperatureModel temperatureModel =
            new PrinterTemperatureModel();

        private readonly PrinterWearIntegrator wearIntegrator =
            new PrinterWearIntegrator();

        private readonly PrinterStateMachine stateMachine =
            new PrinterStateMachine();

        private readonly List<Vector3> calibrationPoints =
            new List<Vector3>();

        private PrinterJob activeJob;
        private PrinterJob completedJob;
        private PrinterJob partialJob;

        private float preheatElapsedSimulated;
        private float wearTimeAccumulator;
        private float wearFilamentAccumulator;
        private int calibrationIndex;
        private float calibrationTimer;
        private CalibrationPhase calibrationPhase;
        private Vector3 calibrationCurrentPosition;
        private Vector3 calibrationTargetPosition;

        private bool isMovingHome;
        private Vector3 homeMovePosition;
        private bool isCalibrated;

        private PrinterEnclosure dockedEnclosure;
        private PartLocation dockedSpoolLocation;
        private SpoolSlot dockedSpoolSlot;

        private enum CalibrationPhase
        {
            MoveToStart,
            MoveToPoint,
            Lower,
            Dwell,
            Raise,
            ReturnToStart
        }

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

        public PrintJobRequest PendingJob { get; private set; }

        public bool HasPendingJob => PendingJob != null;

        public bool HasPartialPrint { get; private set; }

        public bool HasCompletedPrint => completedJob != null;

        public PrinterJob ActiveJob => activeJob;
        public PrinterJob CompletedJob => completedJob;
        public PrinterJob PartialJob => partialJob;
        public SpoolFilamentState ActiveSpool { get; private set; }

        public Device BoundDevice { get; private set; }

        public bool IsBound => BoundDevice != null;

        public PrinterTemperatureState Temperature => temperatureModel.State;

        public bool IsBusy =>
            stateMachine.IsActiveJobState ||
            HasPendingJob ||
            HasPartialPrint ||
            HasCompletedPrint;

        public bool IsPrintActive =>
            stateMachine.IsActiveJobState;

        public bool IsPaused =>
            OperationalState == PrinterOperationalState.Paused;

        public bool IsCalibrated => isCalibrated;

        public string CalibrationWarningText =>
            !isCalibrated &&
            (activeJob != null || completedJob != null || partialJob != null)
                ? "Printer is not calibrated; final quality may be 70–100% of the estimate."
                : string.Empty;

        public bool IsAtSafeServiceTemperature =>
            temperatureModel.IsBelow(SafeServiceTemperatureC);

        public float Progress01 => activeJob != null
            ? activeJob.Progress01
            : partialJob != null
                ? partialJob.Progress01
                : completedJob != null
                    ? 1.0f
                    : 0.0f;

        public float RemainingFilamentMeters =>
            ActiveSpool != null ? ActiveSpool.RemainingMeters : 0.0f;

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

        public PrinterEnclosure DockedEnclosure =>
            dockedEnclosure != null ? dockedEnclosure : FindDockedEnclosure();

        public int InstalledSpoolCount
        {
            get
            {
                int count = 0;

                if (LeftSpoolLocation != null && LeftSpoolLocation.IsOccupied)
                {
                    count++;
                }

                if (RightSpoolLocation != null && RightSpoolLocation.IsOccupied)
                {
                    count++;
                }

                return count;
            }
        }

        public PartLocation GetSpoolLocation(SpoolSlot slot)
        {
            if (dockedSpoolLocation != null && slot == dockedSpoolSlot)
            {
                return dockedSpoolLocation;
            }

            return slot == SpoolSlot.Right
                ? RightSpoolLocation
                : LeftSpoolLocation;
        }

        public static string GetPrinterEnclosureInstallBlockReason(
            ItemDataHolder item,
            PartsAssembly targetAssembly)
        {
            if (item == null || targetAssembly == null)
            {
                return null;
            }

            Printer printer = item.GetComponent<Printer>();

            if (printer == null)
            {
                return null;
            }

            PrinterEnclosure enclosure =
                targetAssembly.GetComponentInParent<PrinterEnclosure>();

            return enclosure != null
                ? enclosure.GetPrinterInstallBlockReason(printer)
                : null;
        }

        /// <summary>
        /// Moves the printer's spool, battery closet, adapter and free parts
        /// onto the enclosure sockets. PartLocation triggers this after the
        /// printer itself has been attached to an enclosure socket.
        /// </summary>
        public void DockToEnclosure()
        {
            if (dockedEnclosure != null)
            {
                return;
            }

            PrinterEnclosure enclosure = FindDockedEnclosure();

            if (enclosure == null ||
                InstalledSpoolCount > 1)
            {
                return;
            }

            dockedEnclosure = enclosure;
            Environment?.SetEnclosure(enclosure, this);

            // The enclosure exposes one spool socket. Preserve the printer's
            // original side when it had a spool; otherwise show the socket on
            // the left so it can be used later.
            dockedSpoolSlot = SpoolSlot.Left;
            dockedSpoolLocation = enclosure.SpoolLocation;

            if (TryGetSingleInstalledSpool(
                    out SpoolSlot spoolSlot,
                    out PartLocation spoolLocation))
            {
                dockedSpoolSlot = spoolSlot;
                spoolLocation.MoveInstalledPartTo(enclosure.SpoolLocation);
            }

            if (BatteryClosetLocation != null)
            {
                BatteryClosetLocation.MoveInstalledPartTo(
                    enclosure.BatteryClosetLocation);
            }

            if (BatteryClosetAdapterLocation != null)
            {
                BatteryClosetAdapterLocation.MoveInstalledPartTo(
                    enclosure.BatteryClosetAdapterLocation);
            }

            ItemDataHolder enclosureItem =
                enclosure.GetComponent<ItemDataHolder>();

            DockFreePart(
                BatteryClosetPlug,
                enclosure.BatteryClosetPlugLocation,
                enclosureItem);

            DockFreePart(
                WallSocketPlug,
                enclosure.WallSocketPlugLocation,
                enclosureItem);

            if (SpoolHolder != null)
            {
                SpoolHolder.SetActive(false);
            }

            wearIntegrator.Refresh(this);
        }

        /// <summary>
        /// Returns docked accessories to the printer before it leaves the
        /// enclosure. Called by PartLocation while the printer still has its
        /// enclosure parent, so the enclosure can be resolved reliably.
        /// </summary>
        public void UndockFromEnclosure()
        {
            if (dockedEnclosure == null)
            {
                return;
            }

            PrinterEnclosure enclosure = dockedEnclosure;
            dockedEnclosure = null;
            Environment?.SetEnclosure(null, this);

            if (SpoolHolder != null)
            {
                SpoolHolder.SetActive(true);
            }

            if (dockedSpoolLocation != null &&
                dockedSpoolLocation.IsOccupied)
            {
                PartLocation target = dockedSpoolSlot == SpoolSlot.Right
                    ? RightSpoolLocation
                    : LeftSpoolLocation;

                dockedSpoolLocation.MoveInstalledPartTo(target);
            }

            dockedSpoolLocation = null;

            if (enclosure.BatteryClosetLocation != null &&
                enclosure.BatteryClosetLocation.IsOccupied)
            {
                enclosure.BatteryClosetLocation.MoveInstalledPartTo(
                    BatteryClosetLocation);
            }

            if (enclosure.BatteryClosetAdapterLocation != null &&
                enclosure.BatteryClosetAdapterLocation.IsOccupied &&
                enclosure.BatteryClosetAdapterLocation.transform.IsChildOf(
                    enclosure.transform))
            {
                enclosure.BatteryClosetAdapterLocation.MoveInstalledPartTo(
                    BatteryClosetAdapterLocation);
            }

            ItemDataHolder enclosureItem =
                enclosure.GetComponent<ItemDataHolder>();

            enclosureItem?.ClearFreePartLocation(
                BatteryClosetPlug != null ? BatteryClosetPlug.gameObject : null);

            enclosureItem?.ClearFreePartLocation(
                WallSocketPlug != null ? WallSocketPlug.gameObject : null);

            ItemData?.ResetFreePartsToLocations();
            wearIntegrator.Refresh(this);
        }

        private PrinterEnclosure FindDockedEnclosure()
        {
            PartsAssembly enclosureAssembly =
                PartsAssembly.FindEnclosingPrinterEnclosure(this);

            return enclosureAssembly != null
                ? enclosureAssembly.GetComponent<PrinterEnclosure>()
                : null;
        }

        private bool TryGetSingleInstalledSpool(
            out SpoolSlot slot,
            out PartLocation location)
        {
            slot = SpoolSlot.Left;
            location = null;

            if (LeftSpoolLocation != null && LeftSpoolLocation.IsOccupied)
            {
                slot = SpoolSlot.Left;
                location = LeftSpoolLocation;
            }

            if (RightSpoolLocation != null && RightSpoolLocation.IsOccupied)
            {
                if (location != null)
                {
                    location = null;
                    return false;
                }

                slot = SpoolSlot.Right;
                location = RightSpoolLocation;
            }

            return location != null;
        }

        private static void DockFreePart(
            ItemFreePartDataHolder freePart,
            Transform location,
            ItemDataHolder enclosureItem)
        {
            if (freePart == null || location == null)
            {
                return;
            }

            ItemPlacementUtility.ResetFreePart(
                freePart.gameObject,
                location);

            enclosureItem?.SetFreePartLocation(
                freePart.gameObject,
                location);
        }

        public PrinterOperationalState OperationalState =>
            stateMachine.State;

        public string StateProgressText
        {
            get
            {
                if (activeJob != null)
                {
                    return $"{StateLabel(OperationalState)} " +
                        $"{activeJob.Progress01 * 100.0f:0}%";
                }

                if (OperationalState == PrinterOperationalState.Calibrating)
                {
                    int total = Mathf.Max(1, calibrationPoints.Count);
                    return $"Calibrating " +
                        $"{Mathf.Min(calibrationIndex + 1, total)}/{total}";
                }

                if (HasPartialPrint)
                {
                    return "Partial print on the bed";
                }

                if (HasCompletedPrint)
                {
                    return "Finished print on the bed";
                }

                return StateLabel(OperationalState);
            }
        }

        public string NozzleTemperatureText =>
            $"N {Temperature.Nozzle:0}°C";

        public string BedTemperatureText =>
            $"B {Temperature.Bed:0}°C";

        public string SpoolMetersText =>
            ActiveSpool != null
                ? $"{ActiveSpool.RemainingMeters:0.0} m left"
                : string.Empty;

        public string PausePenaltyText =>
            activeJob != null && activeJob.PauseQualityPenalty > 0.0f
                ? $"pause -{activeJob.PauseQualityPenalty:0}"
                : string.Empty;

        public string StatusSummary
        {
            get
            {
                string summary = StateProgressText;

                if (!string.IsNullOrEmpty(NozzleTemperatureText))
                {
                    summary += $" · {NozzleTemperatureText}";
                }

                if (!string.IsNullOrEmpty(BedTemperatureText))
                {
                    summary += $" · {BedTemperatureText}";
                }

                if (!string.IsNullOrEmpty(SpoolMetersText))
                {
                    summary += $" · {SpoolMetersText}";
                }

                if (!string.IsNullOrEmpty(PausePenaltyText))
                {
                    summary += $" · {PausePenaltyText}";
                }

                return summary;
            }
        }

        private void Reset()
        {
            AutoAssign();
        }

        private void Awake()
        {
            AutoAssign();

            if (temperatureModel == null)
            {
                temperatureModel = new PrinterTemperatureModel();
            }

            temperatureModel.Initialize(Environment);
            wearIntegrator.Refresh(this);
        }

        private void Start()
        {
            DockToEnclosure();
            Motion?.ApplyHomePosition();
        }

        private void Update()
        {
            float simulatedDelta = Mathf.Min(Time.deltaTime, 0.1f) *
                Mathf.Max(1.0f, PrintSpeedMultiplier);

            if (isMovingHome)
            {
                TickHomeMove(simulatedDelta);
            }

            switch (OperationalState)
            {
                case PrinterOperationalState.Idle:
                    TryStartPendingJob();
                    break;

                case PrinterOperationalState.Preheating:
                    TickPreheating(simulatedDelta);
                    break;

                case PrinterOperationalState.Printing:
                    TickPrinting(simulatedDelta);
                    break;

                case PrinterOperationalState.Paused:
                    TickPaused(simulatedDelta);
                    break;

                case PrinterOperationalState.Cooling:
                    TickCooling(simulatedDelta);
                    break;

                case PrinterOperationalState.Calibrating:
                    TickCalibration(simulatedDelta);
                    break;
            }
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
            if (request == null ||
                HasPendingJob ||
                OperationalState != PrinterOperationalState.Idle ||
                HasPartialPrint ||
                HasCompletedPrint)
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

        public bool TryPause(out string reason)
        {
            reason = null;

            if (OperationalState != PrinterOperationalState.Printing)
            {
                reason = "Only a running print can be paused.";
                return false;
            }

            SetState(PrinterOperationalState.Paused);

            BeginHomeMove();
            return true;
        }

        public bool TryResume(out string reason)
        {
            reason = null;

            if (OperationalState != PrinterOperationalState.Paused)
            {
                reason = "The printer is not paused.";
                return false;
            }

            if (!HasAllCriticalParts(out reason))
            {
                return false;
            }

            if (activeJob != null)
            {
                if (!SpoolFilamentState.TryCreate(
                        GetSpoolLocation(activeJob.Request.SpoolSlot),
                        out SpoolFilamentState spool))
                {
                    reason = "The selected filament spool is missing.";
                    return false;
                }

                ActiveSpool = spool;
            }

            if (activeJob != null)
            {
                activeJob.PauseCount++;
                activeJob.PauseQualityPenalty += Mathf.Max(
                    0.0f,
                    PauseQualityPenalty);
            }

            SetTargetsFromJob();
            preheatElapsedSimulated = 0.0f;
            wearIntegrator.Refresh(this);

            SetState(temperatureModel.IsAtTarget(TemperatureToleranceC)
                ? PrinterOperationalState.Printing
                : PrinterOperationalState.Preheating);

            return true;
        }

        public bool HardStop()
        {
            if (OperationalState == PrinterOperationalState.Calibrating)
            {
                CancelCalibration();
                return true;
            }

            if (OperationalState != PrinterOperationalState.Preheating &&
                OperationalState != PrinterOperationalState.Printing &&
                OperationalState != PrinterOperationalState.Paused)
            {
                return false;
            }

            bool leavesPartial =
                OperationalState == PrinterOperationalState.Printing ||
                (OperationalState == PrinterOperationalState.Paused &&
                 activeJob != null &&
                 activeJob.Progress01 > 0.0f);

            if (activeJob != null)
            {
                activeJob.IsPartial = leavesPartial;
                activeJob.WasHardStopped = true;
            }

            wearIntegrator.ApplyHardStopPenalty(
                this,
                HardStopNozzleBase,
                HardStopNozzleScale,
                HardStopHeaterBase,
                HardStopHeaterScale,
                HardStopBedBase,
                HardStopBedScale);

            Motion?.ApplyHomePosition();
            SetState(PrinterOperationalState.Cooling);
            return true;
        }

        public void EnsureHomePosition()
        {
            if (activeJob == null &&
                OperationalState != PrinterOperationalState.Calibrating)
            {
                Motion?.ApplyHomePosition();
            }
        }

        public bool TryStartCalibration(out string reason)
        {
            reason = null;

            if (OperationalState != PrinterOperationalState.Idle ||
                HasPendingJob ||
                HasPartialPrint ||
                HasCompletedPrint)
            {
                reason = "Calibration requires a clear, idle printer.";
                return false;
            }

            PrintBedAnchor anchor = ResolvedBedAnchor;

            if (Motion == null || anchor == null)
            {
                reason = "Printer motion or bed anchor is missing.";
                return false;
            }

            BuildCalibrationPoints(anchor);
            calibrationIndex = 0;
            calibrationTimer = 0.0f;
            isMovingHome = false;
            calibrationPhase = CalibrationPhase.MoveToStart;
            calibrationCurrentPosition = Motion.GetNozzlePrintPosition();
            calibrationTargetPosition = ClampCalibrationPosition(
                Motion.CalibrationHomePrintPosition);
            SetState(PrinterOperationalState.Calibrating);
            return true;
        }

        public void CancelCalibration()
        {
            if (OperationalState != PrinterOperationalState.Calibrating)
            {
                return;
            }

            Motion?.ApplyHomePosition();
            calibrationPoints.Clear();
            calibrationIndex = 0;
            SetState(PrinterOperationalState.Idle);
        }

        public bool ClearBed()
        {
            if (OperationalState != PrinterOperationalState.Finished &&
                !HasPartialPrint)
            {
                return false;
            }

            completedJob = null;
            partialJob = null;
            HasPartialPrint = false;
            SetState(PrinterOperationalState.Idle);
            return true;
        }

        public string GetPartInstallBlockReason()
        {
            if (!IsPrintActive)
            {
                return null;
            }

            if (OperationalState == PrinterOperationalState.Calibrating)
            {
                return "Printer is calibrating";
            }

            if (OperationalState != PrinterOperationalState.Paused)
            {
                return "Printer is on its job";
            }

            if (!IsAtSafeServiceTemperature)
            {
                return "Wait for the printer to cool";
            }

            return null;
        }

        public string GetPartServiceBlockReason(PartLocation location)
        {
            if (!IsPrintActive)
            {
                return null;
            }

            if (OperationalState == PrinterOperationalState.Calibrating)
            {
                return "Stop calibration before servicing parts.";
            }

            if (OperationalState != PrinterOperationalState.Paused)
            {
                return "Pause the printer before servicing parts.";
            }

            if (!IsAtSafeServiceTemperature)
            {
                return $"Wait for the nozzle and bed to cool below " +
                    $"{SafeServiceTemperatureC:0} °C.";
            }

            if (IsServiceBlocked(location))
            {
                return "This part cannot be swapped during a print. " +
                    "Hard stop first.";
            }

            return null;
        }

        public void NotifyPartChanged()
        {
            if (activeJob != null)
            {
                isCalibrated = false;
            }
        }

        public bool IsServiceBlocked(PartLocation location)
        {
            if (location == null || ServiceBlockedDuringPrint == null)
            {
                return false;
            }

            for (int i = 0; i < ServiceBlockedDuringPrint.Length; i++)
            {
                if (ServiceBlockedDuringPrint[i] == location)
                {
                    return true;
                }
            }

            return false;
        }

        public static Printer FindFor(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Printer printer = target.GetComponentInParent<Printer>();

            if (printer != null)
            {
                return printer;
            }

            PrinterEnclosure enclosure =
                target.GetComponentInParent<PrinterEnclosure>();

            if (enclosure == null)
            {
                enclosure = target.GetComponentInChildren<PrinterEnclosure>(
                    true);
            }

            if (enclosure != null && enclosure.Printer != null)
            {
                return enclosure.Printer;
            }

            return target.GetComponentInChildren<Printer>(true);
        }

        public bool HasAllCriticalParts(out string reason)
        {
            if (!HasAllCriticalParts(CriticalPartLocations, out reason))
            {
                return false;
            }

            PrinterEnclosure enclosure = DockedEnclosure;

            return enclosure == null ||
                HasAllCriticalParts(enclosure.CriticalPartLocations, out reason);
        }

        private static bool HasAllCriticalParts(
            PartLocation[] criticalLocations,
            out string reason)
        {
            reason = null;

            if (criticalLocations == null)
            {
                return true;
            }

            for (int i = 0; i < criticalLocations.Length; i++)
            {
                PartLocation location = criticalLocations[i];

                if (location == null)
                {
                    continue;
                }

                if (!location.IsOccupied || location.InstalledPart == null)
                {
                    reason = $"Missing critical part: {Friendly(location.name)}.";
                    return false;
                }

                ItemWearState wear =
                    location.InstalledPart.GetComponentInChildren<ItemWearState>(
                        true);

                if (wear != null && wear.CurrentCondition <= 0.0f)
                {
                    reason = $"Critical part is worn out: {Friendly(location.name)}.";
                    return false;
                }
            }

            return true;
        }

        private void TryStartPendingJob()
        {
            if (PendingJob == null ||
                HasPartialPrint ||
                HasCompletedPrint)
            {
                return;
            }

            if (!HasAllCriticalParts(out string criticalReason))
            {
                Debug.LogWarning(
                    $"[Printer] {DisplayName} cannot start: {criticalReason}",
                    this);
                PendingJob = null;
                return;
            }

            activeJob = new PrinterJob(PendingJob);
            PendingJob = null;
            completedJob = null;
            partialJob = null;
            HasPartialPrint = false;
            preheatElapsedSimulated = 0.0f;
            wearTimeAccumulator = 0.0f;
            wearFilamentAccumulator = 0.0f;

            if (!SpoolFilamentState.TryCreate(
                    GetSpoolLocation(activeJob.Request.SpoolSlot),
                    out SpoolFilamentState spool))
            {
                Debug.LogWarning(
                    $"[Printer] {DisplayName} cannot start: selected spool is missing.",
                    this);
                activeJob = null;
                PendingJob = null;
                return;
            }

            ActiveSpool = spool;
            SetTargetsFromJob();
            wearIntegrator.Refresh(this);
            SetState(PrinterOperationalState.Preheating);
        }

        private void SetTargetsFromJob()
        {
            if (activeJob == null || activeJob.Request == null)
            {
                return;
            }

            SpoolFilamentState spool = ActiveSpool;

            FilamentMaterialDefaults.Targets(
                spool != null && spool.Data != null
                    ? spool.Data.MaterialType
                    : default,
                spool != null && spool.Data != null
                    ? spool.Data.MaterialProfile
                    : null,
                out float nozzle,
                out float bed,
                out float chamber);

            temperatureModel.SetTargets(nozzle, bed, chamber);
        }

        private void TickPreheating(float simulatedDelta)
        {
            temperatureModel.Tick(
                simulatedDelta,
                Environment,
                true,
                true);
            AccumulateWear(simulatedDelta, 0.0f);

            preheatElapsedSimulated += simulatedDelta;

            if (temperatureModel.IsAtTarget(TemperatureToleranceC))
            {
                preheatElapsedSimulated = 0.0f;
                SetState(PrinterOperationalState.Printing);
                return;
            }

            if (preheatElapsedSimulated >= PreheatTimeoutSimulatedSeconds)
            {
                Debug.LogWarning(
                    $"[Printer] {DisplayName} could not reach target " +
                    "temperature; entering Fault.",
                    this);
                SetState(PrinterOperationalState.Fault);
            }
        }

        private void TickPrinting(float simulatedDelta)
        {
            temperatureModel.Tick(
                simulatedDelta,
                Environment,
                true,
                true);

            AdvancePrint(simulatedDelta);

            if (activeJob != null && activeJob.Progress01 >= 1.0f)
            {
                CompletePrint();
            }
        }

        private void TickPaused(float simulatedDelta)
        {
            temperatureModel.Tick(
                simulatedDelta,
                Environment,
                false,
                true);
            AccumulateWear(simulatedDelta, 0.0f);
        }

        private void TickCooling(float simulatedDelta)
        {
            temperatureModel.Tick(
                simulatedDelta,
                Environment,
                false,
                activeJob != null);

            AccumulateWear(simulatedDelta, 0.0f);

            if (!temperatureModel.IsBelow(
                    Temperature.Ambient + TemperatureToleranceC))
            {
                return;
            }

            FlushWear();
            BeginHomeMove();

            if (activeJob != null && activeJob.IsPartial)
            {
                partialJob = activeJob;
                activeJob = null;
                HasPartialPrint = true;
                SetState(PrinterOperationalState.Idle);
            }
            else if (activeJob != null && activeJob.WasHardStopped)
            {
                activeJob = null;
                SetState(PrinterOperationalState.Idle);
            }
            else
            {
                completedJob = activeJob;
                activeJob = null;
                SetState(PrinterOperationalState.Finished);
            }

            ActiveSpool = null;
        }

        private void BeginHomeMove()
        {
            if (Motion == null)
            {
                return;
            }

            isMovingHome = true;
            homeMovePosition = Motion.GetNozzlePrintPosition();
        }

        private void TickHomeMove(float delta)
        {
            if (Motion == null)
            {
                isMovingHome = false;
                return;
            }

            Vector3 target = Motion.HomePrintPosition;
            float speed = Mathf.Max(
                0.01f,
                CalibrationMoveSpeedMetersPerSecond);

            homeMovePosition = Vector3.MoveTowards(
                homeMovePosition,
                target,
                speed * Mathf.Max(0.0f, delta));

            Motion.ApplyNozzlePosition(homeMovePosition);

            if ((homeMovePosition - target).sqrMagnitude <= 0.000001f)
            {
                Motion.ApplyHomePosition();
                isMovingHome = false;
            }
        }

        private void TickCalibration(float delta)
        {
            if (calibrationPoints.Count == 0)
            {
                CancelCalibration();
                return;
            }

            switch (calibrationPhase)
            {
                case CalibrationPhase.MoveToStart:
                    if (MoveCalibrationToward(calibrationTargetPosition, delta))
                    {
                        BeginCalibrationPoint();
                    }
                    break;

                case CalibrationPhase.MoveToPoint:
                    if (MoveCalibrationToward(calibrationTargetPosition, delta))
                    {
                        Vector3 point = calibrationPoints[calibrationIndex];
                        calibrationTargetPosition = new Vector3(
                            point.x,
                            0.0f,
                            point.z);
                        calibrationPhase = CalibrationPhase.Lower;
                    }
                    break;

                case CalibrationPhase.Lower:
                    if (MoveCalibrationToward(calibrationTargetPosition, delta))
                    {
                        calibrationTimer = 0.0f;
                        calibrationPhase = CalibrationPhase.Dwell;
                    }
                    break;

                case CalibrationPhase.Dwell:
                    calibrationTimer += delta;

                    if (calibrationTimer >= CalibrationPointDwellSeconds)
                    {
                        Vector3 point = calibrationPoints[calibrationIndex];
                        calibrationTargetPosition = new Vector3(
                            point.x,
                            CalibrationLiftY,
                            point.z);
                        calibrationPhase = CalibrationPhase.Raise;
                    }
                    break;

                case CalibrationPhase.Raise:
                    if (MoveCalibrationToward(calibrationTargetPosition, delta))
                    {
                        calibrationIndex++;

                        if (calibrationIndex >= calibrationPoints.Count)
                        {
                            calibrationTargetPosition =
                                ClampCalibrationPosition(
                                    Motion.HomePrintPosition);
                            calibrationPhase =
                                CalibrationPhase.ReturnToStart;
                        }
                        else
                        {
                            BeginCalibrationPoint();
                        }
                    }
                    break;

                case CalibrationPhase.ReturnToStart:
                    if (MoveCalibrationToward(calibrationTargetPosition, delta))
                    {
                        calibrationPoints.Clear();
                        calibrationIndex = 0;
                        isCalibrated = true;
                        SetState(PrinterOperationalState.Idle);
                    }
                    break;
            }
        }

        private float CalibrationLiftY
        {
            get
            {
                float volumeHeight = Motion.BuildVolume.y;
                float lift = Motion.CalibrationHomePrintPosition.y +
                    Mathf.Max(0.005f, CalibrationLiftHeightMeters);

                return Mathf.Clamp(lift, 0.005f, Mathf.Max(0.005f, volumeHeight));
            }
        }

        private Vector3 ClampCalibrationPosition(Vector3 position)
        {
            Vector3 volume = Motion.BuildVolume;

            return new Vector3(
                Mathf.Clamp(position.x, -volume.x * 0.5f, volume.x * 0.5f),
                Mathf.Clamp(position.y, 0.0f, Mathf.Max(0.0f, volume.y)),
                Mathf.Clamp(position.z, -volume.z * 0.5f, volume.z * 0.5f));
        }

        private void BeginCalibrationPoint()
        {
            if (calibrationIndex < 0 ||
                calibrationIndex >= calibrationPoints.Count)
            {
                return;
            }

            Vector3 point = calibrationPoints[calibrationIndex];
            calibrationTargetPosition = ClampCalibrationPosition(new Vector3(
                point.x,
                CalibrationLiftY,
                point.z));
            calibrationPhase = CalibrationPhase.MoveToPoint;
        }

        private bool MoveCalibrationToward(Vector3 target, float delta)
        {
            float speed = Mathf.Max(
                0.01f,
                CalibrationMoveSpeedMetersPerSecond);

            calibrationCurrentPosition = Vector3.MoveTowards(
                calibrationCurrentPosition,
                target,
                speed * Mathf.Max(0.0f, delta));

            Motion.ApplyNozzlePosition(calibrationCurrentPosition);

            return (calibrationCurrentPosition - target).sqrMagnitude <=
                0.000001f;
        }

        private void AdvancePrint(float simulatedDelta)
        {
            if (activeJob == null)
            {
                return;
            }

            float total = Mathf.Max(0.001f, activeJob.Request.EstimatedSeconds);
            float previousProgress = activeJob.Progress01;

            activeJob.ElapsedSeconds = Mathf.Min(
                total,
                activeJob.ElapsedSeconds + simulatedDelta);

            float deltaProgress = activeJob.Progress01 - previousProgress;
            float targetFilament = activeJob.Request.EstimatedFilamentMeters *
                activeJob.Progress01;
            float deltaFilament = Mathf.Max(
                0.0f,
                targetFilament - activeJob.ConsumedFilamentMeters);

            activeJob.ConsumedFilamentMeters += deltaFilament;

            if (ActiveSpool != null)
            {
                ActiveSpool.ApplyConsumed(deltaFilament);
            }

            AccumulateWear(simulatedDelta, deltaFilament);
        }

        private void AccumulateWear(float simulatedDelta, float filamentMeters)
        {
            wearTimeAccumulator += simulatedDelta;
            wearFilamentAccumulator += filamentMeters;

            if (wearTimeAccumulator < 1.0f)
            {
                return;
            }

            ApplyWearBatch();
        }

        private void ApplyWearBatch()
        {
            if (wearTimeAccumulator <= 0.0f)
            {
                return;
            }

            float hardness = ActiveSpool != null && ActiveSpool.Data != null
                ? ActiveSpool.Data.MaterialHardness
                : 0.0f;
            float abrasiveness = ActiveSpool != null && ActiveSpool.Data != null
                ? ActiveSpool.Data.MaterialAbrasiveness
                : 0.0f;

            wearIntegrator.ApplyPrintWear(
                this,
                wearTimeAccumulator,
                wearFilamentAccumulator,
                hardness,
                abrasiveness);

            wearTimeAccumulator = 0.0f;
            wearFilamentAccumulator = 0.0f;
        }

        private void FlushWear()
        {
            ApplyWearBatch();

            if (activeJob == null ||
                activeJob.UsesApplied ||
                ActiveSpool == null ||
                ActiveSpool.Data == null)
            {
                return;
            }

            activeJob.UsesApplied = true;

            if (!activeJob.IsPartial && activeJob.Progress01 <= 0.0f)
            {
                return;
            }

            wearIntegrator.ApplyCompletedPrintUse(
                this,
                ActiveSpool.Data.MaterialHardness);
        }

        private void CompletePrint()
        {
            activeJob.CalibrationQualityMultiplier = IsCalibrated
                ? 1.0f
                : UnityEngine.Random.Range(0.7f, 1.0f);
            activeJob.FinalQuality = UnityEngine.Random.Range(
                activeJob.QualityMin,
                activeJob.QualityMax);

            FlushWear();
            activeJob.ElapsedSeconds = activeJob.Request.EstimatedSeconds;
            activeJob.ConsumedFilamentMeters =
                activeJob.Request.EstimatedFilamentMeters;

            if (ActiveSpool != null)
            {
                ActiveSpool.Refresh();
            }

            SetState(PrinterOperationalState.Cooling);
        }

        private void BuildCalibrationPoints(PrintBedAnchor anchor)
        {
            calibrationPoints.Clear();

            int grid = Mathf.Max(2, CalibrationGridSize);
            Vector3 volume = Motion != null
                ? Motion.BuildVolume
                : new Vector3(0.25f, 0.21f, 0.21f);

            float padding = Mathf.Max(0.0f, CalibrationPaddingMeters);
            float width = Mathf.Max(
                0.0f,
                Mathf.Min(anchor.PlateSize.x, volume.x) - padding * 2.0f);
            float depth = Mathf.Max(
                0.0f,
                Mathf.Min(anchor.PlateSize.y, volume.z) - padding * 2.0f);

            for (int z = 0; z < grid; z++)
            {
                float tz = grid > 1 ? z / (float)(grid - 1) : 0.5f;

                for (int x = 0; x < grid; x++)
                {
                    float tx = grid > 1 ? x / (float)(grid - 1) : 0.5f;

                    calibrationPoints.Add(new Vector3(
                        Mathf.Lerp(-width * 0.5f, width * 0.5f, tx),
                        0.0f,
                        Mathf.Lerp(-depth * 0.5f, depth * 0.5f, tz)));
                }
            }
        }

        private void SetState(PrinterOperationalState state)
        {
            stateMachine.Enter(state);
        }

        private static string Friendly(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return "part";
            }

            return locationName
                .Replace("_location", string.Empty)
                .Replace('_', ' ');
        }

        private static string StateLabel(PrinterOperationalState state)
        {
            switch (state)
            {
                case PrinterOperationalState.Preheating:
                    return "Heating";
                case PrinterOperationalState.Printing:
                    return "Printing";
                case PrinterOperationalState.Paused:
                    return "Paused";
                case PrinterOperationalState.Cooling:
                    return "Cooling";
                case PrinterOperationalState.Finished:
                    return "Finished";
                case PrinterOperationalState.Calibrating:
                    return "Calibrating";
                case PrinterOperationalState.Maintenance:
                    return "Maintenance";
                case PrinterOperationalState.Fault:
                    return "Fault";
                default:
                    return "Idle";
            }
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

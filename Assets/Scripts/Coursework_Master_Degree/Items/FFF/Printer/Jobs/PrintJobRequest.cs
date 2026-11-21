using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// A validated print request. M2 stores it on the printer as a pending
    /// job; M3 consumes it when the printer FSM starts.
    /// </summary>
    public sealed class PrintJobRequest
    {
        public PrintJobRequest(
            Printer printer,
            ObjectToPrintMetaSO objectToPrint,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            float estimatedSeconds,
            float estimatedFilamentMeters,
            float qualityMin,
            float qualityMax,
            float qualityCap,
            SpoolSlotId spoolSlotId,
            PrintPlacement placement = null)
        {
            Printer = printer;
            ObjectToPrint = objectToPrint;
            Infill = infill;
            Supports = supports;
            LayerHeight = layerHeight;
            Speed = speed;
            EstimatedSeconds = estimatedSeconds;
            EstimatedFilamentMeters = estimatedFilamentMeters;
            QualityMin = qualityMin;
            QualityMax = qualityMax;
            QualityCap = qualityCap;
            SpoolSlotId = spoolSlotId;
            Placement = placement;
        }

        public Printer Printer { get; }

        public ObjectToPrintMetaSO ObjectToPrint { get; }

        public InfillPattern Infill { get; }

        public SupportsMode Supports { get; }

        public LayerHeightPreset LayerHeight { get; }

        public PrintSpeedPreset Speed { get; }

        public float EstimatedSeconds { get; }

        public float EstimatedFilamentMeters { get; }

        /// <summary>Base raw quality without the enclosure contribution.</summary>
        public float QualityMin { get; }

        /// <summary>Base raw quality without the enclosure contribution.</summary>
        public float QualityMax { get; }

        /// <summary>Raw cap (90, or 55 when required supports are missing).</summary>
        public float QualityCap { get; }

        public SpoolSlotId SpoolSlotId { get; }

        public PrintPlacement Placement { get; }
    }
}

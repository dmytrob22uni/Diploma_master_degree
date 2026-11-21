using System.Collections.Generic;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Result of validating a candidate job. Hard failures go to Errors and
    /// block Start print; Warnings are informational.
    /// </summary>
    public sealed class PrintJobValidationResult
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public float EstimatedSeconds;
        public float EstimatedFilamentMeters;

        /// <summary>Display estimate with the printer's current enclosure state.</summary>
        public float EstimatedQualityMin;
        public float EstimatedQualityMax;

        /// <summary>Base range without the enclosure contribution.</summary>
        public float QualityMinBase;
        public float QualityMaxBase;

        /// <summary>Raw cap (90, or 55 when required supports are missing).</summary>
        public float QualityCap = PrintQualityEstimator.MaximumRawQuality;

        public bool IsValid => Errors.Count == 0;
    }
}

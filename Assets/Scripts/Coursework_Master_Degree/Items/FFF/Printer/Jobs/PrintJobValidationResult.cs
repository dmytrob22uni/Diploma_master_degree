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
        public float EstimatedQualityMin;
        public float EstimatedQualityMax;
        public bool HasEnclosure;

        public bool IsValid => Errors.Count == 0;
    }
}

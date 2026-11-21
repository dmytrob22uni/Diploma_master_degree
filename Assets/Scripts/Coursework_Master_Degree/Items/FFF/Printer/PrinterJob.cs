using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Jobs;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Runtime state for one print. The immutable request carries the player's
    /// choices; this class tracks progress, consumed material and penalties.
    /// </summary>
    public sealed class PrinterJob
    {
        public PrinterJob(PrintJobRequest request)
        {
            Request = request;
        }

        public PrintJobRequest Request { get; }

        public float ElapsedSeconds;
        public float ConsumedFilamentMeters;

        public int PauseCount;
        public float PauseQualityPenalty;
        public float CalibrationQualityMultiplier = 1.0f;
        public float FinalQuality = -1.0f;

        /// <summary>
        /// Whether this job used a calibrated printer when its final quality
        /// was evaluated. Kept after finalization so the console warning
        /// describes the finished/partial job instead of the next one.
        /// </summary>
        public bool CalibrationApplied;

        public bool IsPartial;
        public bool WasHardStopped;
        public bool UsesApplied;

        public float Progress01
        {
            get
            {
                float total = Request != null ? Request.EstimatedSeconds : 0.0f;

                if (total <= 0.001f)
                {
                    return 1.0f;
                }

                return Mathf.Clamp01(ElapsedSeconds / total);
            }
        }

        public float RemainingSeconds
        {
            get
            {
                float total = Request != null ? Request.EstimatedSeconds : 0.0f;
                return Mathf.Max(0.0f, total - ElapsedSeconds);
            }
        }

        public float RemainingFilamentMeters
        {
            get
            {
                float total = Request != null
                    ? Request.EstimatedFilamentMeters
                    : 0.0f;

                return Mathf.Max(0.0f, total - ConsumedFilamentMeters);
            }
        }

        public float QualityMin => CalculateFinalQuality(Request != null
            ? Request.QualityMin
            : 0.0f);

        public float QualityMax => CalculateFinalQuality(Request != null
            ? Request.QualityMax
            : 0.0f);

        private float CalculateFinalQuality(float value)
        {
            return Mathf.Clamp(
                value * CalibrationQualityMultiplier - PauseQualityPenalty,
                0.0f,
                90.0f);
        }
    }
}

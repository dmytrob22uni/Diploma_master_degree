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

        /// <summary>
        /// Size/workload compensation captured when the job starts. Applied
        /// to time, filament, distance, fan and fumes wear; use wear has its
        /// own multiplier and hard-stop damage remains fixed.
        /// </summary>
        public float WearWorkloadMultiplier = 1.0f;

        /// <summary>
        /// Scale factor for one-use wear (bed and plate). Uses are flat per
        /// job, so this stores the compressed work factor directly rather
        /// than the inverse compensation used for time/filament wear.
        /// </summary>
        public float WearUseMultiplier = 1.0f;

        public int PauseCount;
        public float PauseQualityPenalty;

        /// <summary>
        /// Repeated pauses are capped so a service-heavy job cannot drive a
        /// print to zero quality. The raw penalty is still retained for UI
        /// diagnostics.
        /// </summary>
        public float EffectivePauseQualityPenalty =>
            Mathf.Min(
                PauseQualityPenalty,
                PrintQualityEstimator.MaximumPauseQualityPenalty);

        public float CalibrationQualityMultiplier = 1.0f;
        public float FinalQuality = -1.0f;

        /// <summary>
        /// Whether this job used a calibrated printer when its final quality
        /// was evaluated. Kept after finalization so the console warning
        /// describes the finished/partial job instead of the next one.
        /// </summary>
        public bool CalibrationApplied;

        /// <summary>Enclosure state captured when this job actually started.</summary>
        public bool StartedWithEnclosure;

        /// <summary>One-way loss flag; reinstalling never clears it.</summary>
        public bool EnclosureBonusLost;

        /// <summary>Outcome latched when printing finishes.</summary>
        public bool EnclosureBonusRetained;

        /// <summary>True once the enclosure outcome is frozen for this job.</summary>
        public bool EnclosureOutcomeLatched;

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
            float cap = Request != null
                ? Request.QualityCap
                : PrintQualityEstimator.MaximumRawQuality;

            float raw = PrintQualityEstimator.ApplyEnclosure(
                value,
                cap,
                EnclosureBonusRetained);

            return Mathf.Clamp(
                raw * CalibrationQualityMultiplier -
                EffectivePauseQualityPenalty,
                0.0f,
                PrintQualityEstimator.MaximumRawQuality);
        }
    }
}

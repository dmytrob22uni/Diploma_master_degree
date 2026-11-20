using UnityEngine;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;
using Coursework_Master_Degree.ScriptableObjects.Items.Wear;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Remaining filament for the selected spool. The authoritative value is
    /// the spool's wear condition; a local fallback keeps the simulation
    /// running when a spool asset uses an unexpected wear logic type.
    /// </summary>
    public sealed class SpoolFilamentState
    {
        public ItemFFFSpoolDataHolder Data { get; }
        public ItemWearState Wear { get; }

        public float RemainingMeters { get; private set; }

        public float Condition => Wear != null
            ? Mathf.Clamp(Wear.CurrentCondition, 0.0f, Wear.MaxCondition)
            : Data != null && Data.FilamentLengthSpoolMeters > 0.001f
                ? RemainingMeters / Data.FilamentLengthSpoolMeters * 100.0f
                : 0.0f;

        private float fallbackRemainingMeters;
        private bool warnedAboutLogic;

        private SpoolFilamentState(
            ItemFFFSpoolDataHolder data,
            ItemWearState wear)
        {
            Data = data;
            Wear = wear;
            fallbackRemainingMeters = data != null
                ? data.FilamentLengthSpoolMeters
                : 0.0f;

            Refresh();
        }

        public static bool TryCreate(
            PartLocation location,
            out SpoolFilamentState state)
        {
            state = null;

            if (location == null ||
                !location.IsOccupied ||
                location.InstalledPart == null)
            {
                return false;
            }

            ItemFFFSpoolDataHolder data =
                location.InstalledPart.GetComponentInChildren<
                    ItemFFFSpoolDataHolder>(true);

            if (data == null)
            {
                return false;
            }

            ItemWearState wear =
                data.GetComponentInChildren<ItemWearState>(true);

            state = new SpoolFilamentState(data, wear);
            return true;
        }

        public void ApplyConsumed(float meters)
        {
            if (meters <= 0.0f)
            {
                return;
            }

            if (HasFilamentWearLogic)
            {
                Wear.ApplyWear(new WearDeltaFilament
                {
                    FilamentLength = meters,
                    FilamentLengthSpool = Mathf.Max(
                        0.001f,
                        Data.FilamentLengthSpoolMeters),
                });
            }
            else
            {
                if (!warnedAboutLogic && Wear != null)
                {
                    warnedAboutLogic = true;
                    Debug.LogWarning(
                        $"[SpoolFilamentState] {Data.name} uses " +
                        $"{Wear.ItemWearLogicSO?.GetType().Name ?? "no wear logic"}; " +
                        "using direct condition loss for filament consumption.",
                        Data);
                }

                float directLoss = Data.FilamentLengthSpoolMeters > 0.001f
                    ? meters / Data.FilamentLengthSpoolMeters * 100.0f
                    : 0.0f;

                Wear?.ApplyConditionLoss(directLoss);

                fallbackRemainingMeters = Mathf.Max(
                    0.0f,
                    fallbackRemainingMeters - meters);
            }

            if (Data != null)
            {
                Data.RotateForFilament(meters);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (Data == null)
            {
                RemainingMeters = 0.0f;
                return;
            }

            if (Wear != null)
            {
                float condition = Mathf.Clamp(
                    Wear.CurrentCondition,
                    0.0f,
                    Wear.MaxCondition);

                RemainingMeters =
                    Data.FilamentLengthSpoolMeters * (condition / 100.0f);
                Data.RefreshWearVisual(condition);
            }
            else
            {
                RemainingMeters = Mathf.Clamp(
                    fallbackRemainingMeters,
                    0.0f,
                    Data.FilamentLengthSpoolMeters);

                float condition = Data.FilamentLengthSpoolMeters > 0.001f
                    ? RemainingMeters / Data.FilamentLengthSpoolMeters * 100.0f
                    : 0.0f;

                Data.RefreshWearVisual(condition);
            }
        }

        private bool HasFilamentWearLogic =>
            Wear != null &&
            Wear.ItemWearLogicSO is ItemWearLogicFilamentSO;
    }
}

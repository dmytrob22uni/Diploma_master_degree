using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    /// <summary>
    /// Authoring baseline and target lifetimes for the wear calibration tool.
    /// Runtime wear does not read this asset; it exists so the target print
    /// counts survive editor sessions and can be reviewed as data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "wear_calibration_profile_SO",
        menuName = "Game Items/Wear/Wear Calibration Profile",
        order = 20)]
    public sealed class WearCalibrationProfileSO : ScriptableObject
    {
        [Header("Medium print baseline")]
        [Min(0.01f)]
        public float PrintHours = 2.23f;

        [Min(0.0f)]
        public float PreheatCoolingHours = 0.25f;

        [Min(0.01f)]
        public float FilamentMeters = 28.53f;

        [Range(0.0f, 1.0f)]
        public float MaterialHardness = 0.75f;

        [Range(0.0f, 1.0f)]
        public float MaterialAbrasiveness = 0.15f;

        public float NozzleTemperature = 215.0f;
        public float BedTemperature = 60.0f;
        public float ChamberTemperature = 25.0f;

        [Min(0.0f)]
        public float FanRPM = 3000.0f;

        [Min(0.0f)]
        public float FumesDensity = 1.0f;

        [Min(0.0f)]
        public float AxisTravelMetersPerSecond = 0.15f;

        [Tooltip(
            "Used only for parts with EnclosureAware temperature source. " +
            "The wear callbacks use the chamber under an enclosure and the " +
            "bed otherwise.")]
        public bool UseEnclosure = true;

        [Header("Targets")]
        [Tooltip(
            "Target number of medium prints before a part reaches 0 " +
            "condition. The editor computes each coefficient from this.")]
        public List<WearCalibrationTarget> Targets =
            new List<WearCalibrationTarget>();

        public float HotHours => Mathf.Max(0.0f, PrintHours) +
            Mathf.Max(0.0f, PreheatCoolingHours);

        public float AxisKilometers =>
            Mathf.Max(0.0f, PrintHours) * 3600.0f *
            Mathf.Max(0.0f, AxisTravelMetersPerSecond) / 1000.0f;

        private void OnValidate()
        {
            PrintHours = Mathf.Max(0.01f, PrintHours);
            PreheatCoolingHours = Mathf.Max(0.0f, PreheatCoolingHours);
            FilamentMeters = Mathf.Max(0.01f, FilamentMeters);
            MaterialHardness = Mathf.Clamp01(MaterialHardness);
            MaterialAbrasiveness = Mathf.Clamp01(MaterialAbrasiveness);
            FanRPM = Mathf.Max(0.0f, FanRPM);
            FumesDensity = Mathf.Max(0.0f, FumesDensity);
            AxisTravelMetersPerSecond =
                Mathf.Max(0.0f, AxisTravelMetersPerSecond);

            if (Targets == null)
            {
                Targets = new List<WearCalibrationTarget>();
            }

            for (int i = 0; i < Targets.Count; i++)
            {
                WearCalibrationTarget target = Targets[i];

                if (target != null)
                {
                    target.TargetPrints = Mathf.Max(
                        0.1f,
                        target.TargetPrints);
                }
            }
        }
    }

    [Serializable]
    public sealed class WearCalibrationTarget
    {
        public ItemWearLogicSO Logic;

        [Min(0.1f)]
        public float TargetPrints = 3.0f;
    }
}

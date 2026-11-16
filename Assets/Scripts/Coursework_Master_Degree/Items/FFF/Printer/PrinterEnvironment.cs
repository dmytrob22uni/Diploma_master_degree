using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Thermal context for a printer. M0 only stores the values; M5 uses them
    /// to model chamber heat retention, drafts, and print quality.
    /// </summary>
    public class PrinterEnvironment : MonoBehaviour
    {
        [Header("Enclosure")]
        public bool HasEnclosure;

        [Header("Environment")]
        [Tooltip("Room temperature in degrees Celsius")]
        [Min(0.0f)]
        public float AmbientTemperature = 21.0f;

        [Tooltip("Chamber temperature in degrees Celsius. Ignored without an enclosure.")]
        [Min(0.0f)]
        public float ChamberTemperature = 21.0f;

        [Tooltip("0 = still air, 1 = strong draft")]
        [Range(0.0f, 1.0f)]
        public float DraftFactor = 0.5f;

        [Tooltip("0 = no filtration, 1 = fully filtered enclosure")]
        [Range(0.0f, 1.0f)]
        public float ParticulateFactor = 0.0f;

        /// <summary>
        /// Temperature the print actually experiences. Without an enclosure the
        /// chamber cannot hold heat above ambient.
        /// </summary>
        public float EffectiveChamberTemperature => HasEnclosure
            ? Mathf.Max(AmbientTemperature, ChamberTemperature)
            : AmbientTemperature;

        private void OnValidate()
        {
            if (!HasEnclosure)
            {
                ChamberTemperature = AmbientTemperature;
            }
        }
    }
}

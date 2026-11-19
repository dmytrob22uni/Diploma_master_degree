using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Thermal context for a printer. M3 uses enclosure presence for chamber
    /// heat retention, cooling slowdown and raw quality. M5 adds draft,
    /// particulate, warping and long-pause adhesion effects.
    /// </summary>
    public class PrinterEnvironment : MonoBehaviour
    {
        [Header("Enclosure")]
        public bool HasEnclosure;

        [Tooltip("Detect a PrinterEnclosure around this printer at runtime.")]
        public bool DetectEnclosure = true;

        [Tooltip("Optional explicit enclosure; auto-filled by containment detection.")]
        public PrinterEnclosure Enclosure;

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

        private void Awake()
        {
            if (DetectEnclosure)
            {
                Enclosure = FindEnclosure();

                if (!HasEnclosure)
                {
                    HasEnclosure = Enclosure != null;
                }
            }

            if (!HasEnclosure)
            {
                ChamberTemperature = AmbientTemperature;
            }
        }

        private void OnValidate()
        {
            if (!HasEnclosure)
            {
                ChamberTemperature = AmbientTemperature;
            }
        }

        private PrinterEnclosure FindEnclosure()
        {
            PrinterEnclosure[] enclosures =
                FindObjectsByType<PrinterEnclosure>(
                    FindObjectsInactive.Exclude);

            for (int i = 0; i < enclosures.Length; i++)
            {
                if (enclosures[i] != null &&
                    enclosures[i].Contains(transform.position))
                {
                    Printer owner = GetComponent<Printer>();

                    if (owner == null)
                    {
                        owner = GetComponentInParent<Printer>();
                    }

                    enclosures[i].BindPrinter(owner);
                    return enclosures[i];
                }
            }

            return null;
        }
    }
}

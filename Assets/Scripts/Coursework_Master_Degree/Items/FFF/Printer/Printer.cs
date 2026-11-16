using UnityEngine;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Printer root. Owns the hardware references that later milestones use
    /// for jobs, motion, wear, and binding. M0 only validates the wiring.
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

        public PrintBedAnchor ResolvedBedAnchor =>
            BedAnchor != null ? BedAnchor : Motion != null ? Motion.BedAnchor : null;

        private void Reset()
        {
            AutoAssign();
        }

        private void Awake()
        {
            AutoAssign();
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

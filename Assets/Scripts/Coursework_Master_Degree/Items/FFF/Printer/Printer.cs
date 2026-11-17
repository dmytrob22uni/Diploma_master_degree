using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF
{
    public enum PrinterOperationalState
    {
        Idle,
        Preheating,
        Printing,
        Paused,
        Cooling,
        Finished,
        Maintenance,
        Fault
    }

    /// <summary>
    /// Printer root. Owns the hardware references used for jobs, motion, wear,
    /// and binding. M1 adds the control relationship with a computing device;
    /// the printer keeps ownership of its job and state.
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

        /// <summary>
        /// Device currently controlling this printer. A printer has at most one
        /// controller. Removing the binding never changes the printer state.
        /// </summary>
        public Device BoundDevice { get; private set; }

        public bool IsBound => BoundDevice != null;

        /// <summary>
        /// Placeholder until the printer FSM arrives in M3. Kept on the printer
        /// so the console can bind to a live status source now.
        /// </summary>
        public PrinterOperationalState OperationalState { get; private set; } =
            PrinterOperationalState.Idle;

        public string DisplayName
        {
            get
            {
                if (ItemData != null &&
                    ItemData.ItemMetaData != null &&
                    !string.IsNullOrWhiteSpace(ItemData.ItemMetaData.Name))
                {
                    return ItemData.ItemMetaData.Name;
                }

                return gameObject.name;
            }
        }

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

        internal bool BindToDevice(Device device)
        {
            if (device == null)
            {
                return false;
            }

            if (BoundDevice == device)
            {
                return true;
            }

            if (BoundDevice != null)
            {
                return false;
            }

            BoundDevice = device;
            return true;
        }

        internal void UnbindFromDevice(Device device)
        {
            if (device == null || BoundDevice != device)
            {
                return;
            }

            BoundDevice = null;
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

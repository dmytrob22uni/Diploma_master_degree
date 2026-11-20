using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Runtime metadata attached to a printed object instance. It survives
    /// removal from the printer bed so quality and partial state remain
    /// available when the object is placed or sold like a normal item.
    /// </summary>
    public sealed class PrintedObjectDataHolder : MonoBehaviour
    {
        public Printer Printer { get; private set; }

        public PrinterJob Job { get; private set; }

        /// <summary>Final quality in 0..90, or -1 for a partial print.</summary>
        public float Quality { get; private set; } = -1.0f;

        public bool IsPartial { get; private set; }

        public bool IsCompleted { get; private set; }

        /// <summary>True once cooling and parking are complete.</summary>
        public bool IsFinalized { get; private set; }

        public bool IsReleased { get; private set; }

        public bool IsOnPrintBed => Printer != null && !IsReleased;

        public void Bind(Printer printer, PrinterJob job)
        {
            Printer = printer;
            Job = job;
        }

        public void MarkFinalized(
            PrinterJob job,
            bool isPartial,
            float quality)
        {
            Job = job;
            IsPartial = isPartial;
            IsCompleted = !isPartial;
            Quality = isPartial ? -1.0f : Mathf.Clamp(quality, 0.0f, 90.0f);
            IsFinalized = true;
        }

        public void Release()
        {
            Printer = null;
            IsReleased = true;
        }
    }
}

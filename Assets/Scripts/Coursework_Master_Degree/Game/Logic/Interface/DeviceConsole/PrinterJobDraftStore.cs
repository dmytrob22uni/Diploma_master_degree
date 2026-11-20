using System.Collections.Generic;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// Per-printer console drafts. Each printer keeps its selected object,
    /// spool and per-object job settings; switching printers or objects in the
    /// console restores the matching draft instead of resetting it.
    /// </summary>
    internal sealed class PrinterJobDraftStore
    {
        internal sealed class ObjectDraft
        {
            public InfillPattern Infill;
            public SupportsMode Supports;
            public LayerHeightPreset LayerHeight = LayerHeightPreset.Normal;
            public PrintSpeedPreset Speed = PrintSpeedPreset.Normal;
            public PrintPlacement Placement;

            public ObjectDraft(ObjectToPrintMetaSO meta)
            {
                Infill = meta != null && meta.Requirements != null
                    ? meta.Requirements.DefaultInfill
                    : InfillPattern.Grid;

                Supports = meta != null &&
                    meta.PrintProfile != null &&
                    meta.PrintProfile.SupportsRequired
                        ? SupportsMode.Auto
                        : SupportsMode.None;
            }
        }

        internal sealed class PrinterDraft
        {
            public int SelectedIndex;
            public SpoolSlot SpoolSlot = SpoolSlot.Left;
            public bool SpoolInitialized;

            private readonly Dictionary<ObjectToPrintMetaSO, ObjectDraft> objects =
                new Dictionary<ObjectToPrintMetaSO, ObjectDraft>();

            public void Clear()
            {
                SelectedIndex = 0;
                SpoolSlot = SpoolSlot.Left;
                SpoolInitialized = false;
                objects.Clear();
            }

            public ObjectDraft GetObject(ObjectToPrintMetaSO meta)
            {
                if (meta == null)
                {
                    return null;
                }

                if (!objects.TryGetValue(meta, out ObjectDraft draft))
                {
                    draft = new ObjectDraft(meta);
                    objects.Add(meta, draft);
                }

                return draft;
            }
        }

        private readonly Dictionary<Printer, PrinterDraft> printers =
            new Dictionary<Printer, PrinterDraft>();

        public PrinterDraft Fallback { get; } = new PrinterDraft();

        public PrinterDraft For(Printer printer)
        {
            if (printer == null)
            {
                return Fallback;
            }

            if (!printers.TryGetValue(printer, out PrinterDraft draft))
            {
                draft = new PrinterDraft();
                printers.Add(printer, draft);
            }

            return draft;
        }

        public void Clear()
        {
            printers.Clear();
            Fallback.Clear();
        }
    }
}

using UnityEngine;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Marks a physical enclosure and owns the sockets used when a printer is
    /// installed. A contained printer also gets the enclosure thermal and
    /// quality treatment automatically.
    /// </summary>
    public class PrinterEnclosure : MonoBehaviour
    {
        [Tooltip("Extra space around the renderer bounds when testing containment.")]
        public Vector3 BoundsPadding = new Vector3(0.05f, 0.05f, 0.05f);

        [Header("Printer docking")]
        [Tooltip("Socket for the printer's single external spool.")]
        public PartLocation SpoolLocation;

        [Tooltip("Socket for the battery closet when a printer is installed.")]
        public PartLocation BatteryClosetLocation;

        [Tooltip("Socket for the battery closet adapter when a printer is installed.")]
        public PartLocation BatteryClosetAdapterLocation;

        [Tooltip("Free-part reset point for the battery closet plug.")]
        public Transform BatteryClosetPlugLocation;

        [Tooltip("Free-part reset point for the wall socket plug.")]
        public Transform WallSocketPlugLocation;

        [Header("Print requirements")]
        [Tooltip("Enclosure parts that must be installed before printing.")]
        public PartLocation[] CriticalPartLocations;

        public Printer Printer { get; private set; }

        public void BindPrinter(Printer printer)
        {
            Printer = printer;
        }

        public void UnbindPrinter(Printer printer)
        {
            if (Printer == printer)
            {
                Printer = null;
            }
        }

        public string GetPrinterInstallBlockReason(Printer printer)
        {
            if (printer == null)
            {
                return null;
            }

            int spoolCount = printer.InstalledSpoolCount;

            if (spoolCount > 1)
            {
                return "Remove one spool before installing the printer in the enclosure.";
            }

            if (spoolCount == 1 &&
                SpoolLocation != null &&
                SpoolLocation.IsOccupied)
            {
                return "The enclosure spool slot is already occupied.";
            }

            if (printer.BatteryClosetLocation != null &&
                printer.BatteryClosetLocation.IsOccupied &&
                BatteryClosetLocation != null &&
                BatteryClosetLocation.IsOccupied)
            {
                return "The enclosure battery closet slot is already occupied.";
            }

            if (printer.BatteryClosetAdapterLocation != null &&
                printer.BatteryClosetAdapterLocation.IsOccupied &&
                BatteryClosetAdapterLocation != null &&
                BatteryClosetAdapterLocation.IsOccupied)
            {
                return "The enclosure battery closet adapter slot is already occupied.";
            }

            return null;
        }

        public bool Contains(Vector3 worldPosition)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            bounds.Expand(BoundsPadding * 2.0f);
            return bounds.Contains(worldPosition);
        }
    }
}

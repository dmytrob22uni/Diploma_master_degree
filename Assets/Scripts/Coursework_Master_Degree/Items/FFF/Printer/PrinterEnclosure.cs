using System;
using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF
{
    [Serializable]
    public sealed class PrinterEnclosureSpoolSocket
    {
        [Tooltip(
            "Optional printer spool-slot id. Empty means this socket can take " +
            "any available printer spool.")]
        public string PrinterSlotId;

        [Tooltip("Part location for the external spool socket.")]
        public PartLocation Location;
    }

    [Serializable]
    public sealed class PrinterDockingSource
    {
        [Tooltip("Role matched against PrinterEnclosure docking targets.")]
        public string Role;

        [Tooltip("Friendly label used by conflict messages.")]
        public string Label;

        [Tooltip("Printer socket for this dockable accessory.")]
        public PartLocation Location;
    }

    [Serializable]
    public sealed class PrinterEnclosureDockingTarget
    {
        [Tooltip("Role matched against the printer docking sources.")]
        public string Role;

        [Tooltip("Friendly label used by conflict messages.")]
        public string Label;

        [Tooltip("Enclosure socket for this dockable accessory.")]
        public PartLocation Location;
    }

    [Serializable]
    public sealed class PrinterFreePartDockingSource
    {
        [Tooltip("Role matched against PrinterEnclosure free-part targets.")]
        public string Role;

        [Tooltip("Friendly label used by setup/debug messages.")]
        public string Label;

        [Tooltip("Free part moved to the matching enclosure reset point.")]
        public ItemFreePartDataHolder Part;
    }

    [Serializable]
    public sealed class PrinterEnclosureFreePartDockingTarget
    {
        [Tooltip("Role matched against the printer free-part sources.")]
        public string Role;

        [Tooltip("Friendly label used by setup/debug messages.")]
        public string Label;

        [Tooltip("Enclosure free-part reset transform for this role.")]
        public Transform Location;
    }

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
        [Tooltip("External spool sockets. A printer slot is mapped to one socket while docked.")]
        public List<PrinterEnclosureSpoolSocket> SpoolSockets =
            new List<PrinterEnclosureSpoolSocket>();

        [Tooltip("Dockable installed accessories. Source roles live on the printer.")]
        public List<PrinterEnclosureDockingTarget> DockingTargets =
            new List<PrinterEnclosureDockingTarget>();

        [Tooltip("Dockable free parts and their enclosure reset points.")]
        public List<PrinterEnclosureFreePartDockingTarget>
            FreePartDockingTargets =
                new List<PrinterEnclosureFreePartDockingTarget>();

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
            int socketCount = CountSpoolSockets();
            int emptySocketCount = CountEmptySpoolSockets();

            if (spoolCount > socketCount)
            {
                return socketCount == 0
                    ? "The enclosure has no spool sockets."
                    : "The enclosure does not have enough spool sockets.";
            }

            if (spoolCount > emptySocketCount)
            {
                return socketCount == 1
                    ? "The enclosure spool slot is already occupied."
                    : "The enclosure does not have enough free spool sockets.";
            }

            if (DockingTargets != null)
            {
                foreach (PrinterEnclosureDockingTarget target in DockingTargets)
                {
                    if (target == null || target.Location == null)
                    {
                        continue;
                    }

                    PrinterDockingSource source =
                        printer.GetDockingSource(target.Role);

                    if (source?.Location != null &&
                        source.Location.IsOccupied &&
                        target.Location.IsOccupied)
                    {
                        return "The enclosure " +
                            $"{DockingLabel(target.Label, target.Role)} " +
                            "slot is already occupied.";
                    }
                }
            }

            return null;
        }

        public PrinterEnclosureDockingTarget FindDockingTarget(string role)
        {
            if (DockingTargets == null || string.IsNullOrEmpty(role))
            {
                return null;
            }

            foreach (PrinterEnclosureDockingTarget target in DockingTargets)
            {
                if (target != null && target.Role == role)
                {
                    return target;
                }
            }

            return null;
        }

        public PrinterEnclosureFreePartDockingTarget
            FindFreePartDockingTarget(string role)
        {
            if (FreePartDockingTargets == null ||
                string.IsNullOrEmpty(role))
            {
                return null;
            }

            foreach (PrinterEnclosureFreePartDockingTarget target in
                FreePartDockingTargets)
            {
                if (target != null && target.Role == role)
                {
                    return target;
                }
            }

            return null;
        }

        private static string DockingLabel(string label, string role)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return string.IsNullOrWhiteSpace(role)
                ? "docking"
                : role.Replace('_', ' ');
        }

        public int CountSpoolSockets()
        {
            if (SpoolSockets == null)
            {
                return 0;
            }

            int count = 0;

            foreach (PrinterEnclosureSpoolSocket socket in SpoolSockets)
            {
                if (socket != null && socket.Location != null)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountEmptySpoolSockets()
        {
            if (SpoolSockets == null)
            {
                return 0;
            }

            int count = 0;

            foreach (PrinterEnclosureSpoolSocket socket in SpoolSockets)
            {
                if (socket != null &&
                    socket.Location != null &&
                    !socket.Location.IsOccupied)
                {
                    count++;
                }
            }

            return count;
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

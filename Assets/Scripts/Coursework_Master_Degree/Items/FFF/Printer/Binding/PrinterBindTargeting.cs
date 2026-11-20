using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Center-screen targeting used while the player binds or unbinds printers.
    /// Hover is resolved against the highlight spheres, so the player only
    /// needs to aim at the visible bubble around a printer. A short
    /// line-of-sight check keeps walls and other printers blocking the ray,
    /// while the target printer's own colliders and its installed enclosure
    /// are ignored. Input and state transitions live in DeviceController and
    /// GameStateDeviceBind.
    /// </summary>
    public class PrinterBindTargeting : MonoBehaviour
    {
        [Header("Targeting")]
        public Camera PlayerCamera;

        [Tooltip("Maximum distance from the eye to a printer")]
        [Min(0.1f)]
        public float MaxDistance = 3.0f;

        [Tooltip("Everything that can block the bind ray")]
        public LayerMask RaycastMask = ~0;

        [Header("Read only")]
        [SerializeField]
        private Printer hoveredPrinter;

        private Device device;
        private DeviceBindMode mode;
        private Printer[] printers = System.Array.Empty<Printer>();
        private readonly RaycastHit[] occlusionHits = new RaycastHit[32];
        private bool isActive;

        public Printer HoveredPrinter => hoveredPrinter;

        public void Begin(Device targetDevice, DeviceBindMode bindMode)
        {
            device = targetDevice;
            mode = bindMode;
            isActive = true;
            hoveredPrinter = null;

            CachePrinters();
            RefreshHighlights();

            if (PlayerCamera == null)
            {
                PlayerCamera = Camera.main;
            }

            if (PlayerCamera == null)
            {
                Debug.LogError(
                    "[PrinterBindTargeting] No player camera assigned.",
                    this);
            }
        }

        public void End()
        {
            isActive = false;

            if (hoveredPrinter != null && hoveredPrinter.BindHighlight != null)
            {
                hoveredPrinter.BindHighlight.SetHovered(false);
            }

            hoveredPrinter = null;

            for (int i = 0; i < printers.Length; i++)
            {
                Printer printer = printers[i];
                if (printer != null && printer.BindHighlight != null)
                {
                    printer.BindHighlight.Hide();
                }
            }

            printers = System.Array.Empty<Printer>();
            device = null;
        }

        private void Update()
        {
            if (!isActive || PlayerCamera == null)
            {
                return;
            }

            Printer target = FindPrinterUnderCrosshair();

            if (target == hoveredPrinter)
            {
                return;
            }

            if (hoveredPrinter != null && hoveredPrinter.BindHighlight != null)
            {
                hoveredPrinter.BindHighlight.SetHovered(false);
            }

            hoveredPrinter = target;

            if (hoveredPrinter != null && hoveredPrinter.BindHighlight != null)
            {
                hoveredPrinter.BindHighlight.SetHovered(true);
            }
        }

        private Printer FindPrinterUnderCrosshair()
        {
            Ray ray = PlayerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0.0f));

            Vector3 direction = ray.direction.normalized;
            Printer best = null;
            float bestDistance = MaxDistance;

            for (int i = 0; i < printers.Length; i++)
            {
                Printer printer = printers[i];

                if (printer == null ||
                    printer.BindHighlight == null ||
                    !IsHighlighted(printer))
                {
                    continue;
                }

                if (!printer.BindHighlight.TryGetTargetSphere(
                        out Vector3 center,
                        out float radius))
                {
                    continue;
                }

                if (!TryGetSphereEntry(
                        ray.origin,
                        direction,
                        center,
                        radius,
                        out float entry) ||
                    entry > bestDistance)
                {
                    continue;
                }

                if (IsOccluded(ray, entry, printer))
                {
                    continue;
                }

                best = printer;
                bestDistance = entry;
            }

            return best;
        }

        private bool IsOccluded(Ray ray, float distance, Printer target)
        {
            if (distance <= 0.001f)
            {
                return false;
            }

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                occlusionHits,
                distance,
                RaycastMask,
                QueryTriggerInteraction.Ignore);

            PartsAssembly targetEnclosure =
                PartsAssembly.FindEnclosingPrinterEnclosure(target);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = occlusionHits[i].collider;

                if (hitCollider == null ||
                    IsTargetOwnedCollider(
                        hitCollider,
                        target,
                        targetEnclosure))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsTargetOwnedCollider(
            Collider collider,
            Printer target,
            PartsAssembly targetEnclosure)
        {
            // Ignore the target printer's own colliders.
            if (collider.GetComponentInParent<Printer>() == target)
            {
                return true;
            }

            // Ignore the enclosure assembly that holds the installed printer.
            // Its colliders still block other printers normally.
            return targetEnclosure != null &&
                collider.transform.IsChildOf(targetEnclosure.transform);
        }

        private static bool TryGetSphereEntry(
            Vector3 origin,
            Vector3 direction,
            Vector3 center,
            float radius,
            out float entry)
        {
            Vector3 toCenter = center - origin;
            float projected = Vector3.Dot(toCenter, direction);
            float sqrDistance =
                toCenter.sqrMagnitude - projected * projected;
            float sqrRadius = radius * radius;

            if (sqrDistance > sqrRadius)
            {
                entry = 0.0f;
                return false;
            }

            float halfChord = Mathf.Sqrt(
                Mathf.Max(0.0f, sqrRadius - sqrDistance));

            entry = projected - halfChord;

            if (entry < 0.0f)
            {
                entry = projected + halfChord;
            }

            return entry >= 0.0f;
        }

        private bool IsHighlighted(Printer printer)
        {
            if (printer == null || printer.BindHighlight == null)
            {
                return false;
            }

            return printer.BindHighlight.State !=
                PrinterBindHighlightState.Hidden;
        }

        private void CachePrinters()
        {
            printers = Object.FindObjectsByType<Printer>(
                FindObjectsInactive.Exclude);
        }

        private void RefreshHighlights()
        {
            for (int i = 0; i < printers.Length; i++)
            {
                Printer printer = printers[i];

                if (printer == null || printer.BindHighlight == null)
                {
                    continue;
                }

                if (mode == DeviceBindMode.Unbind)
                {
                    if (printer.BoundDevice == device)
                    {
                        printer.BindHighlight.Show(
                            PrinterBindHighlightState.SelectedForUnbind);
                    }
                    else
                    {
                        printer.BindHighlight.Hide();
                    }

                    continue;
                }

                if (printer.BoundDevice == null)
                {
                    printer.BindHighlight.Show(
                        PrinterBindHighlightState.Bindable);
                }
                else if (printer.BoundDevice == device)
                {
                    printer.BindHighlight.Show(
                        PrinterBindHighlightState.BoundToThisDevice);
                }
                else
                {
                    printer.BindHighlight.Show(
                        PrinterBindHighlightState.BoundToOtherDevice);
                }
            }
        }
    }
}

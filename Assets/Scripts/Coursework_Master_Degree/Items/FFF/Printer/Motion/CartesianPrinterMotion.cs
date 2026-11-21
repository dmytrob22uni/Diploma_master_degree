using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Prusa MK4S style motion: the extruder moves in X and Z, the bed moves
    /// in Y. Axis transforms and directions are serialized, so the same class
    /// can drive other cartesian printers after recalibration.
    /// </summary>
    public class CartesianPrinterMotion : PrinterMotionBase
    {
        [Header("Build Volume (metres)")]
        [SerializeField]
        [Tooltip("Usable print volume: X width, Y height, Z depth.")]
        private Vector3 buildVolume = new Vector3(0.25f, 0.21f, 0.21f);

        [Header("Axes")]
        [SerializeField]
        [Tooltip("MK4S: base/x_axis_base/extruder_base, local X")]
        private AxisBinding horizontalX = new AxisBinding
        {
            LocalAxis = Vector3.right
        };

        [SerializeField]
        [Tooltip(
            "MK4S: base/print_bed_holder_base.\n" +
            "The bed carriage parent is rotated, so its local Y is world " +
            "front/back.")]
        private AxisBinding horizontalZ = new AxisBinding
        {
            LocalAxis = Vector3.up
        };

        [SerializeField]
        [Tooltip(
            "MK4S: base/x_axis_base.\n" +
            "The gantry parent is rotated, so its local Z is world up/down.")]
        private AxisBinding verticalY = new AxisBinding
        {
            LocalAxis = Vector3.forward
        };

        [Header("Bed")]
        [SerializeField]
        private PrintBedAnchor bedAnchor = null;

        public override Vector3 BuildVolume => buildVolume;

        public override int AxisCount => 3;

        public override PrintBedAnchor BedAnchor => bedAnchor;

        protected override bool HorizontalZMovesBed => MovesBed(horizontalZ);

        public AxisBinding HorizontalX => horizontalX;

        public AxisBinding HorizontalZ => horizontalZ;

        public AxisBinding VerticalY => verticalY;

        private void Awake()
        {
            CacheHomePositions();
        }

        /// <summary>
        /// Captures authored local positions. Call again only if an axis was
        /// deliberately re-authored.
        /// </summary>
        public void CacheHomePositions()
        {
            horizontalX.CacheHome();
            horizontalZ.CacheHome();
            verticalY.CacheHome();
        }

        public override string GetAxisName(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return "Horizontal X (extruder)";
                case 1: return "Horizontal Z (bed)";
                case 2: return "Vertical Y (gantry)";
                default: return "Unknown";
            }
        }

        public override float GetAxisNormalized(int axisIndex)
        {
            AxisBinding binding = GetBinding(axisIndex);
            return binding != null ? binding.Normalized : 0.0f;
        }

        public override float GetAxisLocalOffset(int axisIndex)
        {
            AxisBinding binding = GetBinding(axisIndex);
            return binding != null ? binding.CurrentLocal : 0.0f;
        }

        public override void SetAxisNormalized(int axisIndex, float normalized)
        {
            AxisBinding binding = GetBinding(axisIndex);
            if (binding != null)
            {
                binding.SetNormalized(normalized);
            }
        }

        public override void SetAxisLocalOffset(int axisIndex, float offset)
        {
            AxisBinding binding = GetBinding(axisIndex);
            if (binding != null)
            {
                binding.SetLocalOffset(offset);
            }
        }

        public override void Home()
        {
            horizontalX.RestoreHome();
            horizontalZ.RestoreHome();
            verticalY.RestoreHome();
        }

        /// <summary>
        /// True when the bed anchor lives under the axis node, meaning that
        /// axis moves the bed instead of the nozzle. The home API stays in the
        /// printer-front frame; this is only the internal sign selection.
        /// </summary>
        private bool MovesBed(AxisBinding binding)
        {
            if (bedAnchor == null ||
                binding == null ||
                binding.Node == null)
            {
                return false;
            }

            Transform anchor = bedAnchor.transform;

            return anchor == binding.Node ||
                anchor.IsChildOf(binding.Node);
        }

        public override void ApplyNozzlePosition(Vector3 printSpacePosition)
        {
            float halfWidth = buildVolume.x * 0.5f;
            float halfDepth = buildVolume.z * 0.5f;

            SetAxisNormalized(
                0,
                Mathf.InverseLerp(-halfWidth, halfWidth, printSpacePosition.x));

            SetAxisNormalized(
                1,
                Mathf.InverseLerp(-halfDepth, halfDepth, printSpacePosition.z));

            SetBedHeight(printSpacePosition.y);
        }

        public override Vector3 GetNozzlePrintPosition()
        {
            float halfWidth = buildVolume.x * 0.5f;
            float halfDepth = buildVolume.z * 0.5f;

            return new Vector3(
                Mathf.Lerp(
                    -halfWidth,
                    halfWidth,
                    horizontalX.Normalized),
                buildVolume.y * verticalY.Normalized,
                Mathf.Lerp(
                    -halfDepth,
                    halfDepth,
                    horizontalZ.Normalized));
        }

        public override void SetBedHeight(float height)
        {
            float normalizedHeight = buildVolume.y > 0.0001f
                ? Mathf.Clamp01(height / buildVolume.y)
                : 0.0f;

            // 0 = nozzle just above the bed, 1 = full build height.
            // Calibrate VerticalY.MinLocal / MaxLocal with signed offsets:
            // a gantry moving up uses a positive MaxLocal, a dropping bed uses
            // a negative MaxLocal.
            SetAxisNormalized(2, normalizedHeight);
        }

        public override Vector3 GetNozzleWorldPosition()
        {
            return horizontalX.Node != null
                ? horizontalX.Node.position
                : transform.position;
        }

        private AxisBinding GetBinding(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return horizontalX;
                case 1: return horizontalZ;
                case 2: return verticalY;
                default: return null;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            DrawAxisRange(horizontalX, Color.red);
            DrawAxisRange(horizontalZ, Color.blue);
            DrawAxisRange(verticalY, Color.green);
        }

        private static void DrawAxisRange(AxisBinding binding, Color color)
        {
            if (binding == null || !binding.IsValid)
            {
                return;
            }

            Vector3 min = binding.GetWorldPositionAtOffset(binding.MinLocal);
            Vector3 max = binding.GetWorldPositionAtOffset(binding.MaxLocal);

            Gizmos.color = color;
            Gizmos.DrawLine(min, max);
            Gizmos.DrawWireSphere(min, 0.005f);
            Gizmos.DrawWireSphere(max, 0.005f);
        }
    }
}

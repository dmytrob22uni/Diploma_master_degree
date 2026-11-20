using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Visual CoreXY motion model. X and Z are composed onto the same
    /// horizontal carriage because CoreXY mechanics couple those print-space
    /// axes. Belt/motor dynamics are not simulated; the carriage reaches the
    /// requested print-space position directly.
    /// </summary>
    public class CoreXYPrinterMotion : PrinterMotionBase
    {
        [Header("Build Volume (metres)")]
        [SerializeField]
        [Tooltip("Usable print volume: X width, Y height, Z depth.")]
        private Vector3 buildVolume = new Vector3(0.25f, 0.21f, 0.21f);

        [Header("Coupled horizontal carriage")]
        [SerializeField]
        [Tooltip("Transform that carries the nozzle in print-space X and Z.")]
        private Transform carriage = null;

        [SerializeField]
        [Tooltip("Print-space X direction in the carriage parent's local space.")]
        private Vector3 horizontalXLocal = Vector3.right;

        [SerializeField]
        [Tooltip("Print-space Z direction in the carriage parent's local space.")]
        private Vector3 horizontalZLocal = Vector3.forward;

        [SerializeField]
        [Tooltip("Signed travel offset at print-space X = 0.")]
        private float horizontalXMinLocal = -0.127f;

        [SerializeField]
        [Tooltip("Signed travel offset at print-space X = 1.")]
        private float horizontalXMaxLocal = 0.127f;

        [SerializeField]
        [Tooltip("Signed travel offset at print-space Z = 0.")]
        private float horizontalZMinLocal = -0.105f;

        [SerializeField]
        [Tooltip("Signed travel offset at print-space Z = 1.")]
        private float horizontalZMaxLocal = 0.105f;

        [SerializeField]
        private bool invertHorizontalX = false;

        [SerializeField]
        private bool invertHorizontalZ = false;

        [Header("Vertical axis")]
        [SerializeField]
        [Tooltip(
            "Bed or gantry axis. MinLocal is the first-layer offset and " +
            "MaxLocal the full build-height offset.")]
        private AxisBinding verticalY = new AxisBinding
        {
            LocalAxis = Vector3.forward
        };

        [Header("Bed")]
        [SerializeField]
        private PrintBedAnchor bedAnchor = null;

        private Vector3 homeLocalPosition;
        private Vector3 horizontalXAxis = Vector3.right;
        private Vector3 horizontalZAxis = Vector3.forward;
        private float normalizedX;
        private float normalizedZ;
        private float currentXLocal;
        private float currentZLocal;

        public override Vector3 BuildVolume => buildVolume;

        public override int AxisCount => 3;

        public override PrintBedAnchor BedAnchor => bedAnchor;

        public Transform Carriage => carriage;

        public AxisBinding VerticalY => verticalY;

        private void Awake()
        {
            CacheHomePositions();
        }

        /// <summary>
        /// Captures the authored carriage pose and vertical-axis home.
        /// Call again only after deliberately re-authoring the mechanism.
        /// </summary>
        public void CacheHomePositions()
        {
            if (carriage != null)
            {
                homeLocalPosition = carriage.localPosition;
            }

            horizontalXAxis = NormalizedDirection(
                horizontalXLocal,
                Vector3.right);
            horizontalZAxis = NormalizedDirection(
                horizontalZLocal,
                Vector3.forward);

            verticalY.CacheHome();
            SyncFromLocal();
        }

        public override string GetAxisName(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return "Horizontal X (CoreXY carriage)";
                case 1: return "Horizontal Z (CoreXY carriage)";
                case 2: return "Vertical Y (bed/gantry)";
                default: return "Unknown";
            }
        }

        public override float GetAxisNormalized(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return normalizedX;
                case 1: return normalizedZ;
                case 2: return verticalY.Normalized;
                default: return 0.0f;
            }
        }

        public override float GetAxisLocalOffset(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return currentXLocal;
                case 1: return currentZLocal;
                case 2: return verticalY.CurrentLocal;
                default: return 0.0f;
            }
        }

        public override void SetAxisNormalized(int axisIndex, float normalized)
        {
            switch (axisIndex)
            {
                case 0:
                    normalizedX = Mathf.Clamp01(normalized);
                    currentXLocal = LocalFromNormalized(
                        normalizedX,
                        horizontalXMinLocal,
                        horizontalXMaxLocal,
                        invertHorizontalX);
                    ApplyCarriagePosition();
                    break;
                case 1:
                    normalizedZ = Mathf.Clamp01(normalized);
                    currentZLocal = LocalFromNormalized(
                        normalizedZ,
                        horizontalZMinLocal,
                        horizontalZMaxLocal,
                        invertHorizontalZ);
                    ApplyCarriagePosition();
                    break;
                case 2:
                    verticalY.SetNormalized(normalized);
                    break;
            }
        }

        public override void SetAxisLocalOffset(int axisIndex, float offset)
        {
            switch (axisIndex)
            {
                case 0:
                    currentXLocal = offset;
                    normalizedX = NormalizedFromLocal(
                        offset,
                        horizontalXMinLocal,
                        horizontalXMaxLocal,
                        invertHorizontalX);
                    ApplyCarriagePosition();
                    break;
                case 1:
                    currentZLocal = offset;
                    normalizedZ = NormalizedFromLocal(
                        offset,
                        horizontalZMinLocal,
                        horizontalZMaxLocal,
                        invertHorizontalZ);
                    ApplyCarriagePosition();
                    break;
                case 2:
                    verticalY.SetLocalOffset(offset);
                    break;
            }
        }

        public override void Home()
        {
            verticalY.RestoreHome();

            if (carriage != null)
            {
                carriage.localPosition = homeLocalPosition;
            }

            SyncFromLocal();
        }

        public override void ApplyNozzlePosition(Vector3 printSpacePosition)
        {
            float halfWidth = buildVolume.x * 0.5f;
            float halfDepth = buildVolume.z * 0.5f;

            SetAxisNormalized(
                0,
                Mathf.InverseLerp(
                    -halfWidth,
                    halfWidth,
                    printSpacePosition.x));

            SetAxisNormalized(
                1,
                Mathf.InverseLerp(
                    -halfDepth,
                    halfDepth,
                    printSpacePosition.z));

            SetBedHeight(printSpacePosition.y);
        }

        public override Vector3 GetNozzlePrintPosition()
        {
            float halfWidth = buildVolume.x * 0.5f;
            float halfDepth = buildVolume.z * 0.5f;

            return new Vector3(
                Mathf.Lerp(-halfWidth, halfWidth, normalizedX),
                buildVolume.y * verticalY.Normalized,
                Mathf.Lerp(-halfDepth, halfDepth, normalizedZ));
        }

        public override void SetBedHeight(float height)
        {
            float normalizedHeight = buildVolume.y > 0.0001f
                ? Mathf.Clamp01(height / buildVolume.y)
                : 0.0f;

            verticalY.SetNormalized(normalizedHeight);
        }

        public override Vector3 GetNozzleWorldPosition()
        {
            return carriage != null
                ? carriage.position
                : transform.position;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (carriage == null)
            {
                return;
            }

            Transform parent = carriage.parent;
            Vector3 origin = Application.isPlaying
                ? homeLocalPosition
                : carriage.localPosition;
            Vector3 localX = NormalizedDirection(horizontalXLocal, Vector3.right);
            Vector3 localZ = NormalizedDirection(horizontalZLocal, Vector3.forward);

            Gizmos.color = Color.red;
            DrawLocalRange(parent, origin, localX, horizontalXMinLocal, horizontalXMaxLocal);

            Gizmos.color = Color.blue;
            DrawLocalRange(parent, origin, localZ, horizontalZMinLocal, horizontalZMaxLocal);
        }

        private void SyncFromLocal()
        {
            if (carriage != null)
            {
                homeLocalPosition = carriage.localPosition;
                currentXLocal = 0.0f;
                currentZLocal = 0.0f;
            }

            normalizedX = NormalizedFromLocal(
                currentXLocal,
                horizontalXMinLocal,
                horizontalXMaxLocal,
                invertHorizontalX);

            normalizedZ = NormalizedFromLocal(
                currentZLocal,
                horizontalZMinLocal,
                horizontalZMaxLocal,
                invertHorizontalZ);
        }

        private void ApplyCarriagePosition()
        {
            if (carriage == null)
            {
                return;
            }

            carriage.localPosition =
                homeLocalPosition +
                horizontalXAxis * currentXLocal +
                horizontalZAxis * currentZLocal;
        }

        private static float LocalFromNormalized(
            float normalized,
            float min,
            float max,
            bool invert)
        {
            float t = invert ? 1.0f - normalized : normalized;
            return Mathf.Lerp(min, max, t);
        }

        private static float NormalizedFromLocal(
            float local,
            float min,
            float max,
            bool invert)
        {
            if (Mathf.Approximately(min, max))
            {
                return 0.0f;
            }

            float value = Mathf.InverseLerp(min, max, local);
            return invert ? 1.0f - value : value;
        }

        private static Vector3 NormalizedDirection(
            Vector3 direction,
            Vector3 fallback)
        {
            return direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : fallback;
        }

        private static void DrawLocalRange(
            Transform parent,
            Vector3 origin,
            Vector3 direction,
            float min,
            float max)
        {
            Vector3 minLocal = origin + direction * min;
            Vector3 maxLocal = origin + direction * max;

            if (parent != null)
            {
                minLocal = parent.TransformPoint(minLocal);
                maxLocal = parent.TransformPoint(maxLocal);
            }

            Gizmos.DrawLine(minLocal, maxLocal);
            Gizmos.DrawWireSphere(minLocal, 0.005f);
            Gizmos.DrawWireSphere(maxLocal, 0.005f);
        }
    }
}

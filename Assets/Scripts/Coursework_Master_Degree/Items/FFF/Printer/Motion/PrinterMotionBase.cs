using UnityEngine;
using NaughtyAttributes;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Abstraction between print-space coordinates and the concrete transforms
    /// that move. Print jobs only talk to this class, so future printers
    /// (bedslinger, CoreXY, delta) can map the same coordinates differently.
    ///
    /// Print space convention:
    /// - X: metres, centred on the bed.
    /// - Y: metres above the bed surface, 0..BuildVolume.y.
    /// - Z: metres, centred on the bed.
    ///
    /// Player-facing home values use the printer-front frame instead; the
    /// concrete motion converts them to print space and handles axis signs.
    /// </summary>
    public abstract class PrinterMotionBase : MonoBehaviour
    {
        /// <summary>
        /// Default visual home: top-left head with the build volume 3/4
        /// forward. Axis signs are handled inside each motion implementation.
        /// </summary>
        public static readonly Vector3 DefaultHomeNormalized =
            new Vector3(0.0f, 1.0f, 0.75f);

        [Header("Home / start position")]
        [Tooltip(
            "Off = default top-left head with the build volume 3/4 forward. " +
            "On = use the custom normalized X/Y/Z below.")]
        public bool UseCustomHomePosition;

        [ShowIf("UseCustomHomePosition")]
        [Range(0.0f, 1.0f)]
        [Tooltip(
            "Normalized X from the printer-front view. " +
            "0 = left edge, 1 = right edge.")]
        public float HomeX = 0.0f;

        [ShowIf("UseCustomHomePosition")]
        [Range(0.0f, 1.0f)]
        [Tooltip("Normalized Y from the plate (0) to the top of the build volume (1).")]
        public float HomeY = 1.0f;

        [ShowIf("UseCustomHomePosition")]
        [Range(0.0f, 1.0f)]
        [Tooltip(
            "Normalized Z from the printer-front view. 0 = back, 1 = front. " +
            "If the horizontal Z role moves the bed, this is the bed position; " +
            "otherwise it is the nozzle position.")]
        public float HomeZ = 0.75f;

        /// <summary>
        /// Player-facing normalized home in the printer-front bed frame:
        /// X 0 = left to 1 = right, Y 0 = plate to 1 = top, Z 0 = back to
        /// 1 = front. Motion implementations convert a bed-moving Z role to
        /// print space internally, so no axis-inversion knowledge is exposed.
        /// </summary>
        public Vector3 HomePositionNormalized => UseCustomHomePosition
            ? new Vector3(HomeX, HomeY, HomeZ)
            : DefaultHomeNormalized;

        /// <summary>True when the horizontal Z role moves the bed, not the nozzle.</summary>
        protected virtual bool HorizontalZMovesBed => false;

        [Header("Calibration height override")]
        [Tooltip(
            "Off = calibration uses the global home height. On = use the " +
            "custom calibration start/lift height.")]
        public bool UseCustomCalibrationHomeHeight;

        [ShowIf("UseCustomCalibrationHomeHeight")]
        [Range(0.0f, 1.0f)]
        [Tooltip(
            "Normalized calibration start/lift height. Calibration returns " +
            "to the global home position afterwards.")]
        public float CalibrationHomeY = 1.0f;

        public Vector3 CalibrationHomePositionNormalized => new Vector3(
            HomePositionNormalized.x,
            UseCustomCalibrationHomeHeight
                ? CalibrationHomeY
                : HomePositionNormalized.y,
            HomePositionNormalized.z);

        public Vector3 HomePrintPosition =>
            ToPrintPosition(HomePositionNormalized);

        public Vector3 CalibrationHomePrintPosition =>
            ToPrintPosition(CalibrationHomePositionNormalized);

        private Vector3 ToPrintPosition(Vector3 normalized)
        {
            Vector3 volume = BuildVolume;

            // X is the head's left/right position over the bed. Z is the
            // bed's physical travel on a bed-slinger (0 = back, 1 = front)
            // and the nozzle's position on a fixed-bed printer. Flip the Z
            // intermediate only for a bed-moving role; AxisBinding.Invert
            // stays an implementation detail.
            float printX = 1.0f - normalized.x;

            float printZ = HorizontalZMovesBed
                ? 1.0f - normalized.z
                : normalized.z;

            return new Vector3(
                Mathf.Lerp(
                    -volume.x * 0.5f,
                    volume.x * 0.5f,
                    printX),
                volume.y * normalized.y,
                Mathf.Lerp(
                    -volume.z * 0.5f,
                    volume.z * 0.5f,
                    printZ));
        }

        /// <summary>Usable build volume in metres (X width, Y height, Z depth).</summary>
        public abstract Vector3 BuildVolume { get; }

        /// <summary>Number of independently calibratable axes.</summary>
        public abstract int AxisCount { get; }

        /// <summary>Bed attachment point used to place printed objects.</summary>
        public abstract PrintBedAnchor BedAnchor { get; }

        public abstract string GetAxisName(int axisIndex);

        public abstract float GetAxisNormalized(int axisIndex);

        /// <summary>
        /// Current travel offset from the authored local position, in local
        /// units. Used by calibration tools and diagnostics.
        /// </summary>
        public abstract float GetAxisLocalOffset(int axisIndex);

        public abstract void SetAxisNormalized(int axisIndex, float normalized);

        /// <summary>
        /// Sets a raw travel offset, ignoring the calibrated range.
        /// Calibration tools use this to discover safe limits.
        /// </summary>
        public abstract void SetAxisLocalOffset(int axisIndex, float offset);

        /// <summary>Returns every axis to its authored local position.</summary>
        public abstract void Home();

        /// <summary>Moves the nozzle to a point in print space.</summary>
        public abstract void ApplyNozzlePosition(Vector3 printSpacePosition);

        /// <summary>Moves the nozzle to the configured single home position.</summary>
        public void ApplyHomePosition()
        {
            ApplyNozzlePosition(HomePrintPosition);
        }

        public void ApplyCalibrationHomePosition()
        {
            ApplyNozzlePosition(CalibrationHomePrintPosition);
        }

        /// <summary>Current nozzle position in print space.</summary>
        public abstract Vector3 GetNozzlePrintPosition();

        /// <summary>
        /// Sets the vertical nozzle-to-bed separation.
        /// 0 = first layer, BuildVolume.y = full build height.
        /// The concrete axis may move the gantry up or the bed down.
        /// </summary>
        public abstract void SetBedHeight(float height);

        /// <summary>Current nozzle position in world space.</summary>
        public abstract Vector3 GetNozzleWorldPosition();

        protected virtual void OnDrawGizmosSelected()
        {
            PrintBedAnchor anchor = BedAnchor;
            if (anchor == null)
            {
                return;
            }

            Vector3 volume = BuildVolume;
            volume.x = Mathf.Max(0.0f, volume.x);
            volume.y = Mathf.Max(0.0f, volume.y);
            volume.z = Mathf.Max(0.0f, volume.z);

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                anchor.transform.position,
                anchor.transform.rotation,
                Vector3.one);

            Gizmos.color = new Color(0.1f, 0.9f, 1.0f, 0.35f);
            Gizmos.DrawWireCube(
                new Vector3(0.0f, volume.y * 0.5f, 0.0f),
                volume);

            Gizmos.color = new Color(1.0f, 0.6f, 0.1f, 1.0f);
            Gizmos.DrawWireSphere(HomePrintPosition, 0.01f);

            if (UseCustomCalibrationHomeHeight)
            {
                Gizmos.color = new Color(0.3f, 1.0f, 0.7f, 1.0f);
                Gizmos.DrawWireSphere(CalibrationHomePrintPosition, 0.008f);
            }

            Gizmos.matrix = previous;
        }
    }
}

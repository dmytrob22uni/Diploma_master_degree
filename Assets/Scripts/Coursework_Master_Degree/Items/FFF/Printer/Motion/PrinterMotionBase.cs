using UnityEngine;

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
    /// </summary>
    public abstract class PrinterMotionBase : MonoBehaviour
    {
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

            Gizmos.matrix = previous;
        }
    }
}

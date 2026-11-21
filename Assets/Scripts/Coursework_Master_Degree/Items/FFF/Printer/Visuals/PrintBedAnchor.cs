using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Y-up reference for the print plate surface.
    /// Author it under the printer's print plate node; the anchor's local +Z
    /// defines the printer-front direction used by placement framing and the
    /// visual home values. For a bed-moving horizontal axis, keep this anchor
    /// under the bed carriage so CartesianPrinterMotion can detect that role.
    /// Printed objects are parented here so they move with the bed.
    /// </summary>
    public class PrintBedAnchor : MonoBehaviour
    {
        [Tooltip(
            "Distance along local Y from this transform to the plate surface.\n" +
            "Use a small value to lift objects off the plate and avoid z-fighting.")]
        public float SurfaceOffset = 0.0f;

        [Tooltip("Plate size in metres, used for gizmos only.")]
        public Vector2 PlateSize = new Vector2(0.25f, 0.21f);

        public Vector3 SurfacePosition =>
            transform.position + transform.up * SurfaceOffset;

        public Quaternion SurfaceRotation => transform.rotation;

        /// <summary>
        /// Parents and places an object so its origin sits on the plate surface.
        /// Callers that need bottom-centre alignment should offset the child
        /// before placing it.
        /// </summary>
        public void PlaceOnSurface(Transform target)
        {
            target.SetPositionAndRotation(SurfacePosition, SurfaceRotation);
        }

        public Vector3 WorldToAnchor(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                SurfacePosition,
                SurfaceRotation,
                Vector3.one);

            Gizmos.color = new Color(0.1f, 0.9f, 1.0f, 0.8f);
            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(PlateSize.x, 0.001f, PlateSize.y));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.05f);
            Gizmos.DrawWireSphere(Vector3.up * 0.05f, 0.004f);

            Gizmos.matrix = previous;
        }
    }
}

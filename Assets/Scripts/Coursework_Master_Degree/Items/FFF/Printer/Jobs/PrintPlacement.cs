using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// A confirmed draft placement on the print plate. Coordinates are in the
    /// bed anchor's local space: X/Z are on the plate, yaw is degrees around
    /// the plate normal and scale is uniform.
    /// </summary>
    public sealed class PrintPlacement
    {
        public Vector3 LocalPosition;
        public float Yaw;
        public float UniformScale = 1.0f;
        /// <summary>
        /// Transformed corners of the full rendered object bounds used for
        /// plate-fit validation.
        /// </summary>
        public Vector3[] BoundaryCorners;
        public bool IsValid = true;

        public PrintPlacement Clone()
        {
            return new PrintPlacement
            {
                LocalPosition = LocalPosition,
                Yaw = Yaw,
                UniformScale = UniformScale,
                BoundaryCorners = BoundaryCorners != null
                    ? (Vector3[])BoundaryCorners.Clone()
                    : null,
                IsValid = IsValid,
            };
        }
    }
}

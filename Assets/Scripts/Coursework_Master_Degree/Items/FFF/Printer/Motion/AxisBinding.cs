using System;
using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// One printer axis: a transform, the local space direction it travels,
    /// and the calibrated travel range around its authored position.
    /// </summary>
    [Serializable]
    public class AxisBinding
    {
        [Tooltip("Transform moved by this axis")]
        public Transform Node;

        [Tooltip(
            "Travel direction in the node's parent local space.\n" +
            "Normalized automatically.")]
        public Vector3 LocalAxis = Vector3.right;

        [Tooltip(
            "Travel offset from the authored local position along LocalAxis,\n" +
            "in local units (metres).\n" +
            "Example: -0.12 for 12 cm backwards, +0.10 for 10 cm forwards.")]
        public float MinLocal = 0.0f;

        [Tooltip(
            "Travel offset from the authored local position along LocalAxis,\n" +
            "in local units (metres).\n" +
            "Example: -0.12 for 12 cm backwards, +0.10 for 10 cm forwards.")]
        public float MaxLocal = 0.0f;

        [Tooltip(
            "Swap MinLocal and MaxLocal when mapping 0..1 to travel.\n" +
            "Use this to keep positive ranges while the axis physically moves " +
            "in the opposite direction.")]
        public bool Invert = false;

        [Tooltip("Approximate travel speed in local units per second")]
        [Min(0.0001f)]
        public float Speed = 0.15f;

        [NonSerialized] private Vector3 homeLocalPosition;
        [NonSerialized] private Vector3 normalizedAxis = Vector3.right;
        [NonSerialized] private bool isCached;
        [NonSerialized] private float currentLocal;

        public bool IsValid => Node != null && LocalAxis.sqrMagnitude > 0.000001f;

        /// <summary>Current travel offset from the authored local position.</summary>
        public float CurrentLocal => currentLocal;

        /// <summary>Current position mapped back into 0..1.</summary>
        public float Normalized
        {
            get
            {
                if (Mathf.Approximately(MinLocal, MaxLocal))
                {
                    return 0.0f;
                }

                float value = Mathf.InverseLerp(MinLocal, MaxLocal, currentLocal);
                return Invert ? 1.0f - value : value;
            }
        }

        public bool IsCached => isCached;

        public void CacheHome()
        {
            if (Node == null)
            {
                return;
            }

            normalizedAxis = LocalAxis.sqrMagnitude > 0.000001f
                ? LocalAxis.normalized
                : Vector3.right;

            homeLocalPosition = Node.localPosition;
            currentLocal = 0.0f;
            isCached = true;
        }

        /// <summary>
        /// Moves the node along LocalAxis. 0 maps to MinLocal and 1 maps to
        /// MaxLocal, unless Invert swaps them.
        /// </summary>
        public void SetNormalized(float normalized)
        {
            if (!isCached)
            {
                CacheHome();
            }

            if (Node == null)
            {
                return;
            }

            normalized = Mathf.Clamp01(normalized);
            float t = Invert ? 1.0f - normalized : normalized;
            float offset = Mathf.Lerp(MinLocal, MaxLocal, t);

            SetLocalOffset(offset);
        }

        /// <summary>
        /// Sets the travel offset directly, ignoring MinLocal/MaxLocal.
        /// Calibration tools use this to discover the safe range.
        /// </summary>
        public void SetLocalOffset(float offset)
        {
            if (!isCached)
            {
                CacheHome();
            }

            if (Node == null)
            {
                return;
            }

            currentLocal = offset;
            Node.localPosition = homeLocalPosition + normalizedAxis * offset;
        }

        /// <summary>Moves the node back to its authored local position.</summary>
        public void RestoreHome()
        {
            if (Node == null)
            {
                return;
            }

            if (!isCached)
            {
                CacheHome();
                return;
            }

            Node.localPosition = homeLocalPosition;
            currentLocal = 0.0f;
        }

        /// <summary>
        /// World position for a travel offset. Used by calibration gizmos.
        /// </summary>
        public Vector3 GetWorldPositionAtOffset(float offset)
        {
            if (Node == null)
            {
                return Vector3.zero;
            }

            if (!isCached)
            {
                CacheHome();
            }

            return Node.parent != null
                ? Node.parent.TransformPoint(homeLocalPosition + normalizedAxis * offset)
                : homeLocalPosition + normalizedAxis * offset;
        }
    }
}

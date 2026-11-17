using UnityEngine;

namespace Coursework_Master_Degree.Items.Computing
{
    /// <summary>
    /// Marker for the point a console camera should frame.
    /// Local +Z is the screen normal (out of the display).
    /// </summary>
    public class DeviceScreenAnchor : MonoBehaviour
    {
        [Tooltip("Flip the screen normal if local +Z points into the display")]
        public bool InvertNormal = false;

        [Header("Console mask")]
        [Tooltip(
            "Optional UI sprite that clips the console to the display shape. " +
            "Leave empty for square corners or use ScreenCornerRadius.")]
        public Sprite ScreenMaskSprite;

        [Tooltip(
            "Rounded corner radius as a fraction of the logical console width. " +
            "0 keeps square corners.")]
        [Range(0.0f, 0.2f)]
        public float ScreenCornerRadius = 0.0f;

        public Vector3 Center => transform.position;

        public Vector3 Normal => InvertNormal ? -transform.forward : transform.forward;

        public Vector3 Right => InvertNormal ? -transform.right : transform.right;

        public Vector3 Up => transform.up;

        private void OnDrawGizmos()
        {
            Vector3 center = Center;
            Vector3 normal = Normal;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, 0.01f);
            Gizmos.DrawLine(center, center + normal * 0.08f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center + Right * 0.04f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Up * 0.04f);
        }
    }
}

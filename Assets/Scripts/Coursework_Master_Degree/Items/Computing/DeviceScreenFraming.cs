using UnityEngine;

namespace Coursework_Master_Degree.Items.Computing
{
    /// <summary>
    /// Physical screen measurements shared by the focus camera and the
    /// on-screen console. Values are expressed along the screen anchor's
    /// axes, so rotated lids and flat devices behave the same way.
    /// </summary>
    public readonly struct DeviceScreenMetrics
    {
        public readonly Vector3 Center;
        public readonly Vector3 Normal;
        public readonly Vector3 Right;
        public readonly Vector3 Up;
        public readonly float HalfWidth;
        public readonly float HalfHeight;

        /// <summary>
        /// Farthest point of the display volume along the normal, measured
        /// from the center. The console sits in front of this plane so it is
        /// never buried inside a bezel or glass panel.
        /// </summary>
        public readonly float FrontOffset;

        public readonly bool HasRendererBounds;

        public DeviceScreenMetrics(
            Vector3 center,
            Vector3 normal,
            Vector3 right,
            Vector3 up,
            float halfWidth,
            float halfHeight,
            float frontOffset,
            bool hasRendererBounds)
        {
            Center = center;
            Normal = normal;
            Right = right;
            Up = up;
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
            FrontOffset = frontOffset;
            HasRendererBounds = hasRendererBounds;
        }

        public float Width => HalfWidth * 2.0f;

        public float Height => HalfHeight * 2.0f;

        public float Aspect => HalfHeight > 0.0001f ? HalfWidth / HalfHeight : 1.0f;
    }

    /// <summary>
    /// Resolves a device's screen anchor and measures the display surface.
    /// Both the console camera and the world-space console use these numbers
    /// so the UI always sits exactly on the physical screen.
    /// </summary>
    public static class DeviceScreenFraming
    {
        private const float FallbackHalfWidth = 0.12f;
        private const float FallbackHalfHeight = 0.075f;
        private const float MinimumHalfExtent = 0.02f;

        public static DeviceScreenAnchor ResolveAnchor(Device device)
        {
            if (device == null)
            {
                return null;
            }

            DeviceScreenAnchor anchor = device.ScreenAnchor;

            if (anchor == null && device.Computing != null)
            {
                anchor = device.Computing.ScreenGameObject != null
                    ? device.Computing.ScreenGameObject
                        .GetComponentInChildren<DeviceScreenAnchor>(true)
                    : null;
            }

            return anchor;
        }

        public static bool TryGet(Device device, out DeviceScreenMetrics metrics)
        {
            metrics = default;

            DeviceScreenAnchor anchor = ResolveAnchor(device);

            if (anchor == null)
            {
                return false;
            }

            Vector3 normal = anchor.Normal.normalized;
            Vector3 right = anchor.Right.normalized;
            Vector3 up = anchor.Up.normalized;

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.forward;
            }

            if (right.sqrMagnitude < 0.0001f || up.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
                up = Vector3.up;
            }

            if (!TryGetRendererMetrics(
                    device,
                    right,
                    up,
                    normal,
                    out Vector3 center,
                    out float halfWidth,
                    out float halfHeight,
                    out float frontOffset))
            {
                metrics = new DeviceScreenMetrics(
                    anchor.Center,
                    normal,
                    right,
                    up,
                    FallbackHalfWidth,
                    FallbackHalfHeight,
                    0.0f,
                    false);

                return true;
            }

            metrics = new DeviceScreenMetrics(
                center,
                normal,
                right,
                up,
                Mathf.Max(halfWidth, MinimumHalfExtent),
                Mathf.Max(halfHeight, MinimumHalfExtent),
                Mathf.Max(0.0f, frontOffset),
                true);

            return true;
        }

        /// <summary>
        /// Measures the display in the anchor's own frame. World-space bounds
        /// are axis-aligned, so using them would inflate the size and the
        /// front offset whenever the device sits at an arbitrary rotation.
        /// </summary>
        private static bool TryGetRendererMetrics(
            Device device,
            Vector3 right,
            Vector3 up,
            Vector3 normal,
            out Vector3 center,
            out float halfWidth,
            out float halfHeight,
            out float frontOffset)
        {
            center = Vector3.zero;
            halfWidth = 0.0f;
            halfHeight = 0.0f;
            frontOffset = 0.0f;

            GameObject screenRoot = device.Computing != null
                ? device.Computing.ScreenGameObject
                : null;

            if (screenRoot == null)
            {
                return false;
            }

            Renderer[] renderers = screenRoot.GetComponentsInChildren<Renderer>(true);

            float minRight = float.PositiveInfinity;
            float maxRight = float.NegativeInfinity;
            float minUp = float.PositiveInfinity;
            float maxUp = float.NegativeInfinity;
            float minNormal = float.PositiveInfinity;
            float maxNormal = float.NegativeInfinity;
            bool found = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds local = renderer.localBounds;

                if (local.size.sqrMagnitude < 0.0000001f)
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                Vector3 localCenter = local.center;
                Vector3 localExtents = local.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 localCorner = localCenter + new Vector3(
                                localExtents.x * x,
                                localExtents.y * y,
                                localExtents.z * z);

                            Vector3 worldCorner =
                                rendererTransform.TransformPoint(localCorner);

                            float projectedRight =
                                Vector3.Dot(worldCorner, right);
                            float projectedUp =
                                Vector3.Dot(worldCorner, up);
                            float projectedNormal =
                                Vector3.Dot(worldCorner, normal);

                            minRight = Mathf.Min(minRight, projectedRight);
                            maxRight = Mathf.Max(maxRight, projectedRight);
                            minUp = Mathf.Min(minUp, projectedUp);
                            maxUp = Mathf.Max(maxUp, projectedUp);
                            minNormal = Mathf.Min(minNormal, projectedNormal);
                            maxNormal = Mathf.Max(maxNormal, projectedNormal);
                        }
                    }
                }

                found = true;
            }

            if (!found)
            {
                return false;
            }

            float centerRight = (minRight + maxRight) * 0.5f;
            float centerUp = (minUp + maxUp) * 0.5f;
            float centerNormal = (minNormal + maxNormal) * 0.5f;

            center = right * centerRight + up * centerUp + normal * centerNormal;
            halfWidth = (maxRight - minRight) * 0.5f;
            halfHeight = (maxUp - minUp) * 0.5f;

            // Farthest point along the outward normal, measured from the
            // display centre. A flat screen gives zero, so only the small
            // clearance offset remains.
            frontOffset = maxNormal - centerNormal;

            return true;
        }
    }
}

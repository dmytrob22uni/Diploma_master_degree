using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Items.FFF;

namespace Coursework_Master_Degree.Game.Controller
{
    /// <summary>
    /// Dedicated Cinemachine camera for bed placement. The orbit target is the
    /// centre of the printer's usable build volume, derived from the bed anchor
    /// and PrinterMotionBase.BuildVolume so it adapts to future printers.
    /// </summary>
    public sealed class ObjectPlacementCameraController : MonoBehaviour
    {
        private const int ActivePriority = 200;
        private const int InactivePriority = -1;

        [Header("Framing")]
        [Range(20.0f, 80.0f)]
        public float FieldOfView = 50.0f;

        [Range(0.5f, 2.0f)]
        public float FitPadding = 1.15f;

        [Header("Orbit")]
        public float OrbitSensitivity = 0.3f;

        [Range(5.0f, 45.0f)]
        public float MinPitch = 15.0f;

        [Range(45.0f, 89.0f)]
        public float MaxPitch = 75.0f;

        public float InitialPitch = 35.0f;

        [Header("Zoom")]
        public float ZoomSensitivity = 0.0004f;

        [Min(0.0f)]
        [Tooltip("Minimum zoom distance as a fraction of the build-volume radius.")]
        public float MinZoomDistanceFactor = 0.55f;

        [Min(0.0f)]
        [Tooltip("Maximum zoom distance as a fraction of the build-volume radius.")]
        public float MaxZoomDistanceFactor = 5.0f;

        private CinemachineCamera camera;
        private CinemachineBrain brain;
        private DeviceFocusController focusController;
        private Coroutine endRoutine;

        private Vector3 center;
        private Vector3 up;
        private Vector3 forward;
        private float yaw;
        private float pitch;
        private float distance;
        private float minDistance;
        private float maxDistance;
        private bool isActive;

        public Camera EventCamera =>
            brain != null ? brain.OutputCamera : null;

        public Vector3 OrbitCenter => center;

        public void Begin(Printer printer, PrintBedAnchor anchor, Vector3 volume)
        {
            // A new placement always starts clean; an in-flight return blend
            // belongs to the previous placement and must not keep the old
            // camera around.
            StopEndRoutine();
            DestroyCamera();

            if (printer == null || anchor == null)
            {
                return;
            }

            focusController = FindFocusController();
            brain = focusController != null
                ? focusController.Brain
                : null;

            if (brain == null)
            {
                brain = FindAnyObjectByType<CinemachineBrain>();
            }

            CreateCamera();

            up = anchor.transform.up.sqrMagnitude > 0.0001f
                ? anchor.transform.up.normalized
                : Vector3.up;
            forward = Vector3.ProjectOnPlane(
                anchor.transform.forward,
                up);

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(
                    anchor.transform.right,
                    up);
            }

            forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.forward;

            Vector3 usableVolume = volume;
            if (usableVolume.x <= 0.0f ||
                usableVolume.y <= 0.0f ||
                usableVolume.z <= 0.0f)
            {
                usableVolume = new Vector3(
                    anchor.PlateSize.x,
                    0.21f,
                    anchor.PlateSize.y);
            }

            // Centre of the allowed build-volume box: bed centre plus half the
            // usable height. Width/depth are centred on the bed by convention.
            center = anchor.SurfacePosition +
                up * (usableVolume.y * 0.5f);

            float radius = 0.5f * Mathf.Sqrt(
                usableVolume.x * usableVolume.x +
                usableVolume.y * usableVolume.y +
                usableVolume.z * usableVolume.z);

            float tanHalfFov = Mathf.Tan(
                FieldOfView * 0.5f * Mathf.Deg2Rad);
            float fitDistance = radius /
                Mathf.Max(0.1f, tanHalfFov) * FitPadding;

            minDistance = Mathf.Max(
                0.05f,
                radius * Mathf.Max(0.0f, MinZoomDistanceFactor));

            maxDistance = Mathf.Max(
                minDistance + 0.01f,
                radius * Mathf.Max(
                    MinZoomDistanceFactor + 0.01f,
                    MaxZoomDistanceFactor));

            distance = Mathf.Clamp(fitDistance, minDistance, maxDistance);

            yaw = 0.0f;
            pitch = Mathf.Clamp(InitialPitch, MinPitch, MaxPitch);

            Apply();
            camera.Priority = ActivePriority;
            isActive = true;
        }

        public void Orbit(Vector2 delta)
        {
            if (!isActive)
            {
                return;
            }

            yaw += delta.x * OrbitSensitivity;
            pitch = Mathf.Clamp(
                pitch - delta.y * OrbitSensitivity,
                MinPitch,
                MaxPitch);

            Apply();
        }

        public void Zoom(float delta)
        {
            if (!isActive)
            {
                return;
            }

            distance = Mathf.Clamp(
                distance - delta * ZoomSensitivity,
                minDistance,
                maxDistance);

            Apply();
        }

        public void End()
        {
            isActive = false;

            if (camera == null)
            {
                return;
            }

            // Return the blend source to Cinemachine, but keep the camera
            // alive until the brain has finished blending back to the console
            // or player camera. Destroying it immediately would cut straight
            // to the destination and skip the transition.
            camera.Priority = InactivePriority;

            if (brain == null || !isActiveAndEnabled)
            {
                DestroyCamera();
                return;
            }

            StopEndRoutine();
            endRoutine = StartCoroutine(DestroyWhenBlendEnds());
        }

        private void OnDisable()
        {
            isActive = false;
            StopEndRoutine();
            DestroyCamera();
        }

        private IEnumerator DestroyWhenBlendEnds()
        {
            // Let the brain evaluate the priority change before testing.
            yield return null;

            float startDeadline = Time.unscaledTime + 0.5f;

            while (brain != null &&
                   !brain.IsBlending &&
                   Time.unscaledTime < startDeadline)
            {
                yield return null;
            }

            float maxWait = focusController != null
                ? focusController.MaxBlendWait
                : 3.0f;
            float deadline = Time.unscaledTime + maxWait;

            while (brain != null &&
                   brain.IsBlending &&
                   Time.unscaledTime < deadline)
            {
                yield return null;
            }

            endRoutine = null;
            DestroyCamera();
        }

        private void StopEndRoutine()
        {
            if (endRoutine == null)
            {
                return;
            }

            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        private void DestroyCamera()
        {
            if (camera == null)
            {
                return;
            }

            camera.Priority = InactivePriority;
            Destroy(camera.gameObject);
            camera = null;
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("object_place_camera");
            cameraObject.transform.SetParent(transform, false);

            camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Priority = InactivePriority;
            camera.Lens.FieldOfView = FieldOfView;
        }

        private static DeviceFocusController FindFocusController()
        {
            GameStateManager manager = GameStateManager.Instance;

            return manager != null && manager.DeviceController != null
                ? manager.DeviceController.FocusController
                : null;
        }

        private void Apply()
        {
            if (camera == null)
            {
                return;
            }

            float pitchRadians = pitch * Mathf.Deg2Rad;
            Vector3 horizontalDirection =
                Quaternion.AngleAxis(yaw, up) * forward;

            Vector3 position = center +
                horizontalDirection * (distance * Mathf.Cos(pitchRadians)) +
                up * (distance * Mathf.Sin(pitchRadians));

            camera.transform.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(center - position, up));
        }
    }
}

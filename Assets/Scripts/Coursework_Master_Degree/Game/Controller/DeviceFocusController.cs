using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Coursework_Master_Degree.Items.Computing;

namespace Coursework_Master_Degree.Game.Controller
{
    /// <summary>
    /// Moves a dedicated Cinemachine camera in front of a device display and
    /// blends the player camera to it. Framing is derived from the screen
    /// bounds and the anchor normal, so it works for all four device types.
    /// </summary>
    public class DeviceFocusController : MonoBehaviour
    {
        [Header("Cinemachine")]
        public CinemachineCamera ConsoleCamera;
        public CinemachineBrain Brain;

        [Header("Framing")]
        [Range(0.3f, 1.0f)]
        public float ScreenFill = 0.82f;

        [Min(0.05f)]
        public float MinDistance = 0.12f;

        [Min(0.0f)]
        public float EdgePadding = 0.025f;

        [Range(20.0f, 90.0f)]
        public float FieldOfView = 55.0f;

        [Header("Blend")]
        [Min(0.0f)]
        public float BlendTime = 0.55f;

        [Min(0.1f)]
        public float MaxBlendWait = 3.0f;

        private const int ActivePriority = 100;
        private const int InactivePriority = -1;

        private Coroutine blendRoutine;
        private CinemachineBlendDefinition cachedBlend;
        private bool blendOverridden;

        public bool IsFocused { get; private set; }

        /// <summary>
        /// The camera the player actually sees through. The console uses it
        /// for pointer raycasts once the blend settles.
        /// </summary>
        public Camera EventCamera
        {
            get { return Brain != null ? Brain.OutputCamera : null; }
        }

        private void Awake()
        {
            // The player camera uses priority 0. Keep this camera below it
            // when idle so activation-order tie-breaking never picks it.
            if (ConsoleCamera != null)
            {
                ConsoleCamera.Priority = InactivePriority;
            }
        }

        public void Focus(Device device, Action onReady = null)
        {
            if (device == null)
            {
                Debug.LogWarning("[DeviceFocusController] No device to focus.");
                onReady?.Invoke();
                return;
            }

            if (ConsoleCamera == null || Brain == null)
            {
                Debug.LogError(
                    "[DeviceFocusController] ConsoleCamera and Brain must be assigned.",
                    this);
                onReady?.Invoke();
                return;
            }

            if (!TryComputeFraming(device, out Vector3 position, out Quaternion rotation))
            {
                onReady?.Invoke();
                return;
            }

            ConsoleCamera.Lens.FieldOfView = FieldOfView;
            ConsoleCamera.transform.SetPositionAndRotation(position, rotation);

            IsFocused = true;
            ConsoleCamera.Priority = ActivePriority;
            StartBlend(onReady);
        }

        public void Release(Action onReady = null)
        {
            IsFocused = false;

            if (ConsoleCamera == null)
            {
                onReady?.Invoke();
                return;
            }

            ConsoleCamera.Priority = InactivePriority;
            StartBlend(onReady);
        }

        private void OnDisable()
        {
            StopBlendRoutine();
            RestoreBlend();
        }

        private void StartBlend(Action onReady)
        {
            StopBlendRoutine();

            if (Brain == null || BlendTime <= 0.001f)
            {
                onReady?.Invoke();
                return;
            }

            if (!blendOverridden)
            {
                cachedBlend = Brain.DefaultBlend;
                blendOverridden = true;
            }

            Brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut,
                BlendTime);

            blendRoutine = StartCoroutine(WaitForBlend(onReady));
        }

        private IEnumerator WaitForBlend(Action onReady)
        {
            // Let the brain pick up the priority change before testing.
            yield return null;

            // The brain evaluates cameras in LateUpdate. Give it a couple of
            // frames to register the new blend before waiting for it to end.
            float startDeadline = Time.unscaledTime + 0.5f;

            while (Brain != null &&
                   !Brain.IsBlending &&
                   Time.unscaledTime < startDeadline)
            {
                yield return null;
            }

            float deadline = Time.unscaledTime + MaxBlendWait;

            while (Brain != null &&
                   Brain.IsBlending &&
                   Time.unscaledTime < deadline)
            {
                yield return null;
            }

            blendRoutine = null;
            RestoreBlend();
            onReady?.Invoke();
        }

        private void StopBlendRoutine()
        {
            if (blendRoutine == null)
            {
                return;
            }

            StopCoroutine(blendRoutine);
            blendRoutine = null;
        }

        private void RestoreBlend()
        {
            if (!blendOverridden || Brain == null)
            {
                return;
            }

            Brain.DefaultBlend = cachedBlend;
            blendOverridden = false;
        }

        private bool TryComputeFraming(
            Device device,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;

            if (!DeviceScreenFraming.TryGet(device, out DeviceScreenMetrics metrics))
            {
                Debug.LogWarning(
                    $"[DeviceFocusController] {device.DisplayName} has no screen anchor.",
                    device);
                return false;
            }

            Vector3 center = metrics.Center;
            Vector3 normal = metrics.Normal.normalized;
            Vector3 up = metrics.Up.normalized;

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.forward;
            }

            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.up;
            }

            float halfWidth = metrics.HalfWidth;
            float halfHeight = metrics.HalfHeight;

            Camera outputCamera = EventCamera;

            float aspect = outputCamera != null
                ? Mathf.Max(0.2f, outputCamera.aspect)
                : Mathf.Max(
                    0.2f,
                    (float)Screen.width / Mathf.Max(1, Screen.height));

            float tanHalfFov = Mathf.Tan(
                FieldOfView * 0.5f * Mathf.Deg2Rad);

            float distanceForHeight = halfHeight / tanHalfFov;
            float distanceForWidth = halfWidth / (tanHalfFov * aspect);
            float distance =
                Mathf.Max(distanceForHeight, distanceForWidth) /
                Mathf.Clamp(ScreenFill, 0.3f, 1.0f);

            distance = Mathf.Max(MinDistance, distance + EdgePadding);

            position = center + normal * distance;

            Vector3 lookDirection = center - position;

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = -normal;
            }

            rotation = Quaternion.LookRotation(lookDirection, up);
            return true;
        }
    }
}

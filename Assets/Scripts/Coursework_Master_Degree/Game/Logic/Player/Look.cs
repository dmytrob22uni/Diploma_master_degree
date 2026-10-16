using UnityEngine;
using NaughtyAttributes;
using Unity.Cinemachine;

namespace Coursework_Master_Degree.Game.Logic
{
    public class Look : MonoBehaviour
    {
        private bool NotEnabled => false;
        private bool Enabled => true;

        [Header("Components")]
        [Required] public Transform Eyes;
        [Required] public CinemachineCamera PlayerCamera;
        [Required] public Movement Movement;

        [Header("Look")]
        [Min(0.01f)] public float LookSpeed;
        [Range(0.01f, 90.0f)] public float UpLimitLookAngle;
        [Range(0.01f, 90.0f)] public float DownLimitLookAngle;

        [EnableIf("NotEnabled")]
        public float Yaw;
        [EnableIf("NotEnabled")]
        public float Pitch;

        [Header("Zoom")]
        [Min(0.01f)] public float ZoomLookFOV;
        [EnableIf("NotEnabled")]
        [Min(0.01f)] public float ZoomLookSpeed;
        [Min(0.01f)] public float ZoomLookTransitionDuration;

        private float _currentLookSpeed;
        public float CurrentLookSpeed => _currentLookSpeed;

        private bool _isZooming;
        private bool _isZoomLookTransition;
        private float _zoomLookTimePassed;

        private float _regularLookFOV;
        private float _zoomLookFOVFrom;
        private float _zoomLookFOVTo;
        private float _zoomLookSpeedFrom;
        private float _zoomLookSpeedTo;

        private void OnValidate()
        {
            if (PlayerCamera == null)
            {
                return;
            }

            ZoomLookSpeed = CalculateZoomLookSpeed();
        }

        private float CalculateZoomLookSpeed()
        {
            return LookSpeed * ZoomLookFOV / PlayerCamera.Lens.FieldOfView;
        }

        private void Awake()
        {
            ZoomLookSpeed = CalculateZoomLookSpeed();

            _currentLookSpeed = LookSpeed;
            _regularLookFOV = PlayerCamera.Lens.FieldOfView;
        }

        private void Update()
        {
            Yaw = Movement.Yaw;
            CalculateZoomLookState();
        }

        private void CalculateZoomLookState()
        {
            if (_isZoomLookTransition)
            {
                _zoomLookTimePassed += Time.deltaTime / ZoomLookTransitionDuration;
                _zoomLookTimePassed = Mathf.Clamp01(_zoomLookTimePassed);

                PlayerCamera.Lens.FieldOfView = Mathf.Lerp(
                    _zoomLookFOVFrom,
                    _zoomLookFOVTo,
                    _zoomLookTimePassed);

                _currentLookSpeed = Mathf.Lerp(
                    _zoomLookSpeedFrom,
                    _zoomLookSpeedTo,
                    _zoomLookTimePassed);

                if (Mathf.Approximately(
                    PlayerCamera.Lens.FieldOfView,
                    _zoomLookFOVTo))
                {
                    PlayerCamera.Lens.FieldOfView = _zoomLookFOVTo;
                    _currentLookSpeed = _zoomLookSpeedTo;

                    _isZoomLookTransition = default;
                    _zoomLookTimePassed = default;
                }
            }
        }

        // === Player Controller invoked ===

        public void ApplyLookPitch(Vector2 lookValue)
        {
            Pitch -= lookValue.y * _currentLookSpeed * Time.deltaTime;
            Pitch = Mathf.Clamp(Pitch, -UpLimitLookAngle, DownLimitLookAngle);

            Eyes.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
        }

        public void ApplyToggleZoom()
        {
            if (!_isZoomLookTransition)
            {
                _isZooming = !_isZooming;
                _isZoomLookTransition = true;

                if (_isZooming)
                {
                    _zoomLookSpeedFrom = LookSpeed;
                    _zoomLookSpeedTo = ZoomLookSpeed;

                    _zoomLookFOVFrom = _regularLookFOV;
                    _zoomLookFOVTo = ZoomLookFOV;
                }
                else
                {
                    _zoomLookSpeedFrom = ZoomLookSpeed;
                    _zoomLookSpeedTo = LookSpeed;

                    _zoomLookFOVFrom = ZoomLookFOV;
                    _zoomLookFOVTo = _regularLookFOV;
                }
            }
        }
    }
}

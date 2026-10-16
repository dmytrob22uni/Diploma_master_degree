using UnityEngine;
using NaughtyAttributes;

namespace Coursework_Master_Degree.Game.Logic
{
    public class Movement : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        [Header("Components")]
        public Rigidbody PlayerRigidbody;
        [Required]
        public Look Look;

        [Header("Speed")]
        [Min(0.01f)] public float MoveSpeed;
        [Min(0.01f)] public float RunSpeed;
        [Min(0.01f)] public float WalkSpeed;
        [Min(0.01f)] public float CrouchSpeed;

        [Header("Crouch")]
        [Min(0.3f)] public float CrouchScale;
        [Min(0.1f)] public float CrouchTransitionDuration;

        private float _yaw;
        public float Yaw => _yaw;
        private Vector2 _moveValue;

        private bool _isRunning;
        private bool _isWalking;
        private bool _isCrouching;

        private float _moveSpeed;
        private bool _isCrouchTransition;
        private float _crouchTimePassed;

        private Vector3 _regularScale;
        private Vector3 _crouchScale;
        private Vector3 _scaleFrom;
        private Vector3 _scaleTo;

        private void Awake()
        {
            if (PlayerRigidbody == null)
                PlayerRigidbody = GetComponent<Rigidbody>();

            _regularScale = Vector3.one;
            _crouchScale = new Vector3(1f, CrouchScale, 1f);
        }

        private void Start()
        {
            _yaw = PlayerRigidbody.transform.eulerAngles.y;
        }

        private void Update()
        {
            _moveSpeed = CalculateMoveSpeed();
            CalculateCrouchState();
        }

        private float CalculateMoveSpeed()
        {
            if (_isCrouching)
            {
                return CrouchSpeed;
            }
            if (_isRunning)
            {
                return RunSpeed;
            }
            if (_isWalking)
            {
                return WalkSpeed;
            }
            return MoveSpeed;
        }

        private void CalculateCrouchState()
        {
            if (_isCrouchTransition)
            {
                _crouchTimePassed += Time.deltaTime / CrouchTransitionDuration;
                _crouchTimePassed = Mathf.Clamp01(_crouchTimePassed);

                PlayerRigidbody.transform.localScale =
                    Vector3.Lerp(
                        _scaleFrom,
                        _scaleTo,
                        _crouchTimePassed);

                if (Mathf.Approximately(
                    PlayerRigidbody.transform.localScale.y,
                    _scaleTo.y))
                {
                    _isCrouchTransition = false;
                    _crouchTimePassed = 0f;
                }
            }
        }

        private void FixedUpdate()
        {
            Quaternion targetRotation = Quaternion.Euler(0f, _yaw, 0f);
            PlayerRigidbody.MoveRotation(targetRotation);

            Vector3 moveDirectionRaw = targetRotation * new Vector3(_moveValue.x, 0f, _moveValue.y);
            Vector3 moveDirection = Vector3.ClampMagnitude(moveDirectionRaw, 1f);

            Vector3 currentVelocity = PlayerRigidbody.linearVelocity;

            if (_moveValue.sqrMagnitude > 0.001f)
            {
                // apply horizontal target velocity while preserving gravity
                Vector3 targetVelocity = moveDirection * _moveSpeed;
                PlayerRigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            }
            else
            {
                // zero out horizontal momentum on release while keeping gravity
                PlayerRigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            }
        }

        // === Player Controller invoked ===

        public void ApplyMove(Vector2 moveValue)
        {
            _moveValue = moveValue;
        }

        public void ApplyLookYaw(float lookYawValue)
        {
            float speed = Look.CurrentLookSpeed;
            _yaw += lookYawValue * speed * Time.deltaTime;
        }

        public void ApplyInitiateRun(bool isRunning)
        {
            _isRunning = isRunning;
        }

        public void ApplyInitiateWalk(bool isWalking)
        {
            _isWalking = isWalking;
        }

        public void ApplyCrouch()
        {
            if (!_isCrouchTransition)
            {
                _isCrouching = !_isCrouching;
                _isCrouchTransition = true;

                _scaleFrom = PlayerRigidbody.transform.localScale;
                _scaleTo = _isCrouching ? _crouchScale : _regularScale;
                _crouchTimePassed = 0f;
            }
        }
    }
}

using UnityEngine;
using NaughtyAttributes;

namespace Coursework_Master_Degree.Game.Logic
{
    public class Jump : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        [Header("Components")]
        public Rigidbody PlayerRigidbody;

        [Header("Jump")]
        [Min(0.01f)] public float JumpSpeed;

        [Header("Gravity")]
        [MaxValue(-0.1f)]
        [Tooltip("Set Rigidbody custom gravity acceleration")]
        public float BaseGravity = -9.81f;
        [Min(1.0f)]
        [Tooltip(
            "Gravity multiplier on falling down (for negative Y velocity). " +
            "It is better to set this something bigger than 1.0f. Default gravity " +
            "on falling feels like on Moon")]
        public float FallGravityMultiplier = 1.5f;
        [Min(0.1f)]
        [Tooltip("Bottom threshold of absolute vertical velocity to cut gravity on")]
        public float ApexThreshold;
        [Range(0.01f, 1.0f)]
        [Tooltip("Gravity multiplier on passing apex threshold")]
        public float ApexGravityMultiplier = 0.5f;

        [Header("Ground Check")]
        [Required]
        [Tooltip("Position at the base of the player's capsule")]
        public Transform GroundCheckOrigin;
        [Min(0.05f)]
        public float GroundCheckRadius = 0.1f;
        public LayerMask GroundLayer;
        [Min(0.0f)]
        [Tooltip("Keeps player a bit glued to slopes and floors")]
        public float GroundedDownforce;

        private float _verticalVelocity;

        private bool _isGrounded;

        private void OnDrawGizmos()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(GroundCheckOrigin.position, GroundCheckRadius);
        }

        private void Awake()
        {
            if (PlayerRigidbody == null)
                PlayerRigidbody = GetComponent<Rigidbody>();

            PlayerRigidbody.useGravity = false;
        }

        private void FixedUpdate()
        {
            CalculateGroundedState();

            Vector3 velocity = PlayerRigidbody.linearVelocity;

            if (_isGrounded)
            {
                _verticalVelocity = -GroundedDownforce;
            }
            else
            {
                float currentGravity = BaseGravity;

                // Around jump apex
                if (Mathf.Abs(_verticalVelocity) < ApexThreshold)
                {
                    currentGravity *= ApexGravityMultiplier;
                }
                // Falling down
                else if (_verticalVelocity < 0.0f)
                {
                    currentGravity *= FallGravityMultiplier;
                }

                _verticalVelocity += currentGravity * Time.fixedDeltaTime;
            }

            velocity.y = _verticalVelocity;
            PlayerRigidbody.linearVelocity = velocity;
        }

        /// <summary>
        /// Casts a sphere around player's feet.
        /// This method catches edges and slopes much better than a single raycast.
        /// </summary>
        private void CalculateGroundedState()
        {
            _isGrounded = Physics.CheckSphere(
                GroundCheckOrigin.position,
                GroundCheckRadius,
                GroundLayer,
                QueryTriggerInteraction.Ignore);
        }

        // === Player Controller invoked ===

        public void ApplyJump()
        {
            if (_isGrounded)
            {
                PlayerRigidbody.AddForce(
                    Vector3.up * JumpSpeed,
                    ForceMode.VelocityChange);
            }
        }
    }
}

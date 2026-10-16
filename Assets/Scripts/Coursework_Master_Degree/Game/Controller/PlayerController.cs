using UnityEngine;
using Coursework_Master_Degree.Input;
using Coursework_Master_Degree.Game.Logic;

namespace Coursework_Master_Degree.Game.Controller
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Reader")]
        public InputReader InputReader;

        [Header("Logic components")]
        public Movement Movement;
        public Jump Jump;
        public Look Look;

        private void Awake()
        {
            if (Movement == null)
                Movement = GetComponent<Movement>();
            if (Jump == null)
                Jump = GetComponent<Jump>();
            if (Look == null)
                Look = GetComponent<Look>();
        }

        private void OnEnable()
        {
            // Movement
            InputReader.OnMoveEvent += HandleMove;
            InputReader.OnLookEvent += HandleLook;
            InputReader.OnJumpEvent += HandleJump;
            InputReader.OnCrouchEvent += HandleCrouch;
            InputReader.OnInitiateWalkEvent += HandleInitiateWalk;
            InputReader.OnInitiateRunEvent += HandleInitiateRun;
            InputReader.OnToggleZoomEvent += HandleToggleZoom;
        }

        private void OnDisable()
        {
            // Movement
            InputReader.OnMoveEvent -= HandleMove;
            InputReader.OnLookEvent -= HandleLook;
            InputReader.OnJumpEvent -= HandleJump;
            InputReader.OnCrouchEvent -= HandleCrouch;
            InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
            InputReader.OnInitiateRunEvent -= HandleInitiateRun;
            InputReader.OnToggleZoomEvent -= HandleToggleZoom;
        }

        // === Handler methods  ===

        // Movement

        private void HandleMove(Vector2 value)
        {
            Movement.ApplyMove(value);
        }

        private void HandleLook(Vector2 value)
        {
            Movement.ApplyLookYaw(value.x);
            Look.ApplyLookPitch(value);
        }

        private void HandleJump()
        {
            Jump.ApplyJump();
        }

        private void HandleCrouch()
        {
            Movement.ApplyCrouch();
        }

        private void HandleInitiateRun(bool value)
        {
            Movement.ApplyInitiateRun(value);
        }

        private void HandleInitiateWalk(bool value)
        {
            Movement.ApplyInitiateWalk(value);
        }

        private void HandleToggleZoom()
        {
            Look.ApplyToggleZoom();
        }
    }
}

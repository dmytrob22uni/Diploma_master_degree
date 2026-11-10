using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Game.Logic;
using Coursework_Master_Degree.Game.Logic.Hover;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Game.StateMachine.State.ItemPlace;

namespace Coursework_Master_Degree.Game.Controller
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Logic components")]
        [HorizontalLine(1.0f)]
        [Header("Movement Logics")]
        public Movement Movement;
        public Jump Jump;
        public Look Look;
        [Header("FreeWalk Logics")]
        public ItemHoverInfo ItemHoverInfo;
        public ItemHoverInteract ItemHoverInteract;
        [Header("ItemPlace Logics")]
        public ItemHoverPlace ItemHoverPlace;

        public void SetFreeWalkState(bool enable)
        {
            if (enable)
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent += HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent += HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent += HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent += HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent += HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent += HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent += HandleToggleZoom;
                // FreeWalk Inputs
                GameStateManager.Instance.InputReader.OnInteractWithItemEvent += HandleInteractWithItem;
                GameStateManager.Instance.InputReader.OnTakeItemEvent += HandleTakeItem;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // FreeWalk Logics
                ItemHoverInfo.enabled = true;
                ItemHoverInteract.enabled = true;
            }
            else
            {
                // Movement inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // FreeWalk Input
                GameStateManager.Instance.InputReader.OnInteractWithItemEvent -= HandleInteractWithItem;
                GameStateManager.Instance.InputReader.OnTakeItemEvent -= HandleTakeItem;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // FreeWalk Logics
                ItemHoverInfo.enabled = false;
                ItemHoverInteract.enabled = false;
            }
        }

        public void SetItemPlaceState(bool enable, ItemPlaceData itemPlaceData = default, Influence influence = default)
        {
            if (enable)
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent += HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent += HandleLook;
                if (influence.IsAllowedJump)
                {
                    GameStateManager.Instance.InputReader.OnJumpEvent += HandleJump;
                }
                if (influence.IsAllowedCrouch)
                {
                    GameStateManager.Instance.InputReader.OnCrouchEvent += HandleCrouch;
                }
                if (influence.IsAllowedWalk)
                {
                    GameStateManager.Instance.InputReader.OnInitiateWalkEvent += HandleInitiateWalk;
                }
                if (influence.IsAllowedRun)
                {
                    GameStateManager.Instance.InputReader.OnInitiateRunEvent += HandleInitiateRun;
                }
                if (influence.IsAllowedZoom)
                {
                    GameStateManager.Instance.InputReader.OnToggleZoomEvent += HandleToggleZoom;
                }
                // ItemPlace Input
                GameStateManager.Instance.InputReader.OnPlaceItemEvent += HandlePlaceItem;
                GameStateManager.Instance.InputReader.OnRotateItemEvent += HandleRotateItem;
                if (GameStateManager.Instance.PreviousGameState is not GameStateItemsMarket)
                {
                    GameStateManager.Instance.InputReader.OnCancelEvent += HandleCancelItemPlace;
                }

                // Movement Logics
                Movement.enabled = true;
                Movement.Influence = influence;
                Jump.enabled = true;
                Jump.Influence = influence;
                Look.enabled = true;
                Look.Influence = influence;
                // ItemPlace Logics
                ItemHoverPlace.enabled = true;
                ItemHoverPlace.ItemPlaceData = itemPlaceData;
                ItemHoverPlace.Influence = influence;
            }
            else
            {
                // Movement inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // ItemPlace Input
                GameStateManager.Instance.InputReader.OnPlaceItemEvent -= HandlePlaceItem;
                GameStateManager.Instance.InputReader.OnRotateItemEvent -= HandleRotateItem;
                if (GameStateManager.Instance.PreviousGameState is not GameStateItemsMarket)
                {
                    GameStateManager.Instance.InputReader.OnCancelEvent -= HandleCancelItemPlace;
                }

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // ItemPlace Logics
                ItemHoverPlace.enabled = false;
            }
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

        // FreeWalk

        private void HandleTakeItem()
        {
            ItemHoverInfo.ApplyTakeItem();
        }

        private void HandleInteractWithItem()
        {
            ItemHoverInteract.ApplyInteractWithItem();
        }

        // ItemPlace

        private void HandlePlaceItem()
        {
            ItemHoverPlace.ApplyPlaceItem();
        }

        private void HandleRotateItem(Vector2 value)
        {
            if (Mathf.Abs(value.y) < 0.01f)
            {
                return;
            }

            float sign = Mathf.Sign(value.y);
            ItemHoverPlace.ApplyRotateItem(sign);
        }

        private void HandleCancelItemPlace()
        {
            ItemHoverPlace.ApplyCancelItemPlace();
        }
    }
}

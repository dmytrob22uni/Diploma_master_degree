using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
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
        [Header("ItemsSell Logics")]
        public ItemHoverSell ItemHoverSell;
        [Header("ItemPlace Logics")]
        public ItemHoverPlace ItemHoverPlace;
        [Header("Parts Logics")]
        public PartHoverIntegrity PartHoverIntegrity;
        public PartHoverRemove PartHoverRemove;
        public PartHoverInstall PartHoverInstall;
        public PartHoverWear PartHoverWear;

        private int itemPlaceCancelEnableFrame = -1;

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

        public void SetDeviceInterfaceState(bool enable)
        {
            // The console owns the mouse. World movement, look and free-walk
            // hover must stay off while it is open, and stay off when it
            // closes until the next state explicitly enables them.
            Movement.enabled = false;
            Jump.enabled = false;
            Look.enabled = false;
            ItemHoverInfo.enabled = false;
            ItemHoverInteract.enabled = false;
        }

        public void SetDeviceBindState(bool enable)
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

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;

                // Bind targeting does its own raycast; free-walk hover stays off.
                ItemHoverInfo.enabled = false;
                ItemHoverInteract.enabled = false;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;

                ItemHoverInfo.enabled = false;
                ItemHoverInteract.enabled = false;
            }
        }

        public void SetItemsSellState(bool enable)
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
                // ItemsSell Inputs
                GameStateManager.Instance.InputReader.OnSellItemEvent += HandleSellItem;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // ItemsSell Logics
                ItemHoverSell.enabled = true;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // ItemsSell Inputs
                GameStateManager.Instance.InputReader.OnSellItemEvent -= HandleSellItem;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // ItemsSell Logics
                ItemHoverSell.enabled = false;
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
                if (itemPlaceData != null && itemPlaceData.IsCancelAvailable)
                {
                    GameStateManager.Instance.InputReader.OnCancelEvent += HandleCancelItemPlace;
                    itemPlaceCancelEnableFrame = Time.frameCount;
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
                if (ItemHoverPlace.ItemPlaceData != null &&
                    ItemHoverPlace.ItemPlaceData.IsCancelAvailable)
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

        public void SetPartInstallState(
            bool enable,
            ItemDataHolder targetItem = default,
            PartsAssembly targetAssembly = default)
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
                // PartInstall Inputs
                GameStateManager.Instance.InputReader.OnInstallPartEvent += HandleInstallPart;
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartInstallEvent += HandleAnotherHoverLayerPartInstall;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // PartInstall Logics
                PartHoverInstall.SetTargetItem(targetItem, targetAssembly);
                PartHoverInstall.enabled = true;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // PartInstall Inputs
                GameStateManager.Instance.InputReader.OnInstallPartEvent -= HandleInstallPart;
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartInstallEvent -= HandleAnotherHoverLayerPartInstall;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // PartInstall Logics
                PartHoverInstall.enabled = false;
                PartHoverInstall.SetTargetItem(default);
            }
        }

        public void SetPartsIntegrityState(bool enable, PartsAssembly targetAssembly = default)
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
                // PartsIntegrity Inputs
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsIntegrityEvent += HandleAnotherHoverLayerPartsIntegrity;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // PartsIntegrity Logics
                PartHoverIntegrity.SetTargetAssembly(targetAssembly);
                PartHoverIntegrity.enabled = true;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // PartsIntegrity Inputs
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsIntegrityEvent -= HandleAnotherHoverLayerPartsIntegrity;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // PartsIntegrity Logics
                PartHoverIntegrity.enabled = false;
                PartHoverIntegrity.SetTargetAssembly(default);
            }
        }

        public void SetPartsRemoveState(bool enable, PartsAssembly targetAssembly = default)
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
                // PartsRemove Inputs
                GameStateManager.Instance.InputReader.OnRemovePartEvent += HandleRemovePart;
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsRemoveEvent += HandleAnotherHoverLayerPartsRemove;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // PartsRemove Logics
                PartHoverRemove.SetTargetAssembly(targetAssembly);
                PartHoverRemove.enabled = true;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // PartsRemove Inputs
                GameStateManager.Instance.InputReader.OnRemovePartEvent -= HandleRemovePart;
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsRemoveEvent -= HandleAnotherHoverLayerPartsRemove;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // PartsRemove Logics
                PartHoverRemove.enabled = false;
                PartHoverRemove.SetTargetAssembly(default);
            }
        }

        public void SetPartsWearState(
            bool enable,
            PartsAssembly targetAssembly = default,
            ItemDataHolder targetItem = default)
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
                // PartsWear Inputs
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsWearEvent += HandleAnotherHoverLayerPartsWear;

                // Movement Logics
                Movement.enabled = true;
                Jump.enabled = true;
                Look.enabled = true;
                // PartsWear Logics
                PartHoverWear.SetTargetAssembly(targetAssembly);
                PartHoverWear.SetTargetItem(targetItem);
                PartHoverWear.enabled = true;
            }
            else
            {
                // Movement Inputs
                GameStateManager.Instance.InputReader.OnMoveEvent -= HandleMove;
                GameStateManager.Instance.InputReader.OnLookEvent -= HandleLook;
                GameStateManager.Instance.InputReader.OnJumpEvent -= HandleJump;
                GameStateManager.Instance.InputReader.OnCrouchEvent -= HandleCrouch;
                GameStateManager.Instance.InputReader.OnInitiateWalkEvent -= HandleInitiateWalk;
                GameStateManager.Instance.InputReader.OnInitiateRunEvent -= HandleInitiateRun;
                GameStateManager.Instance.InputReader.OnToggleZoomEvent -= HandleToggleZoom;
                // PartsWear Inputs
                GameStateManager.Instance.InputReader.OnAnotherHoverLayerPartsWearEvent -= HandleAnotherHoverLayerPartsWear;

                // Movement Logics
                Movement.enabled = false;
                Jump.enabled = false;
                Look.enabled = false;
                // PartsWear Logics
                PartHoverWear.enabled = false;
                PartHoverWear.SetTargetAssembly(default);
                PartHoverWear.SetTargetItem(default);
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
            if (ItemHoverInfo.CanInspectWear)
            {
                ItemHoverInfo.ApplyInspectWear();

                return;
            }

            ItemHoverInteract.ApplyInteractWithItem();
        }

        // ItemsSell

        private void HandleSellItem()
        {
            ItemHoverSell.ApplySellItem();
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
            // Item place can be resumed from part install inside the Quit
            // callback. Quit and Cancel share the same key, so the Cancel
            // callback of that same press must not cancel the resumed
            // placement immediately.
            if (Time.frameCount == itemPlaceCancelEnableFrame)
            {
                return;
            }

            ItemHoverPlace.ApplyCancelItemPlace();
        }

        // PartsWear

        private void HandleAnotherHoverLayerPartsWear(Vector2 value)
        {
            PartHoverWear.ApplyAnotherHoverLayer(value);
        }

        // PartInstall

        private void HandleInstallPart()
        {
            PartHoverInstall.ApplyInstallPart();
        }

        private void HandleAnotherHoverLayerPartInstall(Vector2 value)
        {
            PartHoverInstall.ApplyAnotherHoverLayer(value);
        }

        // PartsRemove

        private void HandleRemovePart()
        {
            PartHoverRemove.ApplyRemovePart();
        }

        private void HandleAnotherHoverLayerPartsRemove(Vector2 value)
        {
            PartHoverRemove.ApplyAnotherHoverLayer(value);
        }

        // PartsIntegrity

        private void HandleAnotherHoverLayerPartsIntegrity(Vector2 value)
        {
            PartHoverIntegrity.ApplyAnotherHoverLayer(value);
        }
    }
}

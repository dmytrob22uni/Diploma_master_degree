using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Coursework_Master_Degree.Input
{
    [CreateAssetMenu(fileName = "input_reader", menuName = "Game Items/Input/Input Reader")]
    public class InputReader : ScriptableObject,
        GameInputActions.IMovementActions,
        GameInputActions.IQuitCancelActions,
        GameInputActions.IFreeWalkActions,
        GameInputActions.IItemsSellActions,
        GameInputActions.IItemPlaceActions,
        GameInputActions.IPartInstallActions,
        GameInputActions.IPlugConnectActions,
        GameInputActions.IItemApplyActions,
        GameInputActions.IPostProcessActions,
        GameInputActions.IObjectPlaceActions,
        GameInputActions.IDeviceBindActions,
        GameInputActions.ISwitchConnectActions,
        GameInputActions.IPartsWearActions,
        GameInputActions.IPartsIntegrityActions,
        GameInputActions.IPartsRemoveActions
    {
        private GameInputActions gameInputActions;

        // === Movement events ===
        public event Action<Vector2> OnMoveEvent = delegate { };
        public event Action<Vector2> OnLookEvent = delegate { };
        public event Action OnJumpEvent = delegate { };
        public event Action OnCrouchEvent = delegate { };
        public event Action<bool> OnInitiateWalkEvent = delegate { };
        public event Action<bool> OnInitiateRunEvent = delegate { };
        public event Action OnToggleZoomEvent = delegate { };

        // === QuitCancel events ===
        public event Action OnQuitEvent = delegate { };
        public event Action OnCancelEvent = delegate { };

        // === FreeWalk events ===
        public event Action OnItemsMarketEvent = delegate { };
        public event Action OnInteractWithItemEvent = delegate { };
        public event Action OnTakeItemEvent = delegate { };

        // === ItemsSell events ===
        public event Action OnSellItemEvent = delegate { };

        // === ItemPlace events ===
        public event Action OnPlaceItemEvent = delegate { };
        public event Action<float> OnRotateItemEvent = delegate { };

        // === PartInstall events ===
        public event Action OnInstallPartEvent = delegate { };

        // === PlugConnect events ===
        public event Action OnConnectPlugEvent = delegate { };
        public event Action OnDisconnectPlugEvent = delegate { };

        // === ItemApply events ===
        public event Action OnApplyItemEvent = delegate { };

        // === PostProcess events ===
        public event Action<bool> OnInitiateCameraRotationAroundObjectEvent = delegate { };
        public event Action<Vector2> OnRotateCameraAroundObjectEvent = delegate { };
        public event Action<float> OnZoomCameraOnObjectEvent = delegate { };
        public event Action<bool> OnInitiateObjectRotationEvent = delegate { };
        public event Action<Vector2> OnRotateObjectEvent = delegate { };
        public event Action<bool> OnInitiateProcessingEvent = delegate { };
        public event Action<Vector2> OnMoveToolEvent = delegate { };

        // === ObjectPlace events ===
        public event Action<bool> OnInitiateCameraRotationAroundPlateEvent = delegate { };
        public event Action<Vector2> OnRotateCameraAroundPlateEvent = delegate { };
        public event Action<float> OnZoomCameraOnPlateEvent = delegate { };
        public event Action OnObjectRotationModifierOnPlateEvent = delegate { };
        public event Action<Vector2> OnRotateObjectOnPlateEvent = delegate { };
        public event Action OnObjectScaleModifierOnPlateEvent = delegate { };
        public event Action<Vector2> OnScaleObjectOnPlateEvent = delegate { };
        public event Action OnObjectPlacementModifierOnPlateEvent = delegate { };
        public event Action<Vector2> OnObjectPlacementOnPlateEvent = delegate { };
        public event Action OnConfirmObjectPlacementOnPlateEvent = delegate { };

        // === DeviceBind events ===
        public event Action OnBindDeviceEvent = delegate { };
        public event Action OnUnbindDeviceEvent = delegate { };

        // === SwitchConnect events ===
        public event Action OnConnectSwitchEvent = delegate { };
        public event Action OnDisconnectSwitchEvent = delegate { };

        // === PartsWear events ===
        public event Action OnRemoveModeFromPartsWearEvent = delegate { };

        // === PartsIntegrity events ===
        public event Action OnRemoveModeFromPartsIntegrityEvent = delegate { };

        // === PartsRemove events ===
        public event Action OnRemovePartEvent = delegate { };

        private void OnEnable()
        {
            if (gameInputActions == null)
            {
                gameInputActions = new GameInputActions();

                gameInputActions.Movement.SetCallbacks(this);
                gameInputActions.QuitCancel.SetCallbacks(this);
                gameInputActions.FreeWalk.SetCallbacks(this);
                gameInputActions.ItemsSell.SetCallbacks(this);
                gameInputActions.ItemPlace.SetCallbacks(this);
                gameInputActions.PartInstall.SetCallbacks(this);
                gameInputActions.PlugConnect.SetCallbacks(this);
                gameInputActions.ItemApply.SetCallbacks(this);
                gameInputActions.PostProcess.SetCallbacks(this);
                gameInputActions.ObjectPlace.SetCallbacks(this);
                gameInputActions.DeviceBind.SetCallbacks(this);
                gameInputActions.SwitchConnect.SetCallbacks(this);
                gameInputActions.PartsWear.SetCallbacks(this);
                gameInputActions.PartsIntegrity.SetCallbacks(this);
                gameInputActions.PartsRemove.SetCallbacks(this);
            }

            EnableFreeWalkStateInput();
        }

        private void OnDisable()
        {
            DisableAllInput();
        }

        // === State Inputs ============================================================================================

        public void EnableFreeWalkStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.FreeWalk.Enable();
        }

        public void EnableItemsSellStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.ItemsSell.Enable();
        }

        public void EnableItemPlaceStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.ItemPlace.Enable();
        }

        public void EnablePartInstallStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.PartInstall.Enable();
        }

        public void EnablePlugConnectStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.PlugConnect.Enable();
        }

        public void EnableItemApplyStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.ItemApply.Enable();
        }

        public void EnablePostProcessStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.PostProcess.Enable();
        }

        public void EnableObjectPlaceStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.ObjectPlace.Enable();
        }

        public void EnableDeviceBindStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.DeviceBind.Enable();
        }

        public void EnableSwitchConnectStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.SwitchConnect.Enable();
        }

        public void EnablePartsWearStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.PartsWear.Enable();
        }

        public void EnablePartsIntegrityStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.PartsIntegrity.Enable();
        }

        public void EnablePartsRemoveStateInput()
        {
            DisableAllInput();
            gameInputActions.Movement.Enable();
            gameInputActions.QuitCancel.Enable();
            gameInputActions.PartsRemove.Enable();
        }

        public void EnableItemsMarketStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
        }

        public void EnableInteractionsListStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
        }

        public void EnableDeviceInterfaceStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
        }

        public void EnableObjectQualityStateInput()
        {
            DisableAllInput();
            gameInputActions.QuitCancel.Enable();
        }

        public void DisableAllInput()
        {
            gameInputActions.Movement.Disable();
            gameInputActions.QuitCancel.Disable();
            gameInputActions.FreeWalk.Disable();
            gameInputActions.ItemsSell.Disable();
            gameInputActions.ItemPlace.Disable();
            gameInputActions.PartInstall.Disable();
            gameInputActions.PlugConnect.Disable();
            gameInputActions.ItemApply.Disable();
            gameInputActions.PostProcess.Disable();
            gameInputActions.ObjectPlace.Disable();
            gameInputActions.DeviceBind.Disable();
            gameInputActions.SwitchConnect.Disable();
            gameInputActions.PartsWear.Disable();
            gameInputActions.PartsIntegrity.Disable();
            gameInputActions.PartsRemove.Disable();
        }

        // === Movement callbacks ======================================================================================

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnMoveEvent?.Invoke(value);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnLookEvent?.Invoke(value);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnJumpEvent?.Invoke();
            }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnCrouchEvent?.Invoke();
            }
        }

        public void OnInitiateWalk(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateWalkEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateWalkEvent?.Invoke(false);
            }
        }

        public void OnInitiateRun(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateRunEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateRunEvent?.Invoke(false);
            }
        }

        public void OnToggleZoom(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnToggleZoomEvent?.Invoke();
            }
        }

        // === QuitCancel callbacks ====================================================================================

        public void OnQuit(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnQuitEvent?.Invoke();
            }
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnCancelEvent?.Invoke();
            }
        }

        // === FreeWalk callbacks ======================================================================================

        public void OnItemsMarket(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnItemsMarketEvent?.Invoke();
            }
        }

        public void OnInteractWithItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnInteractWithItemEvent?.Invoke();
            }
        }

        public void OnTakeItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnTakeItemEvent?.Invoke();
            }
        }

        // === ItemsSell callbacks =====================================================================================

        public void OnSellItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnSellItemEvent?.Invoke();
            }
        }

        // === ItemPlace callbacks =====================================================================================

        public void OnPlaceItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnPlaceItemEvent?.Invoke();
            }
        }

        public void OnRotateItem(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            OnRotateItemEvent?.Invoke(value);
        }

        // === PartInstall callbacks ===================================================================================

        public void OnInstallPart(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnInstallPartEvent?.Invoke();
            }
        }

        // === PlugConnect callbacks ===================================================================================

        public void OnConnectPlug(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnConnectPlugEvent?.Invoke();
            }
        }

        public void OnDisconnectPlug(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnDisconnectPlugEvent?.Invoke();
            }
        }

        // === ItemApply callbacks =====================================================================================

        public void OnApplyItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnApplyItemEvent?.Invoke();
            }
        }

        // === PostProcess callbacks ===================================================================================

        public void OnInitiateCameraRotationAroundObject(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateCameraRotationAroundObjectEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateCameraRotationAroundObjectEvent?.Invoke(false);
            }
        }

        public void OnRotateCameraAroundObject(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnRotateCameraAroundObjectEvent?.Invoke(value);
        }

        public void OnZoomCameraOnObject(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            OnZoomCameraOnObjectEvent?.Invoke(value);
        }

        public void OnInitiateObjectRotation(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateObjectRotationEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateObjectRotationEvent?.Invoke(false);
            }
        }

        public void OnRotateObject(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnRotateObjectEvent?.Invoke(value);
        }

        public void OnInitiateProcessing(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateProcessingEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateProcessingEvent?.Invoke(false);
            }
        }

        public void OnMoveTool(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnMoveToolEvent?.Invoke(value);
        }

        // === ObjectPlace callbacks ===================================================================================

        public void OnInitiateCameraRotationAroundPlate(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnInitiateCameraRotationAroundPlateEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInitiateCameraRotationAroundPlateEvent?.Invoke(false);
            }
        }

        public void OnRotateCameraAroundPlate(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnRotateCameraAroundPlateEvent?.Invoke(value);
        }

        public void OnZoomCameraOnPlate(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            OnZoomCameraOnPlateEvent?.Invoke(value);
        }

        public void OnObjectRotationModifierOnPlate(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnObjectRotationModifierOnPlateEvent?.Invoke();
            }
        }

        public void OnRotateObjectOnPlate(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnRotateObjectOnPlateEvent?.Invoke(value);
        }

        public void OnObjectScaleModifierOnPlate(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnObjectScaleModifierOnPlateEvent?.Invoke();
            }
        }

        public void OnScaleObjectOnPlate(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnScaleObjectOnPlateEvent?.Invoke(value);
        }

        public void OnObjectPlacementModifierOnPlate(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnObjectPlacementModifierOnPlateEvent?.Invoke();
            }
        }

        public void OnObjectPlacementOnPlate(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnObjectPlacementOnPlateEvent?.Invoke(value);
        }

        public void OnConfirmObjectPlacementOnPlate(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnConfirmObjectPlacementOnPlateEvent?.Invoke();
            }
        }

        // === DeviceBind callbacks ====================================================================================

        public void OnBindDevice(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnBindDeviceEvent?.Invoke();
            }
        }

        public void OnUnbindDevice(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnUnbindDeviceEvent?.Invoke();
            }
        }

        // === SwitchConnect callbacks =================================================================================

        public void OnConnectSwitch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnConnectSwitchEvent?.Invoke();
            }
        }

        public void OnDisconnectSwitch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnDisconnectSwitchEvent?.Invoke();
            }
        }

        // === PartsWear callbacks =====================================================================================

        public void OnRemoveModeFromPartsWear(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnRemoveModeFromPartsWearEvent?.Invoke();
            }
        }

        // === PartsIntegrity callbacks ================================================================================

        public void OnRemoveModeFromPartsIntegrity(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnRemoveModeFromPartsIntegrityEvent?.Invoke();
            }
        }

        // === PartsRemove callbacks ===================================================================================

        public void OnRemovePart(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnRemovePartEvent?.Invoke();
            }
        }
    }
}

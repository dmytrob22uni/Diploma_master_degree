using UnityEngine;
using UnityEngine.InputSystem;
using Coursework_Master_Degree.Game.Logic;

namespace Coursework_Master_Degree.UserInterface.ItemsMarket.Items
{
    public class UserInterfaceItemsInputManager : MonoBehaviour
    {
        public InputActionAsset GlobalInputAction;

        public UserInterfaceItemsNetManager UserInterfaceItemsNetCreator;
        public Movement Movement;

        private InputAction _showCoreItemPickMenuInterfaceInputAction;
        private InputAction _hideAnyMenuInterfaceInputAction;

        private float _showCoreItemPickMenuInterfaceValue;
        private float _hideAnyMenuInterfaceValue;

        private bool _isCoreItemPickMenuInterfaceShown;

        void Awake()
        {
            InputActionMap inputActionMapFreeWalk = GlobalInputAction.FindActionMap("free_walk", true);
            InputActionMap inputActionMapQuitCancel = GlobalInputAction.FindActionMap("quit_cancel", true);
            _showCoreItemPickMenuInterfaceInputAction = inputActionMapFreeWalk.FindAction("items_market", true);
            _hideAnyMenuInterfaceInputAction = inputActionMapQuitCancel.FindAction("quit", true);
        }

        void OnEnable()
        {
            _showCoreItemPickMenuInterfaceInputAction.Enable();
            _hideAnyMenuInterfaceInputAction.Enable();
        }

        void OnDisable()
        {
            _showCoreItemPickMenuInterfaceValue = default;
            _hideAnyMenuInterfaceValue = default;
            _isCoreItemPickMenuInterfaceShown = default;

            _showCoreItemPickMenuInterfaceInputAction.Disable();
            _hideAnyMenuInterfaceInputAction.Disable();
        }

        void Update()
        {
            _showCoreItemPickMenuInterfaceValue = _showCoreItemPickMenuInterfaceInputAction.ReadValue<float>();
            _hideAnyMenuInterfaceValue = _hideAnyMenuInterfaceInputAction.ReadValue<float>();

            if (_showCoreItemPickMenuInterfaceValue > 0.5f)
            {
                if (!_isCoreItemPickMenuInterfaceShown)
                {
                    ShowCursor();
                    Movement.enabled = false;

                    UserInterfaceItemsNetCreator.CoreItemPickCanvasGameObjects.SetActive(true);

                    _isCoreItemPickMenuInterfaceShown = true;
                }
            }

            if (_hideAnyMenuInterfaceValue > 0.5f)
            {
                if (_isCoreItemPickMenuInterfaceShown)
                {
                    HideCursor();
                    Movement.enabled = true;

                    UserInterfaceItemsNetCreator.LastActiveItemsPickCanvasGameObject.SetActive(false);

                    _isCoreItemPickMenuInterfaceShown = false;
                }
            }
        }

        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}

using TMPro;
using UnityEngine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Items.FFF;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceBind
{
    /// <summary>
    /// Fills the authored device_bind_canvas while the player aims at
    /// printers. The header names the mode, the middle line shows the hovered
    /// printer, and failed clicks show their reason for a short time.
    /// </summary>
    public class DeviceBindPanel : MonoBehaviour
    {
        [Header("Texts")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI HoverNameText;
        public TextMeshProUGUI ReasonText;

        [Header("Action hint")]
        public GameObject BindRoot;

        [Header("Behaviour")]
        [Tooltip("How long a failed connection reason stays on screen")]
        [Min(0.5f)]
        public float ReasonDuration = 4.0f;

        private Printer target;
        private DeviceBindMode mode;
        private float reasonHideTime;

        public void Show(DeviceBindMode bindMode)
        {
            mode = bindMode;
            gameObject.SetActive(true);

            if (TitleText != null)
            {
                TitleText.text = mode == DeviceBindMode.Bind
                    ? "Printer binding"
                    : "Printer unbinding";
            }

            target = null;
            ClearReason();
            HideName();
            HideAction();
        }

        public void Hide()
        {
            target = null;
            ClearReason();
            HideName();
            HideAction();
            gameObject.SetActive(false);
        }

        public void SetTarget(Printer printer)
        {
            if (printer == target)
            {
                return;
            }

            target = printer;
            ClearReason();

            if (printer == null)
            {
                HideName();
                HideAction();
                return;
            }

            if (HoverNameText != null)
            {
                HoverNameText.text = printer.DisplayName;
                HoverNameText.gameObject.SetActive(true);
            }

            if (BindRoot != null)
            {
                // The key hint keeps its authored text in both modes.
                BindRoot.SetActive(true);
            }
        }

        public void ShowReason(string message)
        {
            if (ReasonText == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            ReasonText.text = message;
            ReasonText.gameObject.SetActive(true);
            reasonHideTime = Time.unscaledTime +
                Mathf.Max(0.5f, ReasonDuration);
        }

        private void Update()
        {
            if (reasonHideTime > 0.0f && Time.unscaledTime >= reasonHideTime)
            {
                ClearReason();
            }
        }

        private void ClearReason()
        {
            reasonHideTime = 0.0f;

            if (ReasonText != null)
            {
                ReasonText.gameObject.SetActive(false);
            }
        }

        private void HideAction()
        {
            if (BindRoot != null)
            {
                BindRoot.SetActive(false);
            }
        }

        private void HideName()
        {
            if (HoverNameText == null)
            {
                return;
            }

            HoverNameText.text = string.Empty;
            HoverNameText.gameObject.SetActive(false);
        }
    }
}

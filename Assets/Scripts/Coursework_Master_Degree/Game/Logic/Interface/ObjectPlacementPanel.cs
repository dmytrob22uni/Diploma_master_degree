using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Coursework_Master_Degree.Game.Logic.Interface
{
    /// <summary>
    /// Dynamic part of the authored object_place_canvas. The static hints stay
    /// visible; this panel only owns visibility and the reason list.
    /// </summary>
    public sealed class ObjectPlacementPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI reasonText;

        private void Awake()
        {
            if (reasonText == null)
            {
                Transform reason = transform.Find("object_place_reason");
                reasonText = reason != null
                    ? reason.GetComponent<TextMeshProUGUI>()
                    : null;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (visible)
            {
                SetReasons(null);
            }
        }

        public void SetReasons(IReadOnlyList<string> reasons)
        {
            if (reasonText == null)
            {
                return;
            }

            bool hasReasons = reasons != null && reasons.Count > 0;
            reasonText.gameObject.SetActive(hasReasons);

            if (!hasReasons)
            {
                reasonText.text = string.Empty;
                return;
            }

            StringBuilder text = new StringBuilder();

            for (int i = 0; i < reasons.Count; i++)
            {
                if (i > 0)
                {
                    text.Append('\n');
                }

                text.Append("- ").Append(reasons[i]);
            }

            reasonText.text = text.ToString();
        }
    }
}

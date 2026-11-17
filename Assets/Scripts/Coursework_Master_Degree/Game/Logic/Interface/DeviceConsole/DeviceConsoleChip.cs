using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// Key badge followed by an action label, matching the interactions_list
    /// and part install prompts. The badge is sized to its key, so "LMB" and
    /// "Q" both fit without a fixed width.
    /// </summary>
    public sealed class DeviceConsoleChip
    {
        private const float BadgeHeight = 30.0f;
        private const float MinBadgeWidth = 30.0f;
        private const float BadgePadding = 16.0f;
        private const float LabelGap = 10.0f;

        private readonly RectTransform badge;
        private readonly TextMeshProUGUI keyText;
        private readonly TextMeshProUGUI labelText;

        private DeviceConsoleChip(
            RectTransform root,
            RectTransform badgeRect,
            TextMeshProUGUI key,
            TextMeshProUGUI label)
        {
            Root = root;
            badge = badgeRect;
            keyText = key;
            labelText = label;
        }

        public RectTransform Root { get; }

        public static DeviceConsoleChip Create(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Sprite sprite,
            Color labelColor)
        {
            RectTransform root = DeviceConsoleStyle.CreateRect(name, parent);

            // Sliced keeps the rounded corners square; a simple image
            // stretches the whole sprite into a wide pill.
            Image badgeImage = DeviceConsoleStyle.CreateImage(
                "badge",
                root,
                DeviceConsoleStyle.BoxWhite,
                sprite,
                true,
                false);

            RectTransform badgeRect = (RectTransform)badgeImage.transform;
            badgeRect.anchorMin = new Vector2(0.0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.0f, 0.5f);
            badgeRect.pivot = new Vector2(0.0f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI key = DeviceConsoleStyle.CreateText(
                "key",
                badgeRect,
                string.Empty,
                DeviceConsoleStyle.FontBody,
                TextAlignmentOptions.Center,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.Stretch((RectTransform)key.transform);

            TextMeshProUGUI label = DeviceConsoleStyle.CreateText(
                "label",
                root,
                string.Empty,
                DeviceConsoleStyle.FontBody,
                TextAlignmentOptions.MidlineLeft,
                labelColor,
                font);

            DeviceConsoleStyle.Stretch((RectTransform)label.transform);

            return new DeviceConsoleChip(root, badgeRect, key, label);
        }

        public void Set(string key, string text)
        {
            keyText.text = key;
            labelText.text = text;

            float width = Mathf.Max(
                MinBadgeWidth,
                keyText.GetPreferredValues(key).x + BadgePadding);

            badge.sizeDelta = new Vector2(width, BadgeHeight);
            labelText.rectTransform.offsetMin = new Vector2(width + LabelGap, 0.0f);
        }

        public void SetActive(bool active)
        {
            Root.gameObject.SetActive(active);
        }
    }
}

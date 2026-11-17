using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// Visual language shared by the device console and its screen-space
    /// prompts. It mirrors the market prefabs under
    /// Assets/Art/3D/Prefabs/Interface/items_market: one reference resolution,
    /// plain rounded boxes, black text, and no accent colours.
    /// </summary>
    public static class DeviceConsoleStyle
    {
        public const float ReferenceWidth = 1600.0f;
        public const float ReferenceHeight = 900.0f;

        public const float Margin = 24.0f;
        public const float HeaderHeight = 84.0f;
        public const float FooterHeight = 90.0f;
        public const float ListWidth = 400.0f;
        public const float RowHeight = 64.0f;
        public const float RowSpacing = 8.0f;

        public const float FontTitle = 25.0f;
        public const float FontHeading = 20.0f;
        public const float FontBody = 20.0f;
        public const float FontSmall = 18.0f;

        public static readonly Color Panel =
            new Color(1.0f, 1.0f, 1.0f, 0.78f);

        public static readonly Color BoxGray =
            new Color(0.5188679f, 0.5188679f, 0.5188679f, 0.78431374f);

        public static readonly Color BoxWhite =
            new Color(1.0f, 1.0f, 1.0f, 0.78f);

        public static readonly Color RowIdle =
            new Color(1.0f, 1.0f, 1.0f, 0.52f);

        public static readonly Color RowActive =
            new Color(1.0f, 1.0f, 1.0f, 0.94f);

        public static readonly Color Text =
            new Color(0.0f, 0.0f, 0.0f, 1.0f);

        public static readonly Color TextMuted =
            new Color(0.0f, 0.0f, 0.0f, 0.62f);

        public static readonly Color TextDisabled =
            new Color(0.0f, 0.0f, 0.0f, 0.38f);

        public static readonly Color TextOnDark =
            new Color(1.0f, 1.0f, 1.0f, 1.0f);

        public static readonly Color TextOnDarkMuted =
            new Color(1.0f, 1.0f, 1.0f, 0.72f);

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(
            string name,
            Transform parent,
            Color color,
            Sprite sprite = null,
            bool sliced = true,
            bool raycastTarget = false)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }

            return image;
        }

        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            TMP_FontAsset font,
            bool bold = false)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label =
                rect.gameObject.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset fontAsset = font != null
                ? font
                : TMP_Settings.defaultFontAsset;

            if (fontAsset != null)
            {
                label.font = fontAsset;
            }

            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string text,
            TMP_FontAsset font,
            Sprite sprite,
            out TextMeshProUGUI labelText)
        {
            RectTransform rect = CreateRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = BoxWhite;
            image.raycastTarget = true;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.84f, 0.84f, 1.0f);
            colors.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1.0f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.5f);
            button.colors = colors;

            labelText = CreateText(
                "label",
                rect,
                text,
                FontHeading,
                TextAlignmentOptions.Center,
                Text,
                font);

            Stretch((RectTransform)labelText.transform);
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}

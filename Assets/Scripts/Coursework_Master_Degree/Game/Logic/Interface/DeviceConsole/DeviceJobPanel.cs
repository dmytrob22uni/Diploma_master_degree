using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Types;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// M2 job picker, infill selector, estimates and validation, hosted inside
    /// the console's print-job card. Pure view: it reports Start print and lets
    /// DeviceController validate and queue the request.
    /// </summary>
    public sealed class DeviceJobPanel
    {
        private sealed class ObjectRow
        {
            public ObjectToPrintMetaSO Meta;
            public int Index;
            public Image Background;
            public Image SelectionBar;
            public TextMeshProUGUI NameText;
        }

        private const float ListWidth = 320.0f;
        private const float RowHeight = 44.0f;
        private const float RowSpacing = 6.0f;

        private readonly TMP_FontAsset font;
        private readonly Sprite boxSprite;
        private readonly ObjectToPrintMetasListSO catalog;
        private readonly List<ObjectRow> rows = new List<ObjectRow>();

        private Device device;
        private int selectedIndex;
        private InfillPattern infill = InfillPattern.Grid;
        private SupportsMode supports = SupportsMode.None;
        private LayerHeightPreset layerHeight = LayerHeightPreset.Normal;
        private PrintSpeedPreset speed = PrintSpeedPreset.Normal;

        private RawImage icon;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI heightText;
        private TextMeshProUGUI estimateText;
        private ScrollRect validationScroll;
        private RectTransform validationContent;
        private Button startButton;
        private TextMeshProUGUI startLabel;

        private readonly List<Button> infillButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> infillLabels =
            new List<TextMeshProUGUI>();
        private readonly List<Button> layerButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> layerLabels =
            new List<TextMeshProUGUI>();
        private readonly List<Button> speedButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> speedLabels =
            new List<TextMeshProUGUI>();
        private readonly List<Button> supportsButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> supportsLabels =
            new List<TextMeshProUGUI>();
        private string lastValidationSignature = string.Empty;
        private string lastValidationUiSignature = string.Empty;

        public DeviceJobPanel(
            TMP_FontAsset fontAsset,
            Sprite sprite,
            ObjectToPrintMetasListSO objectCatalog)
        {
            font = fontAsset;
            boxSprite = sprite;
            catalog = objectCatalog;
        }

        public event System.Action<
            Printer,
            ObjectToPrintMetaSO,
            InfillPattern,
            SupportsMode,
            LayerHeightPreset,
            PrintSpeedPreset> StartRequested;

        public void Build(RectTransform card)
        {
            TextMeshProUGUI heading = DeviceConsoleStyle.CreateText(
                "job_heading",
                card,
                "PRINT JOB",
                DeviceConsoleStyle.FontHeading,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)heading.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -10.0f),
                new Vector2(300.0f, 24.0f));

            BuildObjectList(card);
            BuildDetails(card);

            ObjectToPrintMetaSO meta = CurrentMeta;

            if (meta != null)
            {
                ApplyProfileDefaults(meta.PrintProfile);
            }

            RefreshRows();
        }

        public void SetDevice(Device target)
        {
            device = target;
        }

        public void Refresh()
        {
            RefreshRows();

            ObjectToPrintMetaSO meta = CurrentMeta;
            Printer printer = device != null ? device.ActivePrinter : null;
            ObjectToPrintPrintProfile profile = meta != null
                ? meta.PrintProfile
                : null;

            if (icon != null)
            {
                icon.texture = meta != null ? meta.Icon : null;
                icon.enabled = icon.texture != null;
            }

            if (nameText != null)
            {
                nameText.text = meta != null
                    ? meta.Name
                    : "No object selected";
            }

            if (heightText != null)
            {
                Vector3 size = ObjectToPrintSizing.GetSizeMeters(meta);
                heightText.text = size.y > 0.0f
                    ? $"{size.y * 100.0f:0.#} cm tall"
                    : "Size unavailable";
            }

            float filament = PrintEstimates.FilamentMeters(
                profile,
                infill,
                supports);
            float seconds = PrintEstimates.TimeSeconds(
                profile,
                infill,
                layerHeight,
                speed,
                supports);

            PrintJobValidationResult result = PrintJobValidator.Validate(
                device,
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed);

            if (estimateText != null)
            {
                estimateText.text =
                    $"{FormatDuration(seconds)}  |  {filament:0.0} m  |  " +
                    $"raw {result.EstimatedQualityMin:0}-" +
                    $"{result.EstimatedQualityMax:0}";
            }

            LogValidationIfChanged(result);
            RefreshValidationUi(result, printer);

            bool canStart = printer != null &&
                !printer.HasPendingJob &&
                result.IsValid;

            if (startButton != null)
            {
                startButton.interactable = canStart;
            }

            if (startLabel != null)
            {
                startLabel.color = canStart
                    ? DeviceConsoleStyle.Text
                    : DeviceConsoleStyle.TextDisabled;
            }

            RefreshPresetStyles(
                layerButtons,
                layerLabels,
                (int)layerHeight);
            RefreshPresetStyles(
                speedButtons,
                speedLabels,
                (int)speed);
            RefreshPresetStyles(
                supportsButtons,
                supportsLabels,
                (int)supports);
            RefreshPresetStyles(
                infillButtons,
                infillLabels,
                (int)infill);
        }

        private ObjectToPrintMetaSO CurrentMeta
        {
            get
            {
                if (catalog == null ||
                    catalog.ObjectToPrintMetasList == null ||
                    catalog.ObjectToPrintMetasList.Length == 0)
                {
                    return null;
                }

                selectedIndex = Mathf.Clamp(
                    selectedIndex,
                    0,
                    catalog.ObjectToPrintMetasList.Length - 1);

                return catalog.ObjectToPrintMetasList[selectedIndex];
            }
        }

        private void BuildObjectList(RectTransform card)
        {
            RectTransform listCard =
                DeviceConsoleStyle.CreateRect("objects_card", card);

            listCard.anchorMin = new Vector2(0.0f, 0.0f);
            listCard.anchorMax = new Vector2(0.0f, 1.0f);
            listCard.pivot = new Vector2(0.0f, 0.5f);
            listCard.offsetMin = new Vector2(DeviceConsoleStyle.Margin, 20.0f);
            listCard.offsetMax = new Vector2(
                DeviceConsoleStyle.Margin + ListWidth,
                -40.0f);

            Image cardImage = listCard.gameObject.AddComponent<Image>();
            cardImage.color = DeviceConsoleStyle.BoxGray;
            cardImage.raycastTarget = false;

            TextMeshProUGUI title = DeviceConsoleStyle.CreateText(
                "objects_title",
                listCard,
                "OBJECTS",
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)title.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(12.0f, -10.0f),
                new Vector2(200.0f, 22.0f));

            RectTransform scroll =
                DeviceConsoleStyle.CreateRect("objects_scroll", listCard);

            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.offsetMin = new Vector2(10.0f, 10.0f);
            scroll.offsetMax = new Vector2(-10.0f, -38.0f);

            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40.0f;

            RectTransform viewport =
                DeviceConsoleStyle.CreateRect("viewport", scroll);

            DeviceConsoleStyle.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            viewportHit.raycastTarget = true;

            RectTransform content =
                DeviceConsoleStyle.CreateRect("content", viewport);

            content.anchorMin = new Vector2(0.0f, 1.0f);
            content.anchorMax = new Vector2(1.0f, 1.0f);
            content.pivot = new Vector2(0.5f, 1.0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = RowSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            BuildObjectRows(content);
        }

        private void BuildObjectRows(RectTransform content)
        {
            ObjectToPrintMetaSO[] metas = catalog != null
                ? catalog.ObjectToPrintMetasList
                : null;

            if (metas == null || metas.Length == 0)
            {
                TextMeshProUGUI emptyText = DeviceConsoleStyle.CreateText(
                    "objects_empty",
                    content,
                    "No objects in the catalog.",
                    DeviceConsoleStyle.FontSmall,
                    TextAlignmentOptions.TopLeft,
                    DeviceConsoleStyle.TextMuted,
                    font);

                RectTransform emptyRect =
                    (RectTransform)emptyText.transform;

                emptyRect.sizeDelta = new Vector2(0.0f, 60.0f);

                LayoutElement emptyLayout =
                    emptyText.gameObject.AddComponent<LayoutElement>();

                emptyLayout.preferredHeight = 60.0f;
                emptyLayout.flexibleWidth = 1.0f;
                return;
            }

            List<ObjectToPrintType?> order = new List<ObjectToPrintType?>();
            Dictionary<ObjectToPrintType?, List<int>> groups =
                new Dictionary<ObjectToPrintType?, List<int>>();

            for (int i = 0; i < metas.Length; i++)
            {
                if (metas[i] == null)
                {
                    continue;
                }

                ObjectToPrintType? type = GetPrimaryType(metas[i]);

                if (!groups.TryGetValue(type, out List<int> indices))
                {
                    indices = new List<int>();
                    groups.Add(type, indices);
                    order.Add(type);
                }

                indices.Add(i);
            }

            for (int i = 0; i < order.Count; i++)
            {
                ObjectToPrintType? type = order[i];

                if (type.HasValue)
                {
                    CreateSectionHeader(
                        content,
                        type.Value.ToString().ToUpperInvariant());
                }
                else
                {
                    CreateSectionHeader(content, "OTHER");
                }

                List<int> indices = groups[type];

                for (int j = 0; j < indices.Count; j++)
                {
                    int index = indices[j];
                    CreateObjectRow(content, metas[index], index);
                }
            }
        }

        private void CreateSectionHeader(RectTransform content, string title)
        {
            TextMeshProUGUI header = DeviceConsoleStyle.CreateText(
                "object_section",
                content,
                title,
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.MidlineLeft,
                DeviceConsoleStyle.TextMuted,
                font,
                true);

            RectTransform headerRect = (RectTransform)header.transform;
            headerRect.sizeDelta = new Vector2(0.0f, 24.0f);

            LayoutElement layout =
                header.gameObject.AddComponent<LayoutElement>();

            layout.preferredHeight = 24.0f;
            layout.flexibleWidth = 1.0f;
        }

        private static ObjectToPrintType? GetPrimaryType(
            ObjectToPrintMetaSO meta)
        {
            if (meta.ObjectToPrintTypes == null ||
                meta.ObjectToPrintTypes.Length == 0)
            {
                return null;
            }

            return meta.ObjectToPrintTypes[0];
        }

        private void CreateObjectRow(
            RectTransform content,
            ObjectToPrintMetaSO meta,
            int index)
        {
            RectTransform row = DeviceConsoleStyle.CreateRect(
                "object_row",
                content);

            row.sizeDelta = new Vector2(0.0f, RowHeight);

            LayoutElement layout =
                row.gameObject.AddComponent<LayoutElement>();

            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1.0f;

            Image background = row.gameObject.AddComponent<Image>();
            background.color = DeviceConsoleStyle.Panel;
            background.raycastTarget = true;

            if (boxSprite != null)
            {
                background.sprite = boxSprite;
                background.type = Image.Type.Sliced;
            }

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.84f, 0.84f, 1.0f);
            colors.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1.0f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.5f);
            button.colors = colors;

            Image selectionBar = DeviceConsoleStyle.CreateImage(
                "selection_bar",
                row,
                DeviceConsoleStyle.Text);

            RectTransform barRect = (RectTransform)selectionBar.transform;
            barRect.anchorMin = new Vector2(0.0f, 0.0f);
            barRect.anchorMax = new Vector2(0.0f, 1.0f);
            barRect.pivot = new Vector2(0.0f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(4.0f, 0.0f);

            TextMeshProUGUI name = DeviceConsoleStyle.CreateText(
                "name",
                row,
                meta.Name,
                DeviceConsoleStyle.FontBody,
                TextAlignmentOptions.MidlineLeft,
                DeviceConsoleStyle.Text,
                font);

            RectTransform nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(14.0f, 8.0f);
            nameRect.offsetMax = new Vector2(-12.0f, -8.0f);

            int capturedIndex = index;

            button.onClick.AddListener(() =>
            {
                selectedIndex = capturedIndex;
                ApplyProfileDefaults(meta.PrintProfile);
                Refresh();
            });

            rows.Add(new ObjectRow
            {
                Meta = meta,
                Index = index,
                Background = background,
                SelectionBar = selectionBar,
                NameText = name
            });
        }

        private void BuildDetails(RectTransform card)
        {
            RectTransform details =
                DeviceConsoleStyle.CreateRect("job_details", card);

            details.anchorMin = Vector2.zero;
            details.anchorMax = Vector2.one;
            details.offsetMin = new Vector2(
                DeviceConsoleStyle.Margin + ListWidth + 20.0f,
                20.0f);
            details.offsetMax = new Vector2(
                -DeviceConsoleStyle.Margin,
                -40.0f);

            RectTransform iconRect =
                DeviceConsoleStyle.CreateRect("object_icon", details);

            icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.color = Color.white;
            icon.raycastTarget = false;

            DeviceConsoleStyle.SetRect(
                iconRect,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                Vector2.zero,
                new Vector2(56.0f, 56.0f));

            nameText = DeviceConsoleStyle.CreateText(
                "object_name",
                details,
                "No object selected",
                DeviceConsoleStyle.FontTitle,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)nameText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(66.0f, -2.0f),
                new Vector2(700.0f, 28.0f));

            heightText = DeviceConsoleStyle.CreateText(
                "object_height",
                details,
                string.Empty,
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.TextMuted,
                font);

            DeviceConsoleStyle.SetRect(
                (RectTransform)heightText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(66.0f, -32.0f),
                new Vector2(700.0f, 22.0f));

            BuildPresetRow(
                details,
                "LAYER",
                -58.0f,
                new[]
                {
                    LayerHeightSettings.Label(LayerHeightPreset.Draft),
                    LayerHeightSettings.Label(LayerHeightPreset.Normal),
                    LayerHeightSettings.Label(LayerHeightPreset.Fine)
                },
                index => layerHeight = (LayerHeightPreset)index,
                layerButtons,
                layerLabels);

            BuildPresetRow(
                details,
                "SPEED",
                -90.0f,
                new[]
                {
                    PrintSpeedSettings.Label(PrintSpeedPreset.Slow),
                    PrintSpeedSettings.Label(PrintSpeedPreset.Normal),
                    PrintSpeedSettings.Label(PrintSpeedPreset.Fast)
                },
                index => speed = (PrintSpeedPreset)index,
                speedButtons,
                speedLabels);

            BuildPresetRow(
                details,
                "INFILL",
                -122.0f,
                new[]
                {
                    InfillSettings.Label(InfillPattern.None),
                    InfillSettings.Label(InfillPattern.Grid),
                    InfillSettings.Label(InfillPattern.Gyroid)
                },
                index => infill = (InfillPattern)index,
                infillButtons,
                infillLabels);

            BuildPresetRow(
                details,
                "SUPPORTS",
                -154.0f,
                new[]
                {
                    SupportsSettings.Label(SupportsMode.None),
                    SupportsSettings.Label(SupportsMode.Auto)
                },
                index => supports = (SupportsMode)index,
                supportsButtons,
                supportsLabels);

            estimateText = DeviceConsoleStyle.CreateText(
                "estimates",
                details,
                string.Empty,
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font);

            DeviceConsoleStyle.SetRect(
                (RectTransform)estimateText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, -188.0f),
                new Vector2(700.0f, 24.0f));

            BuildValidationViewport(details);

            startButton = DeviceConsoleStyle.CreateButton(
                "start_button",
                details,
                "Start print",
                font,
                boxSprite,
                out startLabel);

            DeviceConsoleStyle.SetRect(
                (RectTransform)startButton.transform,
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                Vector2.zero,
                new Vector2(200.0f, 44.0f));

            startButton.onClick.AddListener(HandleStartClicked);
        }

        private void BuildPresetRow(
            RectTransform details,
            string title,
            float y,
            string[] labels,
            System.Action<int> onSelected,
            List<Button> buttons,
            List<TextMeshProUGUI> texts)
        {
            TextMeshProUGUI label = DeviceConsoleStyle.CreateText(
                $"{title}_label",
                details,
                title,
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.MidlineLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            label.textWrappingMode = TextWrappingModes.NoWrap;

            DeviceConsoleStyle.SetRect(
                (RectTransform)label.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, y),
                new Vector2(120.0f, 30.0f));

            for (int i = 0; i < labels.Length; i++)
            {
                int captured = i;

                Button button = DeviceConsoleStyle.CreateButton(
                    $"{title}_{i}",
                    details,
                    labels[i],
                    font,
                    boxSprite,
                    out TextMeshProUGUI buttonLabel);

                DeviceConsoleStyle.SetRect(
                    (RectTransform)button.transform,
                    new Vector2(0.0f, 1.0f),
                    new Vector2(0.0f, 1.0f),
                    new Vector2(128.0f + i * 98.0f, y),
                    new Vector2(92.0f, 30.0f));

                button.onClick.AddListener(() =>
                {
                    onSelected(captured);
                    Refresh();
                });

                buttons.Add(button);
                texts.Add(buttonLabel);
            }
        }

        private void HandleStartClicked()
        {
            Printer printer = device != null ? device.ActivePrinter : null;
            ObjectToPrintMetaSO meta = CurrentMeta;

            if (printer == null || meta == null)
            {
                return;
            }

            StartRequested?.Invoke(
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed);
        }

        private void RefreshRows()
        {
            if (rows.Count == 0)
            {
                return;
            }

            int active = CurrentMeta != null ? selectedIndex : -1;

            for (int i = 0; i < rows.Count; i++)
            {
                ObjectRow row = rows[i];
                bool isActive = row.Index == active;

                row.Background.color = isActive
                    ? DeviceConsoleStyle.RowActive
                    : DeviceConsoleStyle.RowIdle;

                row.SelectionBar.gameObject.SetActive(isActive);
                row.NameText.fontStyle = isActive
                    ? FontStyles.Bold
                    : FontStyles.Normal;
            }
        }

        private ObjectToPrintRequirements GetRequirements()
        {
            ObjectToPrintMetaSO meta = CurrentMeta;
            return meta != null ? meta.Requirements : null;
        }

        private void LogValidationIfChanged(PrintJobValidationResult result)
        {
            System.Text.StringBuilder signature = new System.Text.StringBuilder();

            for (int i = 0; i < result.Errors.Count; i++)
            {
                signature.Append("E:").Append(result.Errors[i]).Append('|');
            }

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                signature.Append("W:").Append(result.Warnings[i]).Append('|');
            }

            string current = signature.ToString();

            if (current == lastValidationSignature)
            {
                return;
            }

            lastValidationSignature = current;

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                return;
            }

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("[PrintJobValidator]");

            for (int i = 0; i < result.Errors.Count; i++)
            {
                report.Append("- BLOCK: ").AppendLine(result.Errors[i]);
            }

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                report.Append("- WARN: ").AppendLine(result.Warnings[i]);
            }

            if (result.Errors.Count > 0)
            {
                Debug.LogError(report.ToString());
            }
            else
            {
                Debug.LogWarning(report.ToString());
            }
        }

        private void ApplyProfileDefaults(ObjectToPrintPrintProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            ObjectToPrintRequirements requirements = GetRequirements();

            infill = requirements != null
                ? requirements.DefaultInfill
                : InfillPattern.Grid;
            supports = profile.SupportsRequired
                ? SupportsMode.Auto
                : SupportsMode.None;
            layerHeight = LayerHeightPreset.Normal;
            speed = PrintSpeedPreset.Normal;
        }

        private static void RefreshPresetStyles(
            List<Button> buttons,
            List<TextMeshProUGUI> labels,
            int selectedIndex)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                bool selected = i == selectedIndex;

                if (labels[i] != null)
                {
                    labels[i].fontStyle = selected
                        ? FontStyles.Bold
                        : FontStyles.Normal;
                }

                if (buttons[i].targetGraphic is Image image)
                {
                    image.color = selected
                        ? DeviceConsoleStyle.BoxWhite
                        : new Color(1.0f, 1.0f, 1.0f, 0.45f);
                }
            }
        }

        private void BuildValidationViewport(RectTransform details)
        {
            RectTransform scrollRectTransform =
                DeviceConsoleStyle.CreateRect("validation_scroll", details);

            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(0.0f, 48.0f);
            scrollRectTransform.offsetMax = new Vector2(0.0f, -212.0f);

            Image background =
                scrollRectTransform.gameObject.AddComponent<Image>();

            background.color = new Color(0.0f, 0.0f, 0.0f, 0.06f);
            background.raycastTarget = false;

            if (boxSprite != null)
            {
                background.sprite = boxSprite;
                background.type = Image.Type.Sliced;
            }

            RectTransform rule =
                DeviceConsoleStyle.CreateRect("validation_rule", details);

            rule.anchorMin = new Vector2(0.0f, 1.0f);
            rule.anchorMax = new Vector2(1.0f, 1.0f);
            rule.pivot = new Vector2(0.5f, 1.0f);
            rule.anchoredPosition = new Vector2(0.0f, -210.0f);
            rule.sizeDelta = new Vector2(0.0f, 2.0f);

            Image ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = new Color(0.0f, 0.0f, 0.0f, 0.35f);
            ruleImage.raycastTarget = false;

            validationScroll =
                scrollRectTransform.gameObject.AddComponent<ScrollRect>();
            validationScroll.horizontal = false;
            validationScroll.vertical = true;
            validationScroll.movementType = ScrollRect.MovementType.Clamped;
            validationScroll.scrollSensitivity = 30.0f;

            RectTransform viewport =
                DeviceConsoleStyle.CreateRect("viewport", scrollRectTransform);

            DeviceConsoleStyle.Stretch(viewport);
            viewport.offsetMin = new Vector2(6.0f, 4.0f);
            viewport.offsetMax = new Vector2(-6.0f, -4.0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            viewportHit.raycastTarget = true;

            validationContent =
                DeviceConsoleStyle.CreateRect("content", viewport);

            validationContent.anchorMin = new Vector2(0.0f, 1.0f);
            validationContent.anchorMax = new Vector2(1.0f, 1.0f);
            validationContent.pivot = new Vector2(0.5f, 1.0f);
            validationContent.anchoredPosition = Vector2.zero;
            validationContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                validationContent.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 2.0f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                validationContent.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            validationScroll.viewport = viewport;
            validationScroll.content = validationContent;
        }

        private void RefreshValidationUi(
            PrintJobValidationResult result,
            Printer printer)
        {
            if (validationContent == null)
            {
                return;
            }

            System.Text.StringBuilder signature = new System.Text.StringBuilder();

            if (printer != null && printer.HasPendingJob)
            {
                signature.Append("PENDING");
            }

            for (int i = 0; i < result.Errors.Count; i++)
            {
                signature.Append("E:").Append(result.Errors[i]).Append('|');
            }

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                signature.Append("W:").Append(result.Warnings[i]).Append('|');
            }

            string current = signature.ToString();

            if (current == lastValidationUiSignature)
            {
                return;
            }

            lastValidationUiSignature = current;

            for (int i = validationContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = validationContent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }

            if (printer != null && printer.HasPendingJob)
            {
                CreateValidationRow(
                    "Job queued. Printing starts in M3.",
                    DeviceConsoleStyle.TextMuted);
            }
            else if (result.Errors.Count > 0)
            {
                for (int i = 0; i < result.Errors.Count; i++)
                {
                    CreateValidationRow(
                        $"- {result.Errors[i]}",
                        DeviceConsoleStyle.Text);
                }

                for (int i = 0; i < result.Warnings.Count; i++)
                {
                    CreateValidationRow(
                        $"- {result.Warnings[i]}",
                        DeviceConsoleStyle.TextMuted);
                }
            }
            else if (result.Warnings.Count > 0)
            {
                for (int i = 0; i < result.Warnings.Count; i++)
                {
                    CreateValidationRow(
                        $"- {result.Warnings[i]}",
                        DeviceConsoleStyle.TextMuted);
                }
            }
            else
            {
                CreateValidationRow(
                    "Ready to print.",
                    DeviceConsoleStyle.TextMuted);
            }

            if (validationScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                validationScroll.verticalNormalizedPosition = 1.0f;
            }
        }

        private void CreateValidationRow(string text, Color color)
        {
            TextMeshProUGUI row = DeviceConsoleStyle.CreateText(
                "validation_row",
                validationContent,
                text,
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.TopLeft,
                color,
                font);

            row.textWrappingMode = TextWrappingModes.Normal;
            row.overflowMode = TextOverflowModes.Overflow;

            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1.0f;
        }

        private static string FormatDuration(float seconds)
        {
            if (seconds <= 0.0f)
            {
                return "--";
            }

            int totalMinutes = Mathf.RoundToInt(seconds / 60.0f);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours > 0)
            {
                return $"{hours} h {minutes} min";
            }

            return $"{Mathf.Max(1, minutes)} min";
        }
    }
}

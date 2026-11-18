using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// One console per device, living on a world-space canvas parented to the
    /// device's screen anchor. The console is always visible on the display,
    /// even while the player is elsewhere; only the focused device enables
    /// its raycaster. While a device is bound to, its header and footer are
    /// hidden; the bind prompt is the screen-space HUD in the presenter.
    /// </summary>
    public class DeviceConsoleView : MonoBehaviour
    {
        private sealed class PrinterRow
        {
            public Printer Printer;
            public Image Background;
            public Image SelectionBar;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI StatusText;
        }

        private readonly List<PrinterRow> rows = new List<PrinterRow>();
        private bool rowsBuilt;

        private Device device;
        private DeviceScreenAnchor anchor;
        private TMP_FontAsset font;
        private Sprite boxSprite;
        private ObjectToPrintMetasListSO catalog;
        private float screenOffset;

        private Canvas canvas;
        private GraphicRaycaster raycaster;
        private RectTransform canvasTransform;

        private Vector3 lastAnchorPosition;
        private Quaternion lastAnchorRotation;
        private Vector3 lastAnchorScale;
        private DeviceScreenMetrics metrics;
        private bool hasMetrics;

        private GameObject maskRoot;
        private RectTransform maskRect;
        private Image maskImage;
        private Transform uiParent;

        private GameObject panelRoot;
        private GameObject consoleRoot;
        private GameObject screenOffRoot;

        private Sprite generatedMaskSprite;
        private Texture2D generatedMaskTexture;
        private float generatedMaskRadius;

        private bool lastScreenVisible = true;
        private RectTransform printerListContent;

        private TextMeshProUGUI deviceNameText;
        private TextMeshProUGUI deviceSubtitleText;
        private TextMeshProUGUI activeNameText;
        private TextMeshProUGUI activeStateText;
        private TextMeshProUGUI emptyListText;

        private Button connectButton;
        private Button disconnectButton;
        private Button powerButton;
        private Button quitButton;

        private TextMeshProUGUI connectLabel;
        private TextMeshProUGUI disconnectLabel;
        private TextMeshProUGUI powerLabel;
        private TextMeshProUGUI quitLabel;

        private DeviceJobPanel jobPanel;

        private RectTransform headerRect;
        private RectTransform footerRect;

        private bool bindLayout;

        public Device Device => device;

        public event Action ConnectRequested;
        public event Action DisconnectRequested;
        public event Action QuitRequested;
        public event Action<
            Printer,
            ObjectToPrintMetaSO,
            InfillPattern,
            SupportsMode,
            LayerHeightPreset,
            PrintSpeedPreset> StartPrintRequested;

        public static DeviceConsoleView Create(
            Device target,
            TMP_FontAsset fontAsset,
            Sprite sprite,
            float screenOffsetMeters,
            ObjectToPrintMetasListSO objectCatalog)
        {
            if (target == null)
            {
                return null;
            }

            DeviceScreenAnchor screenAnchor = DeviceScreenFraming.ResolveAnchor(target);

            if (screenAnchor == null)
            {
                Debug.LogWarning(
                    $"[DeviceConsoleView] {target.DisplayName} has no screen anchor.",
                    target);
                return null;
            }

            GameObject go = new GameObject(
                "device_console_canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

            go.transform.SetParent(screenAnchor.transform, false);

            DeviceConsoleView view = go.AddComponent<DeviceConsoleView>();
            view.Initialize(
                target,
                screenAnchor,
                fontAsset,
                sprite,
                screenOffsetMeters,
                objectCatalog);

            return view;
        }

        private void Initialize(
            Device target,
            DeviceScreenAnchor screenAnchor,
            TMP_FontAsset fontAsset,
            Sprite sprite,
            float screenOffsetMeters,
            ObjectToPrintMetasListSO objectCatalog)
        {
            device = target;
            anchor = screenAnchor;
            font = fontAsset;
            boxSprite = sprite;
            catalog = objectCatalog;
            screenOffset = Mathf.Max(0.0001f, screenOffsetMeters);

            canvasTransform = (RectTransform)transform;
            canvas = GetComponent<Canvas>();
            raycaster = GetComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            raycaster.enabled = false;

            canvasTransform.anchorMin = new Vector2(0.5f, 0.5f);
            canvasTransform.anchorMax = new Vector2(0.5f, 0.5f);
            canvasTransform.pivot = new Vector2(0.5f, 0.5f);
            canvasTransform.anchoredPosition = Vector2.zero;

            BuildUi();

            device.BindingsChanged += HandleDeviceChanged;
            device.ActivePrinterChanged += HandleDeviceChanged;
            device.ScreenStateChanged += HandleDeviceChanged;

            UpdateTransform(true);
            Refresh();
        }

        public void SetInteractive(bool interactive, Camera eventCamera)
        {
            if (raycaster != null)
            {
                raycaster.enabled = interactive;
            }

            if (canvas != null)
            {
                canvas.worldCamera = interactive ? eventCamera : null;
            }
        }

        public void SetBindLayout(bool active)
        {
            bindLayout = active;
            Refresh();
        }

        public void Refresh()
        {
            if (device == null)
            {
                return;
            }

            UpdateTransform();
            ApplyMask();

            // The physical renderers are disabled while a ghost replaces the
            // item, so the console hides with the display instead of floating
            // over the ghost.
            bool screenVisible = IsScreenVisible();
            lastScreenVisible = screenVisible;

            if (maskRoot != null)
            {
                maskRoot.SetActive(screenVisible);
            }

            if (!screenVisible)
            {
                return;
            }

            bool isOn = device.IsDisplayOn;

            if (panelRoot != null)
            {
                panelRoot.SetActive(isOn);
            }

            if (screenOffRoot != null)
            {
                screenOffRoot.SetActive(!isOn);
            }

            if (consoleRoot != null)
            {
                consoleRoot.SetActive(isOn);
            }

            if (headerRect != null)
            {
                headerRect.gameObject.SetActive(!bindLayout);
            }

            if (footerRect != null)
            {
                footerRect.gameObject.SetActive(!bindLayout);
            }

            if (!isOn)
            {
                return;
            }

            RefreshConsole();
        }

        private void RefreshConsole()
        {
            deviceNameText.text = device.DisplayName;
            deviceSubtitleText.text = GetSubtitle(device.BoundPrinters.Count);

            Printer active = device.ActivePrinter;

            if (active == null)
            {
                activeNameText.text = "No printer selected";
                activeStateText.text = "Waiting for a printer";
                activeStateText.color = DeviceConsoleStyle.TextMuted;
            }
            else
            {
                activeNameText.text = active.DisplayName;
                activeStateText.text = GetStateLabel(active.OperationalState);
                activeStateText.color = DeviceConsoleStyle.Text;
            }

            SetButtonEnabled(connectButton, connectLabel, true);
            SetButtonEnabled(disconnectButton, disconnectLabel, active != null);
            SetButtonEnabled(powerButton, powerLabel, true);
            SetButtonEnabled(quitButton, quitLabel, true);

            if (jobPanel != null)
            {
                jobPanel.SetDevice(device);
                jobPanel.Refresh();
            }

            RefreshRows();
        }

        private void OnDestroy()
        {
            if (device != null)
            {
                device.BindingsChanged -= HandleDeviceChanged;
                device.ActivePrinterChanged -= HandleDeviceChanged;
                device.ScreenStateChanged -= HandleDeviceChanged;
            }

            DestroyGeneratedMask();
        }

        private void LateUpdate()
        {
            if (anchor == null)
            {
                return;
            }

            bool screenVisible = IsScreenVisible();

            if (screenVisible != lastScreenVisible)
            {
                lastScreenVisible = screenVisible;
                Refresh();
            }

            Transform parent = anchor.transform;

            if (parent.position != lastAnchorPosition ||
                parent.rotation != lastAnchorRotation ||
                parent.lossyScale != lastAnchorScale)
            {
                UpdateTransform(true);
            }
        }

        private void HandleDeviceChanged(Device changedDevice)
        {
            Refresh();
        }

        private void UpdateTransform(bool remeasure = false)
        {
            if (device == null || canvasTransform == null)
            {
                return;
            }

            if (remeasure || !hasMetrics)
            {
                if (!DeviceScreenFraming.TryGet(device, out DeviceScreenMetrics measured))
                {
                    return;
                }

                metrics = measured;
                hasMetrics = true;
            }

            float aspect = Mathf.Max(0.2f, metrics.Aspect);
            float logicalHeight = DeviceConsoleStyle.ReferenceWidth / aspect;

            canvasTransform.sizeDelta = new Vector2(
                DeviceConsoleStyle.ReferenceWidth,
                logicalHeight);

            float worldScale =
                Mathf.Max(0.0001f, metrics.Width) /
                DeviceConsoleStyle.ReferenceWidth;

            Transform parent = canvasTransform.parent;
            Vector3 parentScale = parent != null
                ? parent.lossyScale
                : Vector3.one;

            canvasTransform.localScale = new Vector3(
                worldScale / SafeScale(parentScale.x),
                worldScale / SafeScale(parentScale.y),
                worldScale / SafeScale(parentScale.z));

            Vector3 normal = metrics.Normal.normalized;
            Vector3 up = metrics.Up.normalized;

            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.up;
            }

            if (remeasure && parent != null)
            {
                lastAnchorPosition = parent.position;
                lastAnchorRotation = parent.rotation;
                lastAnchorScale = parent.lossyScale;
            }

            // UGUI is read correctly when the viewer looks along the canvas's
            // local +Z. The player looks from +normal towards the screen, so
            // the canvas forward must point into the screen.
            Quaternion rotation = Quaternion.LookRotation(-normal, up);

            canvasTransform.SetPositionAndRotation(
                metrics.Center +
                normal * (metrics.FrontOffset + screenOffset),
                rotation);
        }

        private static float SafeScale(float value)
        {
            return Mathf.Abs(value) > 0.00001f ? value : 1.0f;
        }

        // === Build ===========================================================

        private void BuildUi()
        {
            BuildMask();

            panelRoot = new GameObject("panel", typeof(RectTransform));
            panelRoot.transform.SetParent(uiParent, false);

            RectTransform panelRect = (RectTransform)panelRoot.transform;
            DeviceConsoleStyle.Stretch(panelRect);

            Image panel = panelRoot.AddComponent<Image>();
            panel.color = DeviceConsoleStyle.Panel;
            panel.raycastTarget = true;

            if (boxSprite != null)
            {
                panel.sprite = boxSprite;
                panel.type = Image.Type.Sliced;
            }

            BuildConsole();
            BuildScreenOffOverlay();
        }

        private void BuildConsole()
        {
            consoleRoot = new GameObject("console_root", typeof(RectTransform));
            consoleRoot.transform.SetParent(uiParent, false);

            RectTransform root = (RectTransform)consoleRoot.transform;
            DeviceConsoleStyle.Stretch(root);

            BuildHeader(root);

            RectTransform body = DeviceConsoleStyle.CreateRect("body", root);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(
                DeviceConsoleStyle.Margin,
                DeviceConsoleStyle.FooterHeight + 14.0f);
            body.offsetMax = new Vector2(
                -DeviceConsoleStyle.Margin,
                -(DeviceConsoleStyle.HeaderHeight + 12.0f));

            BuildPrinterList(body);
            BuildActiveArea(body);
            BuildFooter(root);
        }

        private void BuildHeader(RectTransform root)
        {
            RectTransform header = DeviceConsoleStyle.CreateRect("header", root);
            headerRect = header;
            header.anchorMin = new Vector2(0.0f, 1.0f);
            header.anchorMax = new Vector2(1.0f, 1.0f);
            header.pivot = new Vector2(0.5f, 1.0f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(
                0.0f,
                DeviceConsoleStyle.HeaderHeight);

            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = DeviceConsoleStyle.BoxGray;
            headerImage.raycastTarget = false;

            deviceNameText = DeviceConsoleStyle.CreateText(
                "device_name",
                header,
                "Device",
                DeviceConsoleStyle.FontTitle,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)deviceNameText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -10.0f),
                new Vector2(640.0f, 34.0f));

            deviceSubtitleText = DeviceConsoleStyle.CreateText(
                "device_subtitle",
                header,
                "No printers connected",
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.TextMuted,
                font);

            DeviceConsoleStyle.SetRect(
                (RectTransform)deviceSubtitleText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -44.0f),
                new Vector2(640.0f, 24.0f));

            powerButton = DeviceConsoleStyle.CreateButton(
                "power_button",
                header,
                "Screen off",
                font,
                boxSprite,
                out powerLabel);

            DeviceConsoleStyle.SetRect(
                (RectTransform)powerButton.transform,
                new Vector2(1.0f, 0.5f),
                new Vector2(1.0f, 0.5f),
                new Vector2(-244.0f, 0.0f),
                new Vector2(200.0f, 44.0f));

            powerButton.onClick.AddListener(OnPowerClicked);

            quitButton = DeviceConsoleStyle.CreateButton(
                "quit_button",
                header,
                "Quit",
                font,
                boxSprite,
                out quitLabel);

            DeviceConsoleStyle.SetRect(
                (RectTransform)quitButton.transform,
                new Vector2(1.0f, 0.5f),
                new Vector2(1.0f, 0.5f),
                new Vector2(-24.0f, 0.0f),
                new Vector2(200.0f, 44.0f));

            quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void BuildPrinterList(RectTransform body)
        {
            RectTransform card = DeviceConsoleStyle.CreateRect(
                "printer_list_card",
                body);

            card.anchorMin = new Vector2(0.0f, 0.0f);
            card.anchorMax = new Vector2(0.0f, 1.0f);
            card.pivot = new Vector2(0.0f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(
                DeviceConsoleStyle.ListWidth,
                0.0f);

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = DeviceConsoleStyle.BoxGray;
            cardImage.raycastTarget = false;

            TextMeshProUGUI title = DeviceConsoleStyle.CreateText(
                "list_title",
                card,
                "PRINTERS",
                DeviceConsoleStyle.FontHeading,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)title.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(16.0f, -12.0f),
                new Vector2(220.0f, 24.0f));

            RectTransform scroll = DeviceConsoleStyle.CreateRect(
                "printer_scroll",
                card);

            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.offsetMin = new Vector2(12.0f, 12.0f);
            scroll.offsetMax = new Vector2(-12.0f, -44.0f);

            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40.0f;

            RectTransform viewport = DeviceConsoleStyle.CreateRect(
                "viewport",
                scroll);

            DeviceConsoleStyle.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            // A transparent target lets the wheel/drag work over empty space,
            // not only over the row buttons.
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            viewportHit.raycastTarget = true;

            printerListContent = DeviceConsoleStyle.CreateRect(
                "content",
                viewport);

            printerListContent.anchorMin = new Vector2(0.0f, 1.0f);
            printerListContent.anchorMax = new Vector2(1.0f, 1.0f);
            printerListContent.pivot = new Vector2(0.5f, 1.0f);
            printerListContent.anchoredPosition = Vector2.zero;
            printerListContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                printerListContent.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = DeviceConsoleStyle.RowSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                printerListContent.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = printerListContent;
        }

        private void BuildActiveArea(RectTransform body)
        {
            RectTransform right = DeviceConsoleStyle.CreateRect(
                "active_area",
                body);

            right.anchorMin = Vector2.zero;
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(
                DeviceConsoleStyle.ListWidth + 20.0f,
                0.0f);
            right.offsetMax = Vector2.zero;

            RectTransform card = DeviceConsoleStyle.CreateRect(
                "active_printer_card",
                right);

            card.anchorMin = new Vector2(0.0f, 1.0f);
            card.anchorMax = new Vector2(1.0f, 1.0f);
            card.pivot = new Vector2(0.5f, 1.0f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(0.0f, 96.0f);

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = DeviceConsoleStyle.BoxGray;
            cardImage.raycastTarget = false;

            TextMeshProUGUI label = DeviceConsoleStyle.CreateText(
                "active_label",
                card,
                "ACTIVE PRINTER",
                DeviceConsoleStyle.FontHeading,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)label.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -6.0f),
                new Vector2(320.0f, 18.0f));

            activeNameText = DeviceConsoleStyle.CreateText(
                "active_name",
                card,
                "No printer selected",
                DeviceConsoleStyle.FontTitle,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.Text,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)activeNameText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -24.0f),
                new Vector2(760.0f, 24.0f));

            activeStateText = DeviceConsoleStyle.CreateText(
                "active_state",
                card,
                "Waiting for a printer",
                DeviceConsoleStyle.FontHeading,
                TextAlignmentOptions.TopLeft,
                DeviceConsoleStyle.TextMuted,
                font);

            DeviceConsoleStyle.SetRect(
                (RectTransform)activeStateText.transform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(DeviceConsoleStyle.Margin, -50.0f),
                new Vector2(760.0f, 20.0f));

            RectTransform info = DeviceConsoleStyle.CreateRect(
                "info_card",
                right);

            info.anchorMin = Vector2.zero;
            info.anchorMax = Vector2.one;
            info.offsetMin = Vector2.zero;
            info.offsetMax = new Vector2(0.0f, -112.0f);

            Image infoImage = info.gameObject.AddComponent<Image>();
            infoImage.color = DeviceConsoleStyle.BoxWhite;
            infoImage.raycastTarget = false;

            jobPanel = new DeviceJobPanel(font, boxSprite, catalog);
            jobPanel.StartRequested += HandleJobStartRequested;
            jobPanel.Build(info);
        }

        private void BuildFooter(RectTransform root)
        {
            RectTransform footer = DeviceConsoleStyle.CreateRect(
                "footer",
                root);
            footerRect = footer;

            footer.anchorMin = new Vector2(0.0f, 0.0f);
            footer.anchorMax = new Vector2(1.0f, 0.0f);
            footer.pivot = new Vector2(0.5f, 0.0f);
            footer.anchoredPosition = Vector2.zero;
            footer.sizeDelta = new Vector2(
                0.0f,
                DeviceConsoleStyle.FooterHeight);

            Image footerImage = footer.gameObject.AddComponent<Image>();
            footerImage.color = DeviceConsoleStyle.BoxGray;
            footerImage.raycastTarget = false;

            DeviceConsoleChip footerChip = DeviceConsoleChip.Create(
                "quit_hint",
                footer,
                font,
                boxSprite,
                DeviceConsoleStyle.Text);

            footerChip.Set("Q", "Quit");

            // Same anchor as the screen-off hint so the badge does not jump
            // when the display is powered off and back on.
            DeviceConsoleStyle.SetRect(
                footerChip.Root,
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(DeviceConsoleStyle.Margin, DeviceConsoleStyle.Margin),
                new Vector2(300.0f, 30.0f));

            connectButton = DeviceConsoleStyle.CreateButton(
                "connect_button",
                footer,
                "Connect printer",
                font,
                boxSprite,
                out connectLabel);

            DeviceConsoleStyle.SetRect(
                (RectTransform)connectButton.transform,
                new Vector2(1.0f, 0.5f),
                new Vector2(1.0f, 0.5f),
                new Vector2(-DeviceConsoleStyle.Margin, 0.0f),
                new Vector2(224.0f, 52.0f));

            connectButton.onClick.AddListener(OnConnectClicked);

            disconnectButton = DeviceConsoleStyle.CreateButton(
                "disconnect_button",
                footer,
                "Disconnect",
                font,
                boxSprite,
                out disconnectLabel);

            DeviceConsoleStyle.SetRect(
                (RectTransform)disconnectButton.transform,
                new Vector2(1.0f, 0.5f),
                new Vector2(1.0f, 0.5f),
                new Vector2(
                    -(DeviceConsoleStyle.Margin + 236.0f),
                    0.0f),
                new Vector2(224.0f, 52.0f));

            disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }

        private void BuildScreenOffOverlay()
        {
            screenOffRoot = new GameObject("screen_off_root", typeof(RectTransform));
            screenOffRoot.transform.SetParent(uiParent, false);

            RectTransform root = (RectTransform)screenOffRoot.transform;
            DeviceConsoleStyle.Stretch(root);

            Image black = screenOffRoot.AddComponent<Image>();
            black.color = Color.black;
            black.raycastTarget = true;

            TextMeshProUGUI title = DeviceConsoleStyle.CreateText(
                "screen_off_title",
                screenOffRoot.transform,
                "Display off",
                DeviceConsoleStyle.FontTitle,
                TextAlignmentOptions.Center,
                DeviceConsoleStyle.TextOnDark,
                font,
                true);

            DeviceConsoleStyle.SetRect(
                (RectTransform)title.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 86.0f),
                new Vector2(600.0f, 40.0f));

            TextMeshProUGUI body = DeviceConsoleStyle.CreateText(
                "screen_off_body",
                screenOffRoot.transform,
                "The console is still running.\n" +
                "Connected printers keep printing while the screen is off.",
                DeviceConsoleStyle.FontBody,
                TextAlignmentOptions.Center,
                DeviceConsoleStyle.TextOnDarkMuted,
                font);

            DeviceConsoleStyle.SetRect(
                (RectTransform)body.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 16.0f),
                new Vector2(760.0f, 70.0f));

            Button powerOnButton = DeviceConsoleStyle.CreateButton(
                "power_on_button",
                screenOffRoot.transform,
                "Power on",
                font,
                boxSprite,
                out _);

            DeviceConsoleStyle.SetRect(
                (RectTransform)powerOnButton.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, -78.0f),
                new Vector2(220.0f, 56.0f));

            powerOnButton.onClick.AddListener(OnPowerOnClicked);

            DeviceConsoleChip screenOffChip = DeviceConsoleChip.Create(
                "quit_hint",
                screenOffRoot.transform,
                font,
                boxSprite,
                DeviceConsoleStyle.TextOnDark);

            screenOffChip.Set("Q", "Quit");
            DeviceConsoleStyle.SetRect(
                screenOffChip.Root,
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(DeviceConsoleStyle.Margin, DeviceConsoleStyle.Margin),
                new Vector2(300.0f, 30.0f));

            screenOffRoot.SetActive(false);
        }

        // === Screen mask =====================================================

        private void BuildMask()
        {
            maskRoot = new GameObject("screen_mask", typeof(RectTransform));
            maskRoot.transform.SetParent(transform, false);

            maskRect = (RectTransform)maskRoot.transform;
            DeviceConsoleStyle.Stretch(maskRect);

            maskImage = maskRoot.AddComponent<Image>();
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;

            Mask mask = maskRoot.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            uiParent = maskRect;
        }

        private void ApplyMask()
        {
            if (maskImage == null)
            {
                return;
            }

            Sprite sprite = null;

            if (anchor != null)
            {
                sprite = anchor.ScreenMaskSprite;

                if (sprite == null && anchor.ScreenCornerRadius > 0.0001f)
                {
                    sprite = GetGeneratedMaskSprite(anchor.ScreenCornerRadius);
                }
            }

            if (maskImage.sprite == sprite)
            {
                return;
            }

            maskImage.sprite = sprite;
            maskImage.type = sprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
        }

        private Sprite GetGeneratedMaskSprite(float radiusFraction)
        {
            int radius = Mathf.Clamp(
                Mathf.RoundToInt(
                    radiusFraction * DeviceConsoleStyle.ReferenceWidth),
                1,
                400);

            if (generatedMaskSprite != null && generatedMaskRadius == radius)
            {
                return generatedMaskSprite;
            }

            DestroyGeneratedMask();

            int size = radius * 2 + 4;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false)
            {
                name = "device_console_mask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float half = size * 0.5f;
            float inner = half - radius;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float py = y + 0.5f - half;
                    float qx = Mathf.Abs(px) - inner;
                    float qy = Mathf.Abs(py) - inner;
                    float outside = new Vector2(
                        Mathf.Max(qx, 0.0f),
                        Mathf.Max(qy, 0.0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0.0f);
                    float distance = outside + inside - radius;
                    byte alpha = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(0.5f - distance) * 255.0f);

                    pixels[y * size + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            generatedMaskSprite = Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, size, size),
                new Vector2(0.5f, 0.5f),
                100.0f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));

            generatedMaskSprite.name = "device_console_mask";
            generatedMaskTexture = texture;
            generatedMaskRadius = radius;

            return generatedMaskSprite;
        }

        private void DestroyGeneratedMask()
        {
            if (generatedMaskSprite != null)
            {
                Destroy(generatedMaskSprite);
                generatedMaskSprite = null;
            }

            if (generatedMaskTexture != null)
            {
                Destroy(generatedMaskTexture);
                generatedMaskTexture = null;
            }

            generatedMaskRadius = 0.0f;
        }

        private bool IsScreenVisible()
        {
            if (device == null)
            {
                return true;
            }

            return device.Screen == null || device.Screen.IsVisible;
        }

        // === Handlers ========================================================

        private void OnConnectClicked()
        {
            ConnectRequested?.Invoke();
        }

        private void OnDisconnectClicked()
        {
            DisconnectRequested?.Invoke();
        }

        private void HandleJobStartRequested(
            Printer printer,
            ObjectToPrintMetaSO meta,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed)
        {
            StartPrintRequested?.Invoke(
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed);
        }

        private void OnQuitClicked()
        {
            QuitRequested?.Invoke();
        }

        private void OnPowerClicked()
        {
            if (device == null || !device.IsDisplayOn)
            {
                return;
            }

            device.SetScreenOn(false);
        }

        private void OnPowerOnClicked()
        {
            if (device == null)
            {
                return;
            }

            device.SetScreenOn(true);
        }

        private void OnPrinterRowClicked(Printer printer)
        {
            if (device == null || printer == null)
            {
                return;
            }

            device.SetActivePrinter(printer);
        }

        // === Printer list ====================================================

        private void RefreshRows()
        {
            if (device == null || printerListContent == null)
            {
                return;
            }

            IReadOnlyList<Printer> boundPrinters = device.BoundPrinters;
            Printer active = device.ActivePrinter;

            bool rebuild = !rowsBuilt ||
                rows.Count != boundPrinters.Count;

            if (!rebuild)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Printer != boundPrinters[i])
                    {
                        rebuild = true;
                        break;
                    }
                }
            }

            if (rebuild)
            {
                RebuildRows(boundPrinters);
                rowsBuilt = true;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                PrinterRow row = rows[i];

                if (row.Printer == null)
                {
                    continue;
                }

                bool isActive = row.Printer == active;
                row.Background.color = isActive
                    ? DeviceConsoleStyle.RowActive
                    : DeviceConsoleStyle.RowIdle;

                row.SelectionBar.gameObject.SetActive(isActive);
                row.NameText.fontStyle = isActive
                    ? FontStyles.Bold
                    : FontStyles.Normal;

                row.StatusText.text = row.Printer.IsBound
                    ? GetStateLabel(row.Printer.OperationalState)
                    : "Not connected";

                row.StatusText.color = isActive
                    ? DeviceConsoleStyle.Text
                    : DeviceConsoleStyle.TextMuted;
            }

            if (emptyListText != null)
            {
                emptyListText.gameObject.SetActive(rows.Count == 0);
            }
        }

        private void RebuildRows(IReadOnlyList<Printer> boundPrinters)
        {
            for (int i = printerListContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = printerListContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            rows.Clear();
            emptyListText = null;

            if (boundPrinters.Count == 0)
            {
                emptyListText = DeviceConsoleStyle.CreateText(
                    "empty_list",
                    printerListContent,
                    "No printers connected.\nUse Connect printer.",
                    DeviceConsoleStyle.FontSmall,
                    TextAlignmentOptions.TopLeft,
                    DeviceConsoleStyle.TextMuted,
                    font);

                RectTransform emptyRect =
                    (RectTransform)emptyListText.transform;

                emptyRect.sizeDelta = new Vector2(0.0f, 76.0f);

                LayoutElement emptyLayout =
                    emptyListText.gameObject.AddComponent<LayoutElement>();

                emptyLayout.preferredHeight = 76.0f;
                emptyLayout.flexibleWidth = 1.0f;
                return;
            }

            for (int i = 0; i < boundPrinters.Count; i++)
            {
                Printer printer = boundPrinters[i];

                if (printer != null)
                {
                    CreatePrinterRow(printer);
                }
            }
        }

        private void CreatePrinterRow(Printer printer)
        {
            RectTransform rowRect = DeviceConsoleStyle.CreateRect(
                "printer_row",
                printerListContent);

            rowRect.sizeDelta = new Vector2(
                0.0f,
                DeviceConsoleStyle.RowHeight);

            LayoutElement layout =
                rowRect.gameObject.AddComponent<LayoutElement>();

            layout.preferredHeight = DeviceConsoleStyle.RowHeight;
            layout.flexibleWidth = 1.0f;

            Image background = rowRect.gameObject.AddComponent<Image>();
            background.color = DeviceConsoleStyle.Panel;
            background.raycastTarget = true;

            if (boxSprite != null)
            {
                background.sprite = boxSprite;
                background.type = Image.Type.Sliced;
            }

            Button button = rowRect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
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

            Image selectionBar = DeviceConsoleStyle.CreateImage(
                "selection_bar",
                rowRect,
                DeviceConsoleStyle.Text);

            RectTransform barRect = (RectTransform)selectionBar.transform;
            barRect.anchorMin = new Vector2(0.0f, 0.0f);
            barRect.anchorMax = new Vector2(0.0f, 1.0f);
            barRect.pivot = new Vector2(0.0f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(4.0f, 0.0f);

            TextMeshProUGUI nameText = DeviceConsoleStyle.CreateText(
                "name",
                rowRect,
                printer.DisplayName,
                DeviceConsoleStyle.FontBody,
                TextAlignmentOptions.MidlineLeft,
                DeviceConsoleStyle.Text,
                font);

            RectTransform nameRect = (RectTransform)nameText.transform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(16.0f, 10.0f);
            nameRect.offsetMax = new Vector2(-106.0f, -10.0f);
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI statusText = DeviceConsoleStyle.CreateText(
                "status",
                rowRect,
                GetStateLabel(printer.OperationalState),
                DeviceConsoleStyle.FontSmall,
                TextAlignmentOptions.MidlineRight,
                DeviceConsoleStyle.TextMuted,
                font);

            RectTransform statusRect = (RectTransform)statusText.transform;
            statusRect.anchorMin = new Vector2(1.0f, 0.0f);
            statusRect.anchorMax = new Vector2(1.0f, 1.0f);
            statusRect.pivot = new Vector2(1.0f, 0.5f);
            statusRect.anchoredPosition = new Vector2(-12.0f, 0.0f);
            statusRect.sizeDelta = new Vector2(92.0f, 0.0f);

            Printer capturedPrinter = printer;

            button.onClick.AddListener(
                () => OnPrinterRowClicked(capturedPrinter));

            rows.Add(new PrinterRow
            {
                Printer = printer,
                Background = background,
                SelectionBar = selectionBar,
                NameText = nameText,
                StatusText = statusText
            });
        }

        private void SetButtonEnabled(
            Button button,
            TextMeshProUGUI label,
            bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }

            if (label != null)
            {
                label.color = enabled
                    ? DeviceConsoleStyle.Text
                    : DeviceConsoleStyle.TextDisabled;
            }
        }

        private static string GetSubtitle(int printerCount)
        {
            if (printerCount == 0)
            {
                return "No printers connected";
            }

            return printerCount == 1
                ? "1 printer connected"
                : $"{printerCount} printers connected";
        }

        private static string GetStateLabel(PrinterOperationalState state)
        {
            switch (state)
            {
                case PrinterOperationalState.Preheating:
                    return "Heating";
                case PrinterOperationalState.Printing:
                    return "Printing";
                case PrinterOperationalState.Paused:
                    return "Paused";
                case PrinterOperationalState.Cooling:
                    return "Cooling";
                case PrinterOperationalState.Finished:
                    return "Finished";
                case PrinterOperationalState.Maintenance:
                    return "Maintenance";
                case PrinterOperationalState.Fault:
                    return "Fault";
                default:
                    return "Idle";
            }
        }
    }
}

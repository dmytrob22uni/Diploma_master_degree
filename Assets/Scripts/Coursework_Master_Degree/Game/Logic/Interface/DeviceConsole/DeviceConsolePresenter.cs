using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.Logic.Interface.DeviceConsole
{
    /// <summary>
    /// Owns one always-visible world-space console per device and forwards the
    /// bind layout. The consoles live on the device screens, so they stay
    /// readable while the player walks away; only the focused device accepts
    /// pointer input. The bind prompt itself is the authored
    /// device_bind_canvas, driven by DeviceBindPanel.
    /// </summary>
    public class DeviceConsolePresenter : MonoBehaviour
    {
        [Header("Style")]
        public TMP_FontAsset Font;
        public Sprite BoxSprite;

        [Header("Console")]
        [Min(0.0001f)]
        public float ScreenOffset = 0.0015f;

        [Tooltip("Catalog shown by the console job picker. M2 uses the authored list.")]
        public ObjectToPrintMetasListSO ObjectCatalog;

        [Header("Refresh")]
        [Min(0.05f)]
        public float RefreshInterval = 0.2f;

        public event Action AddPrinterRequested;
        public event Action DisconnectPrinterRequested;
        public event Action QuitRequested;
        public event Action<
            Device,
            Printer,
            ObjectToPrintMetaSO,
            InfillPattern,
            SupportsMode,
            LayerHeightPreset,
            PrintSpeedPreset,
            SpoolSlot,
            PrintPlacement> StartPrintRequested;

        public event Action<
            Device,
            Printer,
            ObjectToPrintMetaSO,
            InfillPattern,
            SupportsMode,
            LayerHeightPreset,
            PrintSpeedPreset,
            PrintPlacement> PlaceOnBedRequested;

        private const float BackgroundRefreshFactor = 5.0f;
        private const float SyncInterval = 0.5f;

        private readonly Dictionary<Device, DeviceConsoleView> views =
            new Dictionary<Device, DeviceConsoleView>();

        private DeviceConsoleView focusedView;

        private float nextRefreshTime;
        private float nextBackgroundRefreshTime;
        private float nextSyncTime;

        private void Start()
        {
            SyncViews();
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<Device, DeviceConsoleView> pair in views)
            {
                if (pair.Value != null)
                {
                    UnsubscribeView(pair.Value);
                    Destroy(pair.Value.gameObject);
                }
            }

            views.Clear();
            focusedView = null;
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            if (now >= nextSyncTime)
            {
                nextSyncTime = now + SyncInterval;
                SyncViews();
            }

            if (now >= nextRefreshTime)
            {
                nextRefreshTime = now + RefreshInterval;

                if (focusedView != null)
                {
                    focusedView.Refresh();
                }
            }

            if (now >= nextBackgroundRefreshTime)
            {
                nextBackgroundRefreshTime =
                    now + RefreshInterval * BackgroundRefreshFactor;

                foreach (KeyValuePair<Device, DeviceConsoleView> pair in views)
                {
                    if (pair.Value != null && pair.Value != focusedView)
                    {
                        pair.Value.Refresh();
                    }
                }
            }
        }

        // === Focus ===========================================================

        public void Focus(Device device, Camera eventCamera)
        {
            if (device == null)
            {
                return;
            }

            if (focusedView != null)
            {
                focusedView.SetInteractive(false, null);
            }

            focusedView = EnsureView(device);

            if (focusedView == null)
            {
                return;
            }

            focusedView.Refresh();
            focusedView.SetInteractive(
                true,
                eventCamera != null ? eventCamera : Camera.main);
        }

        public void Unfocus()
        {
            if (focusedView != null)
            {
                focusedView.SetInteractive(false, null);
            }

            focusedView = null;
        }

        // === Bind layout =====================================================

        public void SetBindLayout(Device device, bool active)
        {
            DeviceConsoleView view = EnsureView(device);

            if (view != null)
            {
                view.SetBindLayout(active);
            }
        }

        public void Refresh(Device device)
        {
            if (device == null)
            {
                return;
            }

            if (views.TryGetValue(device, out DeviceConsoleView view) &&
                view != null)
            {
                view.Refresh();
            }
        }

        // === View management =================================================

        private void SyncViews()
        {
            IReadOnlyList<Device> devices = Device.ActiveDevices;

            for (int i = 0; i < devices.Count; i++)
            {
                Device device = devices[i];

                if (device != null && !views.ContainsKey(device))
                {
                    EnsureView(device);
                }
            }

            List<Device> stale = null;

            foreach (KeyValuePair<Device, DeviceConsoleView> pair in views)
            {
                if (pair.Key == null ||
                    pair.Value == null)
                {
                    if (stale == null)
                    {
                        stale = new List<Device>();
                    }

                    stale.Add(pair.Key);
                }
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                RemoveView(stale[i]);
            }
        }

        private DeviceConsoleView EnsureView(Device device)
        {
            if (device == null)
            {
                return null;
            }

            if (views.TryGetValue(device, out DeviceConsoleView existing))
            {
                if (existing != null)
                {
                    return existing;
                }

                views.Remove(device);
            }

            DeviceConsoleView view = DeviceConsoleView.Create(
                device,
                Font,
                BoxSprite,
                ScreenOffset,
                ObjectCatalog);

            if (view == null)
            {
                return null;
            }

            view.ConnectRequested += HandleViewConnectRequested;
            view.DisconnectRequested += HandleViewDisconnectRequested;
            view.QuitRequested += HandleViewQuitRequested;
            view.StartPrintRequested += HandleViewStartPrintRequested;
            view.PlaceOnBedRequested += HandleViewPlaceOnBedRequested;

            views.Add(device, view);
            return view;
        }

        /// <summary>
        /// Removes and destroys the cached view for a device that will not be
        /// reactivated, e.g. after it is sold.
        /// </summary>
        public void DisposeView(Device device)
        {
            RemoveView(device);
        }

        private void RemoveView(Device device)
        {
            if (device == null)
            {
                return;
            }

            if (!views.TryGetValue(device, out DeviceConsoleView view))
            {
                return;
            }

            views.Remove(device);

            if (view == null)
            {
                return;
            }

            if (view == focusedView)
            {
                focusedView = null;
            }

            UnsubscribeView(view);
            Destroy(view.gameObject);
        }

        private void UnsubscribeView(DeviceConsoleView view)
        {
            view.ConnectRequested -= HandleViewConnectRequested;
            view.DisconnectRequested -= HandleViewDisconnectRequested;
            view.QuitRequested -= HandleViewQuitRequested;
            view.StartPrintRequested -= HandleViewStartPrintRequested;
            view.PlaceOnBedRequested -= HandleViewPlaceOnBedRequested;
        }

        // === View handlers ===================================================

        private void HandleViewConnectRequested()
        {
            AddPrinterRequested?.Invoke();
        }

        private void HandleViewDisconnectRequested()
        {
            DisconnectPrinterRequested?.Invoke();
        }

        private void HandleViewQuitRequested()
        {
            QuitRequested?.Invoke();
        }

        private void HandleViewStartPrintRequested(
            Printer printer,
            ObjectToPrintMetaSO meta,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            SpoolSlot spoolSlot,
            PrintPlacement placement)
        {
            StartPrintRequested?.Invoke(
                focusedView != null ? focusedView.Device : null,
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed,
                spoolSlot,
                placement);
        }

        private void HandleViewPlaceOnBedRequested(
            Printer printer,
            ObjectToPrintMetaSO meta,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            PrintPlacement placement)
        {
            PlaceOnBedRequested?.Invoke(
                focusedView != null ? focusedView.Device : null,
                printer,
                meta,
                infill,
                supports,
                layerHeight,
                speed,
                placement);
        }

        public void SetPlacement(Device device, PrintPlacement placement)
        {
            if (device != null &&
                views.TryGetValue(device, out DeviceConsoleView view) &&
                view != null)
            {
                view.SetPlacement(placement);
            }
        }
    }
}

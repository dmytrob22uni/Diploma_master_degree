using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.Items.FFF.Spool;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Owns the visible object for one printer job. Live printing uses the
    /// spool's reveal shader and backface lighting. A settled pause or a
    /// finalized partial print gets a sliced, capped surface with the spool's
    /// regular final material; resuming a pause restores the printing shader.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrintedObjectPresenter : MonoBehaviour
    {
        [Header("Reveal")]
        [Min(1.0f)]
        [SerializeField] private float RevealUpdatesPerSecond = 15.0f;

        [Header("Supports")]
        [Tooltip("Maximum visual support columns; this is cosmetic, not a slicer.")]
        [Min(0)]
        [SerializeField] private int MaxSupportColumns = 48;

        [Tooltip("XZ clustering size for visual support columns, in metres.")]
        [Min(0.001f)]
        [SerializeField] private float SupportClusterSizeMeters = 0.02f;

        [Tooltip("Downward-normal threshold for a triangle to receive a column.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float SupportOverhangThreshold = 0.35f;

        [Tooltip(
            "Actual support column radius in metres. The column is twice " +
            "this width.")]
        [Min(0.0001f)]
        [SerializeField] private float SupportColumnRadiusMeters = 0.0015f;

        private readonly PrintRevealController reveal =
            new PrintRevealController();
        private readonly PrinterMotionSimulator motion =
            new PrinterMotionSimulator();
        private readonly PrintedSupportPresenter supports =
            new PrintedSupportPresenter();
        private readonly PrintedMeshCapping capping =
            new PrintedMeshCapping();

        private Printer printer;
        private PrinterJob job;
        private GameObject printedObject;
        private PrintedObjectDataHolder printedData;
        private PrintedMeshData meshData;
        private Bounds localBounds;
        private Collider[] originalColliders;

        private Material printingMaterial;
        private Material finalMaterial;
        private Material partialMaterial;
        private Material fallbackMaterial;

        private Mesh slicedMesh;
        private GameObject slicedObject;

        private float localHeight;
        private float uniformScale = 1.0f;
        private float layerHeightMeters = 0.0002f;
        private float lastRevealTime = -1000.0f;
        private int seed;
        private bool isPartial;
        private bool isCompleted;
        private bool isFinalized;
        private bool isPartialSurfaceVisible;

        public bool HasObject => printedObject != null;
        public bool IsFinalized => isFinalized;
        public bool IsPartial => isPartial;
        public bool IsCompleted => isCompleted;
        public bool IsPartialSurfaceVisible => isPartialSurfaceVisible;
        public GameObject PrintedObject => printedObject;

        public void Bind(Printer owner)
        {
            printer = owner;
        }

        public void BeginJob(PrinterJob printJob)
        {
            if (printedObject != null)
            {
                ClearObject();
            }

            job = printJob;

            if (printer == null ||
                printJob == null ||
                printJob.Request == null ||
                printJob.Request.ObjectToPrint == null ||
                printJob.Request.ObjectToPrint.Prefab == null)
            {
                Debug.LogWarning(
                    "[PrintedObjectPresenter] Cannot create a print object: " +
                    "job, catalog prefab or printer is missing.",
                    this);
                return;
            }

            PrintBedAnchor anchor = printer.ResolvedBedAnchor;

            if (anchor == null)
            {
                Debug.LogWarning(
                    "[PrintedObjectPresenter] Cannot create a print object: " +
                    "the printer has no bed anchor.",
                    this);
                return;
            }

            ObjectToPrintMetaSO meta = printJob.Request.ObjectToPrint;
            PrintPlacement placement = printJob.Request.Placement ??
                new PrintPlacement { UniformScale = 1.0f };

            printingMaterial = GetSpoolMaterial(false);
            finalMaterial = GetSpoolMaterial(true);

            printedObject = Instantiate(meta.Prefab);
            printedObject.name = $"printed_{meta.Id}";
            printedObject.SetActive(true);

            Transform root = printedObject.transform;
            root.SetParent(anchor.transform, false);
            root.localPosition = new Vector3(
                placement.LocalPosition.x,
                anchor.SurfaceOffset,
                placement.LocalPosition.z);
            root.localRotation = Quaternion.Euler(0.0f, placement.Yaw, 0.0f);
            uniformScale = Mathf.Max(0.0001f, placement.UniformScale);
            root.localScale = Vector3.one * uniformScale;

            fallbackMaterial = GetFallbackMaterial();

            if (printingMaterial == null)
            {
                printingMaterial = fallbackMaterial;
                Debug.LogWarning(
                    $"[PrintedObjectPresenter] Spool for '{meta.Name}' has no " +
                    "printing material; the layer reveal will not be visible.",
                    this);
            }

            if (finalMaterial == null)
            {
                finalMaterial = fallbackMaterial;
            }

            partialMaterial = finalMaterial != null
                ? finalMaterial
                : fallbackMaterial != null
                    ? fallbackMaterial
                    : printingMaterial;

            Rigidbody body = printedObject.GetComponentInChildren<Rigidbody>(true);

            if (body != null)
            {
                body.isKinematic = true;
            }

            originalColliders =
                printedObject.GetComponentsInChildren<Collider>(true);
            SetCollidersEnabled(false);

            printedData =
                printedObject.GetComponent<PrintedObjectDataHolder>();

            if (printedData == null)
            {
                printedData =
                    printedObject.AddComponent<PrintedObjectDataHolder>();
            }

            printedData.Bind(printer, printJob);

            ObjectToPrintDataHolder objectData =
                printedObject.GetComponent<ObjectToPrintDataHolder>();

            if (objectData != null)
            {
                objectData.PrintingMaterial = printingMaterial;
            }

            layerHeightMeters = Mathf.Max(
                0.00001f,
                LayerHeightSettings.Millimeters(
                    printJob.Request.LayerHeight) / 1000.0f);
            seed = CalculateSeed(meta);
            lastRevealTime = -1000.0f;
            isPartial = false;
            isCompleted = false;
            isFinalized = false;
            isPartialSurfaceVisible = false;

            reveal.Begin(printedObject, printingMaterial);
            reveal.SetVisible(true);
            meshData = PrintedMeshCapture.Capture(printedObject);

            if (meshData == null || !meshData.IsValid)
            {
                Debug.LogWarning(
                    "[PrintedObjectPresenter] The object has no readable " +
                    "mesh data; partial surfaces, supports and motion bounds " +
                    "are limited.",
                    printedObject);
                localBounds = new Bounds(Vector3.zero, Vector3.zero);
            }
            else
            {
                localBounds = meshData.LocalBounds;
            }

            localHeight = Mathf.Max(0.0f, localBounds.size.y);

            supports.Begin(
                printedObject,
                meshData,
                printingMaterial,
                partialMaterial,
                printJob.Request.Supports,
                MaxSupportColumns,
                SupportClusterSizeMeters,
                SupportOverhangThreshold,
                SupportColumnRadiusMeters,
                localBounds);
            Vector3 buildVolume = printer.Motion != null
                ? printer.Motion.BuildVolume
                : Vector3.zero;
            Matrix4x4 localToBed = anchor.transform.worldToLocalMatrix *
                printedObject.transform.localToWorldMatrix;
            PrintLayerBounds[] layerBounds = PrintLayerBoundsBuilder.Build(
                meshData,
                localBounds,
                localToBed,
                layerHeightMeters,
                buildVolume,
                out Bounds bedBounds);

            motion.Begin(
                printer.Motion,
                bedBounds,
                layerHeightMeters,
                seed,
                layerBounds);

            UpdateVisuals(0.0f);
        }

        public void Tick(PrinterJob printJob, float simulatedDelta)
        {
            if (printedObject == null ||
                printJob == null ||
                printJob != job ||
                isPartialSurfaceVisible)
            {
                return;
            }

            float progress = printJob.Progress01;
            float quantized = Quantize(progress);

            motion.Tick(progress, simulatedDelta);

            if (Time.unscaledTime - lastRevealTime >=
                1.0f / Mathf.Max(1.0f, RevealUpdatesPerSecond))
            {
                lastRevealTime = Time.unscaledTime;
                UpdateVisuals(quantized);
            }
        }

        public void Complete(PrinterJob printJob)
        {
            if (printedObject == null)
            {
                return;
            }

            isCompleted = true;
            isPartial = false;

            ClearPartialSurface();
            reveal.SetVisible(true);
            reveal.SetFull();
            reveal.SetFinalMaterial(finalMaterial);
            supports.SetFinalMaterial();
            supports.Tick(localHeight);
        }

        public void HardStop(PrinterJob printJob)
        {
            if (printedObject == null)
            {
                return;
            }

            isPartial = true;
            isCompleted = false;

            // A pause may already have shown the capped final-material
            // surface. Hard stop returns to the live printing look while the
            // object cools; the capped surface is rebuilt on finalization.
            ClearPartialSurface();
            reveal.SetVisible(true);
            reveal.SetPrintingMaterial(printingMaterial);
            supports.SetPrintingMaterial();

            float quantized = Quantize(printJob != null
                ? printJob.Progress01
                : 0.0f);
            reveal.SetProgress(quantized);
            supports.Tick(quantized * localHeight);
        }

        public void Pause()
        {
            // The printer owns the pause/home move. Live visuals freeze because
            // Tick is no longer called.
        }

        /// <summary>
        /// Called once the paused printer has cooled and parked. Shows the
        /// capped, final-material surface without enabling removal.
        /// </summary>
        public void PauseSettled(PrinterJob printJob)
        {
            if (printedObject == null || isFinalized)
            {
                return;
            }

            supports.SetFinalMaterial();

            float quantized = Quantize(printJob != null
                ? printJob.Progress01
                : 0.0f);
            supports.Tick(quantized * localHeight);
            ShowPartialSurface(quantized * localHeight, false);
        }

        public void Resume()
        {
            if (printedObject == null)
            {
                return;
            }

            ClearPartialSurface();
            reveal.SetVisible(true);
            reveal.SetPrintingMaterial(printingMaterial);
            supports.SetPrintingMaterial();

            float quantized = Quantize(job != null
                ? job.Progress01
                : 0.0f);
            reveal.SetProgress(quantized);
            supports.Tick(quantized * localHeight);
            motion.Resume();
        }

        public void FinalizeCompleted(PrinterJob printJob)
        {
            if (printedObject == null || printJob == null)
            {
                return;
            }

            isFinalized = true;
            isCompleted = true;
            isPartial = false;

            ClearPartialSurface();
            reveal.SetVisible(true);
            reveal.SetFull();
            reveal.SetFinalMaterial(finalMaterial);
            supports.SetFinalMaterial();
            supports.Tick(localHeight);

            SetCollidersEnabled(true);

            printedData?.MarkFinalized(
                printJob,
                false,
                printJob.FinalQuality);

            ObjectToPrintDataHolder objectData =
                printedObject.GetComponent<ObjectToPrintDataHolder>();

            if (objectData != null)
            {
                objectData.IsHaveQuality = true;
            }
        }

        public void FinalizePartial(PrinterJob printJob)
        {
            if (printedObject == null || printJob == null)
            {
                return;
            }

            isFinalized = true;
            isPartial = true;
            isCompleted = false;

            supports.SetFinalMaterial();

            float quantized = Quantize(printJob.Progress01);
            supports.Tick(quantized * localHeight);

            if (isPartialSurfaceVisible && slicedObject != null)
            {
                EnsurePartialCollider();
            }
            else
            {
                ShowPartialSurface(quantized * localHeight, true);
            }

            printedData?.MarkFinalized(printJob, true, -1.0f);

            ObjectToPrintDataHolder objectData =
                printedObject.GetComponent<ObjectToPrintDataHolder>();

            if (objectData != null)
            {
                objectData.IsHaveQuality = false;
            }
        }

        /// <summary>
        /// Detaches a finalized print from the printer. The released object is
        /// now a normal item; the caller is responsible for placement or sale.
        /// </summary>
        public bool TryRelease(GameObject objectToRelease)
        {
            if (printedObject == null ||
                objectToRelease == null ||
                objectToRelease != printedObject ||
                !isFinalized)
            {
                return false;
            }

            printedData?.Release();
            printedObject.transform.SetParent(null, true);

            supports.Clear();
            printedObject = null;
            printedData = null;
            job = null;
            meshData = null;
            originalColliders = null;
            slicedObject = null;
            slicedMesh = null;
            isFinalized = false;
            isPartial = false;
            isCompleted = false;
            isPartialSurfaceVisible = false;

            return true;
        }

        public void ClearObject()
        {
            if (printedObject != null)
            {
                Destroy(printedObject);
            }

            supports.Clear();
            printedObject = null;
            printedData = null;
            job = null;
            meshData = null;
            originalColliders = null;
            slicedObject = null;
            slicedMesh = null;
            isFinalized = false;
            isPartial = false;
            isCompleted = false;
            isPartialSurfaceVisible = false;
        }

        private void OnDestroy()
        {
            if (printedObject != null)
            {
                ClearObject();
            }
        }

        private Material GetSpoolMaterial(bool final)
        {
            ItemFFFSpoolDataHolder spool = printer != null &&
                printer.ActiveSpool != null
                ? printer.ActiveSpool.Data
                : null;

            if (spool == null)
            {
                return null;
            }

            return final ? spool.FinalMaterial : spool.PrintingMaterial;
        }

        private Material GetFallbackMaterial()
        {
            if (printedObject == null)
            {
                return null;
            }

            Renderer[] renderers =
                printedObject.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null &&
                    renderers[i].sharedMaterial != null)
                {
                    return renderers[i].sharedMaterial;
                }
            }

            return null;
        }

        private void UpdateVisuals(float quantizedProgress)
        {
            float clamped = Mathf.Clamp01(quantizedProgress);
            float printedHeight = clamped * localHeight;

            reveal.SetProgress(clamped);
            supports.Tick(printedHeight);
        }

        private void ShowPartialSurface(
            float printedLocalHeight,
            bool enableCollider)
        {
            ClearPartialSurface();

            if (printedObject == null ||
                meshData == null ||
                printedLocalHeight <= 0.0001f)
            {
                reveal.SetVisible(true);

                if (enableCollider)
                {
                    BuildFallbackCollider(printedLocalHeight);
                }

                return;
            }

            if (!capping.BuildSlicedMesh(
                    meshData,
                    printedLocalHeight + 0.00005f))
            {
                Debug.LogWarning(
                    "[PrintedObjectPresenter] Could not build a sliced partial " +
                    "surface; keeping the live printing appearance.",
                    printedObject);
                reveal.SetVisible(true);

                if (enableCollider)
                {
                    BuildFallbackCollider(printedLocalHeight);
                }

                return;
            }

            slicedObject = new GameObject("printed_partial_surface");
            slicedObject.layer = printedObject.layer;
            slicedObject.transform.SetParent(printedObject.transform, false);
            slicedObject.transform.localPosition = Vector3.zero;
            slicedObject.transform.localRotation = Quaternion.identity;
            slicedObject.transform.localScale = Vector3.one;

            slicedMesh = new Mesh
            {
                name = "printed_partial_surface_mesh",
            };
            slicedMesh.SetVertices(capping.SlicedVertices);
            slicedMesh.SetTriangles(capping.SlicedTriangles, 0);
            slicedMesh.RecalculateNormals();
            slicedMesh.RecalculateBounds();

            MeshFilter filter = slicedObject.AddComponent<MeshFilter>();
            filter.sharedMesh = slicedMesh;

            MeshRenderer renderer = slicedObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = partialMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            PrintedMeshOwner meshOwner =
                slicedObject.AddComponent<PrintedMeshOwner>();
            meshOwner.Mesh = slicedMesh;

            if (enableCollider)
            {
                EnsurePartialCollider();
            }

            reveal.SetVisible(false);
            isPartialSurfaceVisible = true;
        }

        private void EnsurePartialCollider()
        {
            if (slicedObject == null || slicedMesh == null)
            {
                return;
            }

            MeshCollider collider =
                slicedObject.GetComponent<MeshCollider>();

            if (collider == null)
            {
                collider = slicedObject.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = slicedMesh;
            collider.convex = false;
            SetCollidersEnabled(false);
        }

        private void BuildFallbackCollider(float printedLocalHeight)
        {
            slicedObject = new GameObject("printed_partial_collider");
            slicedObject.layer = printedObject.layer;
            slicedObject.transform.SetParent(printedObject.transform, false);
            slicedObject.transform.localPosition = Vector3.zero;
            slicedObject.transform.localRotation = Quaternion.identity;
            slicedObject.transform.localScale = Vector3.one;

            BoxCollider box = slicedObject.AddComponent<BoxCollider>();
            float minY = Mathf.Max(0.0f, localBounds.min.y);
            float maxY = Mathf.Min(printedLocalHeight, localBounds.max.y);
            float height = Mathf.Max(0.0001f, maxY - minY);

            box.center = new Vector3(
                localBounds.center.x,
                (minY + maxY) * 0.5f,
                localBounds.center.z);
            box.size = new Vector3(
                Mathf.Max(0.0001f, localBounds.size.x),
                height,
                Mathf.Max(0.0001f, localBounds.size.z));

            SetCollidersEnabled(false);
        }

        private void ClearPartialSurface()
        {
            if (slicedObject != null)
            {
                Destroy(slicedObject);
            }

            slicedObject = null;
            slicedMesh = null;
            isPartialSurfaceVisible = false;
            reveal.SetVisible(true);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (originalColliders == null)
            {
                return;
            }

            for (int i = 0; i < originalColliders.Length; i++)
            {
                if (originalColliders[i] != null)
                {
                    originalColliders[i].enabled = enabled;
                }
            }
        }

        private float Quantize(float progress)
        {
            if (localHeight <= 0.0001f)
            {
                return Mathf.Clamp01(progress);
            }

            float worldHeight = localHeight * uniformScale;
            int layerCount = Mathf.Max(
                1,
                Mathf.CeilToInt(worldHeight / layerHeightMeters));
            int layer = Mathf.FloorToInt(
                Mathf.Clamp01(progress) * layerCount);

            return Mathf.Clamp01((float)layer / layerCount);
        }

        private int CalculateSeed(ObjectToPrintMetaSO meta)
        {
            int metaHash = meta != null && !string.IsNullOrEmpty(meta.Id)
                ? meta.Id.GetHashCode()
                : 0;
            int jobHash = UnityEngine.Random.Range(1, int.MaxValue);

            return (metaHash * 397) ^
                (printer != null ? printer.GetHashCode() : 0) ^
                jobHash;
        }
    }
}

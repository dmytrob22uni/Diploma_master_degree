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
    /// Owns the visible object for one printer job. It instantiates the real
    /// object prefab, applies the selected spool's printing material, drives
    /// the layer reveal/motion/supports, and finalizes the object only after
    /// cooling and parking are complete.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrintedObjectPresenter : MonoBehaviour
    {
        private static readonly int RevealHeightId =
            Shader.PropertyToID("_reveal_height");

        [Header("Reveal")]
        [Min(1.0f)]
        [SerializeField] private float RevealUpdatesPerSecond = 15.0f;

        [Min(0.01f)]
        [SerializeField] private float CapUpdateIntervalSeconds = 0.1f;

        [Header("Supports")]
        [Min(0)]
        [SerializeField] private int MaxSupportColumns = 48;

        [Min(0.001f)]
        [SerializeField] private float SupportClusterSizeMeters = 0.02f;

        [Range(0.0f, 1.0f)]
        [SerializeField] private float SupportOverhangThreshold = 0.35f;

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
        private Material capMaterial;
        private Material fallbackMaterial;

        private Mesh capMesh;
        private GameObject capObject;
        private MeshRenderer capRenderer;

        private Mesh slicedMesh;
        private GameObject slicedObject;

        private float localHeight;
        private float uniformScale = 1.0f;
        private float layerHeightMeters = 0.0002f;
        private float lastRevealTime = -1000.0f;
        private float lastCapTime = -1000.0f;
        private float lastCapHeight = -1.0f;
        private int seed;
        private bool isPartial;
        private bool isCompleted;
        private bool isFinalized;

        public bool HasObject => printedObject != null;
        public bool IsFinalized => isFinalized;
        public bool IsPartial => isPartial;
        public bool IsCompleted => isCompleted;
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

            capMaterial = finalMaterial != null
                ? finalMaterial
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
            lastCapTime = -1000.0f;
            lastCapHeight = -1.0f;
            isPartial = false;
            isCompleted = false;
            isFinalized = false;

            reveal.Begin(printedObject, printingMaterial);
            meshData = PrintedMeshCapture.Capture(printedObject);

            if (meshData == null || !meshData.IsValid)
            {
                Debug.LogWarning(
                    "[PrintedObjectPresenter] The object has no readable " +
                    "mesh data; caps, supports and motion bounds are limited.",
                    printedObject);
                localBounds = new Bounds(Vector3.zero, Vector3.zero);
            }
            else
            {
                localBounds = meshData.LocalBounds;
            }

            localHeight = Mathf.Max(0.0f, localBounds.size.y);

            CreateCap();
            supports.Begin(
                printedObject,
                meshData,
                capMaterial,
                printJob.Request.Supports,
                MaxSupportColumns,
                SupportClusterSizeMeters,
                SupportOverhangThreshold,
                SupportColumnRadiusMeters,
                localBounds);
            motion.Begin(
                printer.Motion,
                placement,
                localBounds,
                uniformScale,
                layerHeightMeters,
                seed);

            UpdateVisuals(0.0f, true);
        }

        public void Tick(PrinterJob printJob, float simulatedDelta)
        {
            if (printedObject == null ||
                printJob == null ||
                printJob != job)
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
                UpdateVisuals(quantized, false);
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
            reveal.SetFull();
            reveal.SetFinalMaterial(finalMaterial);
            HideCap();

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
            float quantized = Quantize(printJob != null
                ? printJob.Progress01
                : 0.0f);
            UpdateVisuals(quantized, true);
        }

        public void Pause()
        {
            // The printer owns the pause/home move. Visuals simply freeze
            // because Tick is no longer called.
        }

        public void Resume()
        {
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

            reveal.SetFull();
            reveal.SetFinalMaterial(finalMaterial);
            HideCap();
            supports.Tick(localHeight);

            ClearPartialCollider();
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

            float quantized = Quantize(printJob.Progress01);
            UpdateVisuals(quantized, true);

            BuildPartialCollider(quantized * localHeight);
            SetCollidersEnabled(false);

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

            if (isCompleted)
            {
                if (capObject != null)
                {
                    Destroy(capObject);
                }

                if (capMesh != null)
                {
                    Destroy(capMesh);
                }
            }

            printedObject = null;
            printedData = null;
            job = null;
            meshData = null;
            originalColliders = null;
            capObject = null;
            capMesh = null;
            capRenderer = null;
            slicedObject = null;
            slicedMesh = null;
            isFinalized = false;
            isPartial = false;
            isCompleted = false;

            return true;
        }

        public void ClearObject()
        {
            if (printedObject != null)
            {
                Destroy(printedObject);
            }

            if (capMesh != null)
            {
                Destroy(capMesh);
            }

            if (slicedMesh != null)
            {
                Destroy(slicedMesh);
            }

            printedObject = null;
            printedData = null;
            job = null;
            meshData = null;
            originalColliders = null;
            capObject = null;
            capMesh = null;
            capRenderer = null;
            slicedObject = null;
            slicedMesh = null;
            isFinalized = false;
            isPartial = false;
            isCompleted = false;
        }

        private void OnDestroy()
        {
            if (printedObject != null)
            {
                ClearObject();
                return;
            }

            if (capMesh != null)
            {
                Destroy(capMesh);
            }

            if (slicedMesh != null)
            {
                Destroy(slicedMesh);
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

        private void UpdateVisuals(float quantizedProgress, bool force)
        {
            float clamped = Mathf.Clamp01(quantizedProgress);
            float printedHeight = clamped * localHeight;

            reveal.SetProgress(clamped);
            supports.Tick(printedHeight);

            if (printedHeight <= 0.0001f)
            {
                HideCap();
                return;
            }

            UpdateCap(printedHeight, force);
        }

        private void UpdateCap(float printedLocalHeight, bool force)
        {
            if (capObject == null || meshData == null)
            {
                return;
            }

            if (!force &&
                Time.unscaledTime - lastCapTime < CapUpdateIntervalSeconds)
            {
                return;
            }

            if (!force &&
                Mathf.Abs(printedLocalHeight - lastCapHeight) < 0.00001f)
            {
                return;
            }

            lastCapTime = Time.unscaledTime;

            if (!capping.BuildCap(meshData, printedLocalHeight + 0.0001f))
            {
                HideCap();
                return;
            }

            capMesh.Clear();
            capMesh.SetVertices(capping.CapVertices);
            capMesh.SetTriangles(capping.CapTriangles, 0);
            capMesh.RecalculateNormals();
            capMesh.RecalculateBounds();
            capObject.SetActive(true);
            lastCapHeight = printedLocalHeight;
        }

        private void HideCap()
        {
            if (capObject != null)
            {
                capObject.SetActive(false);
            }

            lastCapHeight = -1.0f;
        }

        private void CreateCap()
        {
            capObject = new GameObject("printed_layer_cap");
            capObject.layer = printedObject.layer;
            capObject.transform.SetParent(printedObject.transform, false);
            capObject.transform.localPosition = Vector3.zero;
            capObject.transform.localRotation = Quaternion.identity;
            capObject.transform.localScale = Vector3.one;

            MeshFilter filter = capObject.AddComponent<MeshFilter>();
            capMesh = new Mesh { name = "printed_layer_cap_mesh" };
            filter.sharedMesh = capMesh;

            capRenderer = capObject.AddComponent<MeshRenderer>();
            capRenderer.sharedMaterial = capMaterial;
            capRenderer.shadowCastingMode = ShadowCastingMode.Off;
            capRenderer.receiveShadows = false;

            if (capMaterial != null &&
                capMaterial.HasProperty(RevealHeightId))
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(RevealHeightId, 1000.0f);
                capRenderer.SetPropertyBlock(block);
            }

            capObject.SetActive(false);
        }

        private void BuildPartialCollider(float printedLocalHeight)
        {
            ClearPartialCollider();
            slicedObject = new GameObject("printed_partial_collider");
            slicedObject.layer = printedObject.layer;
            slicedObject.transform.SetParent(printedObject.transform, false);
            slicedObject.transform.localPosition = Vector3.zero;
            slicedObject.transform.localRotation = Quaternion.identity;
            slicedObject.transform.localScale = Vector3.one;

            if (meshData != null &&
                capping.BuildSlicedMesh(
                    meshData,
                    printedLocalHeight + 0.00005f))
            {
                slicedMesh = new Mesh
                {
                    name = "printed_partial_collider_mesh",
                };
                slicedMesh.SetVertices(capping.SlicedVertices);
                slicedMesh.SetTriangles(capping.SlicedTriangles, 0);
                slicedMesh.RecalculateNormals();
                slicedMesh.RecalculateBounds();

                MeshCollider collider =
                    slicedObject.AddComponent<MeshCollider>();
                collider.sharedMesh = slicedMesh;
                collider.convex = false;
                return;
            }

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
        }

        private void ClearPartialCollider()
        {
            if (slicedObject != null)
            {
                Destroy(slicedObject);
            }

            slicedObject = null;

            if (slicedMesh != null)
            {
                Destroy(slicedMesh);
            }

            slicedMesh = null;
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

            return (metaHash * 397) ^
                (printer != null ? printer.GetHashCode() : 0);
        }
    }
}

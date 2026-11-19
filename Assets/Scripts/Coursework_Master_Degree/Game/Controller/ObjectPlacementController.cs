using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Coursework_Master_Degree.Game.Logic.Interface;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Input;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.Game.Controller
{
    /// <summary>
    /// Drives M2.5 bed placement: ghost movement, yaw, uniform scale, plate-fit
    /// validation, shader feedback and the placement reasons shown by the UI.
    /// </summary>
    public sealed class ObjectPlacementController : MonoBehaviour
    {
        public enum PlacementMode
        {
            Idle,
            Moving,
            Rotating,
            Scaling,
            OrbitingCamera
        }

        private const float MinScale = 0.001f;
        private const float MaxScale = 100.0f;

        private static readonly int PlacementStatePropertyID =
            Shader.PropertyToID("_printer_bed_placement_state");
        private const string WrongStateKeyword =
            "_PRINTER_BED_PLACEMENT_STATE_WRONG";

        public static ObjectPlacementController Instance { get; private set; }

        [Header("Camera")]
        [Min(0.0f)]
        public float CameraOrbitSensitivity = 0.3f;

        [Min(0.0f)]
        public float ZoomSensitivity = 0.0004f;

        [Range(20.0f, 80.0f)]
        public float CameraFieldOfView = 50.0f;

        [Range(0.0f, 1.0f)]
        public float CameraFitPadding = 1.15f;

        [Range(5.0f, 45.0f)]
        public float CameraMinPitch = 15.0f;

        [Range(45.0f, 89.0f)]
        public float CameraMaxPitch = 75.0f;

        [Range(15.0f, 75.0f)]
        public float CameraInitialPitch = 35.0f;

        [Min(0.0f)]
        [Tooltip("Minimum zoom distance as a fraction of the build-volume radius.")]
        public float MinZoomDistanceFactor = 0.55f;

        [Min(0.0f)]
        [Tooltip("Maximum zoom distance as a fraction of the build-volume radius.")]
        public float MaxZoomDistanceFactor = 5.0f;

        [Header("Object manipulation")]
        [Min(0.0f)]
        public float YawDegreesPerInput = 1.0f;

        [Min(0.0f)]
        public float ScaleSensitivity = 0.01f;

        [Header("Validation")]
        [Min(0.0f)]
        public float PlateMargin = 0.002f;

        [Min(0.0f)]
        public float MinimumObjectSizeMeters = 0.02f;

        private GameStateObjectPlace state;
        private Printer printer;
        private PrintBedAnchor anchor;
        private Vector3 buildVolume;
        private ObjectToPrintMetaSO meta;

        private GameObject objectInstance;
        private ObjectToPrintGhostPrinterBedPlacementDataHolder ghost;
        private Vector3[] localCorners;
        private Vector3 localSize;
        private float objectHeight;

        private PrintPlacement placement;
        private readonly List<string> reasons = new List<string>();
        private string lastReasonSignature = string.Empty;

        private ObjectPlacementCameraController cameraController;

        [Header("UI")]
        [SerializeField]
        private ObjectPlacementPanel panel;

        private InputReader input;
        private MaterialPropertyBlock propertyBlock;

        private bool isActive;
        private bool isMoving;
        private bool isRotating;
        private bool isScaling;
        private bool isOrbiting;

        public IReadOnlyList<string> Reasons => reasons;
        public PrintPlacement Placement => placement;
        public bool IsValid => placement != null && placement.IsValid;

        public Vector3 OrbitCenter => cameraController != null
            ? cameraController.OrbitCenter
            : Vector3.zero;

        public PlacementMode CurrentMode
        {
            get
            {
                if (isOrbiting) return PlacementMode.OrbitingCamera;
                if (isMoving) return PlacementMode.Moving;
                if (isRotating) return PlacementMode.Rotating;
                if (isScaling) return PlacementMode.Scaling;
                return PlacementMode.Idle;
            }
        }

        private void Awake()
        {
            Instance = this;
            propertyBlock = new MaterialPropertyBlock();
            ResolvePanel();
            panel?.SetVisible(false);
        }

        private void OnDestroy()
        {
            if (isActive)
            {
                End();
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDisable()
        {
            if (isActive)
            {
                End();
            }
        }

        private void Update()
        {
            if (!isActive || !isMoving || placement == null)
            {
                return;
            }

            Vector2 pointer = Pointer.current != null
                ? Pointer.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            MoveToPointer(pointer);
        }

        public void Begin(GameStateObjectPlace targetState)
        {
            if (targetState == null)
            {
                return;
            }

            End();

            state = targetState;
            printer = state.Printer;
            meta = state.Meta;
            anchor = printer != null ? printer.ResolvedBedAnchor : null;
            buildVolume = printer != null && printer.Motion != null
                ? printer.Motion.BuildVolume
                : new Vector3(0.25f, 0.21f, 0.21f);

            GameStateManager manager = GameStateManager.Instance;

            if (manager == null)
            {
                return;
            }

            input = manager.InputReader;

            ResolvePanel();
            panel?.SetVisible(true);

            SubscribeInput();

            reasons.Clear();

            if (anchor == null)
            {
                reasons.Add("Print bed is unavailable.");
                isActive = true;
                PublishReasons();
                return;
            }

            if (meta == null || meta.Prefab == null)
            {
                reasons.Add("Object prefab is unavailable.");
                isActive = true;
                PublishReasons();
                return;
            }

            objectInstance = manager.ItemInstancer.InstantiateObjectToPrint(meta);
            objectInstance.SetActive(false);

            ObjectToPrintDataHolder holder =
                objectInstance.GetComponent<ObjectToPrintDataHolder>();

            if (holder == null ||
                !HasPrinterBedPlacementGhost(holder.ObjectToPrintGhostMetaSO))
            {
                reasons.Add("Placement ghost is unavailable.");
                isActive = true;
                PublishReasons();
                return;
            }

            ghost = manager.ItemInstancer.InstantiateObjectToPrintGhost(
                holder,
                ObjectToPrintGhostType.PrinterBedPlacement,
                anchor.SurfacePosition,
                anchor.SurfaceRotation) as ObjectToPrintGhostPrinterBedPlacementDataHolder;

            if (ghost == null)
            {
                reasons.Add("Placement ghost is unavailable.");
                isActive = true;
                PublishReasons();
                return;
            }

            ghost.transform.SetPositionAndRotation(
                anchor.SurfacePosition,
                anchor.SurfaceRotation);
            ghost.transform.localScale = Vector3.one;

            CaptureGhostFootprint();

            placement = state.PreviousPlacement != null
                ? state.PreviousPlacement.Clone()
                : new PrintPlacement();

            placement.LocalPosition = new Vector3(
                placement.LocalPosition.x,
                0.0f,
                placement.LocalPosition.z);

            if (placement.UniformScale <= 0.0f)
            {
                placement.UniformScale = 1.0f;
            }

            cameraController = GetComponent<ObjectPlacementCameraController>();

            if (cameraController == null)
            {
                cameraController = gameObject.AddComponent<ObjectPlacementCameraController>();
            }

            cameraController.FieldOfView = CameraFieldOfView;
            cameraController.FitPadding = CameraFitPadding;
            cameraController.MinPitch = CameraMinPitch;
            cameraController.MaxPitch = CameraMaxPitch;
            cameraController.InitialPitch = CameraInitialPitch;
            cameraController.OrbitSensitivity = CameraOrbitSensitivity;
            cameraController.ZoomSensitivity = ZoomSensitivity;
            cameraController.MinZoomDistanceFactor = MinZoomDistanceFactor;
            cameraController.MaxZoomDistanceFactor = MaxZoomDistanceFactor;

            cameraController.Begin(printer, anchor, buildVolume);

            isActive = true;
            ApplyPlacement();
        }

        public void End()
        {
            if (!isActive && state == null)
            {
                return;
            }

            UnsubscribeInput();

            if (cameraController != null)
            {
                cameraController.End();
            }

            if (ghost != null)
            {
                Destroy(ghost.gameObject);
                ghost = null;
            }

            if (objectInstance != null)
            {
                Destroy(objectInstance);
                objectInstance = null;
            }

            panel?.SetVisible(false);

            isActive = false;
            isMoving = false;
            isRotating = false;
            isScaling = false;
            isOrbiting = false;

            state = null;
            printer = null;
            anchor = null;
            meta = null;
            placement = null;
            localCorners = null;
            objectHeight = 0.0f;
            reasons.Clear();
            lastReasonSignature = string.Empty;
        }

        public void Confirm()
        {
            if (state == null ||
                placement == null ||
                !placement.IsValid)
            {
                return;
            }

            state.Confirm(placement.Clone());
        }

        public void Cancel()
        {
            if (state != null)
            {
                state.Cancel();
            }
        }

        private void SubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.OnObjectPlacementModifierOnPlateEvent += SetMoving;
            input.OnObjectRotationModifierOnPlateEvent += SetRotating;
            input.OnObjectScaleModifierOnPlateEvent += SetScaling;
            input.OnInitiateCameraRotationAroundPlateEvent += SetOrbiting;
            input.OnObjectPlacementOnPlateEvent += HandlePointer;
            input.OnRotateObjectOnPlateEvent += HandleYaw;
            input.OnScaleObjectOnPlateEvent += HandleScale;
            input.OnRotateCameraAroundPlateEvent += HandleCameraOrbit;
            input.OnZoomCameraOnPlateEvent += HandleZoom;
            input.OnConfirmObjectPlacementOnPlateEvent += Confirm;
            input.OnQuitEvent += Cancel;
        }

        private void UnsubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.OnObjectPlacementModifierOnPlateEvent -= SetMoving;
            input.OnObjectRotationModifierOnPlateEvent -= SetRotating;
            input.OnObjectScaleModifierOnPlateEvent -= SetScaling;
            input.OnInitiateCameraRotationAroundPlateEvent -= SetOrbiting;
            input.OnObjectPlacementOnPlateEvent -= HandlePointer;
            input.OnRotateObjectOnPlateEvent -= HandleYaw;
            input.OnScaleObjectOnPlateEvent -= HandleScale;
            input.OnRotateCameraAroundPlateEvent -= HandleCameraOrbit;
            input.OnZoomCameraOnPlateEvent -= HandleZoom;
            input.OnConfirmObjectPlacementOnPlateEvent -= Confirm;
            input.OnQuitEvent -= Cancel;

            input = null;
        }

        private void SetMoving(bool value)
        {
            isMoving = value;
        }

        private void SetRotating(bool value)
        {
            isRotating = value;
        }

        private void SetScaling(bool value)
        {
            isScaling = value;
        }

        private void SetOrbiting(bool value)
        {
            isOrbiting = value;
        }

        private void HandlePointer(Vector2 pointer)
        {
            if (isMoving)
            {
                MoveToPointer(pointer);
            }
        }

        private void MoveToPointer(Vector2 pointer)
        {
            if (anchor == null || placement == null)
            {
                return;
            }

            Camera eventCamera = cameraController != null
                ? cameraController.EventCamera
                : null;

            if (eventCamera == null)
            {
                return;
            }

            Plane platePlane = new Plane(
                anchor.transform.up,
                anchor.SurfacePosition);

            Ray ray = eventCamera.ScreenPointToRay(pointer);

            if (!platePlane.Raycast(ray, out float enter) || enter < 0.0f)
            {
                return;
            }

            Vector3 local = anchor.transform.InverseTransformPoint(
                ray.GetPoint(enter));

            placement.LocalPosition = new Vector3(local.x, 0.0f, local.z);
            ApplyPlacement();
        }

        private void HandleYaw(Vector2 delta)
        {
            if (!isRotating || placement == null || Mathf.Approximately(delta.x, 0.0f))
            {
                return;
            }

            placement.Yaw = Mathf.Repeat(
                placement.Yaw - delta.x * YawDegreesPerInput,
                360.0f);

            ApplyPlacement();
        }

        private void HandleScale(Vector2 delta)
        {
            if (!isScaling || placement == null || Mathf.Approximately(delta.y, 0.0f))
            {
                return;
            }

            placement.UniformScale = Mathf.Clamp(
                placement.UniformScale * Mathf.Exp(delta.y * ScaleSensitivity),
                MinScale,
                MaxScale);

            ApplyPlacement();
        }

        private void HandleCameraOrbit(Vector2 delta)
        {
            if (!isOrbiting || cameraController == null)
            {
                return;
            }

            cameraController.Orbit(delta);
        }

        private void HandleZoom(float delta)
        {
            if (cameraController == null)
            {
                return;
            }

            cameraController.Zoom(delta);
        }

        private static bool HasPrinterBedPlacementGhost(
            ObjectToPrintGhostMetaSO ghostMeta)
        {
            if (ghostMeta == null ||
                ghostMeta.ObjectToPrintTypesToPrefabsList == null)
            {
                return false;
            }

            foreach (ObjectToPrintTypeToPrefab entry in
                ghostMeta.ObjectToPrintTypesToPrefabsList)
            {
                if (entry != null &&
                    entry.ObjectToPrintGhostType ==
                        ObjectToPrintGhostType.PrinterBedPlacement &&
                    entry.ObjectToPrintGhostGameObject != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CaptureGhostFootprint()
        {
            localCorners = null;
            localSize = Vector3.zero;
            objectHeight = 0.0f;

            if (ghost == null)
            {
                return;
            }

            List<Vector2> projectedPoints = new List<Vector2>();
            Bounds localBounds = default;
            bool hasPoints = false;

            MeshFilter[] filters =
                ghost.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter filter in filters)
            {
                if (filter == null ||
                    filter.sharedMesh == null ||
                    !filter.sharedMesh.isReadable)
                {
                    continue;
                }

                Mesh mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices;

                foreach (Vector3 vertex in vertices)
                {
                    Vector3 world = filter.transform.TransformPoint(vertex);
                    Vector3 local = ghost.transform.InverseTransformPoint(world);

                    projectedPoints.Add(new Vector2(local.x, local.z));

                    if (!hasPoints)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        hasPoints = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
                }
            }

            if (hasPoints)
            {
                localSize = localBounds.size;
                objectHeight = localSize.y;

                List<Vector2> hull = ConvexHull(projectedPoints);

                if (hull.Count >= 3)
                {
                    localCorners = new Vector3[hull.Count];

                    for (int i = 0; i < hull.Count; i++)
                    {
                        localCorners[i] = new Vector3(
                            hull[i].x,
                            0.0f,
                            hull[i].y);
                    }

                    return;
                }
            }

            // Fallback for unreadable or degenerate meshes: use the full
            // renderer-bounds box so overhanging geometry is still included.
            if (!CaptureRendererBounds(ref localBounds))
            {
                Vector3 size = ObjectToPrintSizing.GetSizeMeters(meta);
                localSize = size;
                objectHeight = size.y;
                localCorners = BoxCorners(new Bounds(Vector3.zero, size));
                return;
            }

            localSize = localBounds.size;
            objectHeight = localSize.y;

            if (objectHeight <= 0.0f)
            {
                objectHeight = ObjectToPrintSizing.GetSizeMeters(meta).y;
            }

            localCorners = BoxCorners(localBounds);
        }

        private bool CaptureRendererBounds(ref Bounds localBounds)
        {
            bool found = false;
            MeshRenderer[] renderers =
                ghost.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                if (worldBounds.size.sqrMagnitude < 0.0000001f)
                {
                    continue;
                }

                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = center + new Vector3(
                                x * extents.x,
                                y * extents.y,
                                z * extents.z);

                            Vector3 local = ghost.transform.InverseTransformPoint(
                                corner);

                            if (!found)
                            {
                                localBounds = new Bounds(local, Vector3.zero);
                                found = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(local);
                            }
                        }
                    }
                }
            }

            return found;
        }

        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
            {
                return points != null
                    ? new List<Vector2>(points)
                    : new List<Vector2>();
            }

            points.Sort((a, b) =>
                Mathf.Approximately(a.x, b.x)
                    ? a.y.CompareTo(b.y)
                    : a.x.CompareTo(b.x));

            List<Vector2> hull = new List<Vector2>(points.Count * 2);

            foreach (Vector2 point in points)
            {
                while (hull.Count >= 2 &&
                       Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0.0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(point);
            }

            int lowerCount = hull.Count + 1;

            for (int i = points.Count - 2; i >= 0; i--)
            {
                Vector2 point = points[i];

                while (hull.Count >= lowerCount &&
                       Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0.0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(point);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float Cross(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) -
                (a.y - origin.y) * (b.x - origin.x);
        }

        private static Vector3[] BoxCorners(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector3[] corners = new Vector3[8];
            int index = 0;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        corners[index++] = center + new Vector3(
                            x * extents.x,
                            y * extents.y,
                            z * extents.z);
                    }
                }
            }

            return corners;
        }

        private void ApplyPlacement()
        {
            if (anchor == null || ghost == null || placement == null)
            {
                return;
            }

            Vector3 worldPosition = anchor.SurfacePosition +
                anchor.transform.right * placement.LocalPosition.x +
                anchor.transform.forward * placement.LocalPosition.z;

            Quaternion worldRotation =
                anchor.SurfaceRotation * Quaternion.Euler(0.0f, placement.Yaw, 0.0f);

            ghost.transform.SetPositionAndRotation(worldPosition, worldRotation);
            ghost.transform.localScale = Vector3.one * placement.UniformScale;

            BuildReasons();
            placement.IsValid = reasons.Count == 0;

            ApplyGhostState(!placement.IsValid);
            PublishReasons();
        }

        private void BuildReasons()
        {
            reasons.Clear();

            if (placement == null)
            {
                reasons.Add("Placement is unavailable.");
                return;
            }

            if (buildVolume.y <= 0.0f)
            {
                reasons.Add("Build volume is unavailable.");
            }

            Vector3 size = localSize;
            if (size.sqrMagnitude <= 0.0000001f)
            {
                size = ObjectToPrintSizing.GetSizeMeters(meta);
            }

            float largestDimension = Mathf.Max(
                size.x,
                Mathf.Max(size.y, size.z));

            if (largestDimension <= 0.0f)
            {
                largestDimension = objectHeight;
            }

            if (largestDimension * placement.UniformScale < MinimumObjectSizeMeters)
            {
                reasons.Add("Object is too small (minimum 2 cm).");
            }

            if (objectHeight * placement.UniformScale > buildVolume.y + 0.0001f)
            {
                reasons.Add("Object is taller than the build volume.");
            }

            float usableWidth = buildVolume.x > 0.0f
                ? Mathf.Min(anchor.PlateSize.x, buildVolume.x)
                : anchor.PlateSize.x;
            float usableDepth = buildVolume.z > 0.0f
                ? Mathf.Min(anchor.PlateSize.y, buildVolume.z)
                : anchor.PlateSize.y;

            float halfWidth = Mathf.Max(
                0.0f,
                usableWidth * 0.5f - PlateMargin);
            float halfDepth = Mathf.Max(
                0.0f,
                usableDepth * 0.5f - PlateMargin);

            Vector3[] transformedCorners = new Vector3[localCorners.Length];
            bool outsidePlate = false;

            Quaternion yawRotation = Quaternion.Euler(0.0f, placement.Yaw, 0.0f);

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 corner = yawRotation *
                    (localCorners[i] * placement.UniformScale) +
                    placement.LocalPosition;

                transformedCorners[i] = corner;

                if (Mathf.Abs(corner.x) > halfWidth ||
                    Mathf.Abs(corner.z) > halfDepth)
                {
                    outsidePlate = true;
                }
            }

            placement.BoundaryCorners = transformedCorners;

            if (outsidePlate)
            {
                reasons.Add("Footprint does not fit on the print plate.");
            }
        }

        private void ApplyGhostState(bool isWrong)
        {
            if (ghost == null || ghost.MeshRenderersList == null)
            {
                return;
            }

            propertyBlock.Clear();
            propertyBlock.SetFloat(
                PlacementStatePropertyID,
                isWrong ? 1.0f : 0.0f);

            foreach (MeshRenderer renderer in ghost.MeshRenderersList)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.SetPropertyBlock(propertyBlock);

                Material material = renderer.material;

                if (isWrong)
                {
                    material.EnableKeyword(WrongStateKeyword);
                }
                else
                {
                    material.DisableKeyword(WrongStateKeyword);
                }
            }
        }

        private void PublishReasons()
        {
            string signature = string.Join("|", reasons);

            if (signature == lastReasonSignature)
            {
                return;
            }

            lastReasonSignature = signature;

            panel?.SetReasons(reasons);
        }

        private void ResolvePanel()
        {
            if (panel == null)
            {
                panel = FindAnyObjectByType<ObjectPlacementPanel>(
                    FindObjectsInactive.Include);
            }
        }
    }
}

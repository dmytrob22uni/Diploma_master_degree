using UnityEngine;
using UnityEngine.Rendering;

namespace Coursework_Master_Degree.Items.FFF
{
    public enum PrinterBindHighlightState
    {
        Hidden,
        Bindable,
        BoundToThisDevice,
        BoundToOtherDevice,
        SelectedForUnbind
    }

    /// <summary>
    /// Translucent sphere drawn around a printer during bind targeting.
    /// The sphere is created at runtime without a collider, so it never
    /// blocks the hover raycast.
    /// </summary>
    public class PrinterBindHighlight : MonoBehaviour
    {
        [Tooltip("Root used to measure the printer bounds. Defaults to this transform.")]
        public Transform BoundsRoot;

        [Tooltip("Extra sphere radius as a fraction of the largest bounds axis")]
        [Range(0.0f, 0.5f)]
        public float Padding = 0.08f;

        [Tooltip("Optional material. A transparent runtime material is created when empty.")]
        public Material SphereMaterial;

        [Range(0.0f, 1.0f)]
        public float Opacity = 0.18f;

        [Tooltip("Opacity while the printer is hovered. Slightly more solid, never opaque.")]
        [Range(0.0f, 1.0f)]
        public float HoverOpacity = 0.30f;

        [Header("State colours")]
        public Color BindableColor = new Color(0.10f, 0.90f, 1.00f);
        public Color BoundToThisDeviceColor = new Color(0.20f, 1.00f, 0.30f);
        public Color BoundToOtherDeviceColor = new Color(1.00f, 0.20f, 0.20f);
        public Color SelectedForUnbindColor = new Color(1.00f, 0.65f, 0.10f);

        [Header("Debug")]
        [Tooltip("M0 only: change this in the inspector during play to preview a state")]
        public PrinterBindHighlightState DebugState = PrinterBindHighlightState.Hidden;

        public PrinterBindHighlightState State { get; private set; } =
            PrinterBindHighlightState.Hidden;

        public bool IsHovered { get; private set; }

        private GameObject sphere;
        private MeshRenderer sphereRenderer;
        private Material instanceMaterial;
        private Bounds targetBounds;
        private bool hasBounds;

        private void Awake()
        {
            if (BoundsRoot == null)
            {
                BoundsRoot = transform;
            }

            RefreshBounds();
            CreateSphere();
            Hide();
        }

        private void LateUpdate()
        {
            if (State == PrinterBindHighlightState.Hidden || sphere == null)
            {
                return;
            }

            FollowTarget();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (DebugState == PrinterBindHighlightState.Hidden)
            {
                Hide();
            }
            else
            {
                Show(DebugState);
            }
        }

        public void Show(PrinterBindHighlightState state)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            State = state;
            DebugState = state;
            EnsureSphere();
            RefreshBounds();
            FollowTarget();

            if (sphere != null)
            {
                sphere.SetActive(true);
                ApplyColor();
            }
        }

        public void Hide()
        {
            State = PrinterBindHighlightState.Hidden;
            DebugState = PrinterBindHighlightState.Hidden;
            IsHovered = false;

            if (sphere != null)
            {
                sphere.SetActive(false);
            }
        }

        public void SetHovered(bool hovered)
        {
            IsHovered = hovered;
            ApplyColor();
        }

        /// <summary>
        /// The sphere the player sees, used for bind targeting. Aiming uses
        /// this volume instead of the printer colliders, so small or awkward
        /// shapes stay easy to pick.
        /// </summary>
        public bool TryGetTargetSphere(out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0.0f;

            if (State == PrinterBindHighlightState.Hidden)
            {
                return false;
            }

            if (sphere != null)
            {
                center = sphere.transform.position;
                radius = sphere.transform.lossyScale.x * 0.5f;
            }
            else
            {
                if (!hasBounds)
                {
                    RefreshBounds();
                }

                Vector3 size = targetBounds.size;
                center = targetBounds.center;
                radius = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) *
                    (1.0f + Padding) * 0.5f;
            }

            return radius > 0.0001f;
        }

        [ContextMenu("Debug/Show Bindable")]
        private void DebugShowBindable()
        {
            Show(PrinterBindHighlightState.Bindable);
        }

        [ContextMenu("Debug/Show Bound To This Device")]
        private void DebugShowBoundToThisDevice()
        {
            Show(PrinterBindHighlightState.BoundToThisDevice);
        }

        [ContextMenu("Debug/Show Bound To Other Device")]
        private void DebugShowBoundToOtherDevice()
        {
            Show(PrinterBindHighlightState.BoundToOtherDevice);
        }

        [ContextMenu("Debug/Show Selected For Unbind")]
        private void DebugShowSelectedForUnbind()
        {
            Show(PrinterBindHighlightState.SelectedForUnbind);
        }

        [ContextMenu("Debug/Toggle Hovered")]
        private void DebugToggleHovered()
        {
            SetHovered(!IsHovered);
        }

        [ContextMenu("Debug/Hide")]
        private void DebugHide()
        {
            Hide();
        }

        [ContextMenu("Refresh Bounds")]
        public void RefreshBounds()
        {
            if (BoundsRoot == null)
            {
                BoundsRoot = transform;
            }

            Renderer[] renderers = BoundsRoot.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = new Bounds(BoundsRoot.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    renderer == sphereRenderer ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
            {
                bounds = new Bounds(
                    BoundsRoot.position,
                    Vector3.one * 0.5f);
            }

            targetBounds = bounds;
            hasBounds = true;
        }

        private void EnsureSphere()
        {
            if (sphere == null)
            {
                CreateSphere();
            }
        }

        private void CreateSphere()
        {
            if (sphere != null)
            {
                return;
            }

            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "printer_bind_highlight_sphere";
            sphere.hideFlags = HideFlags.DontSave;

            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast >= 0)
            {
                sphere.layer = ignoreRaycast;
            }

            Collider sphereCollider = sphere.GetComponent<Collider>();
            if (sphereCollider != null)
            {
                Destroy(sphereCollider);
            }

            sphereRenderer = sphere.GetComponent<MeshRenderer>();
            if (sphereRenderer != null)
            {
                sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
                sphereRenderer.receiveShadows = false;

                instanceMaterial = SphereMaterial != null
                    ? new Material(SphereMaterial)
                    : CreateTransparentMaterial();

                sphereRenderer.sharedMaterial = instanceMaterial;

                int transparentQueue = (int)RenderQueue.Transparent;
                instanceMaterial.renderQueue = transparentQueue;
            }

            sphere.SetActive(false);
        }

        private void FollowTarget()
        {
            if (sphere == null)
            {
                return;
            }

            if (!hasBounds)
            {
                RefreshBounds();
            }

            Vector3 size = targetBounds.size;
            float diameter =
                Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * (1.0f + Padding);

            sphere.transform.position = targetBounds.center;
            sphere.transform.rotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter);
        }

        private void ApplyColor()
        {
            if (instanceMaterial == null)
            {
                return;
            }

            Color color;
            switch (State)
            {
                case PrinterBindHighlightState.Bindable:
                    color = BindableColor;
                    break;
                case PrinterBindHighlightState.BoundToThisDevice:
                    color = BoundToThisDeviceColor;
                    break;
                case PrinterBindHighlightState.BoundToOtherDevice:
                    color = BoundToOtherDeviceColor;
                    break;
                case PrinterBindHighlightState.SelectedForUnbind:
                    color = SelectedForUnbindColor;
                    break;
                default:
                    return;
            }

            color.a *= IsHovered ? HoverOpacity : Opacity;

            if (instanceMaterial.HasProperty("_BaseColor"))
            {
                instanceMaterial.SetColor("_BaseColor", color);
            }

            if (instanceMaterial.HasProperty("_Color"))
            {
                instanceMaterial.SetColor("_Color", color);
            }
        }

        private static Material CreateTransparentMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = "printer_bind_highlight_runtime"
            };

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1.0f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0.0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0.0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");

            return material;
        }

        private void OnDisable()
        {
            State = PrinterBindHighlightState.Hidden;

            if (sphere != null)
            {
                sphere.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (sphere != null)
            {
                Destroy(sphere);
                sphere = null;
            }

            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
                instanceMaterial = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (BoundsRoot == null)
            {
                return;
            }

            Renderer[] renderers = BoundsRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Gizmos.color = new Color(0.1f, 0.9f, 1.0f, 0.25f);
                Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
            }
        }
    }
}
